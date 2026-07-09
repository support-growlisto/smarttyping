namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Switches the foreground window's input language. Used after an automatic layout correction: the
/// user meant to type the other language, so the rest of what they type should come out in it too —
/// otherwise the correction fixes only the characters typed so far.
/// </summary>
public interface IKeyboardLayoutSwitcher
{
    /// <summary>True when a Thai keyboard layout is installed and can be switched to.</summary>
    bool IsThaiAvailable { get; }

    /// <summary>
    /// Asks the foreground window to switch its input language. Best-effort; returns false when the
    /// requested layout isn't installed.
    /// </summary>
    bool SwitchForeground(bool toThai);
}
