namespace SmartTyping.Application.Language;

/// <summary>
/// Word lists for the two languages, used to decide which one the user meant to type.
/// Implemented in Infrastructure over bundled, public-domain dictionaries.
/// </summary>
public interface ILexicon
{
    /// <summary>False until the dictionaries finish loading; callers must treat that as "don't act".</summary>
    bool IsReady { get; }

    /// <summary>True when <paramref name="word"/> is a Thai word.</summary>
    bool IsThaiWord(string word);

    /// <summary>True when <paramref name="word"/> is an English word (case-insensitive).</summary>
    bool IsEnglishWord(string word);

    /// <summary>
    /// True when the keys in <paramref name="latinTyped"/> spell a Thai word, allowing for typos worth
    /// no more than <paramref name="budget"/> under <see cref="KeyboardCost"/>. Compared in latin space
    /// because the slip happened on a physical key.
    /// </summary>
    bool IsNearThaiWord(string latinTyped, int budget);

    /// <summary>As <see cref="IsNearThaiWord"/>, for the English vocabulary.</summary>
    bool IsNearEnglishWord(string typed, int budget);

    /// <summary>
    /// Adds a word the user taught us — the text they restored by undoing a correction. It joins the
    /// given language's vocabulary, so the decider's veto stops correcting it. Persisted.
    /// </summary>
    void Learn(string word, bool isThai);
}
