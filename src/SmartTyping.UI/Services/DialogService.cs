using System.Windows;
using SmartTyping.UI.ViewModels;
using SmartTyping.UI.Views;

namespace SmartTyping.UI.Services;

/// <summary>WPF implementation of <see cref="IDialogService"/>.</summary>
public sealed class DialogService : IDialogService
{
    public bool ShowSnippetEditor(SnippetEditViewModel viewModel)
    {
        var window = new SnippetEditWindow(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        return window.ShowDialog() == true;
    }

    public bool Confirm(string message, string title)
    {
        var result = System.Windows.MessageBox.Show(
            System.Windows.Application.Current.MainWindow!,
            message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public bool? ConfirmYesNoCancel(string message, string title)
    {
        var result = System.Windows.MessageBox.Show(
            System.Windows.Application.Current.MainWindow!,
            message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        return result switch
        {
            MessageBoxResult.Yes => true,
            MessageBoxResult.No => false,
            _ => null
        };
    }

    public string? OpenFile(string filter, string title)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = filter, Title = title };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SaveFile(string filter, string title, string defaultFileName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = filter,
            Title = title,
            FileName = defaultFileName
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public Domain.ValueObjects.Hotkey? RecordHotkey()
    {
        var window = new HotkeyRecorderWindow { Owner = System.Windows.Application.Current.MainWindow };
        return window.ShowDialog() == true ? window.Result : null;
    }

    public void ShowMessage(string message, string title) =>
        System.Windows.MessageBox.Show(
            System.Windows.Application.Current.MainWindow!, message, title,
            MessageBoxButton.OK, MessageBoxImage.Information);

    public string? Prompt(string title, string label, string initial)
    {
        var window = new InputPromptWindow(title, label, initial) { Owner = Owner() };
        return window.ShowDialog() == true ? window.Value : null;
    }

    public void ShowStats(StatsViewModel viewModel)
    {
        var window = new StatsWindow(viewModel) { Owner = Owner() };
        window.ShowDialog();
    }

    public void ShowCategoryManager(CategoryManagerViewModel viewModel)
    {
        var window = new CategoryManagerWindow(viewModel) { Owner = Owner() };
        window.ShowDialog();
    }

    private static Window Owner() => System.Windows.Application.Current.MainWindow!;
}
