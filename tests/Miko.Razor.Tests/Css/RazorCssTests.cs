using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Testing;
using Shouldly;

namespace Miko.Razor.Tests.Css;

/// <summary>
/// ISSUE-161: CSS in Razor components — <c>style="…"</c>, <c>&lt;style&gt;</c> elements and
/// <c>.razor.css</c> files — is translated into typed C# when the component compiles. These tests
/// render the fixtures and check the computed styles, and read the generated code to check that no
/// CSS text is left for the run time.
/// </summary>
public class RazorCssTests : IDisposable
{
    private readonly TestContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    private static Element One(ComponentUnderTest cut, string className)
        => cut.FindByClass(className).ShouldHaveSingleItem();

    private static ComputedStyle Computed(ComponentUnderTest cut, string className)
        => cut.GetComputedStyle(One(cut, className)).ShouldNotBeNull();

    // -----------------------------------------------------------------
    // Scenario 1: style="…"
    // -----------------------------------------------------------------

    [Fact]
    public void InlineCss_MarginShorthand_SetsEverySide()
    {
        var style = One(_ctx.Render<InlineCssFixture>(), "inline-margin").Style.ShouldNotBeNull();
        style.MarginTop!.Value.Value.ShouldBe(Length.Px(15));
        style.MarginRight!.Value.Value.ShouldBe(Length.Px(15));
        style.MarginBottom!.Value.Value.ShouldBe(Length.Px(15));
        style.MarginLeft!.Value.Value.ShouldBe(Length.Px(15));
    }

    [Fact]
    public void InlineCss_LengthsColorsAndBoxValues_AreTyped()
    {
        var cut = _ctx.Render<InlineCssFixture>();
        var style = Computed(cut, "inline-box");
        style.Height.ShouldBe(Length.Em(2));
        style.PaddingTop.ShouldBe(Length.Px(4));
        style.PaddingLeft.ShouldBe(Length.Px(8));
        style.BackgroundColor.ShouldBe(new Color(0, 0x54, 0xe9));
        style.Color.ShouldBe(new Color(255, 0, 0, 128));
        cut.GetBoxModel(One(cut, "inline-box"))!.BorderBox.Width.ShouldBe(400f + 16f, 0.5f, "width: 50% of the 800px viewport, plus 8px padding each side");
    }

    [Fact]
    public void InlineCss_FlexKeywords_MapToEnums()
    {
        var style = Computed(_ctx.Render<InlineCssFixture>(), "inline-flex");
        style.Display.ShouldBe(Display.Flex);
        style.FlexDirection.ShouldBe(FlexDirection.Column);
        style.JustifyContent.ShouldBe(JustifyContent.SpaceBetween);
        style.AlignItems.ShouldBe(AlignItems.Center);
        style.RowGap.ShouldBe(Length.Px(8));
        style.ColumnGap.ShouldBe(Length.Px(4));
    }

    [Fact]
    public void InlineCss_BorderShorthand_ExpandsToEverySide()
    {
        var style = Computed(_ctx.Render<InlineCssFixture>(), "inline-border");
        foreach (var side in new[] { style.ComputedBorderTop, style.ComputedBorderRight, style.ComputedBorderBottom, style.ComputedBorderLeft })
        {
            side.Width.ShouldBe(Length.Px(2));
            side.Style.ShouldBe(BorderStyle.Dashed);
            side.Color.ShouldBe(Color.Red);
        }

        style.BorderTopLeftRadius.ShouldBe(Length.Px(6));
        style.BorderTopRightRadius.ShouldBe(Length.Px(12));
        style.Opacity.ShouldBe(0.5f);
        style.ZIndex.ShouldBe(3);
    }

