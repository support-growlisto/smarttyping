using System.Windows;
using SmartTyping.UI.ViewModels;

namespace SmartTyping.UI.Views;

public partial class SnippetEditWindow : Window
{
    private readonly SnippetEditViewModel _viewModel;

    public SnippetEditWindow(SnippetEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (await _viewModel.SaveAsync())
        {
            DialogResult = true;
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
