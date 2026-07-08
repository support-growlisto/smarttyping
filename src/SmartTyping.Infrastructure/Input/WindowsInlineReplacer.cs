using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// <see cref="IInlineReplacer"/> for Windows: backspaces over the just-typed text and pastes the
/// replacement via the shared clipboard-based <see cref="ITextInjector"/> (which snapshots and
/// restores the user's clipboard). Best-effort — logs and swallows failures.
/// </summary>
public sealed class WindowsInlineReplacer : IInlineReplacer
{
    private readonly ITextInjector _injector;
    private readonly ILogger<WindowsInlineReplacer> _logger;

    public WindowsInlineReplacer(ITextInjector injector, ILogger<WindowsInlineReplacer> logger)
    {
        _injector = injector;
        _logger = logger;
    }

    public async Task<bool> ReplaceAsync(int charsToDelete, string replacement, int? cursorOffset = null)
    {
        if (charsToDelete <= 0 || string.IsNullOrEmpty(replacement))
        {
            return false;
        }

        try
        {
            // Remove the typed text (word + its delimiter), then paste the replacement.
            KeyboardSender.TapKey(NativeMethods.VK_BACK, charsToDelete);
            await Task.Delay(20);
            return await _injector.InjectAsync(replacement, replaceSelection: false, cursorOffset);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inline replacement failed.");
            return false;
        }
    }
}
