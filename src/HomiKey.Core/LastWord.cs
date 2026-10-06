namespace HomiKey.Core;

/// <summary>Finds the last typed word in the text before the caret.</summary>
public static class LastWord
{
    /// <summary>
    /// The span of the last word: the trailing run of non-whitespace characters,
    /// ignoring whitespace typed after it. ZWNJ is part of a word, not a separator.
    /// Returns a zero-length span when there is no word.
    /// </summary>
    public static (int Start, int Length) Find(string textBeforeCaret)
    {
        ArgumentNullException.ThrowIfNull(textBeforeCaret);
        var end = textBeforeCaret.Length;
        while (end > 0 && IsSeparator(textBeforeCaret[end - 1])) end--;
        var start = end;
        while (start > 0 && !IsSeparator(textBeforeCaret[start - 1])) start--;
        return (start, end - start);
    }

    /// <summary>
    /// Replaces the last word of <paramref name="textBeforeCaret"/> with the result of
    /// <paramref name="convert"/>, keeping everything before it and the whitespace after it.
    /// <paramref name="originalTail"/> and <paramref name="convertedTail"/> are the changed
    /// ending (word plus trailing whitespace) before and after. Returns false when there is no word.
    /// </summary>
    public static bool TryReplace(string textBeforeCaret, Func<string, string> convert, out string result, out string originalTail, out string convertedTail)
    {
        var (start, length) = Find(textBeforeCaret);
        if (length == 0)
        {
            result = textBeforeCaret;
            originalTail = convertedTail = string.Empty;
            return false;
        }
        var word = textBeforeCaret.Substring(start, length);
        var trailing = textBeforeCaret[(start + length)..];
        originalTail = word + trailing;
        convertedTail = convert(word) + trailing;
        result = string.Concat(textBeforeCaret.AsSpan(0, start), convertedTail);
        return true;
    }

    private static bool IsSeparator(char c) => char.IsWhiteSpace(c) && c != '\u200C';
}
