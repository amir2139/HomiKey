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
    private static readonly TimeSpan RetryCopyTimeout = TimeSpan.FromMilliseconds(1200);

    // How long the target app gets to read the clipboard after Ctrl+V before it is restored.
    // Restoring too early makes some apps paste the old clipboard instead.
    private static readonly TimeSpan PasteSettleTime = TimeSpan.FromMilliseconds(300);

    private readonly ConversionEngine _engine;
    private bool _busy;

    public TextReplacer(ConversionEngine engine) => _engine = engine;

    public enum Mode
    {
        /// <summary>Convert the selection; with no selection, optionally the wrong-layout words before the caret.</summary>
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

            // If the clipboard cannot be read now, do nothing: restoring a snapshot we failed
            // to take would wipe what the user had copied.
            var snapshot = ClipboardSnapshot.TryCapture()
                ?? throw new ClipboardUnavailableException();
            uint? pastedSequence = null;
            try
            {
                var result = await ConvertAsync(mode, settings);
                if (result is null) return;

                pastedSequence = await PasteAsync(result.Value.Text);
                if (settings.SwitchInputLanguage && result.Value.Script is { } script)
                    InputLanguages.SwitchTo(window, script);
            }
            finally
            {
                if (pastedSequence is { } sequence)
                {
                    await Task.Delay(PasteSettleTime);
                    // Something new was copied while the target app was pasting; keep it.
                    if (NativeMethods.GetClipboardSequenceNumber() == sequence) snapshot.Restore();
                }
                else
                {
                    snapshot.Restore();
                }
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
            var selected = await CopyAsync(CopyTimeout, KeyboardInput.CtrlC);
            // Code editors copy the whole line when nothing is selected; that is not a selection.
            if (selected.Length > 0 && ClipboardSnapshot.IsEditorLineCopy()) selected = string.Empty;
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
        var beforeCaret = await CopyAsync(CopyTimeout, KeyboardInput.ShiftHome, KeyboardInput.CtrlC);
        // Empty usually means the caret was at the start of the line. It can also mean a slow
        // app missed the copy, which would leave the Shift+Home selection behind for the next
        // keystroke to overwrite, so ask once more with more time.
        if (beforeCaret.Length == 0) beforeCaret = await CopyAsync(RetryCopyTimeout, KeyboardInput.CtrlC);
        // At the start of a line Shift+Home selects nothing, and a code editor then copies the
        // whole line; converting that would paste a second copy of the line.
        if (beforeCaret.Length == 0 || ClipboardSnapshot.IsEditorLineCopy()) return null;

        // The convert hotkey with nothing selected takes every wrong-layout word at the end;
        // the last-word hotkey takes just one, for when the words before it were meant as typed.
        var scope = mode == Mode.Selection ? BeforeCaretScope.WrongLayoutRun : BeforeCaretScope.LastWord;
        var converted = _engine.ConvertBeforeCaret(beforeCaret, now, scope);
        // Nothing to convert: paste the same text back so the selection made above goes away
        // and the caret ends where it was.
        return converted is null ? (beforeCaret, null) : (converted.Replacement, converted.OutputScript);
    }

    /// <summary>Sends the given keys and returns the text the app put on the clipboard, or "".</summary>
    private static async Task<string> CopyAsync(TimeSpan timeout, params Action[] keys)
    {
        ClipboardSnapshot.Clear();
        var sequence = NativeMethods.GetClipboardSequenceNumber();
        foreach (var send in keys)
        {
            send();
            await Task.Delay(20);
        }

        var deadline = DateTime.UtcNow + timeout;
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

    /// <summary>Pastes <paramref name="text"/>; returns the clipboard sequence number it pasted from.</summary>
    private static async Task<uint> PasteAsync(string text)
    {
        ClipboardSnapshot.SetText(text);
        var sequence = NativeMethods.GetClipboardSequenceNumber();
        await Task.Delay(20);
        KeyboardInput.CtrlV();
        return sequence;
    }
}

/// <summary>Another app is holding the clipboard open, so HomiKey cannot safely use it right now.</summary>
internal sealed class ClipboardUnavailableException : Exception
{
}
