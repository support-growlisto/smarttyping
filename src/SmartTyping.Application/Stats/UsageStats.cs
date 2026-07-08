using SmartTyping.Domain.Entities;

namespace SmartTyping.Application.Stats;

/// <summary>A snippet's usage, for the "most used" list.</summary>
public sealed record SnippetUsage(string Trigger, int UsageCount);

/// <summary>Aggregated usage statistics.</summary>
public sealed record UsageStats(
    int TotalSnippets,
    int EnabledSnippets,
    int TotalExpansions,
    int EstimatedSecondsSaved,
    IReadOnlyList<SnippetUsage> TopUsed);

/// <summary>
/// Pure computation of usage statistics from the snippet set. Kept dependency-free so it is easy
/// to unit-test. "Time saved" estimates the typing avoided: for each expansion, the characters of
/// the expanded content beyond the trigger, divided by an average typing speed.
/// </summary>
public static class StatsCalculator
{
    /// <summary>Average typing speed used for the time-saved estimate (characters per second).</summary>
    public const double TypingCharsPerSecond = 4.0;

    public static UsageStats Compute(IReadOnlyList<Snippet> snippets, int topCount = 5)
    {
        var enabled = 0;
        var totalExpansions = 0;
        double charsSaved = 0;

        foreach (var s in snippets)
        {
            if (s.IsEnabled)
            {
                enabled++;
            }

            totalExpansions += s.UsageCount;
            var savedPerUse = Math.Max(0, s.Content.Length - s.Trigger.Length);
            charsSaved += (double)savedPerUse * s.UsageCount;
        }

        var topUsed = snippets
            .Where(s => s.UsageCount > 0)
            .OrderByDescending(s => s.UsageCount)
            .ThenBy(s => s.Trigger, StringComparer.OrdinalIgnoreCase)
            .Take(topCount)
            .Select(s => new SnippetUsage(s.Trigger, s.UsageCount))
            .ToList();

        var secondsSaved = (int)Math.Round(charsSaved / TypingCharsPerSecond);

        return new UsageStats(snippets.Count, enabled, totalExpansions, secondsSaved, topUsed);
    }
}
