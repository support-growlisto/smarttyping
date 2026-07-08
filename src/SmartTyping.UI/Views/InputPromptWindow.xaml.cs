using System.Windows;

namespace SmartTyping.UI.Views;

/// <summary>A minimal single-line text prompt (category name, etc.).</summary>
public partial class InputPromptWindow : Window
{
    public InputPromptWindow(string title, string label, string initial)
    {
        InitializeComponent();
        Icon = Services.AppIcon.TryLoad();
        Title = title;
        PromptLabel.Text = label;
        Input.Text = initial;
        Loaded += (_, _) =>
        {
            Input.Focus();
            Input.SelectAll();
        };
    }

    /// <summary>The entered text, or null if cancelled.</summary>
    public string? Value { get; private set; }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        var text = Input.Text?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            return;
        }

        Value = text;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
