using System.Windows;
using Application = System.Windows.Application;
using Prononce.Models;
using Prononce.Native;
using Prononce.Services;
using Prononce.UI;

namespace Prononce;

/// <summary>
/// Main application lifecycle coordinator for Prononce Windows.
/// </summary>
public partial class App : Application
{
    private static SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Initialize system tray notification icon
        TrayIconController.Shared.Initialize();
        TrayIconController.Shared.SpeakRequested += TriggerPronunciationFlow;
        TrayIconController.Shared.SettingsRequested += ShowSettings;

        // 2. Wire error handler
        SpeechService.Shared.SpeechError += errorMessage =>
        {
            FloatingHudWindow.ShowCard(errorMessage, isError: true);
        };

        // 3. Register global hotkey
        HotkeyService.Shared.HotKeyPressed += TriggerPronunciationFlow;
        HotkeyService.Shared.Register(IntPtr.Zero);

        // 4. First-run onboarding check
        var settings = AppSettings.Shared;
        bool hasCompleted = settings.HasCompletedOnboarding;
        bool hasVoice = VoiceSetupService.Shared.HasFrenchVoiceInstalled();

        if (!hasCompleted || !hasVoice)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var welcome = new WelcomeWindow();
                welcome.Show();
            });
        }
    }

    /// <summary>
    /// Core pronunciation execution flow:
    /// 1. Captures selected text without clobbering clipboard.
    /// 2. If empty, provides gentle audio cue and hint HUD.
    /// 3. If present, immediately pronounces via WinRT speech synthesizer and displays HUD.
    /// </summary>
    public async void TriggerPronunciationFlow()
    {
        var text = await SelectionService.Shared.GetSelectedTextAsync();

        if (string.IsNullOrWhiteSpace(text))
        {
            Win32.MessageBeep(0);
            FloatingHudWindow.ShowCard(
                "No text selected.\nHighlight some French text and press Ctrl + Shift + F.",
                isError: true
            );
            return;
        }

        // Speak text using French TTS
        await SpeechService.Shared.SpeakAsync(text);

        // Show floating pronunciation HUD
        FloatingHudWindow.ShowCard(text);
    }

    public static void ShowSettings()
    {
        Current.Dispatcher.Invoke(() =>
        {
            if (_settingsWindow == null || !_settingsWindow.IsLoaded)
            {
                _settingsWindow = new SettingsWindow();
                _settingsWindow.Closed += (_, _) => _settingsWindow = null;
                _settingsWindow.Show();
            }
            else
            {
                _settingsWindow.Activate();
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        HotkeyService.Shared.Dispose();
        SpeechService.Shared.Dispose();
        TrayIconController.Shared.Dispose();
        base.OnExit(e);
    }
}
