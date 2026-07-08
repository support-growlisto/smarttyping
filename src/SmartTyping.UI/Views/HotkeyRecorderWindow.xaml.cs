using System.Windows;
using System.Windows.Input;
using SmartTyping.Domain.ValueObjects;
using SmartTyping.UI.Services;

namespace SmartTyping.UI.Views;

/// <summary>Captures a single key combination (modifiers + key) from the user.</summary>
public partial class HotkeyRecorderWindow : Window
{
    public HotkeyRecorderWindow()
    {
        InitializeComponent();
        Icon = AppIcon.TryLoad();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    /// <summary>The captured hotkey, or null if the user cancelled.</summary>
    public Hotkey? Result { get; private set; }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Cancel on a bare Escape.
        if (key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
        {
            DialogResult = false;
            return;
        }

        // Ignore standalone modifier presses — wait for a real key.
        if (IsModifierKey(key))
        {
            Preview.Text = DescribeModifiers(Keyboard.Modifiers);
            return;
        }

        var mods = ToHotkeyModifiers(Keyboard.Modifiers);
        var vk = KeyInterop.VirtualKeyFromKey(key);
        var hotkey = new Hotkey(mods, vk);
        Preview.Text = hotkey.ToStorageString();

        if (hotkey.IsValid)
        {
            Result = hotkey;
            DialogResult = true;
        }
    }

    private static bool IsModifierKey(Key key) => key is
        Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or
        Key.LeftAlt or Key.RightAlt or Key.System or Key.LWin or Key.RWin;

    private static HotkeyModifiers ToHotkeyModifiers(ModifierKeys m)
    {
        var result = HotkeyModifiers.None;
        if (m.HasFlag(ModifierKeys.Control)) result |= HotkeyModifiers.Ctrl;
        if (m.HasFlag(ModifierKeys.Shift)) result |= HotkeyModifiers.Shift;
        if (m.HasFlag(ModifierKeys.Alt)) result |= HotkeyModifiers.Alt;
        if (m.HasFlag(ModifierKeys.Windows)) result |= HotkeyModifiers.Win;
        return result;
    }

    private static string DescribeModifiers(ModifierKeys m) =>
        ToHotkeyModifiers(m) is var hm && hm != HotkeyModifiers.None
            ? new Hotkey(hm, 0x20).ToStorageString().Replace("+Space", "+…")
            : "…";
}
