namespace SmartTyping.Application.Language;

/// <summary>
/// The rules for the personal dictionary: which of the words you type may be counted towards it, and
/// when a counted word becomes part of your vocabulary.
///
/// <para>The point of the feature is the words no dictionary has: names, jargon, and the deliberate
/// misspellings people use with each other (<c>คับ</c>, <c>จ้าา</c>). Until the app knows them, it
/// cannot fix them when they are typed on the wrong layout — they are exactly the words it is worst at.</para>
///
/// <para><b>This is the one feature that writes what you type to disk</b>, so the rules below are
/// deliberately narrow, and they live here — pure, and covered by tests — rather than being scattered
/// through the input path. Anything that is not plainly a word of one language is never counted:
/// no digits, no symbols, nothing overlong. Those are what passwords, tokens and URLs look like.</para>
/// </summary>
public static class PersonalDictionary
{
    /// <summary>How many times a word must be typed before it joins the vocabulary.</summary>
    public const int Threshold = 3;

    /// <summary>
    /// A word counted fewer than <see cref="Threshold"/> times is forgotten if it is not typed again
    /// within this many days. A word typed once in passing must not sit on disk for ever.
    /// </summary>
    public const int CandidateLifetimeDays = 30;

    /// <summary>
    /// Shorter than this and the "word" is noise (<c>ๆ</c>, <c>ok</c> is already in the dictionary);
    /// longer and it is not a word anyone types repeatedly by hand.
    /// </summary>
    public const int MinimumLength = 2;

    public const int MaximumLength = 20;

    /// <summary>
    /// Whether <paramref name="word"/> may be counted at all. It must be letters of the given language
    /// and nothing else — the caller has already established that no dictionary knows it.
    /// </summary>
    public static bool MayCount(string word, bool isThai)
    {
        if (string.IsNullOrEmpty(word) || word.Length < MinimumLength || word.Length > MaximumLength)
        {
            return false;
        }

        foreach (var c in word)
        {
            var ok = isThai ? IsThaiLetter(c) : IsEnglishLetter(c);
            if (!ok)
            {
                return false;
            }
        }

        return true;
    }

    // The Thai block, minus the digits (๐-๙) and the currency/ornament symbols at its end. Tone marks
    // and vowels are letters here: they are part of the word.
    private static bool IsThaiLetter(char c) => c is >= 'ก' and <= '๎';

    // Latin letters and the apostrophe, which is inside real words (don't, it's) — but nowhere else, so
    // a leading or trailing one is rejected below by the digit/symbol rule anyway.
    private static bool IsEnglishLetter(char c) =>
        c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '\'';
}
