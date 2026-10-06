using System.Runtime.InteropServices;

namespace HomiKey.App.Native;

/// <summary>
/// A copy of everything on the clipboard, so it can be put back after HomiKey has
/// used the clipboard to read and paste text.
/// </summary>
internal sealed class ClipboardSnapshot
{
    // Formats that ask Windows clipboard history and cloud clipboard to skip the content,
    // so neither the temporary text nor the restored copy shows up twice in Win+V.
    private const string ExcludeFromMonitoring = "ExcludeClipboardContentFromMonitorProcessing";
    private const string CanIncludeInHistory = "CanIncludeInClipboardHistory";
    private const string CanUploadToCloud = "CanUploadToCloudClipboard";

    private readonly DataObject? _data;

    private ClipboardSnapshot(DataObject? data) => _data = data;

    /// <summary>Copies every format that can be read. Formats that refuse to be read are skipped.</summary>
    public static ClipboardSnapshot Capture()
    {
        try
        {
            var source = Clipboard.GetDataObject();
            if (source is null) return new ClipboardSnapshot(null);

            var formats = source.GetFormats(autoConvert: false);
            if (formats.Length == 0) return new ClipboardSnapshot(null);

            var copy = new DataObject();
            foreach (var format in formats)
            {
                try
                {
                    var value = source.GetData(format, autoConvert: false);
                    if (value is not null) copy.SetData(format, autoConvert: false, value);
                }
                catch (Exception ex) when (ex is ExternalException or NotSupportedException or InvalidOperationException or OutOfMemoryException or System.Runtime.Serialization.SerializationException)
                {
                    // Some formats are rendered on demand by their owner and cannot be copied.
                }
            }
            return new ClipboardSnapshot(copy);
        }
        catch (ExternalException)
        {
            return new ClipboardSnapshot(null);
        }
    }

    /// <summary>Puts the captured content back, or empties the clipboard if it was empty.</summary>
    public void Restore()
    {
        try
        {
            if (_data is null)
            {
                Clipboard.Clear();
                return;
            }
            MarkPrivate(_data);
            Clipboard.SetDataObject(_data, copy: true, retryTimes: 10, retryDelay: 50);
        }
        catch (ExternalException)
        {
            // Another app holds the clipboard open; there is nothing more we can do.
        }
    }

    /// <summary>Puts plain text on the clipboard, hidden from clipboard history.</summary>
    public static void SetText(string text)
    {
        var data = new DataObject();
        data.SetData(DataFormats.UnicodeText, autoConvert: false, text);
        MarkPrivate(data);
        Clipboard.SetDataObject(data, copy: true, retryTimes: 10, retryDelay: 50);
    }

    /// <summary>Empties the clipboard so a following copy can be detected.</summary>
    public static void Clear()
    {
        try { Clipboard.Clear(); }
        catch (ExternalException) { }
    }

    /// <summary>The clipboard's plain text, or an empty string.</summary>
    public static string GetText()
    {
        try
        {
            return Clipboard.ContainsText(TextDataFormat.UnicodeText) ? Clipboard.GetText(TextDataFormat.UnicodeText) : string.Empty;
        }
        catch (ExternalException)
        {
            return string.Empty;
        }
    }

    private static void MarkPrivate(DataObject data)
    {
        data.SetData(ExcludeFromMonitoring, autoConvert: false, new MemoryStream(new byte[4]));
        data.SetData(CanIncludeInHistory, autoConvert: false, new MemoryStream(BitConverter.GetBytes(0)));
        data.SetData(CanUploadToCloud, autoConvert: false, new MemoryStream(BitConverter.GetBytes(0)));
    }
}
