using System.Windows;
using Microsoft.Win32;

namespace SmartTyping.UI.Themes;

/// <summary>
/// Applies the light/dark theme by swapping the active brush <see cref="ResourceDictionary"/> in
/// <see cref="Application.Resources"/>. "system" follows the Windows app theme. Styles live in
/// Themes/Styles.xaml (always merged) and reference these brushes via DynamicResource, so a swap
/// updates the whole UI live.
/// </summary>
public static class ThemeManager
{
    public const string System = "system";
    public const string Light = "light";
    public const string Dark = "dark";

    private static ResourceDictionary? _current;

    public static string CurrentSetting { get; private set; } = System;

    public static void Apply(string? setting)
    {
        CurrentSetting = setting switch { Light => Light, Dark => Dark, _ => System };
        var dark = CurrentSetting == Dark || (CurrentSetting == System && SystemUsesDarkTheme());
        var uri = new Uri($"pack://application:,,,/Themes/Theme.{(dark ? "Dark" : "Light")}.xaml", UriKind.Absolute);

        // Load the new palette FIRST. If it fails to parse (e.g. a bad colour value), keep the
        // current theme instead of leaving the app with no brushes at all.
        ResourceDictionary dict;
        try
        {
            dict = new ResourceDictionary { Source = uri };
        }
        catch
        {
            return;
        }

        var app = global::System.Windows.Application.Current;
        if (_current is not null)
        {
            app.Resources.MergedDictionaries.Remove(_current);
        }

        app.Resources.MergedDictionaries.Insert(0, dict);
        _current = dict;
    }

    private static bool SystemUsesDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int light && light == 0;
        }
        catch
        {
            return false;
        }
    }
}
