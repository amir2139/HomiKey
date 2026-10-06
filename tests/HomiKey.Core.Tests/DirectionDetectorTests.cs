using HomiKey.Core;

namespace HomiKey.Core.Tests;

public class DirectionDetectorTests
{
    [Theory]
    [InlineData("sghl", ConversionDirection.LatinToPersian)]
    [InlineData("اثممخ", ConversionDirection.PersianToLatin)]
    [InlineData("hello سلام", ConversionDirection.LatinToPersian)]
    [InlineData("سلام دنیا hi", ConversionDirection.PersianToLatin)]
    [InlineData("۱۲۳", ConversionDirection.PersianToLatin)]
    [InlineData("\u200C", ConversionDirection.PersianToLatin)]
    public void DetectsByMajority(string text, ConversionDirection expected)
    {
        Assert.Equal(expected, DirectionDetector.Detect(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123 ,.;")]
    [InlineData("ab سل")]
    public void UsesFallbackWhenUndecided(string text)
    {
        Assert.Equal(ConversionDirection.PersianToLatin, DirectionDetector.Detect(text, ConversionDirection.PersianToLatin));
        Assert.Equal(ConversionDirection.LatinToPersian, DirectionDetector.Detect(text));
    }

    [Fact]
    public void Ratio()
    {
        Assert.Equal(0, DirectionDetector.PersianRatio(""));
        Assert.Equal(0.5, DirectionDetector.PersianRatio("ab سل"));
        Assert.Equal(1, DirectionDetector.PersianRatio("سلام"));
    }

    [Fact]
    public void ByteOrderMarkIsNotPersian()
    {
        Assert.False(DirectionDetector.IsPersian('\uFEFF'));
    }
}
