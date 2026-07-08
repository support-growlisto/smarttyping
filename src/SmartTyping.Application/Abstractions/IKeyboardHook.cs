namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Abstraction over a global keyboard hook / hotkey source. Implemented in Infrastructure.
/// In the MVP this is passive: it observes input and raises the conversion hotkey; it never
/// rewrites keystrokes (no automatic correction).
/// </summary>
public interface IKeyboardHook : IDisposable
{
    /// <summary>Raised when the language-conversion hotkey (default Ctrl+Shift+L) is pressed.</summary>
    event EventHandler? ConversionHotkeyPressed;

    /// <summary>Raised when the snippet-expansion hotkey (default Ctrl+Shift+E) is pressed.</summary>
    event EventHandler? ExpansionHotkeyPressed;

    /// <summary>Raised when the quick-picker hotkey (default Ctrl+Shift+Space) is pressed.</summary>
    event EventHandler? PickerHotkeyPressed;

    /// <summary>Begins listening for the hotkey.</summary>
    void Start();

    /// <summary>Stops listening.</summary>
    void Stop();
}
