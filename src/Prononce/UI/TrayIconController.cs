using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Prononce.Models;
using Prononce.Services;
using Application = System.Windows.Application;

namespace Prononce.UI;

/// <summary>
/// Controls the Windows System Tray notification area icon and context menu.
/// </summary>
public sealed class TrayIconController : IDisposable
{
    public static TrayIconController Shared { get; } = new();

    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;

    public event Action? SpeakRequested;
    public event Action? SettingsRequested;

    private TrayIconController() { }

    public void Initialize()
    {
        _contextMenu = new ContextMenuStrip();
        _contextMenu.Opening += (_, _) => RebuildMenu();

        _notifyIcon = new NotifyIcon
        {
            Text = "Prononce - Instant French Pronunciation",
            Visible = true,
            ContextMenuStrip = _contextMenu,
            Icon = LoadAppIcon()
        };

        _notifyIcon.DoubleClick += (_, _) =>
        {
            SettingsRequested?.Invoke();
        };

        RebuildMenu();
    }

    private static Icon LoadAppIcon()
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var pathsToTry = new[]
            {
                Path.Combine(baseDir, "Resources", "app.ico"),
                Path.Combine(baseDir, "app.ico"),
                Path.Combine(AppContext.BaseDirectory, "Resources", "app.ico")
            };

            foreach (var path in pathsToTry)
            {
                if (File.Exists(path))
                {
                    return new Icon(path);
                }
            }
        }
        catch { }

        // Fallback to default application icon
        return SystemIcons.Application;
    }

    public void RebuildMenu()
    {
        if (_contextMenu == null) return;
        _contextMenu.Items.Clear();

        var settings = AppSettings.Shared;

        // 1. Header item
        var headerItem = new ToolStripMenuItem($"Prononce ({settings.HotkeyDisplayString})")
        {
            Font = new System.Drawing.Font(_contextMenu.Font, System.Drawing.FontStyle.Bold),
            Enabled = false
        };
        _contextMenu.Items.Add(headerItem);

        // 2. Speak selected text
        var speakItem = new ToolStripMenuItem("Speak Selected Text", null, (_, _) => SpeakRequested?.Invoke());
        _contextMenu.Items.Add(speakItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // 3. Voices submenu
        var voiceSubmenu = new ToolStripMenuItem("Voice");
        var voices = SpeechService.Shared.AvailableFrenchVoices;
        var currentVoice = SpeechService.Shared.CurrentVoiceInfo;

        if (voices.Count == 0)
        {
            var noVoiceItem = new ToolStripMenuItem("No French voices found") { Enabled = false };
            var addVoiceItem = new ToolStripMenuItem("Add French voice in Settings...", null, (_, _) =>
            {
                VoiceSetupService.Shared.OpenSpeechSettings();
            });
            voiceSubmenu.DropDownItems.Add(noVoiceItem);
            voiceSubmenu.DropDownItems.Add(addVoiceItem);
        }
        else
        {
            foreach (var voice in voices)
            {
                var isSelected = currentVoice?.Id == voice.Id;
                var voiceItem = new ToolStripMenuItem(voice.DisplayName, null, (_, _) =>
                {
                    SpeechService.Shared.SelectVoice(voice.Id);
                    RebuildMenu();
                })
                {
                    Checked = isSelected
                };
                voiceSubmenu.DropDownItems.Add(voiceItem);
            }
        }
        _contextMenu.Items.Add(voiceSubmenu);

        // 4. Speed submenu
        var speedSubmenu = new ToolStripMenuItem("Speed");
        foreach (var rate in AppSettings.SpeedPresets)
        {
            var isSelected = Math.Abs(settings.SpeechRate - rate) < 0.01;
            var label = AppSettings.FormatSpeed(rate);
            if (rate == 0.75) label += " (Learner)";
            else if (rate == 1.0) label += " (Normal)";

            var speedItem = new ToolStripMenuItem(label, null, (_, _) =>
            {
                settings.SpeechRate = rate;
                settings.Save();
                RebuildMenu();
            })
            {
                Checked = isSelected
            };
            speedSubmenu.DropDownItems.Add(speedItem);
        }
        _contextMenu.Items.Add(speedSubmenu);

        // 5. Translation mode submenu
        var translationSubmenu = new ToolStripMenuItem("Translation");
        foreach (TranslationMode mode in Enum.GetValues(typeof(TranslationMode)))
        {
            var modeItem = new ToolStripMenuItem(mode.ToString(), null, (_, _) =>
            {
                settings.TranslationMode = mode;
                settings.Save();
                RebuildMenu();
            })
            {
                Checked = settings.TranslationMode == mode
            };
            translationSubmenu.DropDownItems.Add(modeItem);
        }
        _contextMenu.Items.Add(translationSubmenu);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // 6. Floating Panel Toggle
        var hudToggleItem = new ToolStripMenuItem("Show Floating Panel", null, (_, _) =>
        {
            settings.ShowFloatingPanel = !settings.ShowFloatingPanel;
            settings.Save();
            RebuildMenu();
        })
        {
            Checked = settings.ShowFloatingPanel
        };
        _contextMenu.Items.Add(hudToggleItem);

        // 7. Launch at startup
        var startupItem = new ToolStripMenuItem("Launch at Startup", null, (_, _) =>
        {
            bool isCurrentlyEnabled = LaunchAtStartupService.Shared.IsEnabled();
            LaunchAtStartupService.Shared.SetLaunchAtStartup(!isCurrentlyEnabled);
            RebuildMenu();
        })
        {
            Checked = LaunchAtStartupService.Shared.IsEnabled()
        };
        _contextMenu.Items.Add(startupItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // 8. Settings
        var settingsItem = new ToolStripMenuItem("Settings...", null, (_, _) => SettingsRequested?.Invoke());
        _contextMenu.Items.Add(settingsItem);

        // 9. Exit
        var exitItem = new ToolStripMenuItem("Exit Prononce", null, (_, _) =>
        {
            Application.Current.Shutdown();
        });
        _contextMenu.Items.Add(exitItem);
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _contextMenu?.Dispose();
        _contextMenu = null;
    }
}
