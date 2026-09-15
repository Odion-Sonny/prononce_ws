using System.Windows;
using System.Windows.Controls;
using Prononce.Models;
using Prononce.Services;

namespace Prononce.UI;

public partial class SettingsWindow : Window
{
    private bool _isInitializing = true;

    public SettingsWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isInitializing = true;
        var settings = AppSettings.Shared;

        // Populate voices
        VoiceComboBox.Items.Clear();
        var voices = SpeechService.Shared.AvailableFrenchVoices;
        foreach (var voice in voices)
        {
            VoiceComboBox.Items.Add(voice);
        }

        if (SpeechService.Shared.CurrentVoiceInfo != null)
        {
            VoiceComboBox.SelectedItem = voices.FirstOrDefault(v => v.Id == SpeechService.Shared.CurrentVoiceInfo.Id);
        }
        else if (VoiceComboBox.Items.Count > 0)
        {
            VoiceComboBox.SelectedIndex = 0;
        }

        // Playback speed
        SpeedSlider.Value = settings.SpeechRate;
        UpdateSpeedLabel(settings.SpeechRate);

        // Translation modes
        TranslationComboBox.Items.Clear();
        foreach (TranslationMode mode in Enum.GetValues(typeof(TranslationMode)))
        {
            TranslationComboBox.Items.Add(mode);
        }
        TranslationComboBox.SelectedItem = settings.TranslationMode;

        // Preferences
        FloatingPanelCheckBox.IsChecked = settings.ShowFloatingPanel;
        LaunchAtStartupCheckBox.IsChecked = LaunchAtStartupService.Shared.IsEnabled();

        _isInitializing = false;
    }

    private void OnVoiceSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (VoiceComboBox.SelectedItem is FrenchVoiceInfo selected)
        {
            SpeechService.Shared.SelectVoice(selected.Id);
            TrayIconController.Shared.RebuildMenu();
        }
    }

    private async void OnTestVoiceClicked(object sender, RoutedEventArgs e)
    {
        await SpeechService.Shared.SpeakAsync("Bonjour ! Comment allez-vous ?");
    }

    private void OnAddVoicesInWindowsClicked(object sender, RoutedEventArgs e)
    {
        VoiceSetupService.Shared.OpenSpeechSettings();
    }

    private void OnSpeedSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;
        var rate = Math.Round(e.NewValue, 2);
        UpdateSpeedLabel(rate);

        AppSettings.Shared.SpeechRate = rate;
        AppSettings.Shared.Save();
        TrayIconController.Shared.RebuildMenu();
    }

    private void UpdateSpeedLabel(double rate)
    {
        string label = AppSettings.FormatSpeed(rate);
        if (Math.Abs(rate - 0.75) < 0.01) label += " (Learner)";
        else if (Math.Abs(rate - 1.0) < 0.01) label += " (Normal)";
        SpeedLabel.Text = label;
    }

    private void OnTranslationSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (TranslationComboBox.SelectedItem is TranslationMode mode)
        {
            AppSettings.Shared.TranslationMode = mode;
            AppSettings.Shared.Save();
            TrayIconController.Shared.RebuildMenu();
        }
    }

    private void OnFloatingPanelChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        AppSettings.Shared.ShowFloatingPanel = FloatingPanelCheckBox.IsChecked ?? true;
        AppSettings.Shared.Save();
        TrayIconController.Shared.RebuildMenu();
    }

    private void OnLaunchAtStartupChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        bool enable = LaunchAtStartupCheckBox.IsChecked ?? false;
        LaunchAtStartupService.Shared.SetLaunchAtStartup(enable);
        TrayIconController.Shared.RebuildMenu();
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
