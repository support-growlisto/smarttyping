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

    [Fact]
    public void KnownTrigger_IncludesTriggersThatWaitForADelimiter()
    {
        // "/s" never expands on completion, but it is still a trigger: the hook may swallow the space
        // that closes it, because an expansion really is coming.
        var index = new TriggerIndex(new[] { "/s", "/sig" });

        Assert.True(index.IsKnownTrigger("/s"));
        Assert.True(index.IsKnownTrigger("/sig"));
        Assert.True(index.IsKnownTrigger("/SIG"));
    }

    [Fact]
    public void KnownTrigger_RejectsOrdinaryWords()
    {
        // A plain word must keep its space — the hook only swallows a delimiter it is going to replace.
        var index = new TriggerIndex(new[] { "/sig" });

        Assert.False(index.IsKnownTrigger("hello"));
        Assert.False(index.IsKnownTrigger("/si"));
        Assert.False(index.IsKnownTrigger(""));
        Assert.False(TriggerIndex.Empty.IsKnownTrigger("/sig"));
    }
}
