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

    /// <summary>
    /// Brings <paramref name="handle"/> back to the foreground and waits until Windows reports that it
    /// really is the foreground window, instead of sleeping for a guessed interval. Returns false if it
    /// has not become foreground within <paramref name="timeoutMs"/> — the caller must then type
    /// nothing, because it does not know where the keystrokes would land.
    /// </summary>
    Task<bool> RestoreAsync(long handle, int timeoutMs = 500);
}
