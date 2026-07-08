using System.Globalization;
using System.Text;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Application.Templates;

/// <summary>
/// Replaces template variables in snippet content. Supported tokens (case-insensitive name):
/// <list type="bullet">
///   <item><c>{date}</c>, <c>{date:FORMAT}</c>, <c>{date+N}</c>, <c>{date-N}</c>, <c>{date+N:FORMAT}</c> — date, optional .NET format and ±N day offset.</item>
///   <item><c>{time}</c>, <c>{time:FORMAT}</c> — time, optional .NET format.</item>
///   <item><c>{clipboard}</c> — current clipboard text.</item>
///   <item><c>{cursor}</c> — marks where the caret should land after insertion (produces no text).</item>
///   <item><c>{input:Label}</c> — prompts the user for a value on expansion (same label = one prompt).</item>
/// </list>
/// Unknown or malformed tokens are left intact, so content is never lost and new variables can be
/// added without breaking existing snippets.
/// </summary>
public sealed class TemplateEngine : ITemplateEngine
{
    private const string InputPrefix = "input:";

    private readonly IDateTimeProvider _clock;
    private readonly IClipboardService _clipboard;
    private readonly IPlaceholderPrompt? _placeholderPrompt;

    public TemplateEngine(IDateTimeProvider clock, IClipboardService clipboard, IPlaceholderPrompt? placeholderPrompt = null)
    {
        _clock = clock;
        _clipboard = clipboard;
        _placeholderPrompt = placeholderPrompt;
    }

    public async Task<RenderedTemplate> RenderAsync(string content)
    {
        if (string.IsNullOrEmpty(content) || !content.Contains('{'))
        {
            return RenderedTemplate.Plain(content);
        }

        // Resolve {input:…} placeholders up front (one prompt for all fields). A cancel aborts.
        var labels = CollectInputLabels(content);
        IReadOnlyDictionary<string, string> inputs = EmptyInputs;
        if (labels.Count > 0 && _placeholderPrompt is not null)
        {
            var values = await _placeholderPrompt.RequestAsync(labels);
            if (values is null)
            {
                return RenderedTemplate.CancelledResult();
            }

            inputs = values;
        }

        string? clipboardText = null; // resolved lazily, once
        int? cursorOffset = null;

        var result = new StringBuilder(content.Length);
        var i = 0;
        while (i < content.Length)
        {
            var c = content[i];
            if (c != '{')
            {
                result.Append(c);
                i++;
                continue;
            }

            var close = content.IndexOf('}', i + 1);
            if (close < 0)
            {
                result.Append(content, i, content.Length - i);
                break;
            }

            var token = content.Substring(i + 1, close - i - 1);
            var raw = content.Substring(i, close - i + 1); // the original "{...}" including braces

            if (TryResolveInput(token, inputs, out var inputValue))
            {
                result.Append(inputValue);
            }
            else if (TryResolveToken(token, out var replacement, out var isCursor))
            {
                if (isCursor)
                {
                    cursorOffset ??= result.Length; // first cursor marker wins
                }
                else
                {
                    // Resolve clipboard lazily now that we know the token needs it.
                    if (replacement is null)
                    {
                        clipboardText ??= await _clipboard.GetTextAsync();
                        replacement = clipboardText;
                    }

                    result.Append(replacement);
                }
            }
            else
            {
                result.Append(raw); // unknown/malformed: keep verbatim
            }

            i = close + 1;
        }

        return new RenderedTemplate(result.ToString(), cursorOffset);
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyInputs =
        new Dictionary<string, string>();

    /// <summary>Extracts the ordered, de-duplicated <c>{input:Label}</c> labels from the content.</summary>
    private static IReadOnlyList<string> CollectInputLabels(string content)
    {
        var labels = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        var i = 0;
        while (i < content.Length)
        {
            if (content[i] != '{')
            {
                i++;
                continue;
            }

            var close = content.IndexOf('}', i + 1);
            if (close < 0)
            {
                break;
            }

            var token = content.Substring(i + 1, close - i - 1).Trim();
            if (token.StartsWith(InputPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var label = token[InputPrefix.Length..].Trim();
                if (label.Length > 0 && seen.Add(label))
                {
                    labels.Add(label);
                }
            }

            i = close + 1;
        }

        return labels;
    }

    private static bool TryResolveInput(string token, IReadOnlyDictionary<string, string> inputs, out string value)
    {
        value = string.Empty;
        var trimmed = token.Trim();
        if (!trimmed.StartsWith(InputPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var label = trimmed[InputPrefix.Length..].Trim();
        value = inputs.TryGetValue(label, out var v) ? v : string.Empty;
        return true;
    }

    /// <summary>
    /// Resolves a single token. Returns false for unknown/malformed tokens (caller keeps them verbatim).
    /// A null <paramref name="replacement"/> with a true return means "clipboard" (resolved by the caller).
    /// </summary>
    private bool TryResolveToken(string token, out string? replacement, out bool isCursor)
    {
        replacement = string.Empty;
        isCursor = false;

        var trimmed = token.Trim();
        var lower = trimmed.ToLowerInvariant();

        if (lower == "cursor")
        {
            isCursor = true;
            return true;
        }

        if (lower == "clipboard")
        {
            replacement = null; // signal: resolve clipboard
            return true;
        }

        if (lower == "time" || lower.StartsWith("time:", StringComparison.Ordinal))
        {
            var format = ExtractFormat(trimmed);
            replacement = FormatDateTime(_clock.Now, format, isDate: false);
            return replacement is not null;
        }

        if (lower == "date" || lower.StartsWith("date+", StringComparison.Ordinal)
            || lower.StartsWith("date-", StringComparison.Ordinal) || lower.StartsWith("date:", StringComparison.Ordinal))
        {
            return TryResolveDate(trimmed, out replacement);
        }

        return false;
    }

    private bool TryResolveDate(string token, out string? replacement)
    {
        replacement = null;

        // Grammar: date[±N][:FORMAT]
        var rest = token.Substring("date".Length); // preserves case of FORMAT
        string? format = null;

        var colon = rest.IndexOf(':');
        if (colon >= 0)
        {
            format = rest[(colon + 1)..];
            rest = rest[..colon];
        }

        var offsetDays = 0;
        if (rest.Length > 0)
        {
            if (!int.TryParse(rest, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out offsetDays))
            {
                return false; // malformed offset → leave token verbatim
            }
        }

        var value = _clock.Now.Date.AddDays(offsetDays);
        replacement = FormatDateTime(value, format, isDate: true);
        return replacement is not null;
    }

    private static string? FormatDateTime(DateTime value, string? format, bool isDate)
    {
        // Bare {date}/{time} follow the user's locale (short date/time as shown elsewhere in Windows).
        if (string.IsNullOrEmpty(format))
        {
            return isDate ? value.ToShortDateString() : value.ToShortTimeString();
        }

        // An explicit format string means the caller wants an exact, predictable result, so use the
        // invariant (Gregorian) calendar — e.g. {date:yyyy-MM-dd} yields 2026-07-08, not the Thai
        // Buddhist-era 2569 that CurrentCulture would produce on a th-TH system.
        try
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            return null; // invalid format → caller keeps the token verbatim
        }
    }

    private static string? ExtractFormat(string token)
    {
        var colon = token.IndexOf(':');
        return colon >= 0 ? token[(colon + 1)..] : null;
    }
}
