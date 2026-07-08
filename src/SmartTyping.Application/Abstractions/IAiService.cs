namespace SmartTyping.Application.Abstractions;

/// <summary>
/// On-demand AI text assistance (opt-in, network, uses the user's API key). Provider-agnostic port;
/// the default implementation talks to Google Gemini's free tier. Returns null when disabled
/// (no key) or on failure — the caller then does nothing.
/// </summary>
public interface IAiService
{
    /// <summary>True if an API key is configured (the feature is usable).</summary>
    Task<bool> IsConfiguredAsync();

    /// <summary>Improves/polishes <paramref name="text"/> (fixes grammar, clarity), keeping its language.</summary>
    Task<string?> ImproveAsync(string text, CancellationToken cancellationToken = default);
}
