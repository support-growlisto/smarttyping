using SmartTyping.Application.Snippets;
using Xunit;

namespace SmartTyping.Tests;

public sealed class SnippetSearchTests
{
    private static readonly IReadOnlyList<SnippetSearchItem> Items = new[]
    {
        new SnippetSearchItem(1, "/phone", "08x-xxx-xxxx", 2),
        new SnippetSearchItem(2, "/sig", "Best regards, phone owner", 10),
        new SnippetSearchItem(3, "/addr", "123 Phone Street", 5),
        new SnippetSearchItem(4, "/email", "me@example.com", 1),
    };

    [Fact]
    public void EmptyQuery_ReturnsAllSortedByUsageDesc()
    {
        var result = SnippetSearch.Filter(Items, "");

        Assert.Equal(4, result.Count);
        Assert.Equal("/sig", result[0].Trigger);   // usage 10
        Assert.Equal("/addr", result[1].Trigger);  // usage 5
    }

    [Fact]
    public void TriggerPrefix_RanksFirst()
    {
        var result = SnippetSearch.Filter(Items, "/pho");

        Assert.Equal("/phone", result[0].Trigger);
    }

    [Fact]
    public void Query_MatchesTriggerAndContent()
    {
        // "phone" appears in /phone (trigger prefix), /sig (content), /addr (content).
        var result = SnippetSearch.Filter(Items, "phone");

        Assert.Equal("/phone", result[0].Trigger); // trigger-prefix wins
        Assert.Contains(result, i => i.Trigger == "/sig");
        Assert.Contains(result, i => i.Trigger == "/addr");
        Assert.DoesNotContain(result, i => i.Trigger == "/email");
    }

    [Fact]
    public void Query_IsCaseInsensitive()
    {
        var result = SnippetSearch.Filter(Items, "PHONE");
        Assert.Equal("/phone", result[0].Trigger);
    }

    [Fact]
    public void NoMatch_ReturnsEmpty()
    {
        var result = SnippetSearch.Filter(Items, "zzzzz");
        Assert.Empty(result);
    }

    [Fact]
    public void Fuzzy_NonContiguousSubsequenceMatches()
    {
        // "phn" is not a substring of "/phone" but is a subsequence (p-h-o-n-e).
        var result = SnippetSearch.Filter(Items, "phn");

        Assert.Contains(result, i => i.Trigger == "/phone");
        Assert.Equal("/phone", result[0].Trigger);
    }

    [Fact]
    public void Fuzzy_RespectsOrder()
    {
        // "eno" is not a subsequence of "/phone" (o comes before n), so no trigger match.
        var (matched, _) = (FuzzyMatcher.TryMatch("/phone", "eno", out var s), s);
        Assert.False(matched);
    }

    [Fact]
    public void Fuzzy_PrefixScoresHigherThanMidMatch()
    {
        var items = new[]
        {
            new SnippetSearchItem(1, "/abcd", "x", 0),   // "ab" at start
            new SnippetSearchItem(2, "/xabx", "y", 0),   // "ab" in the middle
        };

        var result = SnippetSearch.Filter(items, "ab");
        Assert.Equal("/abcd", result[0].Trigger);
    }

    [Fact]
    public void ContentContains_RanksAfterTriggerMatches()
    {
        var result = SnippetSearch.Filter(Items, "phone");

        // /sig and /addr only match on content, so they come after the trigger match.
        var phoneIndex = IndexOf(result, "/phone");
        var sigIndex = IndexOf(result, "/sig");
        Assert.True(phoneIndex < sigIndex);
    }

    private static int IndexOf(IReadOnlyList<SnippetSearchItem> items, string trigger)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Trigger == trigger)
            {
                return i;
            }
        }

        return -1;
    }
}
