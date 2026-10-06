using System.Text.Json;
using HomiKey.Core;

namespace HomiKey.Core.Tests;

/// <summary>Runs the cases in tests/vectors.json, which the web version is tested against too.</summary>
public class VectorTests
{
    public static IEnumerable<object[]> Cases()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "vectors.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            yield return
            [
                c.GetProperty("layout").GetString()!,
                c.GetProperty("direction").GetString()!,
                c.GetProperty("input").GetString()!,
                c.GetProperty("expected").GetString()!,
            ];
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Converts(string layout, string direction, string input, string expected)
    {
        var kind = layout switch
        {
            "standard" => PersianLayoutKind.Standard,
            "legacy" => PersianLayoutKind.Legacy,
            _ => throw new ArgumentException(layout),
        };
        var converter = new LayoutConverter(new ConversionOptions { PersianLayout = kind });

        var actual = direction switch
        {
            "auto" => converter.Convert(input, out _),
            "latinToPersian" => converter.LatinToPersian(input),
            "persianToLatin" => converter.PersianToLatin(input),
            _ => throw new ArgumentException(direction),
        };

        Assert.Equal(expected, actual);
    }
}
