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

    /// <summary>
    /// When false, <see cref="ShowBalloon"/> does nothing. Gated here rather than at each call site so
    /// no feature can pop a notification the user switched off.
    /// </summary>
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary>Shows a brief balloon notification (e.g. after a conversion).</summary>
    public void ShowBalloon(string title, string message)
    {
        if (_notifyIcon is null || !NotificationsEnabled)
        {
            return;
        }

        _notifyIcon.BalloonTipIcon = WinForms.ToolTipIcon.Info;
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(3000);
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
        // Match the tray slot exactly so the icon renders crisp and never blank (some multi-frame
        // PNG .ico files render empty when the wrong frame is auto-picked).
        var size = WinForms.SystemInformation.SmallIconSize;

        // 1) Embedded resource — works even from a single-file publish (no loose file needed).
        try
        {
            using var stream = typeof(TrayIconService).Assembly
                .GetManifestResourceStream("SmartTyping.UI.app.ico");
            if (stream is not null)
            {
                return new Icon(stream, size);
            }
        }
        catch
        {
            // fall through to the file / stock fallbacks
        }

        // 2) Loose file next to the exe (dev / framework-dependent builds).
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "assets", "app.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath, size);
            }
        }
        catch
        {
            // fall through to the stock icon
        }

        // 3) Last resort so the tray always has *something* clickable.
        return SystemIcons.Application;
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
