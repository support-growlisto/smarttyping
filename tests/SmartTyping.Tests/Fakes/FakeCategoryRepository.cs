using SmartTyping.Application.Abstractions;
using SmartTyping.Domain.Entities;

namespace SmartTyping.Tests.Fakes;

/// <summary>In-memory category repository for tests.</summary>
public sealed class FakeCategoryRepository : ICategoryRepository
{
    private readonly List<Category> _categories = new();
    private int _nextId = 1;

    public IReadOnlyList<Category> Items => _categories;

    public Task<IReadOnlyList<Category>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<Category>>(_categories.ToList());

    public Task<int> AddAsync(Category category)
    {
        category.Id = _nextId++;
        _categories.Add(category);
        return Task.FromResult(category.Id);
    }

    public Task UpdateAsync(Category category)
    {
        var index = _categories.FindIndex(c => c.Id == category.Id);
        if (index >= 0)
        {
            _categories[index] = category;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        _categories.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }
}
