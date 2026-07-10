using System.Text;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// Reads the foreground window's executable name.
///
/// <para>This is consulted from the low-level keyboard hook on every keystroke, and a low-level hook
/// that takes too long is silently removed by Windows. Opening a process handle per keypress would be
/// far too expensive, so the last answer is cached against the window handle: the name only has to be
/// looked up when the user switches windows.</para>
/// </summary>
public sealed class WindowsForegroundApp : IForegroundApp
{
    private IntPtr _cachedWindow = IntPtr.Zero;
    private string? _cachedName;

    public string? GetProcessName()
    {
        var window = NativeMethods.GetForegroundWindow();
        if (window == IntPtr.Zero)
        {
            return null;
        }

        if (window == _cachedWindow)
        {
            return _cachedName;
        }

        _cachedWindow = window;
        _cachedName = ResolveProcessName(window);
        return _cachedName;
    }

    private static string? ResolveProcessName(IntPtr window)
    {
        try
        {
            NativeMethods.GetWindowThreadProcessId(window, out var processId);
            if (processId == 0)
            {
                return null;
            }

            var handle = NativeMethods.OpenProcess(
                NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (handle == IntPtr.Zero)
            {
                return null; // e.g. a protected or elevated process — treated as "unknown"
            }

            try
            {
                var buffer = new StringBuilder(512);
                var size = (uint)buffer.Capacity;
                if (!NativeMethods.QueryFullProcessImageName(handle, 0, buffer, ref size))
                {
                    return null;
                }

                var path = buffer.ToString(0, (int)size);
                return Path.GetFileNameWithoutExtension(path);
            }
            finally
            {
                NativeMethods.CloseHandle(handle);
            }
        }
        catch
        {
            // Never let a hook callback throw.
            return null;
        }
    }
}
