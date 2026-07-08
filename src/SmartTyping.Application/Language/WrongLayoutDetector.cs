namespace SmartTyping.Application.Language;

/// <summary>
/// Conservative, high-precision heuristic for the as-you-type suggestion: decides whether a word
/// typed on a Latin layout looks like it was actually meant to be Thai (i.e. the user forgot to
/// switch layout). Purely used to *suggest* conversion — never to auto-replace — so it errs toward
/// false negatives (miss some) rather than false positives (annoy).
/// </summary>
public static class WrongLayoutDetector
{
    // Keys that are Thai consonants on Kedmanee but punctuation on QWERTY. Their presence *inside* a
    // run of letters is a strong signal of wrong-layout Thai (real English words don't contain ; ' [ ]).
    private static readonly char[] ThaiConsonantPunctuation = { ';', '\'', '[', ']', '\\' };

    /// <summary>
    /// Returns true if <paramref name="latinWord"/> (as physically typed on a QWERTY layout) looks
    /// like wrong-layout Thai.
    /// </summary>
    public static bool LooksLikeWrongLayoutThai(string latinWord)
    {
        if (string.IsNullOrEmpty(latinWord) || latinWord.Length < 2)
        {
            return false;
        }

        var hasLetter = false;
        var hasThaiConsonantPunctuation = false;

        foreach (var c in latinWord)
        {
            if (c is >= 'a' and <= 'z' or >= 'A' and <= 'Z')
            {
                hasLetter = true;
            }
            else if (Array.IndexOf(ThaiConsonantPunctuation, c) >= 0)
            {
                hasThaiConsonantPunctuation = true;
            }
        }

        return hasLetter && hasThaiConsonantPunctuation;
    }
}
