using HomiKey.App.Native;
using HomiKey.App.Resources;

namespace HomiKey.App.Controls;

/// <summary>A text box that records a key combination instead of text.</summary>
internal sealed class HotkeyBox : TextBox
{
    private Hotkey _value;
    private bool _recording;

    public HotkeyBox()
    {
        ShortcutsEnabled = false;
        ContextMenuStrip = new ContextMenuStrip();
        TextAlign = HorizontalAlignment.Center;
        // Key names are Latin and read left to right in both interface languages.
        RightToLeft = RightToLeft.No;
        UpdateText();
    }

    public event EventHandler? ValueChanged;

    public Hotkey Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            UpdateText();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Re-reads the "None" and prompt texts after a language change.</summary>
    public void RefreshText() => UpdateText();

    protected override bool IsInputKey(Keys keyData) => true;

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Keep Tab for moving between controls; everything else is recorded.
        if ((keyData & Keys.KeyCode) == Keys.Tab && (keyData & Keys.Modifiers) is Keys.None or Keys.Shift)
            return base.ProcessCmdKey(ref msg, keyData);
        OnKeyDown(new KeyEventArgs(keyData));
        return true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;

        var modifiers = CurrentModifiers(e);
        var key = e.KeyCode;

        if (modifiers == HotkeyModifiers.None && key is Keys.Back or Keys.Delete)
        {
            _recording = false;
            Value = Hotkey.None;
            UpdateText();
            return;
        }

        if (Hotkey.IsModifierKey(key) || key == Keys.None)
        {
            // Only modifiers so far: show them while the user picks the key.
            _recording = true;
            var preview = new Hotkey(modifiers, Keys.None).ModifiersText();
            Text = preview.Length == 0 ? Strings.HotkeyPrompt : preview + "+…";
            return;
        }

        _recording = false;
        Value = new Hotkey(modifiers, key);
        UpdateText();
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        e.Handled = true;
        if (_recording && CurrentModifiers(e) == HotkeyModifiers.None)
        {
            _recording = false;
            UpdateText();
        }
    }

    protected override void OnKeyPress(KeyPressEventArgs e) => e.Handled = true;

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Text = Strings.HotkeyPrompt;
        SelectAll();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _recording = false;
        UpdateText();
    }

    private void UpdateText()
    {
        if (Focused && _recording) return;
        Text = _value.IsNone ? Strings.HotkeyNone : _value.ToString();
    }

    private static HotkeyModifiers CurrentModifiers(KeyEventArgs e)
    {
        var m = HotkeyModifiers.None;
        if (e.Control) m |= HotkeyModifiers.Control;
        if (e.Alt) m |= HotkeyModifiers.Alt;
        if (e.Shift) m |= HotkeyModifiers.Shift;
        if ((NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0 ||
            (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0)
            m |= HotkeyModifiers.Win;
        return m;
    }
}
