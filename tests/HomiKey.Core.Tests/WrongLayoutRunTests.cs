using HomiKey.Core;

namespace HomiKey.Core.Tests;

public class WrongLayoutRunTests
{
    private readonly LayoutConverter _converter = new();

    private string Replace(string text)
    {
        WrongLayoutRun.TryReplace(text, _converter, out var result, out _, out _, out _);
        return result;
    }

    [Theory]
    // Stops at a word in the other language.
    [InlineData("چطوری sghl phgj", "چطوری سلام حالت")]
    [InlineData("چطوری sghl phgj ", "چطوری سلام حالت ")]
    [InlineData("سلام دنیا hi اثممخ", "سلام دنیا hi hello")]
    [InlineData("I said اثممخ صخقمی", "I said hello world")]
    // Goes all the way to the start of the line.
    [InlineData("sghl phgj", "سلام حالت")]
    [InlineData("اثممخ صخقمی", "hello world")]
    // Stops at a word ending in punctuation that stays the same.
    [InlineData("sghl. phgj", "sghl. حالت")]
    [InlineData("sghl! phgj", "sghl! حالت")]
    [InlineData("اثممخ. صخقمی", "اثممخ. world")]
    // Punctuation that converts is part of the wrong-layout text.
    [InlineData("sghl o,fd? phgj", "سلام خوبی؟ حالت")]
    // Stops at a line break.
    [InlineData("sghl\nphgj", "sghl\nحالت")]
    // Words without letters go along with the run.
    [InlineData("sghl 123 phgj", "سلام ۱۲۳ حالت")]
    // ZWNJ is part of a Persian word, so that word stops a Latin run.
    [InlineData("می\u200Cخواهم sghl", "می\u200Cخواهم سلام")]
    public void ConvertsTheWrongLayoutWordsAtTheEnd(string text, string expected)
    {
        Assert.Equal(expected, Replace(text));
    }

    [Fact]
    public void ReportsSpanAndDirection()
    {
        var (start, length, direction) = WrongLayoutRun.Find("چطوری sghl phgj  ", _converter);
        Assert.Equal(6, start);
        Assert.Equal(9, length);
        Assert.Equal(ConversionDirection.LatinToPersian, direction);
    }

    [Fact]
    public void TailsIncludeTrailingWhitespace()
    {
        Assert.True(WrongLayoutRun.TryReplace("چطوری sghl phgj ", _converter, out _, out var originalTail, out var convertedTail, out var direction));
        Assert.Equal("sghl phgj ", originalTail);
        Assert.Equal("سلام حالت ", convertedTail);
        Assert.Equal(ConversionDirection.LatinToPersian, direction);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NoWordMeansNoRun(string text)
    {
        Assert.Equal(0, WrongLayoutRun.Find(text, _converter).Length);
        Assert.False(WrongLayoutRun.TryReplace(text, _converter, out var result, out _, out _, out _));
        Assert.Equal(text, result);
    }

    [Fact]
    public void UsesTheSelectedPersianLayout()
    {
        var legacy = new LayoutConverter(new ConversionOptions { PersianLayout = PersianLayoutKind.Legacy });
        WrongLayoutRun.TryReplace("چطوری \\nv sghl", legacy, out var result, out _, out _, out _);
        Assert.Equal("چطوری پدر سلام", result);
    }
}
