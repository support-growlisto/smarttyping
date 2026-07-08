using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Update;

namespace SmartTyping.Infrastructure.Update;

/// <summary>
/// Checks the project's GitHub "latest release" endpoint for a newer version. Best-effort: any
/// network/parse failure returns null (no update) and is logged, never thrown.
/// </summary>
public sealed class WindowsUpdateChecker : IUpdateService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/support-growlisto/smarttyping/releases/latest";

    private readonly HttpClient _http;
    private readonly ILogger<WindowsUpdateChecker> _logger;

    public WindowsUpdateChecker(HttpClient http, ILogger<WindowsUpdateChecker> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var release = await _http.GetFromJsonAsync<GitHubRelease>(LatestReleaseUrl, cancellationToken);
            if (release?.TagName is null)
            {
                return null;
            }

            var current = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";
            if (!UpdateComparer.IsNewer(current, release.TagName))
            {
                return null;
            }

            return new UpdateInfo(
                release.TagName.TrimStart('v', 'V'),
                string.IsNullOrWhiteSpace(release.HtmlUrl) ? LatestReleaseUrl : release.HtmlUrl!,
                release.Body ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Update check failed (offline or endpoint unavailable).");
            return null;
        }
    }

    private sealed class GitHubRelease
    {
        [System.Text.Json.Serialization.JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("body")]
        public string? Body { get; set; }
    }
}
