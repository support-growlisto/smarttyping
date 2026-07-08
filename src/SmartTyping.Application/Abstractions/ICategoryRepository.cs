using SmartTyping.Domain.Entities;

namespace SmartTyping.Application.Abstractions;

/// <summary>Persistence port for snippet categories.</summary>
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync();

    Task<int> AddAsync(Category category);

    Task UpdateAsync(Category category);

    Task DeleteAsync(int id);
}
