using HomiKey.App.Native;

namespace HomiKey.App;

/// <summary>Modifier keys of a global hotkey. Values match the Win32 MOD_* flags.</summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8,
}

/// <summary>A global hotkey such as Ctrl+Shift+Space. Stored in settings as that text.</summary>
public readonly record struct Hotkey(HotkeyModifiers Modifiers, Keys Key)
{
    public static Hotkey None => default;

    public bool IsNone => Key == Keys.None;

    /// <summary>
    /// A hotkey must not be an ordinary typing key: it needs Ctrl, Alt or Win, or must be a key
    /// that does not type anything (function keys, Pause, Scroll Lock…).
    /// </summary>
    public bool IsValid =>
        !IsNone && !IsModifierKey(Key) &&
        ((Modifiers & (HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Win)) != 0 || IsStandaloneKey(Key));

    public uint NativeModifiers => (uint)Modifiers | NativeMethods.MOD_NOREPEAT;

    public override string ToString()
    {
        if (IsNone) return string.Empty;
        var parts = new List<string>(5);
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        parts.Add(KeyName(Key));
        return string.Join("+", parts);
    }

    /// <summary>Parses text like "Ctrl+Shift+Space". Empty text is <see cref="None"/>.</summary>
    public static bool TryParse(string? text, out Hotkey hotkey)
    {
        hotkey = None;
        if (string.IsNullOrWhiteSpace(text)) return true;

        var modifiers = HotkeyModifiers.None;
        var key = Keys.None;
        foreach (var raw in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (raw.ToUpperInvariant())
            {
                case "CTRL" or "CONTROL": modifiers |= HotkeyModifiers.Control; break;
                case "ALT": modifiers |= HotkeyModifiers.Alt; break;
                case "SHIFT": modifiers |= HotkeyModifiers.Shift; break;
                case "WIN" or "WINDOWS": modifiers |= HotkeyModifiers.Win; break;
                default:
                    if (key != Keys.None || !TryParseKey(raw, out key)) return false;
                    break;
            }
        }
        if (key == Keys.None) return false;
        hotkey = new Hotkey(modifiers, key);
        return true;
    }

    public static Hotkey Parse(string? text, Hotkey fallback) => TryParse(text, out var h) ? h : fallback;

    public static bool IsModifierKey(Keys key) => key is
        Keys.ControlKey or Keys.LControlKey or Keys.RControlKey or
        Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey or
        Keys.Menu or Keys.LMenu or Keys.RMenu or
        Keys.LWin or Keys.RWin;

    private static bool IsStandaloneKey(Keys key) =>
        key is (>= Keys.F1 and <= Keys.F24) or Keys.Pause or Keys.Scroll or Keys.Insert;

    private static string KeyName(Keys key) => key switch
    {
        >= Keys.D0 and <= Keys.D9 => ((char)('0' + (key - Keys.D0))).ToString(),
        Keys.Oemtilde => "`",
        Keys.OemMinus => "-",
        Keys.Oemplus => "=",
        Keys.OemOpenBrackets => "[",
        Keys.OemCloseBrackets => "]",
        Keys.OemPipe => "\\",
        Keys.OemSemicolon => ";",
        Keys.OemQuotes => "'",
        Keys.Oemcomma => ",",
        Keys.OemPeriod => ".",
        Keys.OemQuestion => "/",
        Keys.Next => "PageDown",
        Keys.Prior => "PageUp",
        _ => key.ToString(),
    };

    private static bool TryParseKey(string text, out Keys key)
    {
        key = text switch
        {
            "`" => Keys.Oemtilde,
            "-" => Keys.OemMinus,
            "=" => Keys.Oemplus,
            "[" => Keys.OemOpenBrackets,
            "]" => Keys.OemCloseBrackets,
            "\\" => Keys.OemPipe,
            ";" => Keys.OemSemicolon,
            "'" => Keys.OemQuotes,
            "," => Keys.Oemcomma,
            "." => Keys.OemPeriod,
            "/" => Keys.OemQuestion,
            _ => Keys.None,
        };
        if (key != Keys.None) return true;
        if (text.Length == 1 && char.IsAsciiDigit(text[0]))
        {
            key = Keys.D0 + (text[0] - '0');
            return true;
        }
        return Enum.TryParse(text, ignoreCase: true, out key) && key != Keys.None && !IsModifierKey(key)
            && (key & Keys.Modifiers) == 0;
    }
}
