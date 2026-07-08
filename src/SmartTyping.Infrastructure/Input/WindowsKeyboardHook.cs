using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// Low-level keyboard hook (<c>WH_KEYBOARD_LL</c>) that raises <see cref="ConversionHotkeyPressed"/>
/// when Ctrl+Shift+L is pressed and <see cref="ExpansionHotkeyPressed"/> when Ctrl+Shift+E is
/// pressed. Passive: it observes input and never rewrites keystrokes (no automatic correction).
/// Any exception in the hook callback is logged and swallowed so the host application can never be
/// crashed by the hook.
///
/// <para><b>Threading:</b> <see cref="Start"/> must be called on a thread with a running message
/// loop (the WPF UI thread). The event is raised on that thread.</para>
/// </summary>
public sealed class WindowsKeyboardHook : IKeyboardHook
{
    private readonly ILogger<WindowsKeyboardHook> _logger;

    // Keep the delegate alive for the lifetime of the hook (prevents GC of the callback).
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private IntPtr _hookHandle = IntPtr.Zero;

    public WindowsKeyboardHook(ILogger<WindowsKeyboardHook> logger)
    {
        _logger = logger;
        _proc = HookCallback;
    }

    public event EventHandler? ConversionHotkeyPressed;

    public event EventHandler? ExpansionHotkeyPressed;

    public event EventHandler? PickerHotkeyPressed;

    public event EventHandler? CaptureHotkeyPressed;

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            return;
        }

        var moduleHandle = NativeMethods.GetModuleHandle("user32");
        _hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _proc, moduleHandle, 0);

        if (_hookHandle == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to install the low-level keyboard hook. The conversion hotkey will be unavailable.");
        }
    }

    public void Stop()
    {
        if (_hookHandle == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0 && IsKeyDown(wParam) && IsCtrlDown() && IsShiftDown())
            {
                var info = System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
                switch (info.vkCode)
                {
                    case NativeMethods.VK_L:
                        Raise(ConversionHotkeyPressed);
                        break;
                    case NativeMethods.VK_E:
                        Raise(ExpansionHotkeyPressed);
                        break;
                    case NativeMethods.VK_SPACE:
                        Raise(PickerHotkeyPressed);
                        break;
                    case NativeMethods.VK_N:
                        Raise(CaptureHotkeyPressed);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keyboard hook callback failed.");
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    // Raise on a thread-pool thread so the hook returns quickly; handlers marshal to UI.
    private void Raise(EventHandler? handler)
    {
        if (handler is not null)
        {
            ThreadPool.QueueUserWorkItem(_ => handler.Invoke(this, EventArgs.Empty));
        }
    }

    private static bool IsKeyDown(IntPtr wParam)
    {
        var msg = (int)wParam;
        return msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN;
    }

    private static bool IsCtrlDown() => (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;

    private static bool IsShiftDown() => (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;

    public void Dispose() => Stop();
}
