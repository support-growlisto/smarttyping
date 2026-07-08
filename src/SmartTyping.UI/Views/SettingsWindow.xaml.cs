using System.Windows;
using SmartTyping.UI.Services;
using SmartTyping.UI.ViewModels;

namespace SmartTyping.UI.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnOpenLogsClick(object sender, RoutedEventArgs e) => DiagnosticsLauncher.OpenLogFolder();
}
