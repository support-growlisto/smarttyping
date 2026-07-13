using SmartTyping.Application.Abstractions;

namespace SmartTyping.IntegrationTests;

/// <summary>In-memory stand-in for the personal-word table, shared by the lexicon tests.</summary>
internal sealed class InMemoryPersonalWords : IPersonalWordRepository
{
    private readonly Dictionary<(string Word, bool IsThai), int> _counts = new();

    public Task<IReadOnlyList<PersonalWord>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<PersonalWord>>(
            _counts.Select(kv => new PersonalWord(kv.Key.Word, kv.Key.IsThai, kv.Value)).ToList());

    public Task<int> RecordAsync(string word, bool isThai, DateTime seenUtc)
    {
        var key = (word, isThai);
        _counts[key] = _counts.TryGetValue(key, out var n) ? n + 1 : 1;
        return Task.FromResult(_counts[key]);
    }

    public Task<int> PruneCandidatesAsync(int threshold, DateTime cutoffUtc) => Task.FromResult(0);

    public Task RemoveAsync(string word, bool isThai)
    {
        _counts.Remove((word, isThai));
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        _counts.Clear();
        return Task.CompletedTask;
    }
}
