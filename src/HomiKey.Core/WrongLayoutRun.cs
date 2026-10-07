namespace HomiKey.Core;

/// <summary>
/// Finds the words at the end of the text before the caret that were all typed on the wrong
/// layout, so they can be converted together ("چطوری sghl phgj" → "چطوری سلام حالت").
/// </summary>
public static class WrongLayoutRun
{
    /// <summary>
    /// The span from the first to the last of those words, and the direction to convert it in.
    /// The last word decides the direction. Going back from it, a word joins the run while it is
    /// in the same script; the run stops at a word in the other script, at the start of the line,
    /// or at a word ending in punctuation the conversion leaves unchanged (such as "." or "!").
    /// Whitespace after the last word is not part of the span. Returns a zero-length span when
    /// there is no word.
    /// </summary>
    public static (int Start, int Length, ConversionDirection Direction) Find(string textBeforeCaret, LayoutConverter converter)
    {
        ArgumentNullException.ThrowIfNull(textBeforeCaret);
        ArgumentNullException.ThrowIfNull(converter);

        var (lastStart, lastLength) = LastWord.Find(textBeforeCaret);
        if (lastLength == 0) return (0, 0, ConversionDirection.LatinToPersian);

        var direction = DirectionDetector.Detect(textBeforeCaret.Substring(lastStart, lastLength));
        var start = lastStart;
        var end = lastStart + lastLength;

        while (true)
        {
            var i = start;
            while (i > 0 && IsSeparator(textBeforeCaret[i - 1]))
            {
                if (textBeforeCaret[i - 1] is '\n' or '\r') return (start, end - start, direction);
                i--;
            }
            if (i == 0) break;

            var wordEnd = i;
            while (i > 0 && !IsSeparator(textBeforeCaret[i - 1])) i--;
            var word = textBeforeCaret[i..wordEnd];

            if (EndsSentence(word, direction, converter)) break;
            if (DirectionDetector.Detect(word, whenUndecided: direction) != direction) break;
            start = i;
        }

        return (start, end - start, direction);
    }

    /// <summary>
    /// Converts the run at the end of <paramref name="textBeforeCaret"/>, keeping everything
    /// before it and the whitespace after it. <paramref name="originalTail"/> and
    /// <paramref name="convertedTail"/> are the changed ending (run plus trailing whitespace)
    /// before and after. Returns false when there is no word.
    /// </summary>
    public static bool TryReplace(string textBeforeCaret, LayoutConverter converter, out string result, out string originalTail, out string convertedTail, out ConversionDirection direction)
    {
        (var start, var length, direction) = Find(textBeforeCaret, converter);
        if (length == 0)
        {
            result = textBeforeCaret;
            originalTail = convertedTail = string.Empty;
            return false;
        }
        var run = textBeforeCaret.Substring(start, length);
        var trailing = textBeforeCaret[(start + length)..];
        originalTail = run + trailing;
        convertedTail = converter.Convert(run, direction) + trailing;
        result = string.Concat(textBeforeCaret.AsSpan(0, start), convertedTail);
        return true;
    }

    // Punctuation that comes out the same after conversion was typed on purpose, so a word
    // ending in it closes the previous sentence. Punctuation that converts ("?" → "؟",
    // "," → "و") belongs to the wrong-layout text and does not stop the run.
    private static bool EndsSentence(string word, ConversionDirection direction, LayoutConverter converter)
    {
        var last = word[^1].ToString();
        return char.IsPunctuation(word[^1]) && converter.Convert(last, direction) == last;
    }

    private static bool IsSeparator(char c) => char.IsWhiteSpace(c);
}
