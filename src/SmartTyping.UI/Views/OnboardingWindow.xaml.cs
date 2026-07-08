using System.Windows;
using SmartTyping.UI.Services;

namespace SmartTyping.UI.Views;

public partial class OnboardingWindow : Window
{
    public OnboardingWindow()
    {
        InitializeComponent();
        Icon = AppIcon.TryLoad();
    }

    private void OnGotItClick(object sender, RoutedEventArgs e) => Close();
}
