using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Settings;

namespace SmartTyping.Infrastructure.Ai;

/// <summary>
/// <see cref="IAiService"/> backed by Google Gemini's Generative Language API (free tier). The user
/// supplies their own API key in Settings. Never throws across the boundary — failures return null
/// and are logged. This is a network, opt-in feature.
/// </summary>
public sealed class GeminiAiService : IAiService
{
    private const string Model = "gemini-1.5-flash";
    private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

    private const string ImprovePrompt =
        "Improve the following text: fix spelling and grammar and make it read naturally, but keep the " +
        "SAME language and meaning. Return ONLY the improved text, with no quotes, labels, or explanation.\n\n";

    // A dedicated client (the shared one carries GitHub headers used by the update check).
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly SettingsService _settings;
    private readonly ILogger<GeminiAiService> _logger;

    public GeminiAiService(SettingsService settings, ILogger<GeminiAiService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> IsConfiguredAsync() =>
        !string.IsNullOrWhiteSpace(await _settings.GetAiApiKeyAsync());

    public async Task<string?> ImproveAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var key = await _settings.GetAiApiKeyAsync();
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        try
        {
            var url = string.Format(Endpoint, Model, key);
            var request = new GeminiRequest(new[]
            {
                new Content(new[] { new Part(ImprovePrompt + text) })
            });

            var response = await _http.PostAsJsonAsync(url, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini request failed: {Status}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken);
            var improved = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            return string.IsNullOrWhiteSpace(improved) ? null : improved.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI improve request failed.");
            return null;
        }
    }

    private sealed record GeminiRequest([property: JsonPropertyName("contents")] IReadOnlyList<Content> Contents);

    private sealed record Content([property: JsonPropertyName("parts")] IReadOnlyList<Part> Parts);

    private sealed record Part([property: JsonPropertyName("text")] string Text);

    private sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private sealed class Candidate
    {
        [JsonPropertyName("content")]
        public ContentDto? Content { get; set; }
    }

    private sealed class ContentDto
    {
        [JsonPropertyName("parts")]
        public List<PartDto>? Parts { get; set; }
    }

    private sealed class PartDto
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
