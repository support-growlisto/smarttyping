using System.Windows;
using SmartTyping.Application.Abstractions;
using SmartTyping.UI.ViewModels;
using SmartTyping.UI.Views;

namespace SmartTyping.UI.Services;

/// <summary>
/// WPF implementation of <see cref="IPlaceholderPrompt"/>. Shows a small form (one field per label)
/// on the UI thread — marshaling from a background caller (the expansion coordinator) as needed.
/// </summary>
public sealed class PlaceholderPromptService : IPlaceholderPrompt
{
    public Task<IReadOnlyDictionary<string, string>?> RequestAsync(IReadOnlyList<string> labels)
    {
        var dispatcher = System.Windows.Application.Current.Dispatcher;
        return dispatcher.InvokeAsync(() => ShowDialog(labels)).Task;
    }

    private static IReadOnlyDictionary<string, string>? ShowDialog(IReadOnlyList<string> labels)
    {
        var viewModel = new PlaceholderInputViewModel(labels);
        var window = new PlaceholderInputWindow(viewModel);

        var owner = System.Windows.Application.Current.MainWindow;
        if (owner is not null && owner.IsVisible)
        {
            window.Owner = owner;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        return window.ShowDialog() == true ? viewModel.ToValues() : null;
    }
}
