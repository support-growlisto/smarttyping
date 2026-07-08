namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Best-effort detection of whether the currently focused control is a secure/password input,
/// so the app can refuse to capture or inject there. Implemented in Infrastructure.
/// </summary>
/// <remarks>
/// Detection is reliable only for native Win32 <c>Edit</c> controls with the password style.
/// Browsers, Electron, and UWP apps do not expose that style, so a password box in those cannot
/// be detected — callers should treat a <c>false</c> result as "not known to be secure", not a
/// guarantee. See ADR-002.
/// </remarks>
public interface ISecureInputDetector
{
    /// <summary>Returns true if the focused control is detected to be a password field.</summary>
    bool IsFocusedFieldSecure();
}
