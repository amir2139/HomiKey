namespace HomiKey.Core;

/// <summary>
/// A keyboard layout described by what each physical key produces.
/// Keys are identified by the character they produce on US QWERTY
/// (so "q" is the key left of "w", "Q" is the same key with Shift).
/// </summary>
public sealed class KeyboardLayout
{
    /// <summary>The 47 printable keys on US QWERTY, unshifted, in row order.</summary>
    public const string UsUnshifted = "`1234567890-=qwertyuiop[]\\asdfghjkl;'zxcvbnm,./";

    /// <summary>The same 47 keys with Shift held.</summary>
    public const string UsShifted = "~!@#$%^&*()_+QWERTYUIOP{}|ASDFGHJKL:\"ZXCVBNM<>?";

    private readonly Dictionary<char, string> _byKey;

    public KeyboardLayout(string id, LayoutScript script, IReadOnlyList<string> unshifted, IReadOnlyList<string> shifted)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        if (unshifted.Count != UsUnshifted.Length)
            throw new ArgumentException($"Expected {UsUnshifted.Length} unshifted keys, got {unshifted.Count}.", nameof(unshifted));
        if (shifted.Count != UsShifted.Length)
            throw new ArgumentException($"Expected {UsShifted.Length} shifted keys, got {shifted.Count}.", nameof(shifted));

        Id = id;
        Script = script;
        _byKey = new Dictionary<char, string>(UsUnshifted.Length * 2);
        for (var i = 0; i < UsUnshifted.Length; i++)
        {
            _byKey[UsUnshifted[i]] = unshifted[i];
            _byKey[UsShifted[i]] = shifted[i];
        }
    }

    /// <summary>Stable identifier, e.g. "fa-isiri9147".</summary>
    public string Id { get; }

    /// <summary>Which script the layout types.</summary>
    public LayoutScript Script { get; }

    /// <summary>
    /// What this layout produces for the key that types <paramref name="usKey"/> on US QWERTY.
    /// An empty string means the key produces nothing in this layout.
    /// </summary>
    public bool TryGetOutput(char usKey, out string output) => _byKey.TryGetValue(usKey, out output!);

    /// <summary>All (US key, output) pairs, unshifted keys first.</summary>
    public IEnumerable<KeyValuePair<char, string>> Keys
    {
        get
        {
            foreach (var key in UsUnshifted) yield return new(key, _byKey[key]);
            foreach (var key in UsShifted) yield return new(key, _byKey[key]);
        }
    }

    public override string ToString() => Id;
}

/// <summary>The script a layout types.</summary>
public enum LayoutScript
{
    Latin,
    Persian,
}
