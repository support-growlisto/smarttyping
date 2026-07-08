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
}
