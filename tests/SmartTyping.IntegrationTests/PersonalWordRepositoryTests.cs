using Microsoft.Extensions.Logging.Abstractions;
using SmartTyping.Application.Language;
using SmartTyping.Infrastructure.Persistence;
using SmartTyping.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SmartTyping.IntegrationTests;

public sealed class PersonalWordRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnectionFactory _factory;
    private readonly PersonalWordRepository _repo;

    public PersonalWordRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"smarttyping-pw-{Guid.NewGuid():N}.db");
        _factory = new SqliteConnectionFactory(_dbPath);
        new DatabaseInitializer(_factory, NullLogger<DatabaseInitializer>.Instance).Initialize();
        _repo = new PersonalWordRepository(_factory);
    }

    [Fact]
    public async Task RecordAsync_CountsUp_AndReportsTheRunningTotal()
    {
        var now = DateTime.UtcNow;

        Assert.Equal(1, await _repo.RecordAsync("คับ", isThai: true, now));
        Assert.Equal(2, await _repo.RecordAsync("คับ", isThai: true, now));
        Assert.Equal(3, await _repo.RecordAsync("คับ", isThai: true, now));

        var all = await _repo.GetAllAsync();
        Assert.Equal(new[] { new Application.Abstractions.PersonalWord("คับ", true, 3) }, all);
    }

    // The same text in the two languages is two different words: ambiguous latin can be both.
    [Fact]
    public async Task TheTallyIsPerLanguage()
    {
        await _repo.RecordAsync("abc", isThai: false, DateTime.UtcNow);
        await _repo.RecordAsync("abc", isThai: false, DateTime.UtcNow);
        Assert.Equal(1, await _repo.RecordAsync("abc", isThai: true, DateTime.UtcNow));
    }

    // A word typed once in passing must not sit on disk for ever — but one the user actually adopted is
    // vocabulary now, and staying away from the keyboard for a month must not cost them it.
    [Fact]
    public async Task PruneCandidatesAsync_DropsTheStaleTallies_AndKeepsTheAdoptedWords()
    {
        var longAgo = DateTime.UtcNow.AddDays(-PersonalDictionary.CandidateLifetimeDays - 1);

        await _repo.RecordAsync("passing", isThai: false, longAgo);       // count 1, stale
        for (var i = 0; i < PersonalDictionary.Threshold; i++)
        {
            await _repo.RecordAsync("adopted", isThai: false, longAgo);   // count 3, stale, but adopted
        }

        await _repo.RecordAsync("recent", isThai: false, DateTime.UtcNow); // count 1, fresh

        var dropped = await _repo.PruneCandidatesAsync(
            PersonalDictionary.Threshold, DateTime.UtcNow.AddDays(-PersonalDictionary.CandidateLifetimeDays));

        Assert.Equal(1, dropped);
        var remaining = (await _repo.GetAllAsync()).Select(w => w.Word).ToHashSet();
        Assert.Equal(new[] { "adopted", "recent" }.ToHashSet(), remaining);
    }

    [Fact]
    public async Task RemoveAsync_And_ClearAsync()
    {
        await _repo.RecordAsync("one", isThai: false, DateTime.UtcNow);
        await _repo.RecordAsync("สอง", isThai: true, DateTime.UtcNow);

        await _repo.RemoveAsync("one", isThai: false);
        Assert.Single(await _repo.GetAllAsync());

        await _repo.ClearAsync();
        Assert.Empty(await _repo.GetAllAsync());
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch { /* best effort */ }
    }
}
