namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Identifies the application the user is currently typing into, so we can refuse to synthesize
/// input in apps where that would be destructive. Implemented in Infrastructure.
/// </summary>
public interface IForegroundApp
{
    /// <summary>
    /// The foreground window's executable name without path or extension (e.g. <c>cmd</c>), or null
    /// when it cannot be determined. Called from the keyboard hook on every keystroke, so
    /// implementations must be cheap.
    /// </summary>
    string? GetProcessName();
}
