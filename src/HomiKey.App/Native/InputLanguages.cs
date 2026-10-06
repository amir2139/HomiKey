using System.Text;
using HomiKey.Core;

namespace HomiKey.App.Native;

/// <summary>Reads and switches the Windows input language.</summary>
internal static class InputLanguages
{
    private const int LangPersian = 0x29;
    private const int LangEnglish = 0x09;

    // Keyboard layout identifiers (KLIDs) of the two Persian layouts.
    private const string KlidPersianLegacy = "00000429";
    private const string KlidPersianStandard = "00050429";

    /// <summary>
    /// Asks the window to switch to the first installed input language of the given script.
    /// Does nothing when no such language is installed.
    /// </summary>
    public static void SwitchTo(IntPtr window, LayoutScript script)
    {
        if (window == IntPtr.Zero) return;
        var wanted = script == LayoutScript.Persian ? LangPersian : LangEnglish;
        var hkl = Installed().FirstOrDefault(h => PrimaryLanguage(h) == wanted);
        if (hkl != IntPtr.Zero)
            NativeMethods.PostMessage(window, NativeMethods.WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, hkl);
    }

    /// <summary>
    /// Which Persian layout the user has installed, or null when there is none.
    /// When both are installed, the one listed first wins.
    /// </summary>
    public static PersianLayoutKind? DetectPersianLayout()
    {
        var original = NativeMethods.GetKeyboardLayout(0);
        try
        {
            foreach (var hkl in Installed().Where(h => PrimaryLanguage(h) == LangPersian))
            {
                // The layout name is only readable for the active layout, so briefly activate
                // each Persian one on HomiKey's own thread. Other apps are not affected.
                NativeMethods.ActivateKeyboardLayout(hkl, 0);
                var name = new StringBuilder(NativeMethods.KL_NAMELENGTH);
                if (!NativeMethods.GetKeyboardLayoutName(name)) continue;
                if (string.Equals(name.ToString(), KlidPersianStandard, StringComparison.OrdinalIgnoreCase)) return PersianLayoutKind.Standard;
                if (string.Equals(name.ToString(), KlidPersianLegacy, StringComparison.OrdinalIgnoreCase)) return PersianLayoutKind.Legacy;
            }
            return null;
        }
        finally
        {
            NativeMethods.ActivateKeyboardLayout(original, 0);
        }
    }

    private static IntPtr[] Installed()
    {
        var count = NativeMethods.GetKeyboardLayoutList(0, null);
        if (count <= 0) return [];
        var list = new IntPtr[count];
        count = NativeMethods.GetKeyboardLayoutList(count, list);
        return list[..Math.Max(0, count)];
    }

    private static int PrimaryLanguage(IntPtr hkl) => (int)((long)hkl & 0x3FF);
}
