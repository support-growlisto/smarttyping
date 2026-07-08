using System.Windows;
using SmartTyping.UI.Services;
using SmartTyping.UI.ViewModels;

namespace SmartTyping.UI.Views;

public partial class StatsWindow : Window
{
    public StatsWindow(StatsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Icon = AppIcon.TryLoad();
        Loaded += async (_, _) => await viewModel.LoadAsync();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
