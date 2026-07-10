using SmartTyping.Domain.ValueObjects;
using Xunit;

namespace SmartTyping.Tests;

public sealed class HotkeyTests
{
    [Fact]
    public void ShiftPlusBackspace_IsValid_BecauseBackspaceTypesNothing()
    {
        // The undo binding. Shift alone is safe here: Backspace never inserts a character.
        var undo = new Hotkey(HotkeyModifiers.Shift, 0x08);

        Assert.True(undo.IsValid);
        Assert.Equal("Shift+Backspace", undo.ToStorageString());

        Assert.True(Hotkey.TryParse("Shift+Backspace", out var parsed));
        Assert.Equal(undo, parsed);
    }

    [Theory]
    [InlineData(0x41)] // A
    [InlineData(0x39)] // 9
    [InlineData(0x20)] // Space
    public void ShiftPlusACharacterKey_IsRejected(int virtualKey)
    {
        // Would hijack ordinary typing (Shift+A must stay "A").
        Assert.False(new Hotkey(HotkeyModifiers.Shift, virtualKey).IsValid);
    }

    [Theory]
    [InlineData(0x2E, "Delete")]
    [InlineData(0x24, "Home")]
    [InlineData(0x25, "Left")]
    [InlineData(0x1B, "Escape")]
    public void NonCharacterKeys_RoundTrip(int virtualKey, string name)
    {
        var hk = new Hotkey(HotkeyModifiers.Shift, virtualKey);

        Assert.Equal($"Shift+{name}", hk.ToStorageString());
        Assert.True(Hotkey.TryParse(hk.ToStorageString(), out var parsed));
        Assert.Equal(hk, parsed);
    }

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
