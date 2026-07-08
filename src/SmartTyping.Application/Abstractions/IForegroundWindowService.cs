namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Captures and restores the foreground window handle. Used by the quick-picker so that, after the
/// picker window takes focus, the previously active application can be refocused before text is
/// injected into it. Implemented in Infrastructure.
/// </summary>
public interface IForegroundWindowService
{
    /// <summary>Returns an opaque handle to the current foreground window.</summary>
    long CaptureForeground();

    /// <summary>Brings the window identified by <paramref name="handle"/> back to the foreground.</summary>
    void Restore(long handle);
}
