namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Port over the system clipboard. Implemented in Infrastructure (Windows).
/// </summary>
public interface IClipboardService
{
    /// <summary>Returns the current clipboard text, or empty string if none/unavailable.</summary>
    Task<string> GetTextAsync();

    /// <summary>Sets the clipboard text.</summary>
    Task SetTextAsync(string text);
}
