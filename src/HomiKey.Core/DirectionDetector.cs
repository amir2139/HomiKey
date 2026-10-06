namespace HomiKey.Core;

/// <summary>Decides which way text should be converted from the characters it contains.</summary>
public static class DirectionDetector
{
    /// <summary>
    /// Persian characters outnumbering Latin letters means the text was typed on the
    /// Persian layout by mistake, so it goes to Latin; anything else goes to Persian.
    /// </summary>
    public static ConversionDirection Detect(string text, ConversionDirection whenUndecided = ConversionDirection.LatinToPersian)
    {
        var (latin, persian) = Count(text);
        if (persian > latin) return ConversionDirection.PersianToLatin;
        if (latin > persian) return ConversionDirection.LatinToPersian;
        return whenUndecided;
    }

    /// <summary>The share of Persian characters among the letters of <paramref name="text"/>, 0–1.</summary>
    public static double PersianRatio(string text)
    {
        var (latin, persian) = Count(text);
        var total = latin + persian;
        return total == 0 ? 0 : (double)persian / total;
    }

    /// <summary>Counts Latin letters and Persian/Arabic characters (letters, digits, marks, ZWNJ).</summary>
    public static (int Latin, int Persian) Count(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        int latin = 0, persian = 0;
        foreach (var c in text)
        {
            if (char.IsAsciiLetter(c)) latin++;
            else if (IsPersian(c)) persian++;
        }
        return (latin, persian);
    }

    /// <summary>True for characters of the Arabic script blocks and for ZWNJ.</summary>
    public static bool IsPersian(char c) =>
        c is (>= '\u0600' and <= '\u06FF')
          or (>= '\u0750' and <= '\u077F')
          or (>= '\uFB50' and <= '\uFDFF')
          or (>= '\uFE70' and <= '\uFEFC')
          or '\u200C';
}
