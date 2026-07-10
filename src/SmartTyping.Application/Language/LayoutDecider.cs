using SmartTyping.Application.Abstractions;
using SmartTyping.Domain.Enums;

namespace SmartTyping.Application.Language;

/// <summary>
/// Decides whether the word the user is typing was meant for the other keyboard layout, by looking it
/// up in both dictionaries rather than guessing from character shapes.
///
/// <para>The rule is symmetric, and each half carries its own veto:</para>
/// <list type="bullet">
/// <item><b>Latin layout</b> — convert only when the Thai the keys would produce <i>is</i> a Thai word
/// <b>and</b> what they typed is <i>not</i> an English word. So <c>soy'lnv</c> → <c>หนังสือ</c>, while
/// <c>world</c> and <c>don't</c> are left alone.</item>
/// <item><b>Thai layout</b> — convert back only when what they typed <i>is</i> an English word
/// <b>and</b> the Thai on screen is <i>not</i> a Thai word. So <c>hello</c> is restored, while
/// <c>ทดสอบ</c> (a real Thai word) is never touched.</item>
/// </list>
///
/// <para>Pure and deterministic: no I/O, no Windows calls.</para>
/// </summary>
public sealed class LayoutDecider
{
    /// <summary>Shorter runs are too ambiguous to act on automatically.</summary>
    public const int MinimumWordLength = 3;

    private readonly ILexicon _lexicon;
    private readonly IKeyboardLayoutConverter _converter;

    public LayoutDecider(ILexicon lexicon, IKeyboardLayoutConverter converter)
    {
        _lexicon = lexicon;
        _converter = converter;
    }

    /// <summary>
    /// Returns the correction to apply, or null when the text should be left alone.
    /// </summary>
    /// <param name="typed">The latin characters the physical keys represent (what the hook buffered).</param>
    /// <param name="thaiLayoutActive">Whether the foreground window's layout is Thai.</param>
    /// <param name="boundary">The delimiter that closed the word, or empty when mid-word.</param>
    public LayoutCorrection? Decide(string typed, bool thaiLayoutActive, string boundary)
    {
        if (!_lexicon.IsReady || string.IsNullOrEmpty(typed) || typed.Length < MinimumWordLength)
        {
            return null;
        }

        // What these keys produce (or already produced) on the Thai layout.
        var thai = _converter.Convert(typed, ConversionDirection.EnglishToThai);

        // A typo shouldn't stop us recognising the language, but a near-miss is only trustworthy once
        // the word is finished. Mid-word (no delimiter) the text is a prefix of something longer, and a
        // fuzzy hit there would fire against a word the user is still typing.
        var budget = boundary.Length > 0 ? KeyboardCost.BudgetFor(typed.Length) : -1;

        if (thaiLayoutActive)
        {
            // The Thai layout rejects impossible sequences, so only part of `thai` reached the screen.
            // Original must be exactly what is there, or we'd backspace over earlier text.
            var onScreen = ThaiInput.Filter(thai);
            if (onScreen.Length == 0)
            {
                return null;
            }

            var meantEnglish = _lexicon.IsEnglishWord(typed) || _lexicon.IsNearEnglishWord(typed, budget);

            // The veto stays exact: real Thai must never be rewritten because it happens to sit one
            // key away from an English word.
            return meantEnglish && !_lexicon.IsThaiWord(onScreen)
                ? new LayoutCorrection(onScreen, typed, boundary, ToThai: false)
                : null;
        }

        // A latin layout inserts every key as typed, so the screen shows `typed` verbatim.
        var meantThai = _lexicon.IsThaiWord(thai) || _lexicon.IsNearThaiWord(typed, budget);

        // Note the suggestion is the literal transliteration of what they typed — not the dictionary
        // word we matched. We fix the layout, never the spelling: a typo stays a typo, in Thai.
        return meantThai && !_lexicon.IsEnglishWord(typed)
            ? new LayoutCorrection(typed, thai, boundary, ToThai: true)
            : null;
    }
}
