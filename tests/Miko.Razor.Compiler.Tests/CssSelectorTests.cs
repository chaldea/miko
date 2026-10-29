using Miko.Styling.Selectors;
using Shouldly;
using static Miko.Razor.Compiler.Tests.CssTestHelpers;

namespace Miko.Razor.Compiler.Tests;

/// <summary>
/// ISSUE-161: selectors are validated against the runtime parser at compile time, normalized, and
/// scoped for .razor.css. Every accepted selector must parse at run time.
/// </summary>
public class CssSelectorTests
{
    [Theory]
    [InlineData(".a>.b", ".a > .b")]
    [InlineData(".a\n  .b", ".a .b")]
    [InlineData("a + b ~ c", "a + b ~ c")]
    [InlineData("DIV.Item", "div.Item")]
    [InlineData(".x:before", ".x::before")]
    [InlineData("li:nth-child( 2n + 1 )", "li:nth-child(2n+1)")]
    [InlineData("li:nth-child(odd)", "li:nth-child(2n+1)")]
    [InlineData("li:nth-last-child(-n+3 of .a)", "li:nth-last-child(-n+3 of .a)")]
    [InlineData("p:only-child", "p:only-child")]
    [InlineData(".a:is(.b,.c) :not(.d .e)", ".a:is(.b, .c) :not(.d .e)")]
    [InlineData(".card:has(>.btn:hover)", ".card:has(> .btn:hover)")]
    [InlineData("input[type=checkbox i]", "input[type=\"checkbox\"]")]
    [InlineData("[data-x='a\"b']", "[data-x='a\"b']")]
    [InlineData(":root", ":root")]
    public void Normalized(string css, string expected)
    {
        var selector = Selectors(css).ShouldHaveSingleItem();
        selector.ShouldBe(expected);
        Should.NotThrow(() => CssSelectorParser.Parse(selector));
    }

    [Fact]
    public void SelectorList_SplitsIntoBranches_AndDropsOnlyTheUnsupportedOne()
    {
        Selectors(".a, .b:visited, .c").ShouldBe([".a", ".c"]);
        SelectorProblems(".a, .b:visited").ShouldHaveSingleItem().ShouldContain(":visited");
    }

    [Theory]
    [InlineData("a:visited", ":visited")]
    [InlineData("p::selection", "::selection")]
    [InlineData(".a & .b", "'&'")]
    [InlineData(".md\\:flex", "escaped")]
    [InlineData("svg|rect", "namespace")]
    [InlineData(".a::before .b", "end of the selector")]
    [InlineData(":not(::before)", "inside a functional pseudo-class")]
    [InlineData(".a >", "must be followed")]
    [InlineData(":nth-child(2 n)", "An+B")]
    [InlineData(":host", ":host")]
    [InlineData(".a ::deep .b", "only meaningful in a .razor.css")]
    public void Unsupported_IsReported(string css, string message)
    {
        Selectors(css).ShouldBeEmpty();
        SelectorProblems(css).ShouldHaveSingleItem().ShouldContain(message);
    }

    [Fact]
    public void VendorPrefixed_IsDroppedSilently()
    {
        Selectors("input::-webkit-input-placeholder").ShouldBeEmpty();
        SelectorProblems("input::-webkit-input-placeholder").ShouldBeEmpty();
    }

    [Fact]
    public void HtmlAndBody_AreTheRoot()
    {
        var diagnostics = new List<Microsoft.AspNetCore.Razor.Language.Components.Css.CssDiagnostic>();
        Microsoft.AspNetCore.Razor.Language.Components.Css.CssSelectorTranslator.Translate("body", 0, 4, null, true, diagnostics)
            .ShouldBe([":root"]);
        diagnostics.ShouldHaveSingleItem().Descriptor.Id.ShouldBe("RZ10108");
    }

    [Theory]
    [InlineData(".a", ".a[b-1]")]
    [InlineData(".a .b", ".a .b[b-1]")]
    [InlineData("div > span:hover", "div > span:hover[b-1]")]
    [InlineData(".a::before", ".a[b-1]::before")]
    [InlineData("::placeholder", "[b-1]::placeholder")]
    [InlineData("*", "*[b-1]")]
    [InlineData(".a ::deep .b", ".a[b-1] .b")]
    [InlineData(".a > ::deep .b", ".a[b-1] > .b")]
    [InlineData(".a::deep .b", ".a[b-1] .b")]
    [InlineData("::deep .b", "[b-1] .b")]
    public void Scoped_LikeBlazorCssIsolation(string css, string expected)
    {
        var selector = Selectors(css, scope: "b-1").ShouldHaveSingleItem();
        selector.ShouldBe(expected);
        Should.NotThrow(() => CssSelectorParser.Parse(selector));
    }

    [Fact]
    public void PseudoElements_AreRejectedInsideMedia()
    {
        Selectors(".a::after", allowPseudoElements: false).ShouldBeEmpty();
    }
}
