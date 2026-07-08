using System.ComponentModel;
using System.Windows;
using SmartTyping.UI.Services;
using SmartTyping.UI.ViewModels;

namespace SmartTyping.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Icon = AppIcon.TryLoad();

        // Show the running version in the title bar so the build in use is unambiguous.
        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        if (version is not null)
        {
            Title = $"SmartTyping {version.Major}.{version.Minor}.{version.Build}";
        }
        Loaded += async (_, _) => await _viewModel.LoadAsync();
    }

    /// <summary>Closing the window hides it to the tray instead of exiting the app.</summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!App.IsShuttingDown)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow(_viewModel.Settings) { Owner = this };
        window.ShowDialog();
    }
}
