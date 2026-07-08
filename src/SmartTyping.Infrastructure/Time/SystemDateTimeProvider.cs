using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Time;

/// <summary>Real system clock implementation of <see cref="IDateTimeProvider"/>.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now;

    public DateTime UtcNow => DateTime.UtcNow;
}
