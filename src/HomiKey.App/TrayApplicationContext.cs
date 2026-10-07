using System.Reflection;
using HomiKey.App.Native;
using HomiKey.App.Resources;
using HomiKey.Core;

namespace HomiKey.App;

/// <summary>The running app: tray icon, menu and hotkeys. There is no main window.</summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private const int ConvertHotkeyId = 1;
    private const int LastWordHotkeyId = 2;
    public const string ShowSettingsEventName = @"Local\HomiKey.ShowSettings";

    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _enabledItem = new();
    private readonly ToolStripMenuItem _settingsItem = new();
    private readonly ToolStripMenuItem _languageItem = new();
    private readonly ToolStripMenuItem _languageSystemItem = new() { Tag = AppSettings.LanguageSystem };
    private readonly ToolStripMenuItem _languageEnglishItem = new() { Tag = AppSettings.LanguageEnglish, RightToLeft = RightToLeft.No };
    private readonly ToolStripMenuItem _languagePersianItem = new() { Tag = AppSettings.LanguagePersian, RightToLeft = RightToLeft.Yes };
    private readonly ToolStripMenuItem _startupItem = new();
    private readonly ToolStripMenuItem _aboutItem = new();
    private readonly ToolStripMenuItem _exitItem = new();
    private readonly HotkeyWindow _hotkeys = new();
    private readonly ConversionEngine _engine;
    private readonly TextReplacer _replacer;
    private readonly SynchronizationContext _ui;
    private readonly EventWaitHandle _showSettingsSignal;
    private readonly RegisteredWaitHandle _showSettingsWait;
    private AppSettings _settings;
    private SettingsForm? _settingsForm;

    public TrayApplicationContext(AppSettings settings, bool launchedAtStartup)
    {
        _settings = settings;
        _engine = new ConversionEngine(settings.ToConversionOptions());
        _replacer = new TextReplacer(_engine);

        BuildMenu();
        _tray = new NotifyIcon { ContextMenuStrip = _menu, Visible = true };
        _tray.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) ShowSettings();
        };
        _hotkeys.HotkeyPressed += OnHotkey;

        // Starting HomiKey again while it runs opens the settings of the running copy.
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _showSettingsSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventName);
        _showSettingsWait = ThreadPool.RegisterWaitForSingleObject(
            _showSettingsSignal, (_, _) => _ui.Post(_ => ShowSettings(), null), null, Timeout.Infinite, executeOnlyOnce: false);
        Localization.Changed += ApplyStrings;

        ApplyStrings();
        ApplySettings();

        if (!_settings.WelcomeShown && !launchedAtStartup)
        {
            _tray.ShowBalloonTip(8000, Strings.AppName, string.Format(Strings.WelcomeBalloon, _settings.ConvertHotkeyValue), ToolTipIcon.Info);
            _settings.WelcomeShown = true;
            TrySave();
        }
    }

    private void BuildMenu()
    {
        _enabledItem.CheckOnClick = true;
        _enabledItem.Click += (_, _) =>
        {
            _settings.Enabled = _enabledItem.Checked;
            TrySave();
            ApplySettings();
        };

        _settingsItem.Click += (_, _) => ShowSettings();
        foreach (var item in new[] { _languageSystemItem, _languageEnglishItem, _languagePersianItem })
        {
            item.Click += (_, _) => ChangeLanguage((string)item.Tag!);
            _languageItem.DropDownItems.Add(item);
        }
        _languageItem.DropDownOpening += (_, _) =>
        {
            foreach (ToolStripMenuItem item in _languageItem.DropDownItems)
                item.Checked = (string)item.Tag! == _settings.Language;
        };

        _startupItem.Click += (_, _) => ToggleStartup();
        _aboutItem.Click += (_, _) => ShowAbout();
        _exitItem.Click += (_, _) => ExitThread();

        _menu.Opening += (_, _) => _startupItem.Checked = StartupRegistration.IsEnabled;
        _menu.Items.AddRange(new ToolStripItem[]
        {
            _settingsItem,
            _enabledItem,
            new ToolStripSeparator(),
            _languageItem,
            _startupItem,
            _aboutItem,
            new ToolStripSeparator(),
            _exitItem,
        });
    }

    private void ApplyStrings()
    {
        Localization.ApplyTo(_menu);
        _settingsItem.Font = Localization.UiFont(style: FontStyle.Bold);
        _settingsItem.Text = Strings.MenuSettings;
        _enabledItem.Text = Strings.MenuEnabled;
        _languageItem.Text = Strings.MenuLanguage;
        _languageSystemItem.Text = Strings.LanguageSystem;
        _languageEnglishItem.Text = Strings.LanguageEnglish;
        _languagePersianItem.Text = Strings.LanguagePersian;
        _startupItem.Text = Strings.MenuRunAtStartup;
        _aboutItem.Text = Strings.MenuAbout;
        _exitItem.Text = Strings.MenuExit;
        UpdateTrayIcon();
    }

    /// <summary>Pushes the current settings into the engine, hotkeys and tray icon.</summary>
    private void ApplySettings()
    {
        var options = _settings.ToConversionOptions();
        if (options != _engine.Options) _engine.Options = options;

        _enabledItem.Checked = _settings.Enabled;
        RegisterHotkeys();
        UpdateTrayIcon();
    }

    private void RegisterHotkeys()
    {
        _hotkeys.UnregisterAll();
        if (!_settings.Enabled) return;

        var failed = new List<string>();
        if (!_hotkeys.Register(ConvertHotkeyId, _settings.ConvertHotkeyValue)) failed.Add(_settings.ConvertHotkeyValue.ToString());
        if (!_hotkeys.Register(LastWordHotkeyId, _settings.LastWordHotkeyValue)) failed.Add(_settings.LastWordHotkeyValue.ToString());
        if (failed.Count > 0)
            _tray?.ShowBalloonTip(8000, Strings.AppName, string.Format(Strings.HotkeyRegisterFailed, string.Join(", ", failed)), ToolTipIcon.Warning);
    }

    private void UpdateTrayIcon()
    {
        if (_tray is null) return;
        _tray.Icon = _settings.Enabled ? AppIcons.Active : AppIcons.Paused;
        var text = _settings.Enabled
            ? string.Format(Strings.TrayTooltip, _settings.ConvertHotkeyValue)
            : Strings.TrayTooltipPaused;
        _tray.Text = text.Length > 127 ? text[..127] : text;
    }

    private async void OnHotkey(int id)
    {
        if (!_settings.Enabled) return;
        var (mode, hotkey) = id == LastWordHotkeyId
            ? (TextReplacer.Mode.LastWord, _settings.LastWordHotkeyValue)
            : (TextReplacer.Mode.Selection, _settings.ConvertHotkeyValue);
        try
        {
            await _replacer.RunAsync(mode, hotkey, _settings);
        }
        catch (Exception ex) when (ex is ClipboardUnavailableException or System.Runtime.InteropServices.ExternalException)
        {
            _tray.ShowBalloonTip(5000, Strings.AppName, Strings.ClipboardBusy, ToolTipIcon.Warning);
        }
        catch (Exception ex)
        {
            // A tray app must not die over one failed conversion; say what went wrong instead.
            _tray.ShowBalloonTip(8000, Strings.AppName, string.Format(Strings.ConversionFailed, ex.Message), ToolTipIcon.Error);
        }
    }

    private void ShowSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }

        // While the window is open the hotkeys are released, so they can be recorded in it.
        _hotkeys.UnregisterAll();
        _settingsForm = new SettingsForm(_settings, edited =>
        {
            SettingsStore.Save(edited);
            _settings = edited;
        });
        _settingsForm.FormClosed += (_, _) =>
        {
            _settingsForm = null;
            Localization.Apply(_settings.Language);
            ApplySettings();
        };
        _settingsForm.Show();
        _settingsForm.Activate();
    }

    private void ChangeLanguage(string language)
    {
        _settings.Language = language;
        TrySave();
        Localization.Apply(language);
    }

    private void ToggleStartup()
    {
        try
        {
            StartupRegistration.Set(!StartupRegistration.IsEnabled);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            Localization.Message(string.Format(Strings.StartupFailed, ex.Message), MessageBoxIcon.Warning);
        }
    }

    private static void ShowAbout()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
        Localization.Message(string.Format(Strings.AboutText, version));
    }

    private void TrySave()
    {
        try
        {
            SettingsStore.Save(_settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Localization.Message(string.Format(Strings.SettingsSaveFailed, ex.Message), MessageBoxIcon.Warning);
        }
    }

    protected override void ExitThreadCore()
    {
        Localization.Changed -= ApplyStrings;
        _showSettingsWait.Unregister(null);
        _showSettingsSignal.Dispose();
        _settingsForm?.Close();
        _hotkeys.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _menu.Dispose();
        base.ExitThreadCore();
    }
}
