using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// Detects a focused native password <c>Edit</c> control via the foreground window's focused HWND
/// and its <c>ES_PASSWORD</c> style. Best-effort only (see <see cref="ISecureInputDetector"/>):
/// non-native inputs (browsers/Electron/UWP) cannot be inspected and return false. Never throws.
/// </summary>
public sealed class WindowsSecureInputDetector : ISecureInputDetector
{
    private readonly ILogger<WindowsSecureInputDetector> _logger;

    public WindowsSecureInputDetector(ILogger<WindowsSecureInputDetector> logger) => _logger = logger;

    public bool IsFocusedFieldSecure()
    {
        try
        {
            var foreground = NativeMethods.GetForegroundWindow();
            if (foreground == IntPtr.Zero)
            {
                return false;
            }

            var threadId = NativeMethods.GetWindowThreadProcessId(foreground, out _);
            var info = new NativeMethods.GUITHREADINFO
            {
                cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.GUITHREADINFO>()
            };

            if (!NativeMethods.GetGUIThreadInfo(threadId, ref info) || info.hwndFocus == IntPtr.Zero)
            {
                return false;
            }

            var style = NativeMethods.GetWindowLongStyle(info.hwndFocus).ToInt64();
            return (style & NativeMethods.ES_PASSWORD) != 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Secure-field detection failed; treating field as not secure.");
            return false;
        }
    }
}
