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

    private const string VisualStudioLineCopy = "VisualStudioEditorOperationsLineCutCopyClipboardTag";
    private const string VisualStudioLineSelect = "MSDEVLineSelect";
    private const string ChromiumCustomData = "Chromium Web Custom MIME Data Format";

    private readonly DataObject? _data;

    private ClipboardSnapshot(DataObject? data) => _data = data;

    /// <summary>
    /// Copies every format that can be read. Formats that refuse to be read are skipped.
    /// Returns null when the clipboard itself cannot be opened (another app holds it), which
    /// is different from an empty clipboard: the caller must then leave the clipboard alone.
    /// </summary>
    public static ClipboardSnapshot? TryCapture()
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
            return null;
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

    /// <summary>
    /// True when the clipboard holds a whole line that a code editor copied because Ctrl+C was
    /// pressed with nothing selected (Visual Studio, VS Code), rather than a real selection.
    /// </summary>
    public static bool IsEditorLineCopy()
    {
        try
        {
            var data = Clipboard.GetDataObject();
            if (data is null) return false;
            // Visual Studio marks a whole-line copy with one of these formats.
            if (data.GetDataPresent(VisualStudioLineCopy, autoConvert: false) || data.GetDataPresent(VisualStudioLineSelect, autoConvert: false))
                return true;
            // VS Code stores {"isFromEmptySelection":true,...} under its own MIME type, which
            // Chromium keeps in one blob of UTF-16 strings.
            if (data.GetDataPresent(ChromiumCustomData, autoConvert: false) &&
                data.GetData(ChromiumCustomData, autoConvert: false) is MemoryStream stream)
            {
                var text = System.Text.Encoding.Unicode.GetString(stream.ToArray());
                return text.Contains("\"isFromEmptySelection\":true", StringComparison.Ordinal);
            }
        }
        catch (Exception ex) when (ex is ExternalException or NotSupportedException or InvalidOperationException)
        {
        }
        return false;
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
