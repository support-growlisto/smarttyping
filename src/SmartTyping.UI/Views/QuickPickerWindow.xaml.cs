using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using SmartTyping.UI.Services;
using SmartTyping.UI.ViewModels;

namespace SmartTyping.UI.Views;

public partial class QuickPickerWindow : Window
{
    private readonly QuickPickerViewModel _viewModel;

    // WPF throws if Close() is called while the window is already closing. Accepting a snippet sets
    // DialogResult (which begins closing) and immediately deactivates the window, so the Deactivated
    // handler would re-enter Close() and crash. This latch makes dismissal idempotent.
    private bool _closing;

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
        Deactivated += (_, _) => DismissOnce();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _closing = true;
        base.OnClosing(e);
    }

    private void DismissOnce()
    {
        if (_closing)
        {
            return;
        }

        _closing = true;
        Close();
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
                DismissOnce();
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
        _closing = true; // setting DialogResult starts the close; keep Deactivated from re-entering
        DialogResult = true;
    }
}
