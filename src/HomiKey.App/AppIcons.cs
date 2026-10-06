using System.Reflection;

namespace HomiKey.App;

/// <summary>The tray icons, loaded from embedded resources.</summary>
internal static class AppIcons
{
    public static Icon Active { get; } = Load("HomiKey.Assets.homikey.ico");

    public static Icon Paused { get; } = Load("HomiKey.Assets.homikey-off.ico");

    private static Icon Load(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Missing resource {name}.");
        return new Icon(stream, SystemInformation.SmallIconSize);
    }
}
