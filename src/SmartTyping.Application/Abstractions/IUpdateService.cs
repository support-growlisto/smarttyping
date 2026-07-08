using SmartTyping.Application.Update;

namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Checks whether a newer release is available. Implemented in Infrastructure via an HTTP request to
/// a release feed. This is the app's only network feature and is opt-in.
/// </summary>
public interface IUpdateService
{
    /// <summary>Returns details of a newer release, or null if up to date / unavailable.</summary>
    Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default);
}
