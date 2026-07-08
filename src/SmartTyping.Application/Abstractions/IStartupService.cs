namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Controls whether the app launches automatically when the user signs in to Windows.
/// Implemented in Infrastructure (per-user registry Run key). Never throws.
/// </summary>
public interface IStartupService
{
    /// <summary>Returns true if start-with-Windows is currently enabled.</summary>
    bool IsEnabled();

    /// <summary>Enables start-with-Windows for the current user.</summary>
    void Enable();

    /// <summary>Disables start-with-Windows for the current user.</summary>
    void Disable();
}
