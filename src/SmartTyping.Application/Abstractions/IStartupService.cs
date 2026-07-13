namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Controls whether the app launches automatically when the user signs in to Windows.
/// Implemented in Infrastructure (per-user registry Run key). Never throws.
/// </summary>
public interface IStartupService
{
    /// <summary>
    /// The command-line flag the Windows sign-in launch passes. It tells the app to start silently in
    /// the tray instead of opening its window — nobody wants a window in their face at every sign-in.
    /// </summary>
    public const string BackgroundFlag = "--background";

    /// <summary>Returns true if start-with-Windows is currently enabled.</summary>
    bool IsEnabled();

    /// <summary>Enables start-with-Windows for the current user.</summary>
    void Enable();

    /// <summary>Disables start-with-Windows for the current user.</summary>
    void Disable();

    /// <summary>
    /// Brings an existing start-with-Windows entry up to date — an entry written by an older version
    /// lacks <see cref="BackgroundFlag"/> and so opens a window at every sign-in, and an upgrade can
    /// move the executable. No-op when start-with-Windows is off.
    /// </summary>
    void RefreshIfEnabled();
}
