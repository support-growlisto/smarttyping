using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// Captures the foreground selection by sending Ctrl+C, reading the clipboard, and restoring the
/// previous clipboard content. Best-effort: returns empty if nothing could be captured.
/// </summary>
public sealed class WindowsSelectionService : ISelectionService
{
    private readonly IClipboardService _clipboard;
    private readonly IClipboardBackup _backup;
    private readonly ILogger<WindowsSelectionService> _logger;

    public WindowsSelectionService(IClipboardService clipboard, IClipboardBackup backup, ILogger<WindowsSelectionService> logger)
    {
        _clipboard = clipboard;
        _backup = backup;
        _logger = logger;
    }

    // Distinctive marker placed on the clipboard before Ctrl+C, so we can tell an actual copy
    // (clipboard changes to the selection) from "nothing selected / copy did nothing". The suffix
    // makes an accidental collision with a real text selection effectively impossible.
    private const string Sentinel = "SmartTyping.capture.6f1a2b";

    // Three chances at ~150 ms each: long enough for a slow or remote app to service the copy, short
    // enough that a genuinely empty selection is reported quickly.
    private const int CopyAttempts = 3;
    private const int CopyTimeoutMs = 150;

    public Task<string> CaptureSelectionAsync() => CaptureCoreAsync(selectPreviousWordFirst: false);

    public Task<string> CaptureLastWordAsync() => CaptureCoreAsync(selectPreviousWordFirst: true);

    private async Task<string> CaptureCoreAsync(bool selectPreviousWordFirst)
    {
        try
        {
            // Snapshot the full clipboard so non-text content (images/files) is preserved.
            var snapshot = _backup.Save();

            // Place the sentinel and confirm it landed *before* sending any keys. Nothing about the
            // selection depends on the clipboard, so doing it first means the two keystrokes below
            // need no delay between them: they enter the target's input queue in order, and the app
            // cannot process the Ctrl+C before the Ctrl+Shift+Left that precedes it.
            await _clipboard.SetTextAsync(Sentinel);
            await ClipboardWait.UntilAsync(
                async () => string.Equals(await _clipboard.GetTextAsync(), Sentinel, StringComparison.Ordinal),
                timeoutMs: 300);

            if (selectPreviousWordFirst)
            {
                // Select the word to the left of the caret; it stays selected for the caller to replace.
                KeyboardSender.SendCtrlShift(NativeMethods.VK_LEFT);
            }

            // Copy, and keep asking until the clipboard actually changes. A single Ctrl+C sometimes does
            // not reach the target in time — the app may still be handling the hotkey's own keystrokes —
            // and the old code simply timed out and reported "nothing selected". Re-sending Ctrl+C is
            // harmless (it copies the same selection again), and the clipboard changing is the real
            // signal that the copy happened, which no fixed sleep can give us. When the selection truly
            // is empty, every attempt leaves the sentinel in place and we say so.
            for (var attempt = 0; attempt < CopyAttempts; attempt++)
            {
                KeyboardSender.SendCtrl(NativeMethods.VK_C);

                if (await ClipboardWait.UntilAsync(
                        async () => !string.Equals(await _clipboard.GetTextAsync(), Sentinel, StringComparison.Ordinal),
                        timeoutMs: CopyTimeoutMs))
                {
                    break;
                }
            }

            var captured = await _clipboard.GetTextAsync();

            // Restore the user's clipboard.
            _backup.Restore(snapshot);

            // If the sentinel is still there, nothing was captured.
            return string.Equals(captured, Sentinel, StringComparison.Ordinal) ? string.Empty : captured;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture the selection.");
            return string.Empty;
        }
    }
}
