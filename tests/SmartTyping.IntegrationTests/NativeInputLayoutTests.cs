using System.Runtime.InteropServices;
using SmartTyping.Infrastructure.Input;
using Xunit;

namespace SmartTyping.IntegrationTests;

public sealed class NativeInputLayoutTests
{
    // SendInput fails silently unless cbSize == the OS INPUT size. The union must be sized to its
    // largest member (MOUSEINPUT): 40 bytes on x64, 28 on x86. Guard against a regression that
    // shrinks the struct (which breaks ALL synthetic input: convert, expand, inject).
    [Fact]
    public void InputStructMatchesOsSize()
    {
        var expected = IntPtr.Size == 8 ? 40 : 28;
        Assert.Equal(expected, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    // Arrow and navigation keys sent without KEYEVENTF_EXTENDEDKEY arrive as their numeric-keypad
    // twins. A bare Left still moves the caret, so the mistake hides: only the combinations break —
    // Ctrl+Shift+Left selects nothing, which silently killed "convert the last word" when nothing was
    // selected. Measured on a real TextBox: without the flag SelectedText was ""; with it, "hello".
    [Theory]
    [InlineData(0x25)] // Left
    [InlineData(0x26)] // Up
    [InlineData(0x27)] // Right
    [InlineData(0x28)] // Down
    [InlineData(0x24)] // Home
    [InlineData(0x23)] // End
    [InlineData(0x2E)] // Delete
    public void NavigationKeysAreExtended(int vk) => Assert.True(NativeMethods.IsExtendedKey(vk));

    [Theory]
    [InlineData(0x41)] // A
    [InlineData(0x08)] // Backspace
    [InlineData(0x0D)] // Enter
    [InlineData(0x10)] // Shift
    [InlineData(0x11)] // Ctrl
    [InlineData(0x43)] // C
    public void OrdinaryKeysAreNotExtended(int vk) => Assert.False(NativeMethods.IsExtendedKey(vk));
}
