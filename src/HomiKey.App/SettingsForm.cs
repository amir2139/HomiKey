using HomiKey.App.Controls;
using HomiKey.App.Native;
using HomiKey.App.Resources;
using HomiKey.Core;

namespace HomiKey.App;

/// <summary>
/// The settings window. Built in code with table layouts so the same layout mirrors
/// cleanly between the English (left-to-right) and Persian (right-to-left) interface.
/// </summary>
internal sealed class SettingsForm : Form
{
    private readonly AppSettings _working;
    private readonly Action<AppSettings> _save;

    private readonly GroupBox _generalGroup = Group();
    private readonly Label _languageLabel = FieldLabel();
    private readonly ComboBox _languageCombo = Combo();
    private readonly CheckBox _enabledCheck = Check();
    private readonly CheckBox _startupCheck = Check();

    private readonly GroupBox _hotkeysGroup = Group();
    private readonly Label _convertHotkeyLabel = FieldLabel();
    private readonly HotkeyBox _convertHotkeyBox = new() { Width = 200 };
    private readonly Button _convertHotkeyReset = SmallButton();
    private readonly Label _lastWordHotkeyLabel = FieldLabel();
    private readonly HotkeyBox _lastWordHotkeyBox = new() { Width = 200 };
    private readonly Button _lastWordHotkeyClear = SmallButton();
    private readonly CheckBox _lastWordWhenNoSelectionCheck = Check();

    private readonly GroupBox _conversionGroup = Group();
    private readonly Label _layoutLabel = FieldLabel();
    private readonly ComboBox _layoutCombo = Combo();
    private readonly CheckBox _switchLanguageCheck = Check();
    private readonly CheckBox _convertDigitsCheck = Check();

    private readonly GroupBox _excludedGroup = Group();
    private readonly TextBox _excludedInput = new() { Width = 220 };
    private readonly Button _excludedAdd = SmallButton();
    private readonly Button _excludedRunning = SmallButton();
    private readonly ListBox _excludedList = new() { Height = 110, IntegralHeight = false, Sorted = true, SelectionMode = SelectionMode.MultiExtended };
    private readonly Button _excludedRemove = SmallButton();
    private readonly ContextMenuStrip _runningMenu = new();

    private readonly GroupBox _tryGroup = Group();
    private readonly TextBox _tryInput = new();
    private readonly Label _tryResultLabel = FieldLabel();
    private readonly TextBox _tryResult = new() { ReadOnly = true, BorderStyle = BorderStyle.None, TabStop = false };

    private readonly Button _saveButton = new() { AutoSize = true, MinimumSize = new Size(90, 0), DialogResult = DialogResult.None };
    private readonly Button _cancelButton = new() { AutoSize = true, MinimumSize = new Size(90, 0), DialogResult = DialogResult.Cancel };

