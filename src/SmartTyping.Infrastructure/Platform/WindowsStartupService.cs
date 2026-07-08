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

            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            key?.SetValue(ValueName, $"\"{exePath}\"");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enable start-with-Windows.");
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
