using SmartTyping.Application.Abstractions;

namespace SmartTyping.Tests.Fakes;

/// <summary>In-memory clipboard for tests.</summary>
public sealed class FakeClipboardService : IClipboardService
{
    private string _text;

    public FakeClipboardService(string initial = "") => _text = initial;

    public Task<string> GetTextAsync() => Task.FromResult(_text);

    public Task SetTextAsync(string text)
    {
        _text = text;
        return Task.CompletedTask;
    }
}
