using System.Windows.Automation;
using System.Windows.Automation.Text;
using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.UI.Services;

/// <summary>
/// <see cref="ICaretContext"/> over UI Automation. Works in anything that exposes a text pattern —
/// Win32 edit controls, WPF, browsers, Electron — and returns null everywhere else.
///
/// <para>It lives in the UI project rather than Infrastructure because the UIAutomationClient assembly
/// ships with WPF, which only this project references.</para>
/// </summary>
public sealed class UiAutomationCaretContext : ICaretContext
{
    private readonly ILogger<UiAutomationCaretContext> _logger;

    public UiAutomationCaretContext(ILogger<UiAutomationCaretContext> logger) => _logger = logger;

    public char? GetCharacterBeforeCaret()
    {
        try
        {
            var focused = AutomationElement.FocusedElement;
            if (focused is null || !focused.TryGetCurrentPattern(TextPattern.Pattern, out var pattern))
            {
                return null;
            }

            var selection = ((TextPattern)pattern).GetSelection();
            if (selection is null || selection.Length == 0)
            {
                return null;
            }

            // Collapse the selection to its start — that is where the caret is when nothing is selected,
            // and the point a replacement would work backwards from — then walk one character left.
            var range = selection[0].Clone();
            range.MoveEndpointByRange(TextPatternRangeEndpoint.End, range, TextPatternRangeEndpoint.Start);

            var moved = range.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -1);
            if (moved == 0)
            {
                return '\0'; // the caret is at the very start: nothing precedes it
            }

            var text = range.GetText(1);
            return string.IsNullOrEmpty(text) ? '\0' : text[0];
        }
        catch (Exception ex)
        {
            // ElementNotAvailable, timeouts, COM failures — all mean "we do not know".
            _logger.LogDebug(ex, "Could not read the character before the caret.");
            return null;
        }
    }
}
