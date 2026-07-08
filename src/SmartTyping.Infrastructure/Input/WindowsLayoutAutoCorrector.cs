using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// <see cref="ILayoutAutoCorrector"/> for Windows: backspaces over the wrong-layout word and pastes
/// the corrected text via the shared clipboard-based <see cref="ITextInjector"/> (which snapshots and
/// restores the user's clipboard). Best-effort — logs and swallows failures.
/// </summary>
public sealed class WindowsLayoutAutoCorrector : ILayoutAutoCorrector
{
    private readonly ITextInjector _injector;
    private readonly ILogger<WindowsLayoutAutoCorrector> _logger;

    public WindowsLayoutAutoCorrector(ITextInjector injector, ILogger<WindowsLayoutAutoCorrector> logger)
    {
        _injector = injector;
        _logger = logger;
    }

    public async Task<bool> ReplaceLastWordAsync(int charsToDelete, string replacement)
    {
        if (charsToDelete <= 0 || string.IsNullOrEmpty(replacement))
        {
            return false;
        }

        try
        {
            // Remove the wrong-layout word and the space that closed it, then paste the fix.
            KeyboardSender.TapKey(NativeMethods.VK_BACK, charsToDelete);
            await Task.Delay(20);
            return await _injector.InjectAsync(replacement, replaceSelection: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Automatic layout correction failed.");
            return false;
        }
    }
}
