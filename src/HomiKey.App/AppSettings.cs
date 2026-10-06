using System.Text.Json;
using System.Text.Json.Serialization;
using HomiKey.App.Native;
using HomiKey.Core;

namespace HomiKey.App;

/// <summary>User settings, stored as JSON in %APPDATA%\HomiKey\settings.json.</summary>
public sealed class AppSettings
{
    public const string LanguageSystem = "system";
    public const string LanguageEnglish = "en";
    public const string LanguagePersian = "fa";

    public static readonly Hotkey DefaultConvertHotkey = new(HotkeyModifiers.Control | HotkeyModifiers.Shift, Keys.Space);
    public static readonly Hotkey DefaultLastWordHotkey = new(HotkeyModifiers.None, Keys.Pause);

    /// <summary>Terminals treat Ctrl+C as "stop the running program", so HomiKey stays out of them by default.</summary>
    public static readonly string[] DefaultExcludedApps =
    [
        "windowsterminal.exe", "cmd.exe", "powershell.exe", "pwsh.exe", "conhost.exe",
        "mintty.exe", "putty.exe", "keepass.exe", "keepassxc.exe",
    ];

    /// <summary>"system", "en" or "fa".</summary>
    public string Language { get; set; } = LanguageSystem;

    public bool Enabled { get; set; } = true;

    public string ConvertHotkey { get; set; } = DefaultConvertHotkey.ToString();

    /// <summary>Empty means no last-word hotkey.</summary>
    public string LastWordHotkey { get; set; } = DefaultLastWordHotkey.ToString();

    /// <summary>When the convert hotkey finds no selection, convert the last typed word instead.</summary>
    public bool LastWordWhenNothingSelected { get; set; } = true;

    /// <summary>Null until detected from the installed layouts on first run.</summary>
    public PersianLayoutKind? PersianLayout { get; set; }

    public bool ConvertDigits { get; set; } = true;

    public bool SwitchInputLanguage { get; set; }

    public List<string> ExcludedApps { get; set; } = [.. DefaultExcludedApps];

    public bool WelcomeShown { get; set; }

    [JsonIgnore]
    public Hotkey ConvertHotkeyValue => Hotkey.Parse(ConvertHotkey, DefaultConvertHotkey);

    [JsonIgnore]
    public Hotkey LastWordHotkeyValue => Hotkey.Parse(LastWordHotkey, Hotkey.None);

    public ConversionOptions ToConversionOptions() => new()
    {
        PersianLayout = PersianLayout ?? PersianLayoutKind.Standard,
        ConvertDigits = ConvertDigits,
    };

    public bool IsExcluded(string? processName) =>
        processName is not null &&
        ExcludedApps.Any(e => string.Equals(ForegroundApp.Normalize(e), ForegroundApp.Normalize(processName), StringComparison.Ordinal));

    public AppSettings Clone() => JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(this, SettingsStore.JsonOptions), SettingsStore.JsonOptions)!;
}

/// <summary>Loads and saves <see cref="AppSettings"/>. Nothing leaves the machine.</summary>
public static class SettingsStore
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Directory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HomiKey");

    public static string FilePath => Path.Combine(Directory, "settings.json");

    /// <summary>Reads the settings; a missing or unreadable file gives the defaults.</summary>
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
        }
        return new AppSettings();
    }

    /// <summary>Writes the settings atomically.</summary>
    public static void Save(AppSettings settings)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temp, FilePath, overwrite: true);
    }
}
