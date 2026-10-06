using HomiKey.App.Native;
using HomiKey.Core;

namespace HomiKey.App;

/// <summary>
/// Reads text from the focused app through the clipboard, converts it and pastes the result,
/// then puts the user's clipboard back as it was.
/// </summary>
internal sealed class TextReplacer
{
    // How long the target app gets to put the selection on the clipboard after Ctrl+C.
    private static readonly TimeSpan CopyTimeout = TimeSpan.FromMilliseconds(600);

    // How long the target app gets to read the clipboard after Ctrl+V before it is restored.
    // Restoring too early makes some apps paste the old clipboard instead.
    private static readonly TimeSpan PasteSettleTime = TimeSpan.FromMilliseconds(300);

    private readonly ConversionEngine _engine;
    private bool _busy;

    public TextReplacer(ConversionEngine engine) => _engine = engine;

    public enum Mode
    {
        /// <summary>Convert the selection; with no selection, optionally the last word.</summary>
        Selection,

        /// <summary>Convert the last word before the caret.</summary>
        LastWord,
    }

    /// <summary>Runs one conversion. Presses that arrive while one is running are ignored.</summary>
    public async Task RunAsync(Mode mode, Hotkey pressed, AppSettings settings)
    {
        if (_busy) return;
        _busy = true;
        try
        {
            var window = NativeMethods.GetForegroundWindow();
            if (settings.IsExcluded(ForegroundApp.ProcessName(window))) return;

            await KeyboardInput.WaitForKeysReleasedAsync(pressed.Key, TimeSpan.FromSeconds(1.5));

            var snapshot = ClipboardSnapshot.Capture();
            var pasted = false;
            try
            {
                var result = await ConvertAsync(mode, settings);
                if (result is null) return;

                await PasteAsync(result.Value.Text);
                pasted = true;
                if (settings.SwitchInputLanguage && result.Value.Script is { } script)
                    InputLanguages.SwitchTo(window, script);
            }
            finally
            {
                if (pasted) await Task.Delay(PasteSettleTime);
                snapshot.Restore();
            }
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>What to paste, and the script it is in (null when the text is put back unchanged).</summary>
    private async Task<(string Text, LayoutScript? Script)?> ConvertAsync(Mode mode, AppSettings settings)
    {
        var now = DateTimeOffset.Now;

        if (mode == Mode.Selection)
        {
            var selected = await CopyAsync(KeyboardInput.CtrlC);
            if (selected.Length > 0)
            {
                var result = _engine.ConvertSelection(selected, now);
                return result is null ? null : (result.Replacement, result.OutputScript);
            }
            if (!settings.LastWordWhenNothingSelected) return null;
        }

        // Nothing selected: select back to the start of the line and work on that text.
        // Replacing the whole line-to-caret span, rather than moving the caret with arrow
        // keys, behaves the same in left-to-right and right-to-left text.
        var beforeCaret = await CopyAsync(KeyboardInput.ShiftHome, KeyboardInput.CtrlC);
        if (beforeCaret.Length == 0) return null;

        var converted = _engine.ConvertBeforeCaret(beforeCaret, now);
        // Nothing to convert: paste the same text back so the selection made above goes away
        // and the caret ends where it was.
        return converted is null ? (beforeCaret, null) : (converted.Replacement, converted.OutputScript);
    }

    /// <summary>Sends the given keys and returns the text the app put on the clipboard, or "".</summary>
    private static async Task<string> CopyAsync(params Action[] keys)
    {
        ClipboardSnapshot.Clear();
        var sequence = NativeMethods.GetClipboardSequenceNumber();
        foreach (var send in keys)
        {
            send();
            await Task.Delay(20);
        }

        var deadline = DateTime.UtcNow + CopyTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (NativeMethods.GetClipboardSequenceNumber() != sequence)
            {
                // The owner may still be writing; give it a moment, then read.
                await Task.Delay(20);
                return ClipboardSnapshot.GetText();
            }
            await Task.Delay(10);
        }
        return string.Empty;
    }

    private static async Task PasteAsync(string text)
    {
        ClipboardSnapshot.SetText(text);
        await Task.Delay(20);
        KeyboardInput.CtrlV();
    }
}
