using SmartTyping.UI.ViewModels;

namespace SmartTyping.UI.Services;

/// <summary>Abstracts modal dialogs so view models stay free of WPF window types.</summary>
public interface IDialogService
{
    /// <summary>Shows the add/edit snippet dialog. Returns true if the user saved.</summary>
    bool ShowSnippetEditor(SnippetEditViewModel viewModel);

    /// <summary>Shows a yes/no confirmation. Returns true for "yes".</summary>
    bool Confirm(string message, string title);

    /// <summary>Shows a yes/no/cancel prompt. Returns true=yes, false=no, null=cancel.</summary>
    bool? ConfirmYesNoCancel(string message, string title);

    /// <summary>Shows an open-file dialog. Returns the chosen path, or null if cancelled.</summary>
    string? OpenFile(string filter, string title);

    /// <summary>Shows a save-file dialog. Returns the chosen path, or null if cancelled.</summary>
    string? SaveFile(string filter, string title, string defaultFileName);
}
