namespace SmartTyping.Application.Language;

/// <summary>
/// A word the user just finished typing plus the delimiter that closed it. Raised by the keyboard
/// hook so the auto-expand coordinator can check whether the word is a snippet trigger.
/// </summary>
/// <param name="Word">The raw typed word (physical QWERTY characters, lower-cased letters).</param>
/// <param name="Boundary">The delimiter to re-insert after the expansion (" ", "\t", or "\r\n").</param>
public sealed record WordBoundary(string Word, string Boundary);
