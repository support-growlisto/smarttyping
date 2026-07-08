namespace SmartTyping.Application.Snippets;

/// <summary>A snippet reduced to what the quick-picker needs.</summary>
public sealed record SnippetSearchItem(int Id, string Trigger, string Preview, int UsageCount);

/// <summary>
/// Pure ranking/filtering for the quick-picker, powered by <see cref="FuzzyMatcher"/>. The query is
/// matched as a subsequence against the trigger (weighted higher) and the content preview; results
/// are ordered by score (desc), then usage (desc), then trigger. An empty query lists everything by
/// usage.
/// </summary>
public static class SnippetSearch
{
    // Ensures any trigger match outranks a content-only match, regardless of raw fuzzy score.
    private const int TriggerBias = 1000;

    public static IReadOnlyList<SnippetSearchItem> Filter(IReadOnlyList<SnippetSearchItem> all, string? query)
    {
        var q = query?.Trim() ?? string.Empty;
        if (q.Length == 0)
        {
            return all
                .OrderByDescending(i => i.UsageCount)
                .ThenBy(i => i.Trigger, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return all
            .Select(i => (item: i, score: Score(i, q)))
            .Where(x => x.score.HasValue)
            .OrderByDescending(x => x.score!.Value)
            .ThenByDescending(x => x.item.UsageCount)
            .ThenBy(x => x.item.Trigger, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.item)
            .ToList();
    }

    /// <summary>Best fuzzy score of an item, or null if neither the trigger nor the preview matched.</summary>
    private static int? Score(SnippetSearchItem item, string query)
    {
        int? best = null;

        if (FuzzyMatcher.TryMatch(item.Trigger, query, out var triggerScore))
        {
            best = triggerScore + TriggerBias;
        }

        if (FuzzyMatcher.TryMatch(item.Preview, query, out var previewScore))
        {
            best = best is null ? previewScore : Math.Max(best.Value, previewScore);
        }

        return best;
    }
}
