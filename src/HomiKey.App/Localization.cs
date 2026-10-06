using System.Drawing.Text;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using HomiKey.App.Native;
using HomiKey.App.Resources;

namespace HomiKey.App;

/// <summary>
/// Interface language: picks the culture, tells open windows to re-read their strings,
/// and supplies the bundled Vazirmatn font for the Persian interface.
/// </summary>
internal static class Localization
{
    private static readonly CultureInfo SystemUiCulture = CultureInfo.CurrentUICulture;
    private static readonly PrivateFontCollection Fonts = new();
    private static FontFamily? _vazirmatn;

    /// <summary>Raised after the interface language changes.</summary>
    public static event Action? Changed;

    public static bool IsRightToLeft => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fa";

    /// <summary>"system", "en" or "fa"; "system" follows the Windows display language.</summary>
    public static void Apply(string language)
    {
        var useFarsi = language switch
        {
            AppSettings.LanguagePersian => true,
            AppSettings.LanguageEnglish => false,
            _ => SystemUiCulture.TwoLetterISOLanguageName == "fa",
        };
        var culture = CultureInfo.GetCultureInfo(useFarsi ? "fa-IR" : "en-US");
        if (Equals(CultureInfo.CurrentUICulture, culture) && Equals(CultureInfo.DefaultThreadCurrentUICulture, culture)) return;

        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Strings.Culture = culture;
        Changed?.Invoke();
    }

    /// <summary>The interface font: Vazirmatn for Persian, the Windows UI font otherwise.</summary>
    public static Font UiFont(float? size = null, FontStyle style = FontStyle.Regular)
    {
        var baseFont = SystemFonts.MessageBoxFont ?? Control.DefaultFont;
        var points = size ?? baseFont.SizeInPoints;
        if (IsRightToLeft && Vazirmatn() is { } family && family.IsStyleAvailable(style))
            return new Font(family, points, style, GraphicsUnit.Point);
        return new Font(baseFont.FontFamily, points, style, GraphicsUnit.Point);
    }

    /// <summary>Sets direction and font on a form; its controls inherit both.</summary>
    public static void ApplyTo(Form form)
    {
        form.RightToLeft = IsRightToLeft ? RightToLeft.Yes : RightToLeft.No;
        form.RightToLeftLayout = IsRightToLeft;
        form.Font = UiFont();
    }

    /// <summary>Sets direction and font on a menu.</summary>
    public static void ApplyTo(ToolStrip menu)
    {
        menu.RightToLeft = IsRightToLeft ? RightToLeft.Yes : RightToLeft.No;
        menu.Font = UiFont();
    }

    /// <summary>A message box that reads right to left in Persian.</summary>
    public static DialogResult Message(string text, MessageBoxIcon icon = MessageBoxIcon.Information, MessageBoxButtons buttons = MessageBoxButtons.OK)
    {
        var options = IsRightToLeft ? MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0;
        return MessageBox.Show(text, Strings.AppName, buttons, icon, MessageBoxDefaultButton.Button1, options);
    }

    private static FontFamily? Vazirmatn()
    {
        if (_vazirmatn is not null) return _vazirmatn;
        foreach (var name in new[] { "HomiKey.Assets.Vazirmatn-Regular.ttf", "HomiKey.Assets.Vazirmatn-Bold.ttf" })
            LoadFont(name);
        _vazirmatn = Fonts.Families.FirstOrDefault(f => f.Name.StartsWith("Vazirmatn", StringComparison.OrdinalIgnoreCase));
        return _vazirmatn;
    }

    private static void LoadFont(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream is null) return;
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);

        // The memory must stay alive for as long as the font is used, i.e. the whole process.
        var memory = Marshal.AllocCoTaskMem(bytes.Length);
        Marshal.Copy(bytes, 0, memory, bytes.Length);
        Fonts.AddMemoryFont(memory, bytes.Length);
        // GDI text rendering (TextRenderer, used by most controls) only sees fonts added this way.
        NativeMethods.AddFontMemResourceEx(memory, (uint)bytes.Length, IntPtr.Zero, out _);
    }
}
