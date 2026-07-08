using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Input;

/// <summary>Win32 implementation of <see cref="IForegroundWindowService"/>.</summary>
public sealed class WindowsForegroundWindowService : IForegroundWindowService
{
    public long CaptureForeground() => NativeMethods.GetForegroundWindow().ToInt64();

    public void Restore(long handle)
    {
        if (handle != 0)
        {
            NativeMethods.SetForegroundWindow(new IntPtr(handle));
        }
    }
}
