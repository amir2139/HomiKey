namespace HomiKey.Core;

/// <summary>
/// Remembers the last conversion so that pressing the hotkey again puts back exactly
/// what was there before, rather than converting the result a second time.
/// </summary>
public sealed class ConversionHistory
{
    private string? _original;
    private string? _converted;
    private DateTimeOffset _at;

    /// <param name="window">How long after a conversion a second press still reverts it.</param>
    public ConversionHistory(TimeSpan? window = null)
    {
        Window = window ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>How long after a conversion a second press still reverts it.</summary>
    public TimeSpan Window { get; }

    /// <summary>Whether a conversion is remembered and still inside the window.</summary>
    public bool HasRecent(DateTimeOffset now)
    {
        var elapsed = now - _at;
        // A negative elapsed time means the clock was set back; treat that as expired.
        return _converted is not null && elapsed >= TimeSpan.Zero && elapsed <= Window;
    }

    /// <summary>Remembers that <paramref name="original"/> was replaced by <paramref name="converted"/>.</summary>
    public void Record(string original, string converted, DateTimeOffset now)
    {
        _original = original;
        _converted = converted;
        _at = now;
    }

    /// <summary>Forgets the last conversion.</summary>
    public void Clear()
    {
        _original = _converted = null;
    }

    /// <summary>
    /// When <paramref name="selected"/> is exactly what the last conversion produced,
    /// gives back what it replaced.
    /// </summary>
    public bool TryRevertSelection(string selected, DateTimeOffset now, out string original)
    {
        original = string.Empty;
        if (!HasRecent(now) || selected.Length == 0 || !string.Equals(selected, _converted, StringComparison.Ordinal))
            return false;
        original = _original!;
        return true;
    }

    /// <summary>
    /// When the text before the caret ends with what the last conversion produced,
    /// gives back the same text with that ending put back as it was.
    /// </summary>
    public bool TryRevertBeforeCaret(string textBeforeCaret, DateTimeOffset now, out string replacement)
    {
        replacement = textBeforeCaret;
        if (!HasRecent(now) || _converted!.Length == 0 || !textBeforeCaret.EndsWith(_converted, StringComparison.Ordinal))
            return false;
        replacement = string.Concat(textBeforeCaret.AsSpan(0, textBeforeCaret.Length - _converted.Length), _original);
        return true;
    }
}
