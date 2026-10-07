using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace HomiKey.Core.Tests;

/// <summary>
/// Keeps the app's English and Persian string tables in step. Lives here because the
/// Windows app itself cannot be tested on every platform.
/// </summary>
public partial class ResourceParityTests
{
    private static string ResourcesDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "HomiKey.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir.FullName, "src", "HomiKey.App", "Resources");
    }

    private static Dictionary<string, string> Read(string file) =>
        XDocument.Load(Path.Combine(ResourcesDir(), file)).Root!
            .Elements("data")
            .ToDictionary(d => (string)d.Attribute("name")!, d => (string)d.Element("value")!);

    [Fact]
    public void PersianHasExactlyTheEnglishKeys()
    {
        var en = Read("Strings.resx");
        var fa = Read("Strings.fa.resx");
        Assert.Empty(en.Keys.Except(fa.Keys));
        Assert.Empty(fa.Keys.Except(en.Keys));
    }

    [Fact]
    public void PlaceholdersMatch()
    {
        var en = Read("Strings.resx");
        var fa = Read("Strings.fa.resx");
        foreach (var (key, english) in en)
        {
            var expected = Placeholder().Matches(english).Select(m => m.Value).Order().ToList();
            var actual = Placeholder().Matches(fa[key]).Select(m => m.Value).Order().ToList();
            Assert.True(expected.SequenceEqual(actual), $"Placeholders differ for {key}");
        }
    }

    [Fact]
    public void NoEmptyStrings()
    {
        foreach (var file in new[] { "Strings.resx", "Strings.fa.resx" })
            foreach (var (key, value) in Read(file))
                Assert.False(string.IsNullOrWhiteSpace(value), $"{file}: {key} is empty");
    }

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();
}
