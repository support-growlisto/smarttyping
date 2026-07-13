namespace SmartTyping.Application.Abstractions;

/// <summary>A word the user taught us by undoing a correction of it.</summary>
/// <param name="Word">The text that was on screen before we changed it.</param>
/// <param name="IsThai">Which language's dictionary it belongs to.</param>
public sealed record LearnedWord(string Word, bool IsThai);

/// <summary>
/// Persistence for words the user has taught the app. Undoing a correction means "this text was
/// right" — remembering it makes the corrector's own veto skip that word from then on.
/// </summary>
public interface ILearnedWordRepository
{
    Task<IReadOnlyList<LearnedWord>> GetAllAsync();

    /// <summary>Idempotent: re-learning a known word is a no-op.</summary>
    Task AddAsync(LearnedWord word, DateTime learnedUtc);

    /// <summary>Forgets one word. Idempotent.</summary>
    Task RemoveAsync(LearnedWord word);

    /// <summary>Forgets every learned word.</summary>
    Task ClearAsync();
}
