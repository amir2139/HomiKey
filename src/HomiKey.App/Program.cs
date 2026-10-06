using HomiKey.App.Native;

namespace HomiKey.App;

internal static class Program
{
    private const string MutexName = @"Local\HomiKey.SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var isFirstInstance);
        if (!isFirstInstance) return;

        ApplicationConfiguration.Initialize();

        var settings = SettingsStore.Load();
        if (settings.PersianLayout is null)
        {
            // First run: match the Persian layout the user actually has installed.
            settings.PersianLayout = InputLanguages.DetectPersianLayout() ?? Core.PersianLayoutKind.Standard;
            try { SettingsStore.Save(settings); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }

        Localization.Apply(settings.Language);
        try { StartupRegistration.RefreshIfPresent(); }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException) { }

        var launchedAtStartup = args.Contains(StartupRegistration.StartupArgument, StringComparer.OrdinalIgnoreCase);
        Application.Run(new TrayApplicationContext(settings, launchedAtStartup));
    }
}
