using System.Reflection;
using Microsoft.AspNetCore.Razor.Language.Components.Css;
using Miko.Common;
using Miko.Styling;
using Shouldly;
using static Miko.Razor.Compiler.Tests.CssTestHelpers;

namespace Miko.Razor.Compiler.Tests;

/// <summary>ISSUE-161: whole stylesheets — rule order, @media, @keyframes, at-rules and recovery.</summary>
public class CssStyleSheetTests
{
    [Fact]
    public void Rules_KeepSourceOrder_AndRepeatedSelectors()
    {
        var sheet = BuildStyleSheet(".a { color: red } .b { color: blue } .a { margin: 1px }");
        sheet.Rules.Select(r => r.Selector.GetType().Name).Count().ShouldBe(3);
        sheet.Rules[0].Style.Color!.Value.Value.ShouldBe(Color.Red);
        sheet.Rules[2].Style.MarginTop!.Value.Value.ShouldBe(Length.Px(1));
    }

    [Fact]
    public void SelectorList_IsOneRulePerBranch()
    {
        var sheet = BuildStyleSheet("#id, .cls { color: red }");
        sheet.Rules.Count.ShouldBe(2);
        sheet.Rules.Select(r => r.Selector.Specificity).ShouldBe([100, 10], "each branch keeps its own specificity");
    }

    [Fact]
    public void PseudoElementRules()
    {
        var sheet = BuildStyleSheet(".a::after { content: \"x\" }");
        sheet.PseudoElementRules.ShouldHaveSingleItem().Type.ShouldBe(PseudoElementType.After);
    }

    [Theory]
    [InlineData("(min-width: 600px)", 600, 400, true)]
    [InlineData("(min-width: 600px)", 599, 400, false)]
    [InlineData("screen and (max-width: 40em)", 640, 400, true)]
    [InlineData("(400px < width <= 800px)", 800, 400, true)]
    [InlineData("(400px < width <= 800px)", 400, 400, false)]
    [InlineData("(orientation: portrait)", 300, 600, true)]
    [InlineData("(min-aspect-ratio: 16/9)", 1600, 900, true)]
    [InlineData("not all and (min-width: 600px)", 500, 400, true)]
    [InlineData("(max-width: 300px), (min-width: 900px)", 1000, 400, true)]
    public void MediaQueries(string query, float width, float height, bool matches)
    {
        var sheet = BuildStyleSheet($"@media {query} {{ .a {{ color: red }} }}");
        sheet.MediaRules.ShouldHaveSingleItem().Condition.Matches(new ViewportInfo(width, height)).ShouldBe(matches);
    }

    [Fact]
    public void MediaQueries_ThatNeverMatch_EmitNothing()
    {
        var (body, diagnostics) = StyleSheet("@media print { .a { color: red } } @media (hover: hover) { .b { color: red } }");
        body.ShouldBeEmpty();
        diagnostics.ShouldHaveSingleItem().Message.ShouldContain("'hover' cannot be evaluated");
    }

    [Fact]
    public void Keyframes_AreTranslatedEvenWhenUnused()
    {
        var (_, diagnostics) = StyleSheet("@keyframes x { from { float: left } }");
        diagnostics.ShouldHaveSingleItem().Descriptor.Id.ShouldBe("RZ10102");
    }

    [Theory]
    [InlineData("@import 'a.css'; .a { color: red }", "RZ10105")]
    [InlineData("@supports (display: grid) { .a { color: red } }", "RZ10105")]
    [InlineData(".a { .b { color: red } }", "RZ10104")]
    [InlineData(".a { color: red", "RZ10106")]
    [InlineData("} .a { color: red }", "RZ10106")]
    public void AtRulesAndSyntaxErrors_AreReported_AndParsingRecovers(string css, string id)
    {
        var (_, diagnostics) = StyleSheet(css);
        diagnostics.ShouldContain(d => d.Descriptor.Id == id);
        if (!css.StartsWith("@supports", StringComparison.Ordinal) && !css.StartsWith(".a { .b", StringComparison.Ordinal))
            BuildStyleSheetIgnoringDiagnostics(css).Rules.ShouldNotBeEmpty("the valid rules still apply");
    }

    private static StyleSheet BuildStyleSheetIgnoringDiagnostics(string css)
    {
        var (body, _) = StyleSheet(css);
        var type = Compile($"public static global::Miko.Styling.StyleSheet Build() {{ var sheet = new global::Miko.Styling.StyleSheet();\n{body} return sheet; }}");
        return (StyleSheet)type.GetMethod("Build")!.Invoke(null, null)!;
    }

    [Fact]
    public void Diagnostics_PointAtTheSource()
    {
        const string css = ".a {\n  color: red;\n  float: left;\n}\n";
        var diagnostic = StyleSheet(css).Diagnostics.ShouldHaveSingleItem();
        var map = CssSourceMap.ForFile("/x/A.razor.css", css);
        var span = map.Map(diagnostic.Start, diagnostic.Length)!.Value;
        (span.LineIndex, span.CharacterIndex, span.Length).ShouldBe((2, 2, 5));
    }

    // -----------------------------------------------------------------
    // The translator's tables against the runtime types
    // -----------------------------------------------------------------

    [Fact]
    public void EveryEnumMapEntry_IsAMemberOfItsEnum()
    {
        var assembly = typeof(Style).Assembly;
        foreach (var (enumType, map) in CssPropertyTranslator.EnumMaps())
        {
            var type = assembly.GetType(enumType).ShouldNotBeNull(enumType);
            foreach (var (keyword, entry) in map)
                System.Enum.GetNames(type).ShouldContain(entry.Split(':')[0], $"{keyword} → {entry} in {enumType}");
        }
    }

    [Fact]
    public void EveryAssignedProperty_IsAStyleProperty()
    {
        var properties = typeof(Style).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToHashSet();
        foreach (var name in CssPropertyTranslator.AssignedStyleProperties.Where(n => n != "all"))
            properties.ShouldContain(name);
    }
}
