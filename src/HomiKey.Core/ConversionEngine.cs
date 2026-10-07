namespace HomiKey.Core;

/// <summary>What to put in place of the text that was read from the app.</summary>
/// <param name="Replacement">The text to paste.</param>
/// <param name="OutputScript">The script the pasted text is meant to be in; the input language can follow it.</param>
/// <param name="IsRevert">True when this undoes the previous conversion.</param>
public sealed record ConversionResult(string Replacement, LayoutScript OutputScript, bool IsRevert);

/// <summary>How much of the text before the caret a no-selection conversion takes.</summary>
public enum BeforeCaretScope
{
    /// <summary>Only the last word.</summary>
    LastWord,

    /// <summary>All the words at the end that were typed on the wrong layout (see <see cref="WrongLayoutRun"/>).</summary>
    WrongLayoutRun,
}

/// <summary>
/// Everything the hotkeys need, independent of how text is read from and written to
/// other apps: auto-detected conversion, last-word conversion, and revert on a second press.
/// </summary>
public sealed class ConversionEngine
{
    private LayoutConverter _converter;
    private LayoutScript _lastOriginalScript;

    public ConversionEngine(ConversionOptions? options = null, ConversionHistory? history = null)
    {
        _converter = new LayoutConverter(options);
        History = history ?? new ConversionHistory();
    }

    /// <summary>The remembered last conversion.</summary>
    public ConversionHistory History { get; }

    /// <summary>The conversion options. Setting them forgets the last conversion.</summary>
    public ConversionOptions Options
    {
        get => _converter.Options;
        set
        {
            _converter = new LayoutConverter(value);
            History.Clear();
        }
    }

    /// <summary>The converter in use.</summary>
    public LayoutConverter Converter => _converter;

    /// <summary>
    /// Converts selected text. If the selection is what the last conversion produced,
    /// returns the text it replaced instead. Returns null when nothing would change.
    /// </summary>
    public ConversionResult? ConvertSelection(string selected, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(selected);
        if (selected.Length == 0) return null;

        if (History.TryRevertSelection(selected, now, out var original))
        {
            History.Clear();
            return new ConversionResult(original, _lastOriginalScript, IsRevert: true);
        }

        var converted = _converter.Convert(selected, out var direction);
        if (converted == selected) return null;

        Remember(selected, converted, direction, now);
        return new ConversionResult(converted, OutputScriptOf(direction), IsRevert: false);
    }

    /// <summary>
    /// Works on the text between the start of the line and the caret, when nothing is selected.
    /// If it ends with what the last conversion produced, that ending is put back; otherwise the
    /// last word, or the run of wrong-layout words at the end, is converted. The result replaces
    /// the whole text that was passed in. Returns null when nothing would change.
    /// </summary>
    public ConversionResult? ConvertBeforeCaret(string textBeforeCaret, DateTimeOffset now, BeforeCaretScope scope = BeforeCaretScope.LastWord)
    {
        ArgumentNullException.ThrowIfNull(textBeforeCaret);

        if (History.TryRevertBeforeCaret(textBeforeCaret, now, out var reverted))
        {
            History.Clear();
            return new ConversionResult(reverted, _lastOriginalScript, IsRevert: true);
        }

        var direction = ConversionDirection.LatinToPersian;
        string result, originalTail, convertedTail;
        var found = scope == BeforeCaretScope.WrongLayoutRun
            ? WrongLayoutRun.TryReplace(textBeforeCaret, _converter, out result, out originalTail, out convertedTail, out direction)
            : LastWord.TryReplace(textBeforeCaret, word => _converter.Convert(word, out direction), out result, out originalTail, out convertedTail);
        if (!found) return null;
        if (originalTail == convertedTail) return null;

        Remember(originalTail, convertedTail, direction, now);
        return new ConversionResult(result, OutputScriptOf(direction), IsRevert: false);
    }

    private void Remember(string original, string converted, ConversionDirection direction, DateTimeOffset now)
    {
        History.Record(original, converted, now);
        _lastOriginalScript = direction == ConversionDirection.LatinToPersian ? LayoutScript.Latin : LayoutScript.Persian;
    }

    private static LayoutScript OutputScriptOf(ConversionDirection direction) =>
        direction == ConversionDirection.LatinToPersian ? LayoutScript.Persian : LayoutScript.Latin;
}
