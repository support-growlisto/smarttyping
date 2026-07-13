using Microsoft.Extensions.Logging.Abstractions;
using SmartTyping.Application.Abstractions;
using SmartTyping.Infrastructure.Persistence;
using SmartTyping.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SmartTyping.IntegrationTests;

/// <summary>
/// Learning is what an undo leaves behind, and until now it could not be taken back: the word stopped
/// being corrected in every application, for ever, with nothing on screen to say why. Forgetting has to
/// reach the database — a word dropped only from memory would come back on the next launch.
/// </summary>
public sealed class LearnedWordRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnectionFactory _factory;
    private readonly LearnedWordRepository _repo;

    public LearnedWordRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"smarttyping-lw-{Guid.NewGuid():N}.db");
        _factory = new SqliteConnectionFactory(_dbPath);
        new DatabaseInitializer(_factory, NullLogger<DatabaseInitializer>.Instance).Initialize();
        _repo = new LearnedWordRepository(_factory);
    }

    [Fact]
    public async Task RemoveAsync_ForgetsOneWord_AndLeavesTheRest()
    {
        await _repo.AddAsync(new LearnedWord("soy'lnv", IsThai: false), DateTime.UtcNow);
        await _repo.AddAsync(new LearnedWord("กขค", IsThai: true), DateTime.UtcNow);

        await _repo.RemoveAsync(new LearnedWord("soy'lnv", IsThai: false));

        var remaining = await _repo.GetAllAsync();
        Assert.Equal(new[] { new LearnedWord("กขค", IsThai: true) }, remaining);
    }

    // The same text can be learned in both languages; forgetting one must not take the other with it.
    [Fact]
    public async Task RemoveAsync_IsPerLanguage()
    {
        await _repo.AddAsync(new LearnedWord("abc", IsThai: false), DateTime.UtcNow);
        await _repo.AddAsync(new LearnedWord("abc", IsThai: true), DateTime.UtcNow);

        await _repo.RemoveAsync(new LearnedWord("abc", IsThai: false));

        var remaining = await _repo.GetAllAsync();
        Assert.Equal(new[] { new LearnedWord("abc", IsThai: true) }, remaining);
    }

    [Fact]
    public async Task RemoveAsync_IsIdempotent()
    {
        await _repo.RemoveAsync(new LearnedWord("never-learned", IsThai: false));
        Assert.Empty(await _repo.GetAllAsync());
    }

    [Fact]
    public async Task ClearAsync_ForgetsEverything()
    {
        await _repo.AddAsync(new LearnedWord("one", IsThai: false), DateTime.UtcNow);
        await _repo.AddAsync(new LearnedWord("สอง", IsThai: true), DateTime.UtcNow);

        await _repo.ClearAsync();

        Assert.Empty(await _repo.GetAllAsync());
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch { /* best effort */ }
    }
}
