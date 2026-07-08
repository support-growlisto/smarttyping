using SmartTyping.Domain.ValueObjects;
using Xunit;

namespace SmartTyping.Tests;

public sealed class TriggerTests
{
    [Fact]
    public void Create_TrimsSurroundingWhitespace()
    {
        var result = Trigger.Create("  /phone  ");
        Assert.True(result.IsSuccess);
        Assert.Equal("/phone", result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsEmpty(string? raw)
    {
        var result = Trigger.Create(raw);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_RejectsTriggerWithSpaces()
    {
        var result = Trigger.Create("/two words");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Equality_IsCaseInsensitive()
    {
        var a = Trigger.Create("/Sig").Value;
        var b = Trigger.Create("/sig").Value;

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Create_KeepsPrefixCharacter()
    {
        var result = Trigger.Create("/date");
        Assert.Equal("/date", result.Value.ToString());
    }
}
