using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// Switches the foreground window to the Thai keyboard layout by posting
/// <c>WM_INPUTLANGCHANGEREQUEST</c>. Never throws; returns false when Thai isn't installed.
/// </summary>
public sealed class WindowsKeyboardLayoutSwitcher : IKeyboardLayoutSwitcher
{
    private readonly ILogger<WindowsKeyboardLayoutSwitcher> _logger;

    public WindowsKeyboardLayoutSwitcher(ILogger<WindowsKeyboardLayoutSwitcher> logger) => _logger = logger;

    public bool IsThaiAvailable => NativeMethods.FindThaiLayout() != IntPtr.Zero;

    public bool SwitchForeground(bool toThai)
    {
        try
        {
            var target = NativeMethods.FindLayout(toThai ? NativeMethods.LANG_THAI : NativeMethods.LANG_ENGLISH);
            if (target == IntPtr.Zero)
            {
                return false;
            }

            var window = NativeMethods.GetForegroundWindow();
            if (window == IntPtr.Zero)
            {
                return false;
            }

            return NativeMethods.PostMessage(window, NativeMethods.WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, target);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to switch the foreground keyboard layout.");
            return false;
        }
    }
}
