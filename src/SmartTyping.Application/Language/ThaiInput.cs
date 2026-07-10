namespace SmartTyping.Application.Language;

/// <summary>
/// Which Thai (Kedmanee) keystrokes produce a structurally possible sequence: a tone mark or an attached
/// vowel needs something to attach to.
///
/// <para>Whether the <i>application</i> enforces this is not up to Windows — it is up to the text
/// control. Measured with the same keys (<c>hello</c>, which maps to <c>้ำสสน</c>): a Win32 EDIT control
/// (WinForms, Notepad) drops the leading tone and sara-am and inserts only <c>สสน</c>, while a control
/// that renders text itself (WPF, and by extension Chrome and Electron) inserts all five characters.</para>
///
/// <para>So the hook cannot <i>predict</i> how many characters a correction must delete. Instead it
/// <i>enforces</i> this rule: a keystroke that <see cref="Accepts"/> rejects is swallowed before it
/// reaches the application. Every app then shows the same thing, and the count is exact.</para>
/// </summary>
public static class ThaiInput
{
    private static bool IsConsonant(char c) => c is >= 'ก' and <= 'ฮ';

    // Independent vowels that may stand before a consonant.
    private static bool IsLeadingVowel(char c) => c is 'เ' or 'แ' or 'โ' or 'ใ' or 'ไ';

    // Vowels written above or below a consonant. They attach to the consonant itself, so they need one
    // directly before them.
    private static bool IsAttachedVowel(char c) =>
        c is 'ั' or 'ิ' or 'ี' or 'ึ' or 'ื' or 'ุ' or 'ู' or '็';

    // Marks that sit on top of the stack: a tone, the thanthakhat that silences a letter, the nikhahit.
    // They may land on a bare consonant or on one that already carries a vowel — which is how สิทธิ์ and
    // พันธุ์ are typed (ธ, then ิ/ุ, then ์). Treating ์ as an attached vowel used to strip it from all
    // 212 such words in the dictionary.
    private static bool IsStackedMark(char c) => c is '่' or '้' or '๊' or '๋' or '์' or 'ํ';

    // ะ, ๅ, ๆ and ฯ occupy a column of their own and attach to nothing, so the layout accepts them
    // anywhere — even first — and they fall through to the "always inserted" branch below. Measured by
    // typing them into a text box: ฯลฯ, ฯพณฯ and ๆๆ survive verbatim, and "ะ้ำ" (what "the" produces on
    // the Thai layout) leaves ะ behind.

    // Sara am is a composed vowel (nikhahit + sara aa), so it does need something to sit on: น้ำ is
    // accepted, but ำ at the start of the text — or after a bare ะ — is not.
    private static bool IsSaraAm(char c) => c is 'ำ';

    /// <summary>
    /// Whether <paramref name="c"/> may follow <paramref name="previous"/> (use <c>'\0'</c> when nothing
    /// precedes it). This is the per-character rule behind <see cref="Filter"/>, exposed so the keyboard
    /// hook can apply it one keystroke at a time.
    /// </summary>
    public static bool Accepts(char previous, char c)
    {
        if (IsAttachedVowel(c))
        {
            return IsConsonant(previous);
        }

        if (IsStackedMark(c))
        {
            return IsConsonant(previous) || IsAttachedVowel(previous);
        }

        if (IsSaraAm(c))
        {
            return IsConsonant(previous) || IsAttachedVowel(previous) || IsStackedMark(previous);
        }

        // Consonants, leading and spacing vowels, the free marks, digits, punctuation, latin.
        return true;
    }

    /// <summary>
    /// Returns the subsequence of <paramref name="thai"/> that the Thai layout would actually insert.
    /// Non-Thai characters pass through unchanged.
    /// </summary>
    public static string Filter(string thai)
    {
        if (string.IsNullOrEmpty(thai))
        {
            return string.Empty;
        }

        var kept = new System.Text.StringBuilder(thai.Length);
        var previous = '\0';

        foreach (var c in thai)
        {
            if (Accepts(previous, c))
            {
                kept.Append(c);
                previous = c;
            }
        }

        return kept.ToString();
    }
}
