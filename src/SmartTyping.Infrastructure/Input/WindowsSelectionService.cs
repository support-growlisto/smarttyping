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

    public Task<string> CaptureSelectionAsync() => CaptureCoreAsync(selectPreviousWordFirst: false);

    public Task<string> CaptureLastWordAsync() => CaptureCoreAsync(selectPreviousWordFirst: true);

    private async Task<string> CaptureCoreAsync(bool selectPreviousWordFirst)
    {
        try
        {
            // Snapshot the full clipboard so non-text content (images/files) is preserved.
            var snapshot = _backup.Save();

            if (selectPreviousWordFirst)
            {
                // Select the word to the left of the caret; it stays selected for the caller to replace.
                KeyboardSender.SendCtrlShift(NativeMethods.VK_LEFT);
                await Task.Delay(40);
            }

            await _clipboard.SetTextAsync(Sentinel);
            await ClipboardWait.UntilAsync(
                async () => string.Equals(await _clipboard.GetTextAsync(), Sentinel, StringComparison.Ordinal),
                timeoutMs: 300);

            KeyboardSender.SendCtrl(NativeMethods.VK_C);

            // Wait until the copy replaces the sentinel (or time out for an empty selection).
            await ClipboardWait.UntilAsync(
                async () => !string.Equals(await _clipboard.GetTextAsync(), Sentinel, StringComparison.Ordinal),
                timeoutMs: 400);

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
