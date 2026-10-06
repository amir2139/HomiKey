using HomiKey.Core;

namespace HomiKey.Core.Tests;

public class LastWordTests
{
    [Theory]
    [InlineData("", 0, 0)]
    [InlineData("   ", 0, 0)]
    [InlineData("sghl", 0, 4)]
    [InlineData("hello sghl", 6, 4)]
    [InlineData("hello sghl  ", 6, 4)]
    [InlineData("a\tb\nc,d", 4, 3)]
    public void FindsTheTrailingWord(string text, int start, int length)
    {
        Assert.Equal((start, length), LastWord.Find(text));
    }

    [Fact]
    public void ZwnjIsPartOfTheWord()
    {
        Assert.Equal((0, 8), LastWord.Find("می\u200Cخواهم"));
    }

    [Fact]
    public void ReplacesOnlyTheLastWord()
    {
        var converter = new LayoutConverter();
        Assert.True(LastWord.TryReplace("hello sghl ", converter.LatinToPersian, out var result, out var originalTail, out var convertedTail));
        Assert.Equal("hello سلام ", result);
        Assert.Equal("sghl ", originalTail);
        Assert.Equal("سلام ", convertedTail);
    }

    [Fact]
    public void NoWordMeansNoChange()
    {
        Assert.False(LastWord.TryReplace("  ", s => s.ToUpperInvariant(), out var result, out _, out _));
        Assert.Equal("  ", result);
    }
}
