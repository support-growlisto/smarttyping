namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Captures the text currently selected in the foreground application. Implemented in
/// Infrastructure (copies the selection to the clipboard, reads it, then restores the clipboard).
/// Returns an empty string when nothing is selected or capture is not possible.
/// </summary>
public interface ISelectionService
{
    /// <summary>Captures the text currently selected in the foreground app (empty if none).</summary>
    Task<string> CaptureSelectionAsync();

    /// <summary>
    /// Selects the word to the left of the caret (as if pressing Ctrl+Shift+Left) and returns it,
    /// leaving it selected so the caller can replace it. Used to convert the last typed word without
    /// the user manually selecting it. Returns empty if nothing could be captured.
    /// </summary>
    Task<string> CaptureLastWordAsync();
}
