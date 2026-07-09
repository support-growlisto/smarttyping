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

    public bool SwitchForegroundToThai()
    {
        try
        {
            var thai = NativeMethods.FindThaiLayout();
            if (thai == IntPtr.Zero)
            {
                return false;
            }

            var window = NativeMethods.GetForegroundWindow();
            if (window == IntPtr.Zero)
            {
                return false;
            }

            return NativeMethods.PostMessage(window, NativeMethods.WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, thai);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to switch the foreground layout to Thai.");
            return false;
        }
    }
}
