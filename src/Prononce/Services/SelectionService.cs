using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using Prononce.Native;

namespace Prononce.Services;

/// <summary>
/// Reliable, lightweight service to capture highlighted text from the frontmost application.
/// Uses a dual-strategy:
/// 1. Microsoft UI Automation (UIA) - Instant, zero clipboard side-effects.
/// 2. Synthetic Ctrl+C fallback with automatic clipboard restoration for non-UIA apps (Chrome, VS Code, etc.).
/// </summary>
public sealed class SelectionService
{
    public static SelectionService Shared { get; } = new();

    public const int MaxTextLength = 2500;

    private SelectionService() { }

    /// <summary>
    /// Captures the currently selected text across any Windows application asynchronously.
    /// </summary>
    public async Task<string?> GetSelectedTextAsync()
    {
        return await Task.Run(() =>
        {
            // Strategy 1: Direct UI Automation
            try
            {
                var uiaText = CaptureViaUiAutomation();
                if (!string.IsNullOrWhiteSpace(uiaText))
                {
                    return CleanText(uiaText);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UIA Capture attempt failed: {ex.Message}");
            }

            // Strategy 2: Synthetic Ctrl+C with clipboard restoration
            try
            {
                var copyText = CaptureViaSyntheticCopy();
                if (!string.IsNullOrWhiteSpace(copyText))
                {
                    return CleanText(copyText);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Synthetic copy attempt failed: {ex.Message}");
            }

            return null;
        });
    }

    // MARK: - Strategy 1: Microsoft UI Automation

    private string? CaptureViaUiAutomation()
    {
        try
        {
            var focusedElement = AutomationElement.FocusedElement;
            if (focusedElement == null) return null;

            // Check if element supports TextPattern
            if (focusedElement.TryGetCurrentPattern(TextPattern.Pattern, out var patternObj) &&
                patternObj is TextPattern textPattern)
            {
                var selectionRanges = textPattern.GetSelection();
                if (selectionRanges != null && selectionRanges.Length > 0)
                {
                    var sb = new StringBuilder();
                    foreach (var range in selectionRanges)
                    {
                        var text = range.GetText(MaxTextLength);
                        if (!string.IsNullOrEmpty(text))
                        {
                            sb.Append(text);
                        }
                    }

                    var result = sb.ToString();
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        return result;
                    }
                }
            }
        }
        catch
        {
            // Fall through to synthetic copy
        }

        return null;
    }

    // MARK: - Strategy 2: Synthetic Ctrl+C with Clipboard Restoration

    private record SavedClipboardEntry(uint Format, byte[] Data);

    private string? CaptureViaSyntheticCopy()
    {
        var savedEntries = new List<SavedClipboardEntry>();
        uint initialSeq = Win32.GetClipboardSequenceNumber();

        // 1. Back up existing clipboard data across all formats
        if (Win32.OpenClipboard(IntPtr.Zero))
        {
            try
            {
                uint format = 0;
                while ((format = Win32.EnumClipboardFormats(format)) != 0)
                {
                    // Skip formats that cannot or shouldn't be backed up directly
                    if (format == Win32.CF_BITMAP) continue;

                    IntPtr hData = Win32.GetClipboardData(format);
                    if (hData != IntPtr.Zero)
                    {
                        UIntPtr size = Win32.GlobalSize(hData);
                        int byteCount = (int)size.ToUInt32();
                        if (byteCount > 0 && byteCount < 10 * 1024 * 1024) // 10MB safety cap
                        {
                            IntPtr pData = Win32.GlobalLock(hData);
                            if (pData != IntPtr.Zero)
                            {
                                try
                                {
                                    byte[] buffer = new byte[byteCount];
                                    Marshal.Copy(pData, buffer, 0, byteCount);
                                    savedEntries.Add(new SavedClipboardEntry(format, buffer));
                                }
                                finally
                                {
                                    Win32.GlobalUnlock(hData);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                Win32.CloseClipboard();
            }
        }

        // 2. Synthesize Ctrl + C via SendInput
        SendCtrlC();

        // 3. Wait up to 150ms for clipboard sequence number to update
        const int pollIntervalMs = 15;
        const int maxWaitMs = 150;
        int elapsed = 0;
        string? capturedText = null;

        while (elapsed < maxWaitMs)
        {
            Thread.Sleep(pollIntervalMs);
            elapsed += pollIntervalMs;

            if (Win32.GetClipboardSequenceNumber() != initialSeq)
            {
                // Clipboard changed, read text
                if (Win32.OpenClipboard(IntPtr.Zero))
                {
                    try
                    {
                        IntPtr hData = Win32.GetClipboardData(Win32.CF_UNICODETEXT);
                        if (hData != IntPtr.Zero)
                        {
                            IntPtr pData = Win32.GlobalLock(hData);
                            if (pData != IntPtr.Zero)
                            {
                                try
                                {
                                    capturedText = Marshal.PtrToStringUni(pData);
                                }
                                finally
                                {
                                    Win32.GlobalUnlock(hData);
                                }
                            }
                        }
                    }
                    finally
                    {
                        Win32.CloseClipboard();
                    }
                }
                break;
            }
        }

        // 4. Restore original clipboard contents immediately
        if (savedEntries.Count > 0)
        {
            if (Win32.OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    Win32.EmptyClipboard();
                    foreach (var entry in savedEntries)
                    {
                        IntPtr hGlobal = Win32.GlobalAlloc(Win32.GMEM_MOVEABLE, (UIntPtr)entry.Data.Length);
                        if (hGlobal != IntPtr.Zero)
                        {
                            IntPtr pGlobal = Win32.GlobalLock(hGlobal);
                            if (pGlobal != IntPtr.Zero)
                            {
                                Marshal.Copy(entry.Data, 0, pGlobal, entry.Data.Length);
                                Win32.GlobalUnlock(hGlobal);
                                Win32.SetClipboardData(entry.Format, hGlobal);
                            }
                        }
                    }
                }
                finally
                {
                    Win32.CloseClipboard();
                }
            }
        }
        else if (capturedText != null)
        {
            // Clipboard was previously empty; clear the temporary copy
            if (Win32.OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    Win32.EmptyClipboard();
                }
                finally
                {
                    Win32.CloseClipboard();
                }
            }
        }

        return capturedText;
    }

    private static void SendCtrlC()
    {
        var inputs = new Win32.INPUT[4];

        // Ctrl down
        inputs[0].type = Win32.INPUT_KEYBOARD;
        inputs[0].u.ki.wVk = Win32.VK_CONTROL;

        // C down
        inputs[1].type = Win32.INPUT_KEYBOARD;
        inputs[1].u.ki.wVk = Win32.VK_C;

        // C up
        inputs[2].type = Win32.INPUT_KEYBOARD;
        inputs[2].u.ki.wVk = Win32.VK_C;
        inputs[2].u.ki.dwFlags = Win32.KEYEVENTF_KEYUP;

        // Ctrl up
        inputs[3].type = Win32.INPUT_KEYBOARD;
        inputs[3].u.ki.wVk = Win32.VK_CONTROL;
        inputs[3].u.ki.dwFlags = Win32.KEYEVENTF_KEYUP;

        Win32.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Win32.INPUT>());
    }

    private static string? CleanText(string raw)
    {
        var trimmed = raw.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return null;

        if (trimmed.Length > MaxTextLength)
        {
            return trimmed.Substring(0, MaxTextLength) + "...";
        }

        return trimmed;
    }
}
