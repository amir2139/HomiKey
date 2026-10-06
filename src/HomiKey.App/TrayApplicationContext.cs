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

    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _enabledItem = new();
    private readonly ToolStripMenuItem _startupItem = new();
    private readonly ToolStripMenuItem _aboutItem = new();
    private readonly ToolStripMenuItem _exitItem = new();
    private readonly HotkeyWindow _hotkeys = new();
    private readonly ConversionEngine _engine;
    private readonly TextReplacer _replacer;
    private AppSettings _settings;

    public TrayApplicationContext(AppSettings settings, bool launchedAtStartup)
    {
        _settings = settings;
        _engine = new ConversionEngine(settings.ToConversionOptions());
        _replacer = new TextReplacer(_engine);

        BuildMenu();
        _tray = new NotifyIcon { ContextMenuStrip = _menu, Visible = true };
        _hotkeys.HotkeyPressed += OnHotkey;
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

        _startupItem.Click += (_, _) => ToggleStartup();
        _aboutItem.Click += (_, _) => ShowAbout();
        _exitItem.Click += (_, _) => ExitThread();

        _menu.Opening += (_, _) => _startupItem.Checked = StartupRegistration.IsEnabled;
        _menu.Items.AddRange(new ToolStripItem[]
        {
            _enabledItem,
            new ToolStripSeparator(),
            _startupItem,
            _aboutItem,
            new ToolStripSeparator(),
            _exitItem,
        });
    }

    private void ApplyStrings()
    {
        Localization.ApplyTo(_menu);
        _enabledItem.Text = Strings.MenuEnabled;
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
        catch (Exception)
        {
            // Usually the clipboard was locked by another app. A tray app must not die over
            // that; the user can simply press the hotkey again.
        }
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
        _hotkeys.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _menu.Dispose();
        base.ExitThreadCore();
    }
}
