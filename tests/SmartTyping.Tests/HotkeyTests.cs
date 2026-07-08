using SmartTyping.Domain.ValueObjects;
using Xunit;

namespace SmartTyping.Tests;

public sealed class HotkeyTests
{
    [Fact]
    public void ToStorageString_FormatsModifiersAndKey()
    {
        var hk = new Hotkey(HotkeyModifiers.Ctrl | HotkeyModifiers.Shift, 0x4C); // L
        Assert.Equal("Ctrl+Shift+L", hk.ToStorageString());
    }

    [Fact]
    public void ToStorageString_Space()
    {
        var hk = new Hotkey(HotkeyModifiers.Ctrl | HotkeyModifiers.Shift, 0x20);
        Assert.Equal("Ctrl+Shift+Space", hk.ToStorageString());
    }

    [Theory]
    [InlineData("Ctrl+Shift+L")]
    [InlineData("Ctrl+Alt+F5")]
    [InlineData("Shift+Win+Space")]   // canonical modifier order: Ctrl, Shift, Alt, Win
    [InlineData("Alt+9")]
    public void Parse_RoundTrips(string text)
    {
        Assert.True(Hotkey.TryParse(text, out var hk));
        Assert.Equal(text, hk.ToStorageString());
    }

    [Fact]
    public void Parse_NormalizesModifierOrder()
    {
        Assert.True(Hotkey.TryParse("Win+Shift+Space", out var hk));
        Assert.Equal("Shift+Win+Space", hk.ToStorageString());
    }

    [Fact]
    public void Parse_IsCaseInsensitiveForModifiers()
    {
        Assert.True(Hotkey.TryParse("ctrl+shift+l", out var hk));
        Assert.Equal(new Hotkey(HotkeyModifiers.Ctrl | HotkeyModifiers.Shift, 0x4C), hk);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ctrl+Shift")]      // no key
    [InlineData("Ctrl+@@")]         // unparseable key
    public void Parse_RejectsInvalid(string? text)
    {
        Assert.False(Hotkey.TryParse(text, out _));
    }

    [Fact]
    public void IsValid_RequiresNonShiftModifier()
    {
        Assert.True(new Hotkey(HotkeyModifiers.Ctrl, 0x4C).IsValid);
        Assert.True(new Hotkey(HotkeyModifiers.Alt, 0x4C).IsValid);
        Assert.False(new Hotkey(HotkeyModifiers.Shift, 0x4C).IsValid);   // Shift-only would hijack typing
        Assert.False(new Hotkey(HotkeyModifiers.None, 0x4C).IsValid);
    }
}
