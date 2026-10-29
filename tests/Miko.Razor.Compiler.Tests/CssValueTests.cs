using Miko.Common;
using Miko.Styling;
using Shouldly;
using static Miko.Razor.Compiler.Tests.CssTestHelpers;

namespace Miko.Razor.Compiler.Tests;

/// <summary>
/// ISSUE-161: CSS values → typed C#. Each case compiles the emitted code with Roslyn against Miko
/// and checks the built <see cref="Style"/>, so the tests cover the generated code, not its text.
/// </summary>
public class CssValueTests
{
    private static T V<T>(StyleProperty<T>? property) => property.ShouldNotBeNull().Value;

    [Theory]
    [InlineData("10px", 10f, LengthUnit.Px)]
    [InlineData("1.5rem", 1.5f, LengthUnit.Rem)]
    [InlineData("2em", 2f, LengthUnit.Em)]
    [InlineData("50%", 50f, LengthUnit.Percent)]
    [InlineData("10vw", 10f, LengthUnit.Vw)]
    [InlineData("100dvh", 100f, LengthUnit.Vh)]
    [InlineData("0", 0f, LengthUnit.Px)]
    [InlineData(".5px", 0.5f, LengthUnit.Px)]
    public void Lengths(string css, float value, LengthUnit unit)
    {
        V(BuildStyle($"width: {css}").Width).ShouldBe(new Length(value, unit));
    }

    [Theory]
    [InlineData("12pt", 16f)]
    [InlineData("1in", 96f)]
    [InlineData("2.54cm", 96f)]
    [InlineData("1pc", 16f)]
    public void AbsoluteUnits_FoldToPixels(string css, float px)
    {
        V(BuildStyle($"width: {css}").Width).Value.ShouldBe(px, 0.01f);
    }

    [Fact]
    public void LengthKeywords()
    {
        V(BuildStyle("width: auto").Width).IsAuto.ShouldBeTrue();
        V(BuildStyle("width: fit-content").Width).IsFitContent.ShouldBeTrue();
        BuildStyle("max-width: none").MaxWidth!.Value.Keyword.ShouldBe(StyleKeyword.Initial);
        BuildStyle("min-height: auto").MinHeight!.Value.Keyword.ShouldBe(StyleKeyword.Initial);
        V(BuildStyle("border-top-width: thin").BorderTopWidth).ShouldBe(Length.Px(1));
        V(BuildStyle("font-size: large").FontSize).ShouldBe(Length.Px(18));
        V(BuildStyle("font-size: 150%").FontSize).ShouldBe(Length.Em(1.5f));
        V(BuildStyle("line-height: 1.4").LineHeight).ShouldBe(Length.Number(1.4f));
        BuildStyle("line-height: normal").LineHeight!.Value.Keyword.ShouldBe(StyleKeyword.Initial);
    }

    [Fact]
    public void Calc_KeepsUnitsApartUntilLayout()
    {
        V(BuildStyle("width: calc(100% - 20px)").Width).ShouldBe(Length.Percent(100) - Length.Px(20));
        V(BuildStyle("width: calc((10px + 2em) * 2)").Width).ShouldBe((Length.Px(10) + Length.Em(2)) * 2);
        V(BuildStyle("width: calc(100px / 4)").Width).ShouldBe(Length.Px(25));
        V(BuildStyle("opacity: calc(1 / 2)").Opacity).ShouldBe(0.5f);
    }

    [Fact]
    public void Calc_WithVar_IsResolvedByTheCascade()
    {
        var style = BuildStyle("margin-left: calc(-1 * var(--gap)); width: calc(100% - var(--side))");
        style.MarginLeft!.Value.IsCalc.ShouldBeTrue();
        style.MarginLeft!.Value.TryEvaluateCalc(new() { ["--gap"] = Length.Px(8) }, out var margin).ShouldBeTrue();
        margin.ShouldBe(Length.Px(-8));
        style.Width!.Value.TryEvaluateCalc(new() { ["--side"] = Length.Px(30) }, out var width).ShouldBeTrue();
        width.ShouldBe(Length.Percent(100) - Length.Px(30));
    }

    [Fact]
    public void Env_SafeAreaInsets()
    {
        V(BuildStyle("padding-top: env(safe-area-inset-top)").PaddingTop).ShouldBe(Length.SafeAreaInsetTop);
        V(BuildStyle("padding-bottom: calc(env(safe-area-inset-bottom) + 8px)").PaddingBottom)
            .ShouldBe(Length.SafeAreaInsetBottom + Length.Px(8));
    }

    [Theory]
    [InlineData("width: 10", "needs a unit")]
    [InlineData("padding-top: -2px", "negative")]
    [InlineData("width: 10vmin", "'vmin'")]
    [InlineData("width: calc(100%-20px)", "")]
    [InlineData("width: calc(10px * 2px)", "multiply two lengths")]
    [InlineData("width: max(10px, 2em)", "max()")]
    [InlineData("border-width: 10%", "percentages")]
    public void InvalidLengths_AreReported(string css, string message)
    {
        var problems = Problems(css);
        problems.ShouldHaveSingleItem().ShouldStartWith("RZ10103");
        problems[0].ShouldContain(message);
    }

