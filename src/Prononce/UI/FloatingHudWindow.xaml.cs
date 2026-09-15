using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Prononce.Models;
using Prononce.Native;
using Prononce.Services;

namespace Prononce.UI;

/// <summary>
/// Non-activating floating pronunciation card.
/// Guarantees that opening the card NEVER steals keyboard focus from active documents/browsers.
/// </summary>
public partial class FloatingHudWindow : Window
{
    private static FloatingHudWindow? _currentInstance;
    private DispatcherTimer? _dismissTimer;
    private bool _isMouseOver;
    private string _currentText = string.Empty;

    public FloatingHudWindow()
    {
        InitializeComponent();
        MouseEnter += (_, _) => { _isMouseOver = true; };
        MouseLeave += (_, _) =>
        {
            _isMouseOver = false;
            RestartDismissTimer(2.0);
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var helper = new WindowInteropHelper(this);
        var hWnd = helper.Handle;

        // Apply WS_EX_NOACTIVATE and WS_EX_TOOLWINDOW extended styles
        var exStyle = Win32.GetWindowLongPtr(hWnd, Win32.GWL_EXSTYLE).ToInt64();
        exStyle |= Win32.WS_EX_NOACTIVATE | Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_TOPMOST;
        Win32.SetWindowLongPtr(hWnd, Win32.GWL_EXSTYLE, new IntPtr(exStyle));
    }

    public static void ShowCard(string text, bool isError = false)
    {
        var settings = AppSettings.Shared;
        if (!settings.ShowFloatingPanel && !isError) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_currentInstance == null)
            {
                _currentInstance = new FloatingHudWindow();
            }

            _currentInstance.Display(text, isError);
        });
    }

    private void Display(string text, bool isError)
    {
        _currentText = text;
        SpokenTextBlock.Text = text;

        var voice = SpeechService.Shared.CurrentVoiceInfo;
        VoiceLabel.Text = voice != null ? $"• {voice.Name}" : "";

        // Check translation
        var settings = AppSettings.Shared;
        if (!isError && settings.TranslationMode != TranslationMode.Disabled)
        {
            var definition = DictionaryService.Shared.Lookup(text);
            if (!string.IsNullOrEmpty(definition))
            {
                TranslationTextBlock.Text = definition;
                TranslationBorder.Visibility = settings.TranslationMode == TranslationMode.Automatic
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                TranslateToggleBtn.Visibility = settings.TranslationMode == TranslationMode.OnDemand
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            else
            {
                TranslationBorder.Visibility = Visibility.Collapsed;
                TranslateToggleBtn.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            TranslationBorder.Visibility = Visibility.Collapsed;
            TranslateToggleBtn.Visibility = Visibility.Collapsed;
        }

        UpdateLayout();

        // Reposition at bottom right of primary work area
        var workArea = SystemParameters.WorkArea;
        const double margin = 24;
        Left = workArea.Right - ActualWidth - margin;
        Top = workArea.Bottom - ActualHeight - margin;

        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            Win32.ShowWindow(helper.Handle, Win32.SW_SHOWNOACTIVATE);
        }
        else
        {
            Show();
        }

        double duration = isError ? 4.5 : (TranslationBorder.Visibility == Visibility.Visible ? 5.5 : 3.5);
        RestartDismissTimer(duration);
    }

    private void RestartDismissTimer(double seconds)
    {
        _dismissTimer?.Stop();
        _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        _dismissTimer.Tick += (_, _) =>
        {
            if (!_isMouseOver)
            {
                _dismissTimer?.Stop();
                HideHud();
            }
        };
        _dismissTimer.Start();
    }

    private void HideHud()
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            Win32.ShowWindow(helper.Handle, Win32.SW_HIDE);
        }
        else
        {
            Hide();
        }
    }

    private async void OnReplayClicked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentText))
        {
            await SpeechService.Shared.SpeakAsync(_currentText);
            RestartDismissTimer(4.0);
        }
    }

    private async void OnSlowClicked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentText))
        {
            await SpeechService.Shared.SpeakAsync(_currentText, 0.75);
            RestartDismissTimer(4.5);
        }
    }

    private async void OnNormalClicked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentText))
        {
            await SpeechService.Shared.SpeakAsync(_currentText, 1.0);
            RestartDismissTimer(3.5);
        }
    }

    private void OnTranslateToggleClicked(object sender, RoutedEventArgs e)
    {
        if (TranslationBorder.Visibility == Visibility.Visible)
        {
            TranslationBorder.Visibility = Visibility.Collapsed;
            TranslateToggleBtn.Content = "Translate";
        }
        else
        {
            TranslationBorder.Visibility = Visibility.Visible;
            TranslateToggleBtn.Content = "Hide";
            RestartDismissTimer(5.5);
        }
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        _dismissTimer?.Stop();
        HideHud();
    }
}
