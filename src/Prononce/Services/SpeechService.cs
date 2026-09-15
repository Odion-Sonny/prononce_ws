using System.IO;
using System.Media;
using Windows.Media.SpeechSynthesis;
using Prononce.Models;

namespace Prononce.Services;

/// <summary>
/// Native Windows Speech Synthesis service.
/// Uses Windows.Media.SpeechSynthesis with installed high-quality offline French voices.
/// Employs a non-linear pedagogical speed curve for language learners.
/// </summary>
public sealed class SpeechService : IDisposable
{
    public static SpeechService Shared { get; } = new();

    private SpeechSynthesizer? _synthesizer;
    private SoundPlayer? _currentPlayer;
    private readonly object _lock = new();

    public List<FrenchVoiceInfo> AvailableFrenchVoices { get; private set; } = new();
    public FrenchVoiceInfo? CurrentVoiceInfo { get; private set; }

    public event Action? SpeechCompleted;
    public event Action<string>? SpeechError;

    private SpeechService()
    {
        InitializeSynthesizer();
    }

    private void InitializeSynthesizer()
    {
        try
        {
            _synthesizer = new SpeechSynthesizer();
            ReloadVoices();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize WinRT SpeechSynthesizer: {ex.Message}");
        }
    }

    /// <summary>
    /// Scans installed Windows speech synthesis voices for French locales.
    /// </summary>
    public void ReloadVoices()
    {
        var frenchVoices = new List<FrenchVoiceInfo>();

        try
        {
            var allVoices = SpeechSynthesizer.AllVoices;
            foreach (var voice in allVoices)
            {
                if (voice.Language.StartsWith("fr", StringComparison.OrdinalIgnoreCase))
                {
                    string region = "France";
                    if (voice.Language.IndexOf("CA", StringComparison.OrdinalIgnoreCase) >= 0)
                        region = "Canada";
                    else if (voice.Language.IndexOf("BE", StringComparison.OrdinalIgnoreCase) >= 0)
                        region = "Belgium";
                    else if (voice.Language.IndexOf("CH", StringComparison.OrdinalIgnoreCase) >= 0)
                        region = "Switzerland";

                    string gender = voice.Gender == VoiceGender.Female ? "Female" : "Male";

                    frenchVoices.Add(new FrenchVoiceInfo
                    {
                        Id = voice.Id,
                        Name = voice.DisplayName,
                        DisplayName = $"{voice.DisplayName} ({region})",
                        Language = voice.Language,
                        Region = region,
                        Gender = gender
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error querying system voices: {ex.Message}");
        }

        // Sort: France voices first, then alphabetical by name
        AvailableFrenchVoices = frenchVoices
            .OrderByDescending(v => v.Region == "France")
            .ThenBy(v => v.Name)
            .ToList();

        UpdateCurrentVoice();
    }

    private void UpdateCurrentVoice()
    {
        var settings = AppSettings.Shared;
        if (!string.IsNullOrEmpty(settings.SelectedVoiceId))
        {
            CurrentVoiceInfo = AvailableFrenchVoices.FirstOrDefault(v => v.Id == settings.SelectedVoiceId);
            if (CurrentVoiceInfo != null) return;
        }

        // Default to first France voice or any French voice
        CurrentVoiceInfo = AvailableFrenchVoices.FirstOrDefault(v => v.Region == "France")
            ?? AvailableFrenchVoices.FirstOrDefault();
    }

    public void SelectVoice(string voiceId)
    {
        AppSettings.Shared.SelectedVoiceId = voiceId;
        AppSettings.Shared.Save();
        UpdateCurrentVoice();
    }

    /// <summary>
    /// Stops any ongoing speech playback immediately.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            try
            {
                _currentPlayer?.Stop();
                _currentPlayer?.Dispose();
                _currentPlayer = null;
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }

    /// <summary>
    /// Speaks French text with learner-calibrated pedagogical speed curve.
    /// </summary>
    public async Task SpeakAsync(string text, double? customRate = null)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return;

        Stop();

        if (AvailableFrenchVoices.Count == 0)
        {
            SpeechError?.Invoke("No French voice is installed on this PC.\nOpen Settings to download a French voice.");
            return;
        }

        try
        {
            if (_synthesizer == null)
            {
                _synthesizer = new SpeechSynthesizer();
            }

            // Set chosen voice
            var selectedVoice = CurrentVoiceInfo;
            if (selectedVoice != null)
            {
                var voiceObj = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Id == selectedVoice.Id);
                if (voiceObj != null)
                {
                    _synthesizer.Voice = voiceObj;
                }
            }

            // Calculate speech rate
            double multiplier = customRate ?? AppSettings.Shared.SpeechRate;
            double effectiveRate = EffectiveSpeakingRate(multiplier);
            _synthesizer.Options.SpeakingRate = effectiveRate;

            // Synthesize stream to memory
            using var speechStream = await _synthesizer.SynthesizeTextToStreamAsync(trimmed);
            var memoryStream = new MemoryStream();
            using (var netStream = speechStream.AsStreamForRead())
            {
                await netStream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            lock (_lock)
            {
                _currentPlayer = new SoundPlayer(memoryStream);
                _currentPlayer.Play();
            }

            SpeechCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Speech synthesis error: {ex.Message}");
            SpeechError?.Invoke($"Speech synthesis failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Non-linear pedagogical rate curve:
    /// In TTS engines, linear scaling makes 0.75x almost indistinguishable from 1.0x.
    /// This curve provides noticeably distinct articulation:
    /// - 0.50x -> 0.60 (slow, distinct phoneme articulation)
    /// - 0.75x -> 0.78 (pedagogical tempo for learners)
    /// - 1.00x -> 1.00 (natural conversational speed)
    /// - 1.25x -> 1.25 (brisk)
    /// - 1.50x -> 1.50 (fast)
    /// </summary>
    public static double EffectiveSpeakingRate(double multiplier)
    {
        if (multiplier <= 0.5) return 0.60;
        if (multiplier <= 0.75)
        {
            double t = (multiplier - 0.5) / 0.25;
            return 0.60 + t * (0.78 - 0.60);
        }
        if (multiplier <= 1.0)
        {
            double t = (multiplier - 0.75) / 0.25;
            return 0.78 + t * (1.00 - 0.78);
        }
        return Math.Min(multiplier, 2.0);
    }

    public void Dispose()
    {
        Stop();
        _synthesizer?.Dispose();
        _synthesizer = null;
    }
}
