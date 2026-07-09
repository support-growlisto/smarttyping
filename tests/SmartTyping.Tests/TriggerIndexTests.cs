using SmartTyping.Application.Snippets;
using Xunit;

namespace SmartTyping.Tests;

public sealed class TriggerIndexTests
{
    [Fact]
    public void CompleteTrigger_ExpandsImmediately()
    {
        var index = new TriggerIndex(new[] { "/sig", "/phone", "/date" });

        Assert.True(index.IsCompleteTrigger("/sig"));
        Assert.True(index.IsCompleteTrigger("/phone"));
    }

    [Fact]
    public void PartialTypedText_DoesNotExpand()
    {
        var index = new TriggerIndex(new[] { "/sig" });

        Assert.False(index.IsCompleteTrigger("/s"));
        Assert.False(index.IsCompleteTrigger("/si"));
        Assert.False(index.IsCompleteTrigger("/sigg"));
    }

    [Fact]
    public void PrefixOfAnotherTrigger_NeverExpandsOnCompletion()
    {
        // Typing "/s" must not fire, otherwise "/sig" could never be typed.
        var index = new TriggerIndex(new[] { "/s", "/sig" });

        Assert.False(index.IsCompleteTrigger("/s"));
        Assert.True(index.IsCompleteTrigger("/sig"));
    }

    [Fact]
    public void MatchIsCaseInsensitive()
    {
        var index = new TriggerIndex(new[] { "/Sig" });

        Assert.True(index.IsCompleteTrigger("/sig"));
        Assert.True(index.IsCompleteTrigger("/SIG"));
    }

    [Fact]
    public void EmptyIndex_MatchesNothing()
    {
        Assert.False(TriggerIndex.Empty.IsCompleteTrigger("/sig"));
        Assert.False(new TriggerIndex(new[] { "/sig" }).IsCompleteTrigger(""));
    }
}
