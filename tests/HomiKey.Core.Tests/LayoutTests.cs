using HomiKey.Core;

namespace HomiKey.Core.Tests;

public class LayoutTests
{
    public static TheoryData<PersianLayoutKind> Kinds => new() { PersianLayoutKind.Standard, PersianLayoutKind.Legacy };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void EveryKeyHasADistinctOutput(PersianLayoutKind kind)
    {
        var outputs = Layouts.Persian(kind).Keys.Select(k => k.Value).Where(v => v.Length > 0).ToList();
        Assert.Equal(94, outputs.Count);
        Assert.Equal(outputs.Count, outputs.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void EveryKeyRoundTrips(PersianLayoutKind kind)
    {
        var converter = new LayoutConverter(new ConversionOptions { PersianLayout = kind });
        foreach (var key in KeyboardLayout.UsUnshifted + KeyboardLayout.UsShifted)
        {
            var persian = converter.LatinToPersian(key.ToString());
            Assert.Equal(key.ToString(), converter.PersianToLatin(persian));
        }
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void AllPersianLettersAreReachable(PersianLayoutKind kind)
    {
        var outputs = Layouts.Persian(kind).Keys.Select(k => k.Value).ToHashSet();
        foreach (var letter in "ابپتثجچحخدذرزژسشصضطظعغفقکگلمنوهی")
            Assert.Contains(letter.ToString(), outputs);
        foreach (var digit in "۰۱۲۳۴۵۶۷۸۹")
            Assert.Contains(digit.ToString(), outputs);
        foreach (var punctuation in "،؛؟«»")
            Assert.Contains(punctuation.ToString(), outputs);
    }

    [Fact]
    public void StandardLayoutTypesZwnjWithShiftB()
    {
        Assert.True(Layouts.PersianStandard.TryGetOutput('B', out var output));
        Assert.Equal("\u200C", output);
    }

    [Fact]
    public void UsLayoutIsIdentity()
    {
        foreach (var (key, output) in Layouts.UsQwerty.Keys)
            Assert.Equal(key.ToString(), output);
    }

    [Fact]
    public void RejectsWrongKeyCount()
    {
        Assert.Throws<ArgumentException>(() => new KeyboardLayout("x", LayoutScript.Latin, ["a"], ["A"]));
    }
}
