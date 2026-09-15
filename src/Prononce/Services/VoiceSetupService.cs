using System.Diagnostics;

namespace Prononce.Services;

/// <summary>
/// Service to verify Windows French voice availability and launch system settings deep-links.
/// </summary>
public sealed class VoiceSetupService
{
    public static VoiceSetupService Shared { get; } = new();

    private VoiceSetupService() { }

    /// <summary>
    /// Checks whether at least one French speech synthesis voice is installed on Windows.
    /// </summary>
    public bool HasFrenchVoiceInstalled()
    {
        SpeechService.Shared.ReloadVoices();
        return SpeechService.Shared.AvailableFrenchVoices.Count > 0;
    }

    /// <summary>
    /// Opens Windows 10/11 Settings directly to the Speech & Voices page.
    /// </summary>
    public void OpenSpeechSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:speech") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open ms-settings:speech: {ex.Message}");
            // Fallback to general language settings
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:regionlanguage") { UseShellExecute = true });
            }
            catch { }
        }
    }
}
