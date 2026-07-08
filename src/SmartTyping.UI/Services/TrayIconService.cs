using System.Drawing;
using System.IO;
using System.Windows;
using SmartTyping.UI.Localization;
using WinForms = System.Windows.Forms;

namespace SmartTyping.UI.Services;

/// <summary>
/// Manages the system-tray (notification area) icon and its context menu. Uses the WinForms
/// <see cref="WinForms.NotifyIcon"/>. Falls back to a stock icon so it works without an asset.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private WinForms.NotifyIcon? _notifyIcon;
    private Window? _mainWindow;
    private Action? _exit;

    public void Initialize(Window mainWindow, Action exit)
    {
        _mainWindow = mainWindow;
        _exit = exit;

        var loc = LocalizationManager.Instance;
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add(loc["Tray_Show"], null, (_, _) => ShowMainWindow());
        menu.Items.Add(loc["Tray_OpenLogs"], null, (_, _) => DiagnosticsLauncher.OpenLogFolder());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(loc["Tray_Exit"], null, (_, _) => _exit?.Invoke());

        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "SmartTyping",
            Visible = true,
            ContextMenuStrip = menu
        };

        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    /// <summary>Shows a brief balloon notification (e.g. after a conversion).</summary>
    public void ShowBalloon(string title, string message)
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(1500);
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private static Icon LoadIcon()
    {
        // Prefer a bundled app icon if present; otherwise use a stock icon so the tray always works.
        var iconPath = Path.Combine(AppContext.BaseDirectory, "assets", "app.ico");
        try
        {
            return File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    public void Dispose()
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}
