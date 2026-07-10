using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Input;

/// <summary>Win32 implementation of <see cref="IForegroundWindowService"/>.</summary>
public sealed class WindowsForegroundWindowService : IForegroundWindowService
{
    private const int PollIntervalMs = 10;

    public long CaptureForeground() => NativeMethods.GetForegroundWindow().ToInt64();

    public void Restore(long handle)
    {
        if (handle != 0)
        {
            NativeMethods.SetForegroundWindow(new IntPtr(handle));
        }
    }

    public async Task<bool> RestoreAsync(long handle, int timeoutMs = 500)
    {
        if (handle == 0)
        {
            return false;
        }

        Restore(handle);

        // SetForegroundWindow only asks: the activation is posted, and the window becomes foreground
        // some indeterminate time later. Watch for it instead of assuming how long that takes.
        var target = new IntPtr(handle);
        for (var waited = 0; waited < timeoutMs; waited += PollIntervalMs)
        {
            if (NativeMethods.GetForegroundWindow() == target)
            {
                return true;
            }

            await Task.Delay(PollIntervalMs);
        }

        return NativeMethods.GetForegroundWindow() == target;
    }
}
