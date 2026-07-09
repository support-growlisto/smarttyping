namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Switches the foreground window's input language. Used after an automatic layout correction: the
/// user meant to type Thai, so the rest of what they type should come out Thai too — otherwise the
/// correction fixes only the characters typed so far.
/// </summary>
public interface IKeyboardLayoutSwitcher
{
    /// <summary>True when a Thai keyboard layout is installed and can be switched to.</summary>
    bool IsThaiAvailable { get; }

    /// <summary>Asks the foreground window to switch to the Thai layout. Best-effort.</summary>
    bool SwitchForegroundToThai();
}
