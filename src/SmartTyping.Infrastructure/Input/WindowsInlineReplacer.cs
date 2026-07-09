using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// <see cref="IInlineReplacer"/> for Windows: backspaces over the just-typed text and types the
/// replacement as Unicode keystrokes.
///
/// <para>Deliberately does <b>not</b> use the clipboard-based <see cref="ITextInjector"/>. That path
/// snapshots the clipboard, sets it, sends Ctrl+V, then restores the snapshot ~80 ms later — for an
/// inline replacement fired straight from the keyboard hook, the target app often reads the clipboard
/// only after the restore, pasting the *old* contents (i.e. nothing). Typing the characters directly
/// is deterministic and leaves the user's clipboard untouched.</para>
///
/// Best-effort — logs and swallows failures.
/// </summary>
public sealed class WindowsInlineReplacer : IInlineReplacer
{
    private readonly ILogger<WindowsInlineReplacer> _logger;

    public WindowsInlineReplacer(ILogger<WindowsInlineReplacer> logger) => _logger = logger;

    public Task<bool> ReplaceAsync(int charsToDelete, string replacement, int? cursorOffset = null)
    {
        if (charsToDelete <= 0 || string.IsNullOrEmpty(replacement))
        {
            return Task.FromResult(false);
        }

        return ReplaceCoreAsync(charsToDelete, replacement, cursorOffset);
    }

    private async Task<bool> ReplaceCoreAsync(int charsToDelete, string replacement, int? cursorOffset)
    {
        try
        {
            // The keystroke that triggered this replacement is still in flight: the hook raises its
            // event before returning CallNextHookEx, so the character has not reached the focused
            // window yet. Backspacing immediately would delete the wrong characters. Let it land —
            // but keep this short, because the user is still typing.
            await Task.Delay(35);

            // Walk the caret back to the {cursor} marker. A '\n' costs two caret positions because the
            // Enter we send inserts a CRLF; '\r' itself is never typed.
            var leftTaps = 0;
            if (cursorOffset is int offset && offset >= 0 && offset < replacement.Length)
            {
                for (var i = offset; i < replacement.Length; i++)
                {
                    if (replacement[i] == '\r')
                    {
                        continue;
                    }

                    leftTaps += replacement[i] == '\n' ? 2 : 1;
                }
            }

            // One atomic SendInput: the user's next keystroke cannot land inside our edit.
            KeyboardSender.ReplaceInline(charsToDelete, replacement, leftTaps);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inline replacement failed.");
            return false;
        }
    }
}
