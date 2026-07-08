using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SmartTyping.UI.Services;
using SmartTyping.UI.ViewModels;

namespace SmartTyping.UI.Views;

public partial class PlaceholderInputWindow : Window
{
    public PlaceholderInputWindow(PlaceholderInputViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Icon = AppIcon.TryLoad();
        Loaded += (_, _) => FocusFirstField();
    }

    private void OnInsertClick(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void FocusFirstField()
    {
        var first = FindFirstTextBox(this);
        first?.Focus();
    }

    private static TextBox? FindFirstTextBox(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TextBox tb)
            {
                return tb;
            }

            var nested = FindFirstTextBox(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
