using SmartTyping.Application.Language;
using Xunit;

namespace SmartTyping.Tests;

public sealed class KeyboardCostTests
{
    [Fact]
    public void SameCharacterIsFree() => Assert.Equal(0, KeyboardCost.Substitution('f', 'f'));

    [Theory]
    [InlineData('d', 'f')]  // home row neighbours
    [InlineData('f', 'd')]
    [InlineData('o', 'i')]
    [InlineData(';', 'l')]
    public void NeighbouringKeysAreCheap(char typed, char intended) =>
        Assert.Equal(KeyboardCost.AdjacentKey, KeyboardCost.Substitution(typed, intended));

    [Theory]
    [InlineData('a', 'A')]
    [InlineData(';', ':')]
    [InlineData('1', '!')]
    public void SameKeyWrongShift(char typed, char intended) =>
        Assert.Equal(KeyboardCost.WrongShift, KeyboardCost.Substitution(typed, intended));

    [Theory]
    [InlineData('d', 'e')]  // row above, same column-ish
    [InlineData('f', 'c')]  // row below
    public void KeysOnTheNeighbouringRowCostMore(char typed, char intended) =>
        Assert.Equal(KeyboardCost.NeighbouringRow, KeyboardCost.Substitution(typed, intended));

    [Theory]
    [InlineData('q', 'p')]  // opposite ends of a row
    [InlineData('a', 'm')]
    [InlineData('f', 'ก')]  // not on the latin keyboard at all
    public void DistantOrUnknownKeysAreUnrelated(char typed, char intended) =>
        Assert.Equal(KeyboardCost.Unrelated, KeyboardCost.Substitution(typed, intended));

    [Fact]
    public void AdjacentKeyPlusWrongShiftAddsUp() =>
        Assert.Equal(KeyboardCost.AdjacentKey + KeyboardCost.WrongShift, KeyboardCost.Substitution('D', 'f'));

    // ---- Budget ----

    [Fact]
    public void BudgetGrowsWithLengthButIsCapped()
    {
        Assert.Equal(24, KeyboardCost.BudgetFor(2));
        Assert.Equal(72, KeyboardCost.BudgetFor(6));
        Assert.Equal(75, KeyboardCost.BudgetFor(10));  // capped
        Assert.Equal(75, KeyboardCost.BudgetFor(40));
    }

    [Fact]
    public void BudgetNeverPaysForAnUnrelatedSubstitution()
    {
        // The cap exists precisely so a long word can't buy a genuinely different character.
        for (var length = 1; length <= 50; length++)
        {
            Assert.True(KeyboardCost.BudgetFor(length) < KeyboardCost.Unrelated);
        }
    }

    // ---- Distance ----

    [Fact]
    public void OneNeighbouringSlipIsWithinBudget()
    {
        // "l;ylfu" is สวัสดี; typing 'd' for 'f' gives "l;yldu" — the key next door.
        const string intended = "l;ylfu";
        const string typed = "l;yldu";

        Assert.Equal(KeyboardCost.AdjacentKey, KeyboardCost.Distance(typed, intended, KeyboardCost.BudgetFor(6)));
    }

    [Fact]
    public void TranspositionIsRejected()
    {
        // "wrold" for "world" is two unrelated substitutions (200), far past any budget.
        Assert.Equal(-1, KeyboardCost.Distance("wrold", "world", KeyboardCost.BudgetFor(5)));
    }

    [Fact]
    public void DifferentLengthsNeverMatch() =>
        Assert.Equal(-1, KeyboardCost.Distance("abc", "abcd", 1000));

    [Fact]
    public void ExceedingTheBudgetGivesUp() =>
        Assert.Equal(-1, KeyboardCost.Distance("dd", "ff", KeyboardCost.AdjacentKey));

    [Fact]
    public void IdenticalWordsCostNothing() =>
        Assert.Equal(0, KeyboardCost.Distance("hello", "hello", 0));
}
