using HomiKey.Core;

namespace HomiKey.Core.Tests;

public class ConverterTests
{
    private readonly LayoutConverter _converter = new();

    [Fact]
    public void EmptyStaysEmpty()
    {
        Assert.Equal("", _converter.LatinToPersian(""));
        Assert.Equal("", _converter.PersianToLatin(""));
    }

    [Fact]
    public void KeepsWhitespaceAndLineBreaks()
    {
        Assert.Equal("سلام\r\n\tسلام", _converter.LatinToPersian("sghl\r\n\tsghl"));
    }

    [Fact]
    public void LeavesTheOtherScriptAlone()
    {
        Assert.Equal("سلام سلام", _converter.LatinToPersian("sghl سلام"));
        Assert.Equal("hello hello", _converter.PersianToLatin("اثممخ hello"));
    }

    [Fact]
    public void LeavesUnknownCharactersAlone()
    {
        Assert.Equal("é€😀", _converter.LatinToPersian("é€😀"));
        Assert.Equal("é€😀", _converter.PersianToLatin("é€😀"));
    }

    [Fact]
    public void DigitsCanBeKept()
    {
        var converter = new LayoutConverter(new ConversionOptions { ConvertDigits = false });
        Assert.Equal("سلام 123", converter.LatinToPersian("sghl 123"));
        Assert.Equal("hello ۱۲۳", converter.PersianToLatin("اثممخ ۱۲۳"));
    }

    [Fact]
    public void ConvertsLongText()
    {
        const string latin = "sghl o,fd? ldBo,hil fv,l lnvsi.";
        var persian = _converter.LatinToPersian(latin);
        Assert.Equal("سلام خوبی؟ می\u200Cخواهم بروم مدرسه.", persian);
        Assert.Equal(latin, _converter.PersianToLatin(persian));
    }

    [Fact]
    public void ArabicYehAndKafReverseOnStandard()
    {
        Assert.Equal("D", _converter.PersianToLatin("ي"));
        Assert.Equal("Z", _converter.PersianToLatin("ك"));
    }

    [Fact]
    public void FallsBackToTheOtherPersianLayoutForCharactersTheSelectedOneLacks()
    {
        // ۀ only exists on the legacy layout (Shift+G).
        Assert.Equal("G", _converter.PersianToLatin("ۀ"));
        // ASCII the selected layout cannot type is not taken from the other layout.
        Assert.Equal(",", _converter.PersianToLatin(","));
    }

    [Fact]
    public void AutoReportsDirection()
    {
        _converter.Convert("sghl", out var d1);
        _converter.Convert("اثممخ", out var d2);
        Assert.Equal(ConversionDirection.LatinToPersian, d1);
        Assert.Equal(ConversionDirection.PersianToLatin, d2);
    }
}