    [Theory]
    [InlineData("#0054e9", 0, 0x54, 0xe9, 255)]
    [InlineData("#fff", 255, 255, 255, 255)]
    [InlineData("#11223380", 0x11, 0x22, 0x33, 0x80)]
    [InlineData("#f008", 255, 0, 0, 0x88)]
    [InlineData("rebeccapurple", 0x66, 0x33, 0x99, 255)]
    [InlineData("RED", 255, 0, 0, 255)]
    [InlineData("transparent", 0, 0, 0, 0)]
    [InlineData("rgb(10, 20, 30)", 10, 20, 30, 255)]
    [InlineData("rgba(10, 20, 30, 0.5)", 10, 20, 30, 128)]
    [InlineData("rgb(10 20 30 / 25%)", 10, 20, 30, 64)]
    [InlineData("rgb(100% 0% 0%)", 255, 0, 0, 255)]
    [InlineData("hsl(120, 100%, 50%)", 0, 255, 0, 255)]
    [InlineData("hsl(0.5turn 100% 25% / .5)", 0, 128, 128, 128)]
    public void Colors_FoldAtCompileTime(string css, int r, int g, int b, int a)
    {
        V(BuildStyle($"background-color: {css}").BackgroundColor).ShouldBe(new Color((byte)r, (byte)g, (byte)b, (byte)a));
    }

    [Fact]
    public void CurrentColor_IsInheritForColorOnly()
    {
        BuildStyle("color: currentcolor").Color!.Value.Keyword.ShouldBe(StyleKeyword.Inherit);
        Problems("border-color: currentcolor").ShouldHaveSingleItem().ShouldContain("currentcolor");
    }

    [Theory]
    [InlineData("color: oklch(70% 0.1 200)", "oklch()")]
    [InlineData("color: #12345", "hex color")]
    [InlineData("color: notacolor", "not a color")]
    public void InvalidColors_AreReported(string css, string message)
    {
        Problems(css).ShouldHaveSingleItem().ShouldContain(message);
    }

    [Fact]
    public void Var_WithAndWithoutFallback()
    {
        var style = BuildStyle("color: var(--fg); margin-top: var(--gap, 12px); opacity: var(--o, .5); z-index: var(--z, 2); font-family: var(--font, Arial)");
        style.Color!.Value.VarRef.ShouldBe(new VarReference("--fg"));
        style.MarginTop!.Value.VarRef.Fallback!.Value.As<Length>().ShouldBe(Length.Px(12));
        style.Opacity!.Value.VarRef.Fallback!.Value.As<float>().ShouldBe(0.5f);
        style.ZIndex!.Value.VarRef.Fallback!.Value.As<int>().ShouldBe(2);
        style.FontFamily!.Value.VarRef.Fallback!.Value.As<string>().ShouldBe("Arial");
        BuildStyle("display: var(--d)").Display!.Value.VarRef.Name.ShouldBe("--d");
    }

    [Fact]
    public void CustomProperties_AreTypedByValue()
    {
        var vars = BuildStyle("--c: #102030; --l: 4px; --n: 1.5; --s: \"x\"; --k: start; --f: 'Open Sans', sans-serif; --e:").Vars.ShouldNotBeNull();
        vars["--c"].As<Color>().ShouldBe(new Color(0x10, 0x20, 0x30));
        vars["--l"].As<Length>().ShouldBe(Length.Px(4));
        vars["--n"].As<float>().ShouldBe(1.5f);
        vars["--s"].As<string>().ShouldBe("x");
        vars["--k"].As<string>().ShouldBe("start");
        vars["--f"].As<string>().ShouldBe("Open Sans, sans-serif");
        vars["--e"].As<string>().ShouldBe("");
        Problems("--a: var(--b)").ShouldHaveSingleItem().ShouldContain("another custom property");
    }

    [Theory]
    [InlineData("inherit", StyleKeyword.Inherit)]
    [InlineData("initial", StyleKeyword.Initial)]
    [InlineData("unset", StyleKeyword.Unset)]
    [InlineData("revert", StyleKeyword.Revert)]
    public void WideKeywords_ApplyToEveryLonghand(string keyword, StyleKeyword expected)
    {
        var style = BuildStyle($"margin: {keyword}; color: {keyword}");
        style.MarginTop!.Value.Keyword.ShouldBe(expected);
        style.MarginLeft!.Value.Keyword.ShouldBe(expected);
        style.Color!.Value.Keyword.ShouldBe(expected);
    }

    [Fact]
    public void LastDeclarationWins()
    {
        var style = BuildStyle("margin: 1px; margin-top: 5px; color: red; color: blue");
        V(style.MarginTop).ShouldBe(Length.Px(5));
        V(style.MarginBottom).ShouldBe(Length.Px(1));
        V(style.Color).ShouldBe(Color.Blue);
    }

    [Fact]
    public void InvalidDeclaration_IsDroppedAsAWhole()
    {
        // As in a browser: an invalid margin does not leave half of its sides set.
        var (block, diagnostics) = Declarations("margin: 1px 2px bogus; color: red");
        block.Properties.ShouldBe(["Color"]);
        diagnostics.ShouldHaveSingleItem().Descriptor.Id.ShouldBe("RZ10103");
    }
}
