using Dapper;
using SmartTyping.Application.Abstractions;
using SmartTyping.Infrastructure.Time;

namespace SmartTyping.Infrastructure.Persistence.Repositories;

/// <summary>Dapper/SQLite implementation of <see cref="IPersonalWordRepository"/>.</summary>
public sealed class PersonalWordRepository : IPersonalWordRepository
{
    private readonly ISqlConnectionFactory _factory;

    public PersonalWordRepository(ISqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<PersonalWord>> GetAllAsync()
    {
        using var db = _factory.CreateOpenConnection();
        var rows = await db.QueryAsync<(string Word, long IsThai, long Count)>(
            "SELECT Word, IsThai, Count FROM personal_words ORDER BY Count DESC, Word;");

        return rows.Select(r => new PersonalWord(r.Word, r.IsThai != 0, (int)r.Count)).ToList();
    }

    public async Task<int> RecordAsync(string word, bool isThai, DateTime seenUtc)
    {
        using var db = _factory.CreateOpenConnection();
        var stamp = SqliteTime.ToStorage(seenUtc);

        // One statement, so two sightings racing each other cannot both insert or lose a count.
        return await db.ExecuteScalarAsync<int>(
            """
            INSERT INTO personal_words (Word, IsThai, Count, FirstSeenUtc, LastSeenUtc)
            VALUES (@Word, @IsThai, 1, @Stamp, @Stamp)
            ON CONFLICT(Word, IsThai) DO UPDATE SET
                Count = Count + 1,
                LastSeenUtc = @Stamp
            RETURNING Count;
            """,
            new { Word = word, IsThai = isThai ? 1 : 0, Stamp = stamp });
    }

    public async Task<int> PruneCandidatesAsync(int threshold, DateTime cutoffUtc)
    {
        using var db = _factory.CreateOpenConnection();
        return await db.ExecuteAsync(
            "DELETE FROM personal_words WHERE Count < @Threshold AND LastSeenUtc < @Cutoff;",
            new { Threshold = threshold, Cutoff = SqliteTime.ToStorage(cutoffUtc) });
    }

    public async Task RemoveAsync(string word, bool isThai)
    {
        using var db = _factory.CreateOpenConnection();
        await db.ExecuteAsync(
            "DELETE FROM personal_words WHERE Word = @Word AND IsThai = @IsThai;",
            new { Word = word, IsThai = isThai ? 1 : 0 });
    }

    public async Task ClearAsync()
    {
        using var db = _factory.CreateOpenConnection();
        await db.ExecuteAsync("DELETE FROM personal_words;");
    }
}
