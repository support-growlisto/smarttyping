using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Platform;

/// <summary>
/// Start-with-Windows via the per-user Run registry key (no elevation required). Points at the
/// current executable. All registry access is guarded and logged — failures degrade to no-op.
/// </summary>
public sealed class WindowsStartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SmartTyping";

    private readonly ILogger<WindowsStartupService> _logger;

    public WindowsStartupService(ILogger<WindowsStartupService> logger) => _logger = logger;

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read start-with-Windows state.");
            return false;
        }
    }

    public void Enable()
    {
        try
        {
            var exePath = global::System.Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                return;
            }

            // The flag makes the sign-in launch go straight to the tray instead of opening a window.
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            key?.SetValue(ValueName, $"\"{exePath}\" {IStartupService.BackgroundFlag}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enable start-with-Windows.");
        }
    }

    /// <summary>
    /// Rewrites the Run entry if it exists but is out of date — an old install wrote the bare path with
    /// no <see cref="IStartupService.BackgroundFlag"/> (so it popped a window at every sign-in), and an
    /// upgrade can move the executable. Does nothing when start-with-Windows is off.
    /// </summary>
    public void RefreshIfEnabled()
    {
        try
        {
            var exePath = global::System.Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                return;
            }

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key?.GetValue(ValueName) is not string current)
            {
                return;
            }

            var expected = $"\"{exePath}\" {IStartupService.BackgroundFlag}";
            if (!string.Equals(current, expected, StringComparison.OrdinalIgnoreCase))
            {
                key.SetValue(ValueName, expected);
                _logger.LogInformation("Updated the start-with-Windows entry to launch in the background.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh the start-with-Windows entry.");
        }
    }

    public void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to disable start-with-Windows.");
        }
    }
}
