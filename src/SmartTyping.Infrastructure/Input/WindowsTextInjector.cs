using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// Injects text into the foreground app using a clipboard paste (Ctrl+V) — the most reliable
/// method across applications. The previous clipboard contents are saved and restored so the
/// user's clipboard is not clobbered. Never throws across the boundary; returns false on failure.
/// </summary>
public sealed class WindowsTextInjector : ITextInjector
{
    private readonly IClipboardService _clipboard;
    private readonly IClipboardBackup _backup;
    private readonly ILogger<WindowsTextInjector> _logger;

    // Serializes the save→set→paste→restore sequence so two concurrent injections (e.g. an auto
    // action firing while a hotkey injection is mid-flight) can't interleave and clobber each other's
    // clipboard snapshot, which would otherwise leave injected text on the user's clipboard.
    private readonly SemaphoreSlim _gate = new(1, 1);

    public WindowsTextInjector(IClipboardService clipboard, IClipboardBackup backup, ILogger<WindowsTextInjector> logger)
    {
        _clipboard = clipboard;
        _backup = backup;
        _logger = logger;
    }

    public async Task<bool> InjectAsync(string text, bool replaceSelection, int? cursorOffset = null)
    {
        // When replaceSelection is true we simply paste over the current selection; the caller is
        // expected to have a selection active (e.g. the text just converted). Ctrl+V handles both.
        _ = replaceSelection;

        await _gate.WaitAsync();
        try
        {
            // Snapshot the full clipboard (all formats) so images/files/rich text survive.
            var snapshot = _backup.Save();
            await _clipboard.SetTextAsync(text);

            // Wait until the clipboard actually holds our text before pasting, rather than a blind
            // fixed sleep — more reliable on slow/remote apps.
            await ClipboardWait.UntilAsync(
                async () => string.Equals(await _clipboard.GetTextAsync(), text, StringComparison.Ordinal),
                timeoutMs: 500);

            KeyboardSender.SendCtrl(NativeMethods.VK_V);

            // Give the target app time to consume the paste before we restore the clipboard.
            await Task.Delay(80);

            // Position the caret at the {cursor} marker by walking back from the end of the text.
            if (cursorOffset is int offset && offset >= 0 && offset < text.Length)
            {
                var back = text.Length - offset;

                // Many editors store a lone '\n' as '\r\n', inserting one extra character per line
                // break that our char count didn't include. Count bare '\n' in the tail and step back
                // one more per line so the caret still lands on the {cursor} marker.
                for (var i = offset; i < text.Length; i++)
                {
                    if (text[i] == '\n' && (i == 0 || text[i - 1] != '\r'))
                    {
                        back++;
                    }
                }

                KeyboardSender.TapKey(NativeMethods.VK_LEFT, back);
            }

            _backup.Restore(snapshot);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Text injection failed; the converted text remains on the clipboard.");
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }
}
