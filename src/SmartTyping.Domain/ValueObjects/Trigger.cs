using SmartTyping.Shared;

namespace SmartTyping.Domain.ValueObjects;

/// <summary>
/// A snippet trigger token (e.g. <c>/phone</c>). Value object: normalized, compared case-insensitively.
/// </summary>
public readonly struct Trigger : IEquatable<Trigger>
{
    private Trigger(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Creates a trigger from raw text. Trims surrounding whitespace and rejects empty input.
    /// The leading prefix (e.g. <c>/</c>) is part of the value and is kept as-is.
    /// </summary>
    public static Result<Trigger> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Result.Failure<Trigger>("Trigger cannot be empty.");
        }

        var normalized = raw.Trim();
        if (normalized.Contains(' '))
        {
            return Result.Failure<Trigger>("Trigger cannot contain spaces.");
        }

        return Result.Success(new Trigger(normalized));
    }

    public bool Equals(Trigger other) =>
        string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is Trigger other && Equals(other);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(Value ?? string.Empty);

    public override string ToString() => Value;
}
