using System.Windows.Interop;
using Prononce.Models;
using Prononce.Native;

namespace Prononce.Services;

/// <summary>
/// Low-level Win32 global hotkey service.
/// Consumes 0.0% CPU while idle, event-driven by the Windows message pump.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    public static HotkeyService Shared { get; } = new();

    private const int HotKeyId = 0x5052; // 'PR'
    private HwndSource? _hwndSource;
    private bool _isRegistered;

    public event Action? HotKeyPressed;

    private HotkeyService() { }

    /// <summary>
    /// Initializes the hotkey listener using an existing or message-only HWND.
    /// </summary>
    public bool Register(IntPtr hWnd)
    {
        Unregister();

        if (hWnd == IntPtr.Zero)
        {
            var parameters = new HwndSourceParameters("PrononceHotkeyListener")
            {
                WindowStyle = 0,
                ExtendedWindowStyle = 0,
                ParentWindow = new IntPtr(-3) // HWND_MESSAGE
            };
            _hwndSource = new HwndSource(parameters);
            hWnd = _hwndSource.Handle;
        }

        var settings = AppSettings.Shared;
        uint modifiers = settings.HotkeyModifiers | Win32.MOD_NOREPEAT;
        uint key = settings.HotkeyKey;

        _isRegistered = Win32.RegisterHotKey(hWnd, HotKeyId, modifiers, key);

        if (_isRegistered)
        {
            if (_hwndSource != null)
            {
                _hwndSource.AddHook(WndProc);
            }
            System.Diagnostics.Debug.WriteLine($"Prononce: Successfully registered global shortcut ({settings.HotkeyDisplayString})");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Prononce: Failed to register hotkey. Error: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
        }

        return _isRegistered;
    }

    public void AttachToHwndSource(HwndSource source)
    {
        _hwndSource = source;
        _hwndSource.AddHook(WndProc);
        Register(source.Handle);
    }

    public void Unregister()
    {
        if (_hwndSource != null && _isRegistered)
        {
            Win32.UnregisterHotKey(_hwndSource.Handle, HotKeyId);
            _isRegistered = false;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32.WM_HOTKEY && wParam.ToInt32() == HotKeyId)
        {
            handled = true;
            HotKeyPressed?.Invoke();
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
        _hwndSource = null;
    }
}
