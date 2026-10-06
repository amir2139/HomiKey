namespace HomiKey.App.Native;

/// <summary>A hidden message-only window that owns the global hotkeys.</summary>
internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private static readonly IntPtr HwndMessage = new(-3);
    private readonly HashSet<int> _registered = new();

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams { Parent = HwndMessage });
    }

    /// <summary>Raised on the UI thread with the id the hotkey was registered under.</summary>
    public event Action<int>? HotkeyPressed;

    /// <summary>Registers <paramref name="hotkey"/> under <paramref name="id"/>, replacing what was there.</summary>
    /// <returns>False when Windows refused, usually because another program owns the combination.</returns>
    public bool Register(int id, Hotkey hotkey)
    {
        Unregister(id);
        if (hotkey.IsNone) return true;
        if (!NativeMethods.RegisterHotKey(Handle, id, hotkey.NativeModifiers, (uint)hotkey.Key)) return false;
        _registered.Add(id);
        return true;
    }

    public void Unregister(int id)
    {
        if (_registered.Remove(id)) NativeMethods.UnregisterHotKey(Handle, id);
    }

    public void UnregisterAll()
    {
        foreach (var id in _registered.ToArray()) Unregister(id);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
        {
            HotkeyPressed?.Invoke(m.WParam.ToInt32());
            return;
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        UnregisterAll();
        DestroyHandle();
    }
}