    [Fact]
    public void InlineCss_Expressions_SupplyNumbersAndColors()
    {
        InlineCssFixture.Width = 120;
        InlineCssFixture.Opacity = 0.25;
        InlineCssFixture.Background = "#00ff00";
        InlineCssFixture.Gap = 10;
        var cut = _ctx.Render<InlineCssFixture>();
        var style = Computed(cut, "inline-dynamic");
        style.Width.ShouldBe(Length.Px(120));
        style.Opacity.ShouldBe(0.25f);
        style.BackgroundColor.ShouldBe(new Color(0, 255, 0));
        // calc(100% - @(Gap)px): the declared value (the auto-height host gives the percentage no base).
        One(cut, "inline-dynamic").Style!.Height!.Value.Value.ShouldBe(Length.Percent(100) - Length.Px(10));
    }

    [Fact]
    public void InlineCss_CustomProperties_DeclareAndResolve()
    {
        var style = Computed(_ctx.Render<InlineCssFixture>(), "inline-var");
        style.Color.ShouldBe(new Color(0x11, 0x22, 0x33));
        style.MarginLeft.ShouldBe(Length.Px(10), "var(--gap, 10px) falls back");
    }

    [Fact]
    public void InlineStyleObject_IsStillBound()
    {
        Computed(_ctx.Render<InlineCssFixture>(), "inline-object").MarginTop.ShouldBe(Length.Px(7));
    }

    [Fact]
    public void InlineCss_IsTranslatedAtCompileTime()
    {
        var code = RazorTestFiles.GeneratedCode("InlineCssFixture");
        code.ShouldContain("SetStyle(__e1, new global::Miko.Styling.Style");
        code.ShouldContain("MarginTop = global::Miko.Common.Length.Px(15f)");
        code.ShouldContain("Width = global::Miko.Common.Length.Px(global::Miko.Components.RenderTreeBuilder.CssNumber((");
        // No CSS text is left for the run time.
        code.ShouldNotContain("\"margin");
        code.ShouldNotContain("\"style\"");
    }

    // -----------------------------------------------------------------
    // Scenario 2: <style> elements
    // -----------------------------------------------------------------

    [Fact]
    public void StyleElement_IsNotRendered()
    {
        var cut = _ctx.Render<StyleElementFixture>();
        cut.FindByTagName("style").ShouldBeEmpty();
        cut.Root.ShouldBeOfType<DivElement>().Class.ShouldBe("style-host");
    }

    [Fact]
    public void StyleElement_RulesApply()
    {
        var style = Computed(_ctx.Render<StyleElementFixture>(), "style-text");
        style.MarginTop.ShouldBe(Length.Px(15));
        style.MarginLeft.ShouldBe(Length.Px(15));
        style.FontWeight.ShouldBe(FontWeight.Bold);
        style.TextTransform.ShouldBe(TextTransform.Uppercase);
        style.Color.ShouldBe(new Color(0, 0x54, 0xe9), "var(--brand), declared on :root");
    }

    [Fact]
    public void StyleElement_SelectorListBranches_AndPseudoElements()
    {
        var cut = _ctx.Render<StyleElementFixture>();
        Computed(cut, "style-badge").BorderTopLeftRadius.ShouldBe(Length.Percent(50));
        var text = cut.FindLayoutBox(One(cut, "style-text")).ShouldNotBeNull();
        var before = text.Children.Select(c => c.Element).OfType<PseudoElement>().ShouldHaveSingleItem();
        before.TextContent.ShouldBe("*");
    }

    [Fact]
    public void StyleElement_PseudoClasses_AndSourceOrder()
    {
        var cut = _ctx.Render<StyleElementFixture>();
        var items = cut.FindByClass("style-item");
        items.Count.ShouldBe(2);
        // .style-item:first-child (0,2,0) beats the later .style-item (0,1,0).
        cut.GetComputedStyle(items[0])!.Width.ShouldBe(Length.Px(10));
        cut.GetComputedStyle(items[1])!.Width.ShouldBe(Length.Px(20));
    }

    [Theory]
    [InlineData(800f, 20f)]
    [InlineData(400f, 12f)]
    public void StyleElement_MediaRules_FollowTheViewport(float width, float fontSize)
    {
        _ctx.ViewportWidth = width;
        Computed(_ctx.Render<StyleElementFixture>(), "style-text").FontSize.ShouldBe(Length.Px(fontSize));
    }

