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

    // The apostrophe is 'ง' on Kedmanee — one of the most common Thai consonants (หนังสือ, ของ, อย่าง),
    // so dropping it from the automatic path would miss most Thai words. It is kept, and English
    // contractions are excluded precisely instead — see CouldBeEnglishContraction.
    private static readonly char[] ThaiConsonantPunctuationStrict = { ';', '\'', '[', ']', '\\' };

    // What follows the apostrophe in an English contraction: don't, it's, we're, we'll, I've, I'm, he'd.
    private static readonly string[] ContractionSuffixes = { "t", "s", "re", "ll", "ve", "m", "d" };

    /// <summary>
    /// Returns true if <paramref name="latinWord"/> (as physically typed on a QWERTY layout) looks
    /// like wrong-layout Thai. When <paramref name="strict"/> is true the apostrophe is ignored, so
    /// English contractions are never flagged (used for automatic replacement).
    /// </summary>
    public static bool LooksLikeWrongLayoutThai(string latinWord, bool strict = false)
    {
        if (string.IsNullOrEmpty(latinWord) || latinWord.Length < 2)
        {
            return false;
        }

        var triggers = strict ? ThaiConsonantPunctuationStrict : ThaiConsonantPunctuation;
        var hasLetter = false;
        var hasThaiConsonantPunctuation = false;

        foreach (var c in latinWord)
        {
            if (c is >= 'a' and <= 'z' or >= 'A' and <= 'Z')
            {
                hasLetter = true;
            }
            else if (Array.IndexOf(triggers, c) >= 0)
            {
                hasThaiConsonantPunctuation = true;
            }
        }

        return hasLetter && hasThaiConsonantPunctuation;
    }

    /// <summary>
    /// True when <paramref name="latinWord"/> is (or is still growing into) an English contraction such
    /// as <c>don't</c>, <c>it's</c>, <c>we'll</c> — in which case the apostrophe is a real apostrophe,
    /// not the Thai consonant 'ง', and the word must not be auto-corrected.
    ///
    /// <para>Mid-word this also returns true while the suffix is merely a *prefix* of a contraction
    /// ending (<c>don'</c>, <c>we'l</c>), so we wait for another keystroke before deciding rather than
    /// corrupting a word the user is halfway through typing.</para>
    /// </summary>
    public static bool CouldBeEnglishContraction(string latinWord)
    {
        var apostrophe = latinWord.LastIndexOf('\'');
        if (apostrophe < 0)
        {
            return false;
        }

        // Everything before the apostrophe must look like an English word for this to be a contraction.
        for (var i = 0; i < apostrophe; i++)
        {
            var c = latinWord[i];
            if (c is not (>= 'a' and <= 'z' or >= 'A' and <= 'Z'))
            {
                return false;
            }
        }

        var suffix = latinWord[(apostrophe + 1)..];
        foreach (var contraction in ContractionSuffixes)
        {
            // Exact ("don't") or still ambiguous ("don'", "we'l").
            if (contraction.StartsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // Thai characters that can never begin a syllable: tone marks, dependent (attached) vowels, and
    // standalone marks. A word starting with one of these is not Thai the user meant to type.
    private const string NonInitial = "ัิีึืุู็่้๊๋์ํำะๅๆฯ";

    private static bool IsToneMark(char c) => c is '่' or '้' or '๊' or '๋';

    // Vowels that attach above/below a consonant. Thai stacks a vowel and a tone (ที่) or a tone and
    // sara-am (น้ำ), so only vowel-on-vowel and tone-on-tone are impossible.
    private static bool IsAttachedVowel(char c) => c is 'ั' or 'ิ' or 'ี' or 'ึ' or 'ื' or 'ุ' or 'ู' or '็' or '์' or 'ํ';

    // Anything that cannot begin a syllable.
    private static bool IsDependentMark(char c) => NonInitial.IndexOf(c) >= 0;

    private static bool IsThai(char c) => c is >= '฀' and <= '๿';

    /// <summary>
    /// Returns true when <paramref name="thaiText"/> — the Thai that appeared on screen because the
    /// Thai layout was active — is structurally impossible, i.e. the user actually meant to type
    /// English. Conservative: it only fires on sequences real Thai cannot produce (a word beginning
    /// with a tone mark or attached vowel, or two attached marks in a row), so correctly typed Thai is
    /// never touched. It therefore misses English words whose Thai rendering happens to look valid.
    /// </summary>
    public static bool LooksLikeWrongLayoutEnglish(string thaiText)
    {
        if (string.IsNullOrEmpty(thaiText) || thaiText.Length < 2)
        {
            return false;
        }

        var thaiCount = 0;
        foreach (var c in thaiText)
        {
            if (IsThai(c))
            {
                thaiCount++;
            }
        }

        // Require it to actually be Thai text; a mixed/latin string isn't our business here.
        if (thaiCount != thaiText.Length)
        {
            return false;
        }

        if (IsDependentMark(thaiText[0]))
        {
            return true;
        }

        for (var i = 1; i < thaiText.Length; i++)
        {
            var prev = thaiText[i - 1];
            var cur = thaiText[i];

            // Two tones, or two attached vowels, in a row cannot occur.
            if ((IsToneMark(cur) && IsToneMark(prev)) ||
                (IsAttachedVowel(cur) && IsAttachedVowel(prev)))
            {
                return true;
            }

            // A tone mark must sit on a consonant or its vowel — never on a leading vowel.
            if (IsToneMark(cur) && prev is 'เ' or 'แ' or 'โ' or 'ใ' or 'ไ')
            {
                return true;
            }
        }

        return false;
    }
}
