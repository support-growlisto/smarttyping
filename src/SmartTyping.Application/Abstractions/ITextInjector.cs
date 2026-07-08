namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Injects text into the foreground application. Implemented in Infrastructure.
/// Implementations should prefer a reliable method (e.g. clipboard paste) and never throw
/// across the boundary — failures are reported via the return value.
/// </summary>
public interface ITextInjector
{
    /// <summary>
    /// Attempts to insert <paramref name="text"/> into the active application, optionally
    /// replacing the current selection. Returns true if injection was performed.
    /// </summary>
    /// <param name="cursorOffset">
    /// If set, the caret is moved to this index within <paramref name="text"/> after insertion
    /// (best-effort, via left-arrow taps) — used for the <c>{cursor}</c> template marker.
    /// </param>
    Task<bool> InjectAsync(string text, bool replaceSelection, int? cursorOffset = null);
}
