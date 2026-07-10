using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// <see cref="IInlineReplacer"/> for Windows: backspaces over the just-typed text and types the
/// replacement as Unicode keystrokes, all in one <c>SendInput</c> call.
///
/// <para>Deliberately does <b>not</b> use the clipboard-based <see cref="ITextInjector"/>. That path
/// snapshots the clipboard, sets it, sends Ctrl+V, then restores the snapshot — for a replacement
/// fired straight from the keyboard hook, the target app often reads the clipboard only after the
/// restore, pasting the *old* contents. Typing the characters directly is deterministic and leaves the
/// user's clipboard untouched.</para>
///
/// <para>There is also no sleep here waiting for the triggering keystroke to arrive. The hook swallows
/// that key, so nothing is in flight and there is nothing to wait for: what we send is the only input
/// the target will see, and it arrives in the order we queued it.</para>
///
/// Best-effort — logs and swallows failures.
/// </summary>
public sealed class WindowsInlineReplacer : IInlineReplacer
{
    private readonly ILogger<WindowsInlineReplacer> _logger;

    public WindowsInlineReplacer(ILogger<WindowsInlineReplacer> logger) => _logger = logger;

    public Task<bool> ReplaceAsync(int charsToDelete, string replacement, int? cursorOffset = null)
    {
        if (charsToDelete < 0 || string.IsNullOrEmpty(replacement))
        {
            return Task.FromResult(false);
        }

        try
        {
            // One atomic SendInput: the user's next keystroke cannot land inside our edit.
            var leftTaps = KeyboardSender.LeftTapsFor(replacement, cursorOffset);
            KeyboardSender.ReplaceInline(charsToDelete, replacement, leftTaps);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inline replacement failed.");
            return Task.FromResult(false);
        }
    }

    public Task<bool> TypeAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Task.FromResult(false);
        }

        try
        {
            KeyboardSender.ReplaceInline(backspaces: 0, text, leftTaps: 0);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to type back the swallowed keystroke.");
            return Task.FromResult(false);
        }
    }
}