    /// <param name="settings">Current settings; the form edits a copy.</param>
    /// <param name="save">Called with the edited copy when the user saves. May throw to keep the form open.</param>
    public SettingsForm(AppSettings settings, Action<AppSettings> save)
    {
        _working = settings.Clone();
        _save = save;

        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);
        Icon = AppIcons.Active;
        ShowInTaskbar = true;
        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        Controls.Add(BuildLayout());
        WireEvents();
        LoadValues();
        ApplyStrings();
        Localization.Changed += ApplyStrings;
    }

    private Control BuildLayout()
    {
        var root = Table(1);
        root.Dock = DockStyle.Fill;

        // General
        var general = Table(2);
        general.Controls.Add(_languageLabel, 0, 0);
        general.Controls.Add(_languageCombo, 1, 0);
        AddSpanning(general, _enabledCheck, 1, 2);
        AddSpanning(general, _startupCheck, 2, 2);
        _generalGroup.Controls.Add(general);

        // Hotkeys
        var hotkeys = Table(3);
        hotkeys.Controls.Add(_convertHotkeyLabel, 0, 0);
        hotkeys.Controls.Add(_convertHotkeyBox, 1, 0);
        hotkeys.Controls.Add(_convertHotkeyReset, 2, 0);
        hotkeys.Controls.Add(_lastWordHotkeyLabel, 0, 1);
        hotkeys.Controls.Add(_lastWordHotkeyBox, 1, 1);
        hotkeys.Controls.Add(_lastWordHotkeyClear, 2, 1);
        AddSpanning(hotkeys, _lastWordWhenNoSelectionCheck, 2, 3);
        _hotkeysGroup.Controls.Add(hotkeys);

        // Conversion
        var conversion = Table(2);
        conversion.Controls.Add(_layoutLabel, 0, 0);
        conversion.Controls.Add(_layoutCombo, 1, 0);
        AddSpanning(conversion, _switchLanguageCheck, 1, 2);
        AddSpanning(conversion, _convertDigitsCheck, 2, 2);
        _conversionGroup.Controls.Add(conversion);

        // Excluded programs
        var excluded = Table(3);
        excluded.Controls.Add(_excludedInput, 0, 0);
        excluded.Controls.Add(_excludedAdd, 1, 0);
        excluded.Controls.Add(_excludedRunning, 2, 0);
        _excludedList.Dock = DockStyle.Fill;
        excluded.Controls.Add(_excludedList, 0, 1);
        excluded.SetColumnSpan(_excludedList, 2);
        _excludedRemove.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        excluded.Controls.Add(_excludedRemove, 2, 1);
        _excludedGroup.Controls.Add(excluded);

        // Try it
        var tryIt = Table(2);
        _tryInput.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        AddSpanning(tryIt, _tryInput, 0, 2);
        tryIt.Controls.Add(_tryResultLabel, 0, 1);
        _tryResult.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _tryResult.BackColor = SystemColors.Control;
        tryIt.Controls.Add(_tryResult, 1, 1);
        tryIt.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, 100);
        _tryGroup.Controls.Add(tryIt);

        // Buttons: Save then Cancel at the far end of the row (mirrored in Persian).
        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 8, 0, 0),
            WrapContents = false,
        };
        buttons.Controls.Add(_cancelButton);
        buttons.Controls.Add(_saveButton);

        foreach (var group in new[] { _generalGroup, _hotkeysGroup, _conversionGroup, _excludedGroup, _tryGroup })
        {
            group.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            root.Controls.Add(group);
        }
        root.Controls.Add(buttons);
        return root;
    }

    private void WireEvents()
    {
        _convertHotkeyReset.Click += (_, _) => _convertHotkeyBox.Value = AppSettings.DefaultConvertHotkey;
        _lastWordHotkeyClear.Click += (_, _) => _lastWordHotkeyBox.Value = Hotkey.None;

        _excludedAdd.Click += (_, _) => AddExcluded(_excludedInput.Text);
        _excludedInput.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            AddExcluded(_excludedInput.Text);
        };
        _excludedRemove.Click += (_, _) =>
        {
            foreach (var item in _excludedList.SelectedItems.Cast<string>().ToList()) _excludedList.Items.Remove(item);
        };
        _excludedRunning.Click += (_, _) => ShowRunningMenu();

        _layoutCombo.SelectedIndexChanged += (_, _) => UpdateTryResult();
        _convertDigitsCheck.CheckedChanged += (_, _) => UpdateTryResult();
        _tryInput.TextChanged += (_, _) => UpdateTryResult();

        _saveButton.Click += (_, _) => Save();
    }

    private void LoadValues()
    {
        _enabledCheck.Checked = _working.Enabled;
        _startupCheck.Checked = SafeStartupState();
        _convertHotkeyBox.Value = _working.ConvertHotkeyValue;
        _lastWordHotkeyBox.Value = _working.LastWordHotkeyValue;
        _lastWordWhenNoSelectionCheck.Checked = _working.LastWordWhenNothingSelected;
        _switchLanguageCheck.Checked = _working.SwitchInputLanguage;
        _convertDigitsCheck.Checked = _working.ConvertDigits;
        foreach (var app in _working.ExcludedApps.Select(ForegroundApp.Normalize).Distinct())
            _excludedList.Items.Add(app);
    }

    private void ApplyStrings()
    {
        Localization.ApplyTo(this);
        Localization.ApplyTo(_runningMenu);
        Text = Strings.SettingsTitle;

        _generalGroup.Text = Strings.GroupGeneral;
        _languageLabel.Text = Strings.LabelLanguage;
        var language = _languageCombo.SelectedItem is ComboItem selectedLanguage ? selectedLanguage.Value : _working.Language;
        FillCombo(_languageCombo,
            (AppSettings.LanguageSystem, Strings.LanguageSystem),
            (AppSettings.LanguageEnglish, Strings.LanguageEnglish),
            (AppSettings.LanguagePersian, Strings.LanguagePersian));
        SelectValue(_languageCombo, language);
        _enabledCheck.Text = Strings.CheckEnabled;
        _startupCheck.Text = Strings.CheckRunAtStartup;

        _hotkeysGroup.Text = Strings.GroupHotkeys;
        _convertHotkeyLabel.Text = Strings.LabelConvertHotkey;
        _convertHotkeyReset.Text = Strings.ButtonReset;
        _lastWordHotkeyLabel.Text = Strings.LabelLastWordHotkey;
        _lastWordHotkeyClear.Text = Strings.ButtonClear;
        _lastWordWhenNoSelectionCheck.Text = Strings.CheckLastWordWhenNothingSelected;
        _convertHotkeyBox.RefreshText();
        _lastWordHotkeyBox.RefreshText();

        _conversionGroup.Text = Strings.GroupConversion;
        _layoutLabel.Text = Strings.LabelPersianLayout;
        var layout = _layoutCombo.SelectedItem is ComboItem current ? current.Value : (_working.PersianLayout ?? PersianLayoutKind.Standard).ToString();
        FillCombo(_layoutCombo,
            (nameof(PersianLayoutKind.Standard), Strings.LayoutStandard),
            (nameof(PersianLayoutKind.Legacy), Strings.LayoutLegacy));
        SelectValue(_layoutCombo, layout);
        _switchLanguageCheck.Text = Strings.CheckSwitchLanguage;
        _convertDigitsCheck.Text = Strings.CheckConvertDigits;

        _excludedGroup.Text = Strings.GroupExcluded;
        _excludedInput.PlaceholderText = Strings.ExcludedPlaceholder;
        _excludedAdd.Text = Strings.ButtonAdd;
        _excludedRunning.Text = Strings.ButtonAddRunning;
        _excludedRemove.Text = Strings.ButtonRemove;

        _tryGroup.Text = Strings.GroupTry;
        _tryInput.PlaceholderText = Strings.TryPlaceholder;
        _tryResultLabel.Text = Strings.TryResultLabel;

        _saveButton.Text = Strings.ButtonSave;
        _cancelButton.Text = Strings.ButtonCancel;

        // Program names and the conversion preview are not prose; keep their natural direction.
        _excludedInput.RightToLeft = RightToLeft.No;
        _excludedList.RightToLeft = RightToLeft.No;
        UpdateTryResult();
    }

    private void UpdateTryResult()
    {
        if (_layoutCombo.SelectedItem is not ComboItem layoutItem) return;
        var options = new ConversionOptions
        {
            PersianLayout = Enum.Parse<PersianLayoutKind>(layoutItem.Value),
            ConvertDigits = _convertDigitsCheck.Checked,
        };
        var converted = new LayoutConverter(options).Convert(_tryInput.Text, out var direction);
        _tryResult.Text = converted;
        _tryResult.RightToLeft = direction == ConversionDirection.LatinToPersian ? RightToLeft.Yes : RightToLeft.No;
        _tryInput.RightToLeft = direction == ConversionDirection.PersianToLatin ? RightToLeft.Yes : RightToLeft.No;
    }

    private void AddExcluded(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var name = ForegroundApp.Normalize(text);
        if (!_excludedList.Items.Contains(name)) _excludedList.Items.Add(name);
        _excludedInput.Clear();
    }

    private void ShowRunningMenu()
    {
        _runningMenu.Items.Clear();
        var running = ForegroundApp.RunningWithWindows();
        if (running.Count == 0)
        {
            _runningMenu.Items.Add(new ToolStripMenuItem(Strings.NoRunningApps) { Enabled = false });
        }
        foreach (var name in running)
        {
            var item = new ToolStripMenuItem(name.ToLowerInvariant()) { RightToLeft = RightToLeft.No };
            item.Click += (_, _) => AddExcluded(name);
            _runningMenu.Items.Add(item);
        }
        _runningMenu.Show(_excludedRunning, new Point(0, _excludedRunning.Height));
    }

    private void Save()
    {
        var convert = _convertHotkeyBox.Value;
        var lastWord = _lastWordHotkeyBox.Value;
        if (convert.IsNone)
        {
            Localization.Message(Strings.ConvertHotkeyRequired, MessageBoxIcon.Warning);
            _convertHotkeyBox.Focus();
            return;
        }
        foreach (var (hotkey, box) in new[] { (convert, _convertHotkeyBox), (lastWord, _lastWordHotkeyBox) })
        {
            if (hotkey.IsNone || hotkey.IsValid) continue;
            Localization.Message(string.Format(Strings.HotkeyInvalid, hotkey), MessageBoxIcon.Warning);
            box.Focus();
            return;
        }
        if (convert == lastWord)
        {
            Localization.Message(Strings.HotkeysSame, MessageBoxIcon.Warning);
            _lastWordHotkeyBox.Focus();
            return;
        }

        _working.Language = ((ComboItem)_languageCombo.SelectedItem!).Value;
        _working.Enabled = _enabledCheck.Checked;
        _working.ConvertHotkey = convert.ToString();
        _working.LastWordHotkey = lastWord.ToString();
        _working.LastWordWhenNothingSelected = _lastWordWhenNoSelectionCheck.Checked;
        _working.PersianLayout = Enum.Parse<PersianLayoutKind>(((ComboItem)_layoutCombo.SelectedItem!).Value);
        _working.SwitchInputLanguage = _switchLanguageCheck.Checked;
        _working.ConvertDigits = _convertDigitsCheck.Checked;
        _working.ExcludedApps = _excludedList.Items.Cast<string>().ToList();

        try
        {
            if (_startupCheck.Checked != SafeStartupState()) StartupRegistration.Set(_startupCheck.Checked);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            Localization.Message(string.Format(Strings.StartupFailed, ex.Message), MessageBoxIcon.Warning);
        }

        try
        {
            _save(_working);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Localization.Message(string.Format(Strings.SettingsSaveFailed, ex.Message), MessageBoxIcon.Warning);
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }

    private static bool SafeStartupState()
    {
        try { return StartupRegistration.IsEnabled; }
        catch (Exception ex) when (ex is System.Security.SecurityException or IOException) { return false; }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        Localization.Changed -= ApplyStrings;
        _runningMenu.Dispose();
        base.OnFormClosed(e);
    }

    // ---- small layout helpers ----

    private sealed record ComboItem(string Value, string Text)
    {
        public override string ToString() => Text;
    }

    private static void FillCombo(ComboBox combo, params (string Value, string Text)[] items)
    {
        combo.BeginUpdate();
        combo.Items.Clear();
        foreach (var (value, text) in items) combo.Items.Add(new ComboItem(value, text));
        combo.EndUpdate();
    }

    private static void SelectValue(ComboBox combo, string value)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboItem item && item.Value == value)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }

    private static TableLayoutPanel Table(int columns)
    {
        var table = new TableLayoutPanel
        {
            ColumnCount = columns,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            Padding = new Padding(4),
        };
        for (var i = 0; i < columns; i++) table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return table;
    }

    private static void AddSpanning(TableLayoutPanel table, Control control, int row, int span)
    {
        table.Controls.Add(control, 0, row);
        table.SetColumnSpan(control, span);
    }

    private static GroupBox Group() => new()
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(8, 4, 8, 8),
        Margin = new Padding(0, 0, 0, 8),
    };

    private static Label FieldLabel() => new() { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 8, 3) };

    private static ComboBox Combo() => new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };

    private static CheckBox Check() => new() { AutoSize = true, Margin = new Padding(3, 4, 3, 2) };

    private static Button SmallButton() => new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(80, 0) };
}
