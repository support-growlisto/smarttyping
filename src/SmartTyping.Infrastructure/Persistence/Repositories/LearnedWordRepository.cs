using Dapper;
using SmartTyping.Application.Abstractions;
using SmartTyping.Infrastructure.Time;

namespace SmartTyping.Infrastructure.Persistence.Repositories;

/// <summary>Dapper/SQLite implementation of <see cref="ILearnedWordRepository"/>.</summary>
public sealed class LearnedWordRepository : ILearnedWordRepository
{
    private readonly ISqlConnectionFactory _factory;

    public LearnedWordRepository(ISqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<LearnedWord>> GetAllAsync()
    {
        using var db = _factory.CreateOpenConnection();
        var rows = await db.QueryAsync<(string Word, long IsThai)>(
            "SELECT Word, IsThai FROM learned_words;");

        return rows.Select(r => new LearnedWord(r.Word, r.IsThai != 0)).ToList();
    }

    public async Task AddAsync(LearnedWord word, DateTime learnedUtc)
    {
        using var db = _factory.CreateOpenConnection();
        await db.ExecuteAsync(
            """
            INSERT INTO learned_words (Word, IsThai, LearnedUtc) VALUES (@Word, @IsThai, @LearnedUtc)
            ON CONFLICT(Word, IsThai) DO NOTHING;
            """,
            new
            {
                word.Word,
                IsThai = word.IsThai ? 1 : 0,
                LearnedUtc = SqliteTime.ToStorage(learnedUtc)
            });
    }

    public async Task RemoveAsync(LearnedWord word)
    {
        using var db = _factory.CreateOpenConnection();
        await db.ExecuteAsync(
            "DELETE FROM learned_words WHERE Word = @Word AND IsThai = @IsThai;",
            new { word.Word, IsThai = word.IsThai ? 1 : 0 });
    }

    public async Task ClearAsync()
    {
        using var db = _factory.CreateOpenConnection();
        await db.ExecuteAsync("DELETE FROM learned_words;");
    }
}
