namespace SmartTyping.Application.Abstractions;

/// <summary>A word the user types that no bundled dictionary knows, and how often they have typed it.</summary>
/// <param name="Word">The text as it appears on screen.</param>
/// <param name="IsThai">Which language's vocabulary it would join.</param>
/// <param name="Count">How many times it has been typed. At <c>PersonalDictionary.Threshold</c> it is in.</param>
public sealed record PersonalWord(string Word, bool IsThai, int Count);

/// <summary>
/// Persistence for the personal dictionary. Implemented in Infrastructure. Never throws.
/// </summary>
public interface IPersonalWordRepository
{
    /// <summary>Every word and its tally, including candidates that have not reached the threshold.</summary>
    Task<IReadOnlyList<PersonalWord>> GetAllAsync();

    /// <summary>
    /// Counts one more sighting of <paramref name="word"/>, inserting it if this is the first. Returns
    /// the new tally, so the caller knows when the threshold has just been crossed.
    /// </summary>
    Task<int> RecordAsync(string word, bool isThai, DateTime seenUtc);

    /// <summary>
    /// Drops candidates below <paramref name="threshold"/> that have not been typed since
    /// <paramref name="cutoffUtc"/>. Words that reached the threshold are kept — they are vocabulary now,
    /// not a tally. Returns how many were dropped.
    /// </summary>
    Task<int> PruneCandidatesAsync(int threshold, DateTime cutoffUtc);

    Task RemoveAsync(string word, bool isThai);

    Task ClearAsync();
}
