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
}
