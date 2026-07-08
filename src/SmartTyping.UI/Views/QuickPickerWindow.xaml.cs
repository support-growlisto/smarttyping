using System.Windows;
using System.Windows.Input;
using SmartTyping.UI.Services;
using SmartTyping.UI.ViewModels;

namespace SmartTyping.UI.Views;

public partial class QuickPickerWindow : Window
{
    private readonly QuickPickerViewModel _viewModel;

    public QuickPickerWindow(QuickPickerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Icon = AppIcon.TryLoad();

        Loaded += async (_, _) =>
        {
            SearchBox.Focus();
            await _viewModel.LoadAsync();
        };

        // Dismiss if the user clicks away.
        Deactivated += (_, _) => Close();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    /// <summary>The trigger the user chose, or null if the picker was dismissed.</summary>
    public string? SelectedTrigger { get; private set; }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                _viewModel.MoveSelection(1);
                ResultList.ScrollIntoView(_viewModel.Selected);
                e.Handled = true;
                break;
            case Key.Up:
                _viewModel.MoveSelection(-1);
                ResultList.ScrollIntoView(_viewModel.Selected);
                e.Handled = true;
                break;
            case Key.Enter:
                Accept();
                e.Handled = true;
                break;
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
        }
    }

    private void OnItemDoubleClick(object sender, MouseButtonEventArgs e) => Accept();

    private void Accept()
    {
        if (_viewModel.Selected is null)
        {
            return;
        }

        SelectedTrigger = _viewModel.Selected.Trigger;
        DialogResult = true;
    }
}
