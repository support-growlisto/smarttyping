using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// Clipboard access via WinForms <see cref="Clipboard"/>. Clipboard APIs require an STA thread;
/// calls are marshaled onto a dedicated STA worker so this service is safe to call from anywhere.
/// Failures are logged and degrade to empty/no-op rather than throwing.
/// </summary>
public sealed class WindowsClipboardService : IClipboardService, IClipboardBackup
{
    private readonly ILogger<WindowsClipboardService> _logger;

    public WindowsClipboardService(ILogger<WindowsClipboardService> logger) => _logger = logger;

    public Task<string> GetTextAsync() => Task.FromResult(RunSta(() =>
    {
        try
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read clipboard.");
            return string.Empty;
        }
    }));

    public Task SetTextAsync(string text)
    {
        RunSta(() =>
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    Clipboard.Clear();
                }
                else
                {
                    Clipboard.SetText(text);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set clipboard.");
            }

            return true;
        });

        return Task.CompletedTask;
    }

    public ClipboardSnapshot Save() => RunSta(() =>
    {
        try
        {
            var current = Clipboard.GetDataObject();
            if (current is null)
            {
                return ClipboardSnapshot.Empty;
            }

            // Copy each available format into a detached DataObject; the live one becomes stale
            // once we overwrite the clipboard. Per-format try/catch: a single unreadable format
            // (some apps advertise formats they cannot serve) must not lose the rest.
            var copy = new DataObject();
            foreach (var format in current.GetFormats(autoConvert: false))
            {
                try
                {
                    var data = current.GetData(format, autoConvert: false);
                    if (data is not null)
                    {
                        copy.SetData(format, data);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Skipping unreadable clipboard format {Format}.", format);
                }
            }

            return new ClipboardSnapshot(copy);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to snapshot clipboard; restore will be a no-op.");
            return ClipboardSnapshot.Empty;
        }
    });

    public void Restore(ClipboardSnapshot snapshot) => RunSta(() =>
    {
        try
        {
            if (snapshot.Data is null)
            {
                Clipboard.Clear();
            }
            else
            {
                Clipboard.SetDataObject(snapshot.Data, copy: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore clipboard snapshot.");
        }

        return true;
    });

    /// <summary>Runs a clipboard operation on a short-lived STA thread and returns its result.</summary>
    private static T RunSta<T>(Func<T> func)
    {
        T result = default!;
        var thread = new Thread(() => result = func());
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();
        return result;
    }
}
