using System.Runtime.CompilerServices;

namespace SmartTyping.Shared;

/// <summary>
/// Small argument-validation helpers. Pure, dependency-free.
/// </summary>
public static class Guard
{
    public static string AgainstNullOrWhiteSpace(string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
        }

        return value;
    }

    public static T AgainstNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        return value ?? throw new ArgumentNullException(paramName);
    }
}
