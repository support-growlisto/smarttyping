using SmartTyping.Application.Abstractions;

namespace SmartTyping.Tests.Fakes;

/// <summary>
/// Test double for <see cref="IPlaceholderPrompt"/>. Returns preset values (or null to simulate a
/// cancel) and records the labels it was asked for.
/// </summary>
public sealed class FakePlaceholderPrompt : IPlaceholderPrompt
{
    private readonly IReadOnlyDictionary<string, string>? _values;

    public FakePlaceholderPrompt(IReadOnlyDictionary<string, string>? values) => _values = values;

    public IReadOnlyList<string>? LastLabels { get; private set; }

    public Task<IReadOnlyDictionary<string, string>?> RequestAsync(IReadOnlyList<string> labels)
    {
        LastLabels = labels;
        return Task.FromResult(_values);
    }
}
