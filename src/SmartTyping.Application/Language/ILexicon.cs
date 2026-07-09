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
}
