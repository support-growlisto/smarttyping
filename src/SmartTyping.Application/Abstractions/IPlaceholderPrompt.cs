namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Prompts the user to fill <c>{input:Label}</c> placeholders found in a snippet during expansion.
/// Implemented in the UI (a small dynamic form). Returns the entered values keyed by label, or
/// null if the user cancelled.
/// </summary>
public interface IPlaceholderPrompt
{
    Task<IReadOnlyDictionary<string, string>?> RequestAsync(IReadOnlyList<string> labels);
}
