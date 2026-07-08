using SmartTyping.Application.Abstractions;

namespace SmartTyping.Tests.Fakes;

/// <summary>Deterministic clock for tests.</summary>
public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider(DateTime now, DateTime utcNow)
    {
        Now = now;
        UtcNow = utcNow;
    }

    public DateTime Now { get; }

    public DateTime UtcNow { get; }
}
