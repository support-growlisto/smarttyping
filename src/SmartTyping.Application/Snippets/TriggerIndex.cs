namespace SmartTyping.Application.Snippets;

/// <summary>
/// An in-memory snapshot of the enabled snippet triggers, used to decide whether the word the user
/// has typed so far is a complete trigger that can be expanded *immediately* (without waiting for a
/// space).
///
/// <para>A trigger only fires on completion when it is not a strict prefix of another trigger. If
/// both <c>/s</c> and <c>/sig</c> exist, typing <c>/s</c> must not expand — otherwise the user could
/// never type <c>/sig</c>. Such ambiguous triggers still expand on a space/tab delimiter.</para>
///
/// <para>Immutable and safe to read from the keyboard-hook thread.</para>
/// </summary>
public sealed class TriggerIndex
{
    public static readonly TriggerIndex Empty = new(Array.Empty<string>());

    private readonly HashSet<string> _all;
    private readonly HashSet<string> _expandOnCompletion;

    public TriggerIndex(IEnumerable<string> triggers)
    {
        _all = triggers
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _expandOnCompletion = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var trigger in _all)
        {
            var isPrefixOfAnother = _all.Any(other =>
                other.Length > trigger.Length &&
                other.StartsWith(trigger, StringComparison.OrdinalIgnoreCase));

            if (!isPrefixOfAnother)
            {
                _expandOnCompletion.Add(trigger);
            }
        }
    }

    /// <summary>
    /// True when <paramref name="typed"/> is an enabled trigger that can be expanded the moment it is
    /// finished, because no longer trigger starts with it.
    /// </summary>
    public bool IsCompleteTrigger(string typed) =>
        !string.IsNullOrEmpty(typed) && _expandOnCompletion.Contains(typed);

    /// <summary>
    /// True when <paramref name="typed"/> is an enabled trigger at all — including one that is a prefix
    /// of a longer trigger and therefore waits for a delimiter. The hook asks this before it swallows
    /// the delimiter, so it only takes a keystroke that an expansion is actually going to replace.
    /// </summary>
    public bool IsKnownTrigger(string typed) =>
        !string.IsNullOrEmpty(typed) && _all.Contains(typed);
}
