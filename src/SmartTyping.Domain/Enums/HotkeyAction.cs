namespace SmartTyping.Domain.Enums;

/// <summary>The rebindable global-hotkey actions.</summary>
public enum HotkeyAction
{
    /// <summary>Convert the layout of the selection / last word.</summary>
    Convert,

    /// <summary>Expand the selected trigger.</summary>
    Expand,

    /// <summary>Open the quick-picker.</summary>
    Picker,

    /// <summary>Add a snippet from the current selection.</summary>
    Capture,

    /// <summary>Improve the selected text with AI.</summary>
    AiImprove
}
