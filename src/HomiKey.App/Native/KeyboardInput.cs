using System.Runtime.InteropServices;
using static HomiKey.App.Native.NativeMethods;

namespace HomiKey.App.Native;

/// <summary>Synthesizes key presses in whatever app has focus.</summary>
internal static class KeyboardInput
{
    private static readonly ushort[] ModifierKeys = [VK_SHIFT, VK_CONTROL, VK_MENU, VK_LWIN, VK_RWIN];

    /// <summary>
    /// Waits until the user lets go of the hotkey. Sending Ctrl+C while Shift is still held
    /// would arrive as Ctrl+Shift+C, which many apps treat differently.
    /// </summary>
    public static async Task WaitForKeysReleasedAsync(Keys hotkeyKey, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (!ModifierKeys.Any(IsDown) && (hotkeyKey == Keys.None || !IsDown((ushort)hotkeyKey))) return;
            await Task.Delay(15);
        }
        // Still held after the timeout: release the modifiers ourselves so the copy is clean.
        var ups = ModifierKeys.Where(IsDown).Select(vk => Key(vk, up: true)).ToArray();
        if (ups.Length > 0) Send(ups);
    }

    public static void CtrlC() => Chord(VK_CONTROL, VK_C);

    public static void CtrlV() => Chord(VK_CONTROL, VK_V);

    /// <summary>Shift+Home: selects from the caret to the start of the line.</summary>
    public static void ShiftHome() => Chord(VK_SHIFT, VK_HOME);

    private static void Chord(ushort modifier, ushort key) =>
        Send(Key(modifier, up: false), Key(key, up: false), Key(key, up: true), Key(modifier, up: true));

    private static bool IsDown(ushort vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    private static INPUT Key(ushort vk, bool up)
    {
        var flags = up ? KEYEVENTF_KEYUP : 0;
        if (IsExtended(vk)) flags |= KEYEVENTF_EXTENDEDKEY;
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    wScan = (ushort)MapVirtualKey(vk, MAPVK_VK_TO_VSC),
                    dwFlags = flags,
                },
            },
        };
    }

    // Navigation keys share scan codes with the numeric keypad; the flag says which one is meant.
    private static bool IsExtended(ushort vk) => vk is >= 0x21 and <= 0x2E || vk is VK_LWIN or VK_RWIN;

    private static void Send(params INPUT[] inputs) =>
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
}
