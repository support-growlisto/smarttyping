namespace SmartTyping.Application.Language;

/// <summary>
/// An ordinary word the user has just finished typing — the raw material of the personal dictionary.
/// </summary>
/// <param name="Word">
/// The text as it stands on screen: Thai when the Thai layout was active, latin otherwise. Not the keys
/// pressed — the point is to learn the word the user meant, in the language they meant it.
/// </param>
/// <param name="IsThai">The language of the layout it was typed on.</param>
public sealed record WordObserved(string Word, bool IsThai);
