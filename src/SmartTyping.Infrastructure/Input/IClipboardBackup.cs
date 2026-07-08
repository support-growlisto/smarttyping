using System.Windows.Forms;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// Infrastructure-internal capability to snapshot and restore the <b>entire</b> clipboard (all
/// formats, not just text), so that using the clipboard as a transport for conversion/expansion
/// does not destroy the user's existing clipboard content (images, files, rich text).
/// </summary>
public interface IClipboardBackup
{
    ClipboardSnapshot Save();

    void Restore(ClipboardSnapshot snapshot);
}

/// <summary>An opaque capture of the clipboard's contents at a point in time.</summary>
public sealed class ClipboardSnapshot
{
    internal ClipboardSnapshot(IDataObject? data) => Data = data;

    internal IDataObject? Data { get; }

    /// <summary>An empty snapshot (nothing to restore).</summary>
    public static ClipboardSnapshot Empty { get; } = new((IDataObject?)null);
}
