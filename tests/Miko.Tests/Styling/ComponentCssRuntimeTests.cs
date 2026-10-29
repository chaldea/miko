using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Components;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

/// <summary>
/// ISSUE-161: the run-time side of compiled component CSS — the registry the generated code
/// registers with, the scope a .razor.css selector requires, :root, media blocks and the helpers
/// that convert C# values in a style attribute.
/// </summary>
public class ComponentCssRuntimeTests
{
    private sealed class ComponentA;
    private sealed class ComponentB;

    private static StyleSheet Sheet(string selector, Style style)
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject { [selector] = Css(style) });
        return sheet;
    }

    private static CssObject Css(Style style)
    {
        var css = new CssObject();
        css.Merge(style);
        return css;
    }

    [Fact]
    public void Registry_RegistersOncePerType_InFirstRenderOrder()
    {
        var registry = new ComponentStyleRegistry();
        var builds = 0;
        registry.Register(typeof(ComponentA), "h1", () => { builds++; return Sheet(".a", new Style { Width = Length.Px(1) }); });
        registry.Register(typeof(ComponentB), "h1", () => Sheet(".b", new Style { Width = Length.Px(2) }));
        registry.Register(typeof(ComponentA), "h1", () => { builds++; return Sheet(".a", new Style()); });

        builds.ShouldBe(1);
        registry.StyleSheet.Rules.Count.ShouldBe(2);
        registry.StyleSheet.Rules[0].Selector.ShouldBeOfType<ClassSelector>().ClassName.ShouldBe("a");
    }

    [Fact]
    public void Registry_ReplacesAComponentsRules_WhenItsCssChanges()
    {
        var registry = new ComponentStyleRegistry();
        registry.Register(typeof(ComponentA), "v1", () => Sheet(".a", new Style { Width = Length.Px(1) }));
        registry.Register(typeof(ComponentB), "v1", () => Sheet(".b", new Style { Width = Length.Px(2) }));
        var sheet = registry.StyleSheet;
        var version = sheet.Version;

        // Hot reload: ComponentA recompiled with other CSS.
        registry.Register(typeof(ComponentA), "v2", () => Sheet(".a2", new Style { Width = Length.Px(3) }));

        registry.StyleSheet.ShouldBeSameAs(sheet, "the engine keeps the sheet it was given");
        sheet.Version.ShouldBeGreaterThan(version);
        sheet.Rules.Select(r => ((ClassSelector)r.Selector).ClassName).ShouldBe(["a2", "b"]);
    }

    [Fact]
    public void Registry_NewRules_AreSeenByTheNextLayout()
    {
        var registry = new ComponentStyleRegistry();
        var sheets = new List<StyleSheet> { registry.StyleSheet };
        var root = new DivElement { Class = "late" };
        var engine = new LayoutEngine();
        engine.Layout(root, sheets, 800, 600).ComputedStyle.Width.IsAuto.ShouldBeTrue();

        registry.Register(typeof(ComponentA), "h", () => Sheet(".late", new Style { Width = Length.Px(42) }));

        engine.Layout(root, sheets, 800, 600).ComputedStyle.Width.ShouldBe(Length.Px(42));
    }

    [Fact]
    public void AppBuilder_AddsTheComponentSheet_AfterTheAppsOwn()
    {
        var builder = MikoAppBuilder.CreateDefault();
        var appSheet = new StyleSheet();
        builder.AddStyleSheet(appSheet);
        using var provider = builder.Services.BuildServiceProvider();

        var registry = provider.GetRequiredService<ComponentStyleRegistry>();
        provider.GetRequiredService<IOptions<MikoAppOptions>>().Value.StyleSheets.ShouldBe([appSheet, registry.StyleSheet]);
    }

    [Fact]
    public void ScopeAttribute_MatchesTheElementsCssScope()
    {
        var selector = CssSelectorParser.Parse(".a[b-1234567890]");
        selector.Matches(new DivElement { Class = "a", CssScope = "b-1234567890" }).ShouldBeTrue();
        selector.Matches(new DivElement { Class = "a", CssScope = "b-0000000000" }).ShouldBeFalse();
        selector.Matches(new DivElement { Class = "a" }).ShouldBeFalse();
        selector.Specificity.ShouldBe(20, "the scope adds attribute specificity, as in Blazor");
    }

    [Fact]
    public void CssScope_IsNotAnAttributeOfItsOwn()
    {
        new DivElement { CssScope = "b-1" }.TryGetAttributeValue("CssScope", out _).ShouldBeFalse();
    }

    [Fact]
    public void Root_MatchesTheElementWithoutAParent_AndCarriesGlobalVariables()
    {
        var child = new SpanElement { Class = "c" };
        var root = new DivElement();
        root.AddChild(child);
        var root2 = new RootSelector();
        root2.Matches(root).ShouldBeTrue();
        root2.Matches(child).ShouldBeFalse();

        var sheet = new StyleSheet();
        var vars = new CssObject { Vars = new() { ["--brand"] = Color.Red } };
        sheet.Add(new CssObject { [":root"] = vars, [".c"] = new CssObject { Color = Miko.Styling.Css.Var("--brand") } });
        var layout = new LayoutEngine().Layout(root, [sheet], 800, 600);
        layout.Children.Single().ComputedStyle.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void OnlyChild_AndOnlyOfType_HaveOnePseudoClassOfSpecificity()
    {
        var only = new SpanElement();
        var parent = new DivElement();
        parent.AddChild(only);
        var pair = new DivElement();
        var a = new SpanElement();
        var b = new ParagraphElement();
        pair.AddChild(a);
        pair.AddChild(b);

        var onlyChild = CssSelectorParser.Parse(":only-child");
        onlyChild.Matches(only).ShouldBeTrue();
        onlyChild.Matches(a).ShouldBeFalse();
        onlyChild.Specificity.ShouldBe(10);

        var onlyOfType = CssSelectorParser.Parse("span:only-of-type");
        onlyOfType.Matches(a).ShouldBeTrue("the only <span> among its siblings");
        onlyOfType.Matches(only).ShouldBeTrue();
        onlyOfType.Specificity.ShouldBe(11);
    }

    [Fact]
    public void AddWithMedia_AddsMediaRules()
    {
        var sheet = new StyleSheet();
        var media = MediaCondition.MinWidth(600);
        sheet.Add(new CssObject { [".a"] = new CssObject { Width = Length.Px(1) }, [".b"] = new CssObject { Width = Length.Px(2) } }, media);

        sheet.Rules.ShouldBeEmpty();
        sheet.MediaRules.ShouldHaveSingleItem().Rules.Count.ShouldBe(2);
        Should.Throw<NotSupportedException>(() => sheet.Add(new CssObject { [".a::before"] = new CssObject { Content = "x" } }, media));
    }

    [Theory]
    [InlineData("12.5", 12.5f)]
    [InlineData("abc", 0f)]
    [InlineData(null, 0f)]
    [InlineData("NaN", 0f)]
    public void CssNumber_FromAString(string? value, float expected)
    {
        RenderTreeBuilder.CssNumber(value).ShouldBe(expected);
    }

    [Theory]
    [InlineData("#ff0000", 255, 0, 0, 255)]
    [InlineData(" #0f08 ", 0, 255, 0, 136)]
    [InlineData("red", 0, 0, 0, 0)]
    [InlineData("#12345", 0, 0, 0, 0)]
    [InlineData(null, 0, 0, 0, 0)]
    public void CssColor_FromAString_AcceptsHexOnly(string? value, int r, int g, int b, int a)
    {
        RenderTreeBuilder.CssColor(value).ShouldBe(new Color((byte)r, (byte)g, (byte)b, (byte)a));
    }
}
