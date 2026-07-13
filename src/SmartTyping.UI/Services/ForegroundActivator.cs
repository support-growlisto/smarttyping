using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SmartTyping.UI.Services;

/// <summary>
/// Brings one of our own windows to the front <b>with the keyboard</b>.
///
/// <para>Windows will not let a background process steal the foreground: <c>SetForegroundWindow</c> from
/// an app the user is not currently in gets the window drawn on top and nothing else. Measured on the
/// quick picker: the palette appeared over Chrome, and every keystroke still went to Chrome — you could
/// see the search box but not type in it.</para>
///
/// <para>The way out is the one every launcher uses: attach our input queue to the foreground window's
/// thread for the moment it takes to claim the foreground. While attached, the two threads share an input
/// state, and the call is allowed.</para>
/// </summary>
internal static class ForegroundActivator
{
    public static void ForceActivate(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var foreground = GetForegroundWindow();
        var foregroundThread = GetWindowThreadProcessId(foreground, out _);
        var thisThread = GetCurrentThreadId();

        var attached = foregroundThread != 0
                       && foregroundThread != thisThread
                       && AttachThreadInput(thisThread, foregroundThread, true);

        try
        {
            SetForegroundWindow(hwnd);
            BringWindowToTop(hwnd);
            window.Activate();
        }
        finally
        {
            if (attached)
            {
                AttachThreadInput(thisThread, foregroundThread, false);
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
}
