using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prononce.Models;

public enum TranslationMode
{
    Automatic,
    OnDemand,
    Disabled
}

/// <summary>
/// Persistent application settings stored in AppData.
/// Zero external database, zero bloat, instant JSON persistence.
/// </summary>
public class AppSettings
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Prononce");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    private static AppSettings? _instance;
    private static readonly object _lock = new();

    public static AppSettings Shared
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= Load();
                }
            }
            return _instance;
        }
    }

    public string SelectedVoiceId { get; set; } = string.Empty;

    public double SpeechRate { get; set; } = 1.0;

    public bool ShowFloatingPanel { get; set; } = true;

    public bool HasCompletedOnboarding { get; set; } = false;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TranslationMode TranslationMode { get; set; } = TranslationMode.Automatic;

    public uint HotkeyModifiers { get; set; } = 0x0006; // MOD_CONTROL (0x2) | MOD_SHIFT (0x4)

    public uint HotkeyKey { get; set; } = 0x46; // VK_F

    public string HotkeyDisplayString => "Ctrl + Shift + F";

    /// <summary>
    /// Learner-calibrated speed presets
    /// </summary>
    public static readonly double[] SpeedPresets = new[] { 0.5, 0.75, 1.0, 1.25, 1.5 };

    public static string FormatSpeed(double rate)
    {
        return rate % 1 == 0 ? $"{rate:0.0}x" : $"{rate:0.##}x";
    }

    public void Save()
    {
        try
        {
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    private static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }

        return new AppSettings();
    }
}
