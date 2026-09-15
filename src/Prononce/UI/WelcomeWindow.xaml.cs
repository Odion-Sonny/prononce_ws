using System.Windows;
using System.Windows.Media;
using Prononce.Models;
using Prononce.Services;

namespace Prononce.UI;

public partial class WelcomeWindow : Window
{
    public WelcomeWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CheckVoices();
    }

    private void CheckVoices()
    {
        bool hasVoice = VoiceSetupService.Shared.HasFrenchVoiceInstalled();
        if (hasVoice)
        {
            var voice = SpeechService.Shared.CurrentVoiceInfo;
            VoiceStatusText.Text = $"Ready • Using {voice?.Name ?? "French voice"}";
            VoiceStatusText.Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153)); // Emerald green
            InstallVoiceBtn.Visibility = Visibility.Collapsed;
        }
        else
        {
            VoiceStatusText.Text = "No French voice detected on this PC";
            VoiceStatusText.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113)); // Soft red
            InstallVoiceBtn.Visibility = Visibility.Visible;
        }
    }

    private void OnInstallVoiceClicked(object sender, RoutedEventArgs e)
    {
        VoiceSetupService.Shared.OpenSpeechSettings();
    }

    private async void OnTestClicked(object sender, RoutedEventArgs e)
    {
        await SpeechService.Shared.SpeakAsync("Bonjour ! Bienvenue sur Prononce.");
    }

    private void OnGetStartedClicked(object sender, RoutedEventArgs e)
    {
        AppSettings.Shared.HasCompletedOnboarding = true;
        AppSettings.Shared.Save();
        Close();
    }
}
