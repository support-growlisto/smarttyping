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

    private static bool IsAttachedVowel(char c) =>
        c is 'ั' or 'ิ' or 'ี' or 'ึ' or 'ื' or 'ุ' or 'ู' or '็' or '์' or 'ํ';

    private static bool IsTone(char c) => c is '่' or '้' or '๊' or '๋';

    // Marks that follow a consonant (optionally with a vowel/tone on it): น้ำ, กะ, ไปๆ.
    private static bool IsTrailingMark(char c) => c is 'ำ' or 'ะ' or 'ๅ' or 'ๆ' or 'ฯ';

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
            else if (IsTone(c))
            {
                accepted = IsConsonant(previous) || IsAttachedVowel(previous);
            }
            else if (IsTrailingMark(c))
            {
                accepted = IsConsonant(previous) || IsAttachedVowel(previous) || IsTone(previous);
            }
            else
            {
                // Consonants, leading vowels, digits, punctuation, latin — always inserted.
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
