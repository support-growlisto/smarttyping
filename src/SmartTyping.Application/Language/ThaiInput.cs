namespace SmartTyping.Application.Language;

/// <summary>
/// Models the input validation the Windows Thai (Kedmanee) layout applies: a keystroke that would
/// produce a structurally impossible sequence — a tone mark or attached vowel with nothing to attach
/// to — is silently rejected and never reaches the document.
///
/// <para>This matters for the automatic correction. When the user types <c>hello</c> with the Thai
/// layout active, the keys map to <c>้ำสสน</c> but only <c>สสน</c> is actually inserted. Deleting five
/// characters to fix it would destroy two characters of whatever came before.</para>
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
            bool accepted;

            if (IsAttachedVowel(c))
            {
                accepted = IsConsonant(previous);
            }
            else if (IsStackedMark(c))
            {
                accepted = IsConsonant(previous) || IsAttachedVowel(previous);
            }
            else if (IsSaraAm(c))
            {
                accepted = IsConsonant(previous) || IsAttachedVowel(previous) || IsStackedMark(previous);
            }
            else
            {
                // Consonants, leading and spacing vowels, the free marks, digits, punctuation, latin.
                accepted = true;
            }

            if (accepted)
            {
                kept.Append(c);
                previous = c;
            }
        }

        return kept.ToString();
    }
}
