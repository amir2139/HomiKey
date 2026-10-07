using HomiKey.Core;

namespace HomiKey.Core.Tests;

public class ConversionEngineTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ConvertsSelectionAndReportsTargetScript()
    {
        var engine = new ConversionEngine();
        var result = engine.ConvertSelection("sghl", T0);
        Assert.NotNull(result);
        Assert.Equal("سلام", result.Replacement);
        Assert.Equal(LayoutScript.Persian, result.OutputScript);
        Assert.False(result.IsRevert);
    }

    [Fact]
    public void SecondPressOnTheConvertedSelectionRevertsExactly()
    {
        var engine = new ConversionEngine();
        // "Hello" is not what converting "اثممخ" back would give ("hello"), so this proves
        // the original is restored rather than re-converted.
        var first = engine.ConvertSelection("Hello", T0)!;
        var second = engine.ConvertSelection(first.Replacement, T0.AddSeconds(2));
        Assert.NotNull(second);
        Assert.True(second.IsRevert);
        Assert.Equal("Hello", second.Replacement);
        Assert.Equal(LayoutScript.Latin, second.OutputScript);
    }

    [Fact]
    public void ThirdPressConvertsAgain()
    {
        var engine = new ConversionEngine();
        engine.ConvertSelection("sghl", T0);
        engine.ConvertSelection("سلام", T0.AddSeconds(1));
        var third = engine.ConvertSelection("sghl", T0.AddSeconds(2))!;
        Assert.False(third.IsRevert);
        Assert.Equal("سلام", third.Replacement);
    }

    [Fact]
    public void RevertExpiresAfterTheWindow()
    {
        var engine = new ConversionEngine(history: new ConversionHistory(TimeSpan.FromSeconds(5)));
        engine.ConvertSelection("Hello", T0);
        var later = engine.ConvertSelection("اثممخ", T0.AddSeconds(10))!;
        Assert.False(later.IsRevert);
        Assert.Equal("hello", later.Replacement);
    }

    [Fact]
    public void ClockSetBackDoesNotRevert()
    {
        var engine = new ConversionEngine();
        var first = engine.ConvertSelection("Hello", T0)!;
        var second = engine.ConvertSelection(first.Replacement, T0.AddHours(-1))!;
        Assert.False(second.IsRevert);
    }

    [Fact]
    public void NothingToChangeReturnsNull()
    {
        var engine = new ConversionEngine();
        Assert.Null(engine.ConvertSelection("", T0));
        Assert.Null(engine.ConvertSelection("é € 😀", T0));
        Assert.Null(engine.ConvertBeforeCaret("   ", T0));
    }

    [Fact]
    public void ConvertsTheLastWordBeforeTheCaret()
    {
        var engine = new ConversionEngine();
        var result = engine.ConvertBeforeCaret("I said sghl ", T0)!;
        Assert.Equal("I said سلام ", result.Replacement);
        Assert.Equal(LayoutScript.Persian, result.OutputScript);
    }

    [Fact]
    public void LastWordDirectionIsDetectedFromTheWordOnly()
    {
        var engine = new ConversionEngine();
        var result = engine.ConvertBeforeCaret("hello there اثممخ", T0)!;
        Assert.Equal("hello there hello", result.Replacement);
        Assert.Equal(LayoutScript.Latin, result.OutputScript);
    }

    [Fact]
    public void SecondPressBeforeTheCaretReverts()
    {
        var engine = new ConversionEngine();
        var first = engine.ConvertBeforeCaret("ok sghl ", T0)!;
        var second = engine.ConvertBeforeCaret(first.Replacement, T0.AddSeconds(1))!;
        Assert.True(second.IsRevert);
        Assert.Equal("ok sghl ", second.Replacement);
    }

    [Fact]
    public void ConvertsTheWholeWrongLayoutRunBeforeTheCaret()
    {
        var engine = new ConversionEngine();
        var result = engine.ConvertBeforeCaret("چطوری sghl phgj", T0, BeforeCaretScope.WrongLayoutRun)!;
        Assert.Equal("چطوری سلام حالت", result.Replacement);
        Assert.Equal(LayoutScript.Persian, result.OutputScript);
        Assert.False(result.IsRevert);
    }

    [Fact]
    public void SecondPressRevertsTheWholeRun()
    {
        var engine = new ConversionEngine();
        var first = engine.ConvertBeforeCaret("چطوری sghl phgj ", T0, BeforeCaretScope.WrongLayoutRun)!;
        var second = engine.ConvertBeforeCaret(first.Replacement, T0.AddSeconds(29), BeforeCaretScope.WrongLayoutRun)!;
        Assert.True(second.IsRevert);
        Assert.Equal("چطوری sghl phgj ", second.Replacement);
        Assert.Equal(LayoutScript.Latin, second.OutputScript);
    }

    [Fact]
    public void RunRevertExpiresAfter30Seconds()
    {
        var engine = new ConversionEngine();
        var first = engine.ConvertBeforeCaret("sghl phgj", T0, BeforeCaretScope.WrongLayoutRun)!;
        var later = engine.ConvertBeforeCaret(first.Replacement, T0.AddSeconds(31), BeforeCaretScope.WrongLayoutRun)!;
        Assert.False(later.IsRevert);
        Assert.Equal("sghl phgj", later.Replacement);
    }

    [Fact]
    public void LastWordScopeStillTakesOneWord()
    {
        var engine = new ConversionEngine();
        var result = engine.ConvertBeforeCaret("چطوری sghl phgj", T0, BeforeCaretScope.LastWord)!;
        Assert.Equal("چطوری sghl حالت", result.Replacement);
    }

    [Fact]
    public void SelectionConversionCanBeRevertedFromBeforeTheCaret()
    {
        // After pasting, the selection is gone and the caret sits after the pasted text.
        var engine = new ConversionEngine();
        var first = engine.ConvertSelection("sghl o,fd?", T0)!;
        var second = engine.ConvertBeforeCaret("prefix " + first.Replacement, T0.AddSeconds(1))!;
        Assert.True(second.IsRevert);
        Assert.Equal("prefix sghl o,fd?", second.Replacement);
    }

    [Fact]
    public void ChangingOptionsForgetsHistory()
    {
        var engine = new ConversionEngine();
        var first = engine.ConvertSelection("Hello", T0)!;
        engine.Options = engine.Options with { PersianLayout = PersianLayoutKind.Legacy };
        var second = engine.ConvertSelection(first.Replacement, T0.AddSeconds(1))!;
        Assert.False(second.IsRevert);
    }
}
