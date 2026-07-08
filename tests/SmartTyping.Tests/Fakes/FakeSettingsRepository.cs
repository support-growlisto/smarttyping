using SmartTyping.Application.Abstractions;

namespace SmartTyping.Tests.Fakes;

/// <summary>In-memory settings repository for tests.</summary>
public sealed class FakeSettingsRepository : ISettingsRepository
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public FakeSettingsRepository(params (string Key, string Value)[] seed)
    {
        foreach (var (key, value) in seed)
        {
            _values[key] = value;
        }
    }

    public Task<IReadOnlyDictionary<string, string>> GetAllAsync() =>
        Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(_values));

    public Task<string?> GetAsync(string key) =>
        Task.FromResult(_values.TryGetValue(key, out var value) ? value : null);

    public Task SetAsync(string key, string value)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }
}
