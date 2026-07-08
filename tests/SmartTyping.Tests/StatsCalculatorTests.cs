using SmartTyping.Application.Stats;
using SmartTyping.Domain.Entities;
using Xunit;

namespace SmartTyping.Tests;

public sealed class StatsCalculatorTests
{
    private static Snippet S(string trigger, string content, int usage, bool enabled = true) =>
        new() { Trigger = trigger, Content = content, UsageCount = usage, IsEnabled = enabled };

    [Fact]
    public void Compute_CountsTotalsAndEnabled()
    {
        var stats = StatsCalculator.Compute(new[]
        {
            S("/a", "alpha", 3),
            S("/b", "beta", 0, enabled: false),
            S("/c", "gamma", 2),
        });

        Assert.Equal(3, stats.TotalSnippets);
        Assert.Equal(2, stats.EnabledSnippets);
        Assert.Equal(5, stats.TotalExpansions); // 3 + 0 + 2
    }

    [Fact]
    public void Compute_TopUsed_OrderedByUsageDescending()
    {
        var stats = StatsCalculator.Compute(new[]
        {
            S("/low", "x", 1),
            S("/high", "x", 9),
            S("/mid", "x", 5),
            S("/never", "x", 0),
        });

        Assert.Equal(new[] { "/high", "/mid", "/low" }, stats.TopUsed.Select(t => t.Trigger));
        Assert.DoesNotContain(stats.TopUsed, t => t.Trigger == "/never"); // zero-usage excluded
    }

    [Fact]
    public void Compute_TimeSaved_UsesCharsBeyondTrigger()
    {
        // content 24 chars, trigger 4 chars -> 20 saved per use; 10 uses -> 200 chars; /4 cps = 50s.
        var stats = StatsCalculator.Compute(new[]
        {
            S("/sig", new string('x', 24), 10),
        });

        Assert.Equal(50, stats.EstimatedSecondsSaved);
    }

    [Fact]
    public void Compute_Empty_ReturnsZeros()
    {
        var stats = StatsCalculator.Compute(Array.Empty<Snippet>());
        Assert.Equal(0, stats.TotalSnippets);
        Assert.Equal(0, stats.TotalExpansions);
        Assert.Empty(stats.TopUsed);
    }
}
