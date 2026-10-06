namespace HomiKey.Core;

/// <summary>Which Persian layout the user types with.</summary>
public enum PersianLayoutKind
{
    /// <summary>Iranian national standard ISIRI 9147 ("Persian (Standard)" in Windows).</summary>
    Standard,

    /// <summary>The older Microsoft layout ("Persian" in Windows).</summary>
    Legacy,
}

/// <summary>The built-in layouts.</summary>
public static class Layouts
{
    private const string Zwnj = "\u200C";
    private const string Zwj = "\u200D";

    /// <summary>US QWERTY. Every key produces itself.</summary>
    public static KeyboardLayout UsQwerty { get; } = new(
        "en-us",
        LayoutScript.Latin,
        KeyboardLayout.UsUnshifted.Select(c => c.ToString()).ToArray(),
        KeyboardLayout.UsShifted.Select(c => c.ToString()).ToArray());

    /// <summary>ISIRI 9147, the Iranian standard Persian layout.</summary>
    public static KeyboardLayout PersianStandard { get; } = new(
        "fa-isiri9147",
        LayoutScript.Persian,
        [
            // ` 1 2 3 4 5 6 7 8 9 0 - =
            Zwj, "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹", "۰", "-", "=",
            // q w e r t y u i o p [ ] \
            "ض", "ص", "ث", "ق", "ف", "غ", "ع", "ه", "خ", "ح", "ج", "چ", "\\",
            // a s d f g h j k l ; '
            "ش", "س", "ی", "ب", "ل", "ا", "ت", "ن", "م", "ک", "گ",
            // z x c v b n m , . /
            "ظ", "ط", "ز", "ر", "ذ", "د", "پ", "و", ".", "/",
        ],
        [
            // ~ ! @ # $ % ^ & * ( ) _ +
            "÷", "!", "\u066C", "\u066B", "\uFDFC", "\u066A", "×", "،", "*", ")", "(", "ـ", "+",
            // Q W E R T Y U I O P { } |
            "\u0652", "\u064C", "\u064D", "\u064B", "\u064F", "\u0650", "\u064E", "\u0651", "]", "[", "}", "{", "|",
            // A S D F G H J K L : "
            "ؤ", "ئ", "ي", "إ", "أ", "آ", "ة", "»", "«", ":", "؛",
            // Z X C V B N M < > ?
            "ك", "\u0653", "ژ", "\u0670", Zwnj, "\u0654", "ء", ">", "<", "؟",
        ]);

    /// <summary>The legacy Microsoft Persian layout.</summary>
    public static KeyboardLayout PersianLegacy { get; } = new(
        "fa-legacy",
        LayoutScript.Persian,
        [
            // ` 1 2 3 4 5 6 7 8 9 0 - =
            Zwj, "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹", "۰", "-", "=",
            // q w e r t y u i o p [ ] \
            "ض", "ص", "ث", "ق", "ف", "غ", "ع", "ه", "خ", "ح", "ج", "چ", "پ",
            // a s d f g h j k l ; '
            "ش", "س", "ی", "ب", "ل", "ا", "ت", "ن", "م", "ک", "گ",
            // z x c v b n m , . /
            "ظ", "ط", "ز", "ر", "ذ", "د", "ئ", "و", ".", "/",
        ],
        [
            // ~ ! @ # $ % ^ & * ( ) _ +
            "÷", "!", "@", "#", "$", "%", "^", "&", "*", ")", "(", "_", "+",
            // Q W E R T Y U I O P { } |
            "\u064B", "\u064C", "\u064D", "ريال", "،", "؛", ",", "]", "[", "\\", "}", "{", "|",
            // A S D F G H J K L : "
            "\u064E", "\u064F", "\u0650", "\u0651", "ۀ", "آ", "ـ", "«", "»", ":", "\"",
            // Z X C V B N M < > ?
            "ة", "ي", "ژ", "ؤ", "إ", "أ", "ء", ">", "<", "؟",
        ]);

    /// <summary>The Persian layout of the given kind.</summary>
    public static KeyboardLayout Persian(PersianLayoutKind kind) => kind switch
    {
        PersianLayoutKind.Standard => PersianStandard,
        PersianLayoutKind.Legacy => PersianLegacy,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>The Persian layout that is not <paramref name="kind"/>.</summary>
    public static KeyboardLayout OtherPersian(PersianLayoutKind kind) =>
        kind == PersianLayoutKind.Standard ? PersianLegacy : PersianStandard;
}
