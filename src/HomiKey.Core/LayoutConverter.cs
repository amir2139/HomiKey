using System.Text;

namespace HomiKey.Core;

/// <summary>Which way a piece of text is converted.</summary>
public enum ConversionDirection
{
    /// <summary>Text typed on the Latin layout that was meant to be Persian ("sghl" → "سلام").</summary>
    LatinToPersian,

    /// <summary>Text typed on the Persian layout that was meant to be Latin ("اثممخ" → "hello").</summary>
    PersianToLatin,
}

/// <summary>Options that change how characters are mapped.</summary>
public sealed record ConversionOptions
{
    /// <summary>Default options: Persian layout is ISIRI 9147, digits are converted.</summary>
    public static ConversionOptions Default { get; } = new();

    /// <summary>The Persian layout the user types with.</summary>
    public PersianLayoutKind PersianLayout { get; init; } = PersianLayoutKind.Standard;

    /// <summary>When false, 0–9 and ۰–۹ are left as they are.</summary>
    public bool ConvertDigits { get; init; } = true;
}

/// <summary>
/// Converts text between US QWERTY and a Persian layout by mapping each character
/// back to the physical key that typed it and then forward to what the other layout
/// would have produced. Characters that no key produces are left untouched, so
/// mixed text only has its wrong-layout part changed.
/// </summary>
public sealed class LayoutConverter
{
    private static readonly string[] ArabicIndicDigits = ["٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩"];

    private readonly Dictionary<char, string> _latinToPersian = new();
    private readonly Dictionary<string, string> _persianToLatin = new(StringComparer.Ordinal);
    private readonly int _longestPersianOutput;

    public LayoutConverter(ConversionOptions? options = null)
    {
        Options = options ?? ConversionOptions.Default;
        var latin = Layouts.UsQwerty;
        var persian = Layouts.Persian(Options.PersianLayout);

        // Latin character -> the key that typed it -> what the Persian layout puts there.
        var keyOfLatin = new Dictionary<char, char>();
        foreach (var (key, output) in latin.Keys)
            if (output.Length == 1) keyOfLatin.TryAdd(output[0], key);

        foreach (var (latinChar, key) in keyOfLatin)
        {
            if (!Options.ConvertDigits && char.IsAsciiDigit(latinChar)) continue;
            if (persian.TryGetOutput(key, out var output) && output.Length > 0)
                _latinToPersian[latinChar] = output;
        }

        // Persian output -> the key that typed it -> the Latin character. The selected
        // layout wins; the other Persian layout fills in characters it never produces.
        AddReverse(persian, latin, isFallback: false);
        AddReverse(Layouts.OtherPersian(Options.PersianLayout), latin, isFallback: true);
        if (Options.ConvertDigits)
        {
            for (var d = 0; d < 10; d++)
                _persianToLatin.TryAdd(ArabicIndicDigits[d], ((char)('0' + d)).ToString());
        }

        _longestPersianOutput = _persianToLatin.Keys.Max(k => k.Length);
    }

    /// <summary>The options this converter was built with.</summary>
    public ConversionOptions Options { get; }

    /// <summary>
    /// Converts <paramref name="text"/> in the direction detected from its characters.
    /// </summary>
    public string Convert(string text, out ConversionDirection direction)
    {
        direction = DirectionDetector.Detect(text);
        return Convert(text, direction);
    }

    /// <summary>Converts <paramref name="text"/> in the given direction.</summary>
    public string Convert(string text, ConversionDirection direction) => direction switch
    {
        ConversionDirection.LatinToPersian => LatinToPersian(text),
        ConversionDirection.PersianToLatin => PersianToLatin(text),
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };

    /// <summary>"sghl" → "سلام".</summary>
    public string LatinToPersian(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (_latinToPersian.TryGetValue(c, out var mapped)) sb.Append(mapped);
            else sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>"اثممخ" → "hello".</summary>
    public string PersianToLatin(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var sb = new StringBuilder(text.Length);
        var i = 0;
        while (i < text.Length)
        {
            var matched = false;
            // Longest match first: the legacy layout has a key that types the whole word "ريال".
            for (var len = Math.Min(_longestPersianOutput, text.Length - i); len >= 1; len--)
            {
                if (_persianToLatin.TryGetValue(text.Substring(i, len), out var mapped))
                {
                    sb.Append(mapped);
                    i += len;
                    matched = true;
                    break;
                }
            }
            if (!matched)
            {
                sb.Append(text[i]);
                i++;
            }
        }
        return sb.ToString();
    }

    private void AddReverse(KeyboardLayout persian, KeyboardLayout latin, bool isFallback)
    {
        foreach (var (key, output) in persian.Keys)
        {
            if (output.Length == 0) continue;
            // ASCII the selected layout cannot type came from somewhere else; leave it alone.
            if (isFallback && output.All(char.IsAscii)) continue;
            if (!Options.ConvertDigits && output.Length == 1 && IsPersianDigit(output[0])) continue;
            if (!latin.TryGetOutput(key, out var latinOutput) || latinOutput.Length == 0) continue;
            _persianToLatin.TryAdd(output, latinOutput);
        }
    }

    private static bool IsPersianDigit(char c) => c is >= '۰' and <= '۹';
}
