using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// Injects text into the foreground app by typing it as Unicode keystrokes (<c>KEYEVENTF_UNICODE</c>)
/// in a single <c>SendInput</c> call. Never throws across the boundary; returns false on failure.
///
/// <para>This used to paste via the clipboard: snapshot, set, Ctrl+V, sleep 80 ms, restore. The sleep
/// was a guess about how long the target needed to read the clipboard, and apps that read it later got
/// the *restored* contents — the text vanished, or the user's previous clipboard was pasted instead.
/// There is no way to observe another process reading the clipboard, so no amount of polling could fix
/// that; the only honest fix is not to use the clipboard. Typing the characters is deterministic, needs
/// no snapshot/restore dance, and leaves the user's clipboard untouched.</para>
///
/// <para>A selection under the caret is replaced by the first character we type, so
/// <c>replaceSelection</c> needs no special handling.</para>
/// </summary>
public sealed class WindowsTextInjector : ITextInjector
{
    private readonly ILogger<WindowsTextInjector> _logger;

    // Serializes injections so two concurrent ones (an auto action firing while a hotkey injection is
    // mid-flight) cannot interleave their keystrokes into each other's text.
    private readonly SemaphoreSlim _gate = new(1, 1);

    public WindowsTextInjector(ILogger<WindowsTextInjector> logger) => _logger = logger;

    public async Task<bool> InjectAsync(string text, bool replaceSelection, int? cursorOffset = null)
    {
        _ = replaceSelection;

        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        await _gate.WaitAsync();
        try
        {
            var leftTaps = KeyboardSender.LeftTapsFor(text, cursorOffset);
            KeyboardSender.ReplaceInline(backspaces: 0, text, leftTaps);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Text injection failed.");
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }
}