    [Fact]
    public void StyleElement_TransitionsAndKeyframeAnimations()
    {
        var style = Computed(_ctx.Render<StyleElementFixture>(), "first");
        var transition = style.Transitions.ShouldHaveSingleItem();
        transition.Property.ShouldBe(nameof(Style.Opacity));
        transition.Duration.ShouldBe(0.2f);
        transition.TimingFunction.ShouldBe(TimingFunction.EaseInOut);

        var animation = style.Animations.ShouldHaveSingleItem();
        animation.Name.ShouldBe("pulse");
        animation.Duration.ShouldBe(1f);
        animation.Infinite.ShouldBeTrue();
        animation.Keyframes.Select(k => k.Offset).ShouldBe([0f, 0.5f, 1f]);
        animation.Keyframes[1].Style.Opacity!.Value.Value.ShouldBe(0.5f);
    }

    [Fact]
    public void ComponentStyles_RegisterOncePerComponentType()
    {
        _ctx.Render<StyleElementFixture>();
        var rules = _ctx.ComponentStyles.StyleSheet.Rules.Count;
        var version = _ctx.ComponentStyles.StyleSheet.Version;
        rules.ShouldBeGreaterThan(0);

        _ctx.Render<StyleElementFixture>();
        _ctx.ComponentStyles.StyleSheet.Rules.Count.ShouldBe(rules);
        _ctx.ComponentStyles.StyleSheet.Version.ShouldBe(version);
        _ctx.ComponentStyles.IsRegistered(typeof(StyleElementFixture)).ShouldBeTrue();
    }

    [Fact]
    public void ComponentStyles_AreNotUserStyleSheets()
    {
        _ctx.Render<StyleElementFixture>();
        _ctx.StyleSheets.ShouldBeEmpty("component CSS lives in ComponentStyles, not the test's own sheets");
    }

    // -----------------------------------------------------------------
    // Scenario 3: .razor.css (CSS isolation)
    // -----------------------------------------------------------------

    [Fact]
    public void ScopedCss_AppliesToTheComponentsOwnElements()
    {
        var cut = _ctx.Render<ScopedCssFixture>();
        var own = cut.FindByClass("scoped-text").Single(e => e.Parent!.Class == "scoped-host");
        var style = cut.GetComputedStyle(own)!;
        style.MarginTop.ShouldBe(Length.Px(15));
        style.Color.ShouldBe(Color.Red);
        Computed(cut, "scoped-host").PaddingTop.ShouldBe(Length.Px(4));
    }

    [Fact]
    public void ScopedCss_DoesNotLeakIntoChildComponents()
    {
        var cut = _ctx.Render<ScopedCssFixture>();
        var child = cut.FindByClass("scoped-text").Single(e => e.Parent!.Class == "child-host");
        child.CssScope.ShouldBeNull();
        var style = cut.GetComputedStyle(child)!;
        style.MarginTop.ShouldNotBe(Length.Px(15), "the scoped rule must not reach a child component's <p> (it keeps the UA margin)");
        style.Color.ShouldBe(Color.Black);
    }

    [Fact]
    public void ScopedCss_DeepReachesIntoChildComponents()
    {
        Computed(_ctx.Render<ScopedCssFixture>(), "deep-target").MarginTop.ShouldBe(Length.Px(9));
    }

    [Fact]
    public void ScopedCss_SelectorsCarryTheScope()
    {
        var code = RazorTestFiles.GeneratedCode("ScopedCssFixture");
        var scope = System.Text.RegularExpressions.Regex.Match(code, "CssScope = \"(b-[a-z0-9]+)\"").Groups[1].Value;
        scope.ShouldNotBeNullOrEmpty();
        code.ShouldContain($"[\".scoped-text[{scope}]\"]");
        code.ShouldContain($"[\".scoped-host[{scope}] .deep-target\"]");
        RazorTestFiles.GeneratedCode("ScopedCssChild").ShouldNotContain("CssScope");
    }
}
