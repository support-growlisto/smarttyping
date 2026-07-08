using SmartTyping.Application.Abstractions;
using SmartTyping.Domain.Entities;

namespace SmartTyping.Tests.Fakes;

/// <summary>In-memory snippet repository for tests, mirroring the SQLite semantics we rely on.</summary>
public sealed class FakeSnippetRepository : ISnippetRepository
{
    private readonly List<Snippet> _snippets = new();
    private int _nextId = 1;

    public int RegisterUsageCallCount { get; private set; }

    public IReadOnlyList<Snippet> Items => _snippets;

    public Snippet Seed(string trigger, string content, bool enabled = true)
    {
        var snippet = new Snippet
        {
            Id = _nextId++,
            Trigger = trigger,
            Content = content,
            IsEnabled = enabled,
            CreatedUtc = DateTime.UnixEpoch,
            UpdatedUtc = DateTime.UnixEpoch
        };
        _snippets.Add(snippet);
        return snippet;
    }

    public Task<IReadOnlyList<Snippet>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<Snippet>>(_snippets.ToList());

    public Task<Snippet?> GetByIdAsync(int id) =>
        Task.FromResult(_snippets.FirstOrDefault(s => s.Id == id));

    public Task<Snippet?> FindByTriggerAsync(string trigger) =>
        Task.FromResult(_snippets.FirstOrDefault(s =>
            string.Equals(s.Trigger, trigger, StringComparison.OrdinalIgnoreCase)));

    public Task<int> AddAsync(Snippet snippet)
    {
        snippet.Id = _nextId++;
        _snippets.Add(snippet);
        return Task.FromResult(snippet.Id);
    }

    public Task UpdateAsync(Snippet snippet)
    {
        var index = _snippets.FindIndex(s => s.Id == snippet.Id);
        if (index >= 0)
        {
            _snippets[index] = snippet;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        _snippets.RemoveAll(s => s.Id == id);
        return Task.CompletedTask;
    }

    public Task RegisterUsageAsync(int snippetId, DateTime usedUtc)
    {
        RegisterUsageCallCount++;
        var snippet = _snippets.FirstOrDefault(s => s.Id == snippetId);
        snippet?.RegisterUse(usedUtc);
        return Task.CompletedTask;
    }
}
