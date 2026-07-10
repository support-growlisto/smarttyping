namespace SmartTyping.Application.Language;

/// <summary>
/// How "expensive" it is to have typed one character when another was meant, measured by where the
/// keys sit on a US-QWERTY keyboard. A neighbouring key is a plausible slip; a key on the far side of
/// the board is not.
///
/// <para>Used to decide whether a word with a typo was still meant for the other layout. Only
/// substitutions are modelled — never insertions or deletions — so the compared strings always have
/// the same length and a transposition (<c>wrold</c> for <c>world</c>) costs two full substitutions
/// and is therefore rejected.</para>
/// </summary>
public static class KeyboardCost
{
    /// <summary>A slip onto the key next door.</summary>
    public const int AdjacentKey = 15;

    /// <summary>The right key, the wrong shift state.</summary>
    public const int WrongShift = 40;

    /// <summary>A key on the row above or below.</summary>
    public const int NeighbouringRow = 70;

    /// <summary>Anything else: not a plausible typo.</summary>
    public const int Unrelated = 100;

    /// <summary>Budget per character of the word.</summary>
    private const int QuotaPerCharacter = 12;

    /// <summary>
    /// Hard ceiling on the budget. Without it a long word would accumulate enough allowance to pay for
    /// an <see cref="Unrelated"/> substitution — i.e. a genuinely different word.
    /// </summary>
    private const int MaximumBudget = 75;

    private static readonly string[] Unshifted =
    {
        "`1234567890-=",
        "qwertyuiop[]\\",
        "asdfghjkl;'",
        "zxcvbnm,./"
    };

    private static readonly string[] Shifted =
    {
        "~!@#$%^&*()_+",
        "QWERTYUIOP{}|",
        "ASDFGHJKL:\"",
        "ZXCVBNM<>?"
    };

    private readonly record struct KeyPosition(int Row, int Column, bool Shift);

    private static readonly Dictionary<char, KeyPosition> Keys = BuildKeyMap();

    /// <summary>The total substitution cost allowed for a word of <paramref name="length"/> characters.</summary>
    public static int BudgetFor(int length) => Math.Min(QuotaPerCharacter * length, MaximumBudget);

    /// <summary>Cost of having typed <paramref name="typed"/> where <paramref name="intended"/> was meant.</summary>
    public static int Substitution(char typed, char intended)
    {
        if (typed == intended)
        {
            return 0;
        }

        if (!Keys.TryGetValue(typed, out var a) || !Keys.TryGetValue(intended, out var b))
        {
            return Unrelated;
        }

        var shiftPenalty = a.Shift == b.Shift ? 0 : WrongShift;

        // Same physical key, so the only mistake was the shift state.
        if (a.Row == b.Row && a.Column == b.Column)
        {
            return shiftPenalty;
        }

        var rowGap = Math.Abs(a.Row - b.Row);
        var columnGap = Math.Abs(a.Column - b.Column);

        if (rowGap == 0 && columnGap == 1)
        {
            return AdjacentKey + shiftPenalty;
        }

        if (rowGap == 1 && columnGap <= 1)
        {
            return NeighbouringRow + shiftPenalty;
        }

        return Unrelated;
    }

    /// <summary>
    /// Total cost of typing <paramref name="typed"/> when <paramref name="intended"/> was meant, or
    /// <c>-1</c> when the words differ in length or the cost exceeds <paramref name="budget"/>.
    /// Gives up as soon as the budget is blown, which is what keeps a dictionary scan cheap.
    /// </summary>
    public static int Distance(string typed, string intended, int budget)
    {
        if (typed.Length != intended.Length)
        {
            return -1;
        }

        var total = 0;
        for (var i = 0; i < typed.Length; i++)
        {
            total += Substitution(typed[i], intended[i]);
            if (total > budget)
            {
                return -1;
            }
        }

        return total;
    }

    private static Dictionary<char, KeyPosition> BuildKeyMap()
    {
        var map = new Dictionary<char, KeyPosition>();
        for (var row = 0; row < Unshifted.Length; row++)
        {
            for (var column = 0; column < Unshifted[row].Length; column++)
            {
                map[Unshifted[row][column]] = new KeyPosition(row, column, Shift: false);
            }

            for (var column = 0; column < Shifted[row].Length; column++)
            {
                map[Shifted[row][column]] = new KeyPosition(row, column, Shift: true);
            }
        }

        // Letters are typed unshifted or shifted on the same physical key.
        for (var c = 'A'; c <= 'Z'; c++)
        {
            var lower = char.ToLowerInvariant(c);
            if (map.TryGetValue(lower, out var position))
            {
                map[c] = position with { Shift = true };
            }
        }

        return map;
    }
}
