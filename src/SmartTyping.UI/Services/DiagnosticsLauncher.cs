using System.Diagnostics;
using SmartTyping.Infrastructure.Persistence;

namespace SmartTyping.UI.Services;

/// <summary>Opens diagnostic locations (e.g. the log folder) in Explorer for support.</summary>
public static class DiagnosticsLauncher
{
    public static void OpenLogFolder() => Open(AppPaths.LogDirectory);

    /// <summary>Opens a URL (or path) in the default handler. Best-effort.</summary>
    public static void Open(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
        }
        catch
        {
            // Best-effort; nothing actionable if the shell fails to launch.
        }
    }
}
