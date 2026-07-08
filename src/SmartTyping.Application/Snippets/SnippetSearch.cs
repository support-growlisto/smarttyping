namespace SmartTyping.Application.Snippets;

/// <summary>A snippet reduced to what the quick-picker needs.</summary>
public sealed record SnippetSearchItem(int Id, string Trigger, string Preview, int UsageCount);

/// <summary>
/// Pure ranking/filtering for the quick-picker. Trigger-prefix matches rank first, then
/// trigger-contains, then content-contains; ties break by usage count (desc) then trigger.
/// </summary>
public static class SnippetSearch
{
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
            .Select(i => (item: i, rank: Rank(i, q)))
            .Where(x => x.rank >= 0)
            .OrderBy(x => x.rank)
            .ThenByDescending(x => x.item.UsageCount)
            .ThenBy(x => x.item.Trigger, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.item)
            .ToList();
    }

    private static int Rank(SnippetSearchItem item, string query)
    {
        if (item.Trigger.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (item.Trigger.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (item.Preview.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return -1;
    }
}
