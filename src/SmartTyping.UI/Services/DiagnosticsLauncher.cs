using System.Diagnostics;
using SmartTyping.Infrastructure.Persistence;

namespace SmartTyping.UI.Services;

/// <summary>Opens diagnostic locations (e.g. the log folder) in Explorer for support.</summary>
public static class DiagnosticsLauncher
{
    public static void OpenLogFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AppPaths.LogDirectory,
                UseShellExecute = true
            });
        }
        catch
        {
            // Best-effort; nothing actionable if Explorer fails to launch.
        }
    }
}
