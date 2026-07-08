namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Abstracts the system clock so time-dependent logic (template variables, timestamps)
/// is deterministic and unit-testable.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Current local time (used for user-facing <c>{date}</c>/<c>{time}</c> tokens).</summary>
    DateTime Now { get; }

    /// <summary>Current UTC time (used for persisted timestamps).</summary>
    DateTime UtcNow { get; }
}
