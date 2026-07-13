using SmartTyping.Infrastructure.Input;
using Xunit;

namespace SmartTyping.IntegrationTests;

/// <summary>
/// Where a multi-line snippet leaves the caret. The arithmetic looks obvious and was wrong for months:
/// a line break is stored as CRLF — two characters — so the code walked the caret back over it twice.
/// But Left is a caret movement, and the caret crosses a line break in a single press. Measured against a
/// real text box: the marker landed at the end of the previous line, one place too far back per break.
/// </summary>
public sealed class CaretPlacementTests
{
    // "Dear team,\n{cursor}\nRegards" — the marker sits at index 11, and everything after it is
    // "\nRegards": one line break plus seven letters, so eight Left taps. Nine would land the caret at
    // the end of "Dear team," instead of on the empty line.
    [Fact]
    public void ALineBreakCostsOneTap_NotTwo()
    {
        const string text = "Dear team,\n\nRegards";
        Assert.Equal(8, KeyboardSender.LeftTapsFor(text, cursorOffset: 11));
    }

    [Fact]
    public void CarriageReturnsCostNothing_BecauseTheyAreNeverTyped()
    {
        // A snippet stored with CRLF must behave exactly like one stored with LF: only the '\n' is sent
        // (as Enter), so only it may be counted.
        Assert.Equal(
            KeyboardSender.LeftTapsFor("a\nb", cursorOffset: 0),
            KeyboardSender.LeftTapsFor("a\r\nb", cursorOffset: 0));
    }

    [Fact]
    public void EveryOtherCharacterCostsOneTap()
    {
        Assert.Equal(5, KeyboardSender.LeftTapsFor("hello", cursorOffset: 0));
        Assert.Equal(2, KeyboardSender.LeftTapsFor("hello", cursorOffset: 3));
    }

    [Fact]
    public void NoMarker_MeansNoTaps()
    {
        Assert.Equal(0, KeyboardSender.LeftTapsFor("hello", cursorOffset: null));
        Assert.Equal(0, KeyboardSender.LeftTapsFor("hello", cursorOffset: 5));   // at the very end
        Assert.Equal(0, KeyboardSender.LeftTapsFor("hello", cursorOffset: -1));  // nonsense
    }
}
