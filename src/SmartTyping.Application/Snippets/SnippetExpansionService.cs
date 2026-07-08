using SmartTyping.Application.Abstractions;

namespace SmartTyping.Application.Snippets;

/// <summary>
/// Coordinates snippet expansion: look up the trigger, render its template variables, and
/// record usage. Called on an explicit user action — never automatically (MVP constraint).
/// </summary>
public sealed class SnippetExpansionService
{
    private readonly ISnippetRepository _snippets;
    private readonly ITemplateEngine _templateEngine;
    private readonly IDateTimeProvider _clock;

    public SnippetExpansionService(
        ISnippetRepository snippets,
        ITemplateEngine templateEngine,
        IDateTimeProvider clock)
    {
        _snippets = snippets;
        _templateEngine = templateEngine;
        _clock = clock;
    }

    /// <summary>
    /// Attempts to expand <paramref name="trigger"/>. On a hit for an enabled snippet, returns the
    /// rendered text and records usage; otherwise returns a miss. Disabled snippets never expand.
    /// </summary>
    public async Task<ExpansionResult> TryExpandAsync(string trigger)
    {
        if (string.IsNullOrWhiteSpace(trigger))
        {
            return ExpansionResult.Miss();
        }

        var snippet = await _snippets.FindByTriggerAsync(trigger.Trim());
        if (snippet is null || !snippet.IsEnabled)
        {
            return ExpansionResult.Miss();
        }

        var rendered = await _templateEngine.RenderAsync(snippet.Content);

        // The user cancelled an {input:…} prompt — don't insert anything or count usage.
        if (rendered.Cancelled)
        {
            return ExpansionResult.Miss();
        }

        // Usage is recorded by the caller via RegisterUsageAsync only after the text is actually
        // injected, so a failed paste (secure field, clipboard timeout) doesn't inflate the stats.
        return ExpansionResult.Hit(snippet.Id, rendered.Text, rendered.CursorOffset);
    }

    /// <summary>Records one successful use of a snippet. Call after the expansion is injected.</summary>
    public Task RegisterUsageAsync(int snippetId) => _snippets.RegisterUsageAsync(snippetId, _clock.UtcNow);
}
