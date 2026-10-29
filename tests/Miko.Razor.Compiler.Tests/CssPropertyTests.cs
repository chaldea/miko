using Miko.Animation;
using Miko.Common;
using Miko.Styling;
using Shouldly;
using static Miko.Razor.Compiler.Tests.CssTestHelpers;

namespace Miko.Razor.Compiler.Tests;

/// <summary>ISSUE-161: keywords, shorthands, transforms, shadows, transitions and grid.</summary>
public class CssPropertyTests
{
    private static T V<T>(StyleProperty<T>? property) => property.ShouldNotBeNull().Value;

    [Fact]
    public void Keywords_MapToEnums()
    {
        var style = BuildStyle("display: inline-flex; position: sticky; flex-direction: row-reverse; flex-wrap: wrap; " +
            "justify-content: space-evenly; align-items: baseline; align-self: flex-end; box-sizing: border-box; " +
            "text-align: center; white-space: nowrap; text-overflow: ellipsis; overflow-x: hidden; cursor: pointer; " +
            "visibility: hidden; pointer-events: none; user-select: none; direction: rtl; font-style: italic");
        V(style.Display).ShouldBe(Display.InlineFlex);
        V(style.Position).ShouldBe(Position.Sticky);
        V(style.FlexDirection).ShouldBe(FlexDirection.RowReverse);
        V(style.FlexWrap).ShouldBe(FlexWrap.Wrap);
        V(style.JustifyContent).ShouldBe(JustifyContent.SpaceEvenly);
        V(style.AlignItems).ShouldBe(AlignItems.Baseline);
        V(style.AlignSelf).ShouldBe(AlignSelf.FlexEnd);
        V(style.BoxSizing).ShouldBe(BoxSizing.BorderBox);
        V(style.TextAlign).ShouldBe(TextAlign.Center);
        V(style.WhiteSpace).ShouldBe(WhiteSpace.Nowrap);
        V(style.TextOverflow).ShouldBe(TextOverflow.Ellipsis);
        V(style.OverflowX).ShouldBe(Overflow.Hidden);
        V(style.Cursor).ShouldBe(Cursor.Pointer);
        V(style.Visibility).ShouldBe(Visibility.Hidden);
        V(style.PointerEvents).ShouldBe(PointerEvents.None);
        V(style.UserSelect).ShouldBe(UserSelect.None);
        V(style.Direction).ShouldBe(Direction.Rtl);
        V(style.FontStyle).ShouldBe(FontStyle.Italic);
    }

    [Theory]
    [InlineData("display: flow-root", "'flow-root' is not supported and is treated as 'block'")]
    [InlineData("cursor: grab", "treated as 'move'")]
    [InlineData("border-style: groove", "treated as 'solid'")]
    [InlineData("justify-content: safe center", "'safe'")]
    [InlineData("font-weight: 650", "rounded to 700")]
    public void ApproximatedKeywords_AreReportedAndKept(string css, string message)
    {
        var (block, diagnostics) = Declarations(css);
        block.IsEmpty.ShouldBeFalse();
        diagnostics.ShouldHaveSingleItem().Descriptor.Id.ShouldBe("RZ10108");
        diagnostics[0].Message.ShouldContain(message);
    }

    [Theory]
    [InlineData("display: ruby")]
    [InlineData("position: absolutely")]
    [InlineData("justify-content: last baseline")]
    public void UnknownKeywords_AreReported(string css)
    {
        Problems(css).ShouldHaveSingleItem().ShouldStartWith("RZ10103");
    }

    [Theory]
    [InlineData("float: left")]
    [InlineData("filter: blur(2px)")]
    [InlineData("list-style: none")]
    [InlineData("text-shadow: 1px 1px red")]
    public void UnsupportedProperties_AreReported(string css)
    {
        Problems(css).ShouldHaveSingleItem().ShouldStartWith("RZ10102");
    }

    [Fact]
    public void VendorPrefixedAndInertProperties_AreDroppedSilently()
    {
        var (block, diagnostics) = Declarations("-webkit-transition: none; -webkit-font-smoothing: antialiased; will-change: transform; appearance: none");
        block.IsEmpty.ShouldBeTrue();
        diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("1px", 1, 1, 1, 1)]
    [InlineData("1px 2px", 1, 2, 1, 2)]
    [InlineData("1px 2px 3px", 1, 2, 3, 2)]
    [InlineData("1px 2px 3px 4px", 1, 2, 3, 4)]
    public void BoxShorthands(string css, float top, float right, float bottom, float left)
    {
        var style = BuildStyle($"padding: {css}");
        V(style.PaddingTop).ShouldBe(Length.Px(top));
        V(style.PaddingRight).ShouldBe(Length.Px(right));
        V(style.PaddingBottom).ShouldBe(Length.Px(bottom));
        V(style.PaddingLeft).ShouldBe(Length.Px(left));
    }

    [Fact]
    public void MarginAuto_AndLogicalShorthands()
    {
        var style = BuildStyle("margin: 0 auto; padding-inline: 4px 8px; inset-block: 2px");
        V(style.MarginLeft).IsAuto.ShouldBeTrue();
        V(style.MarginTop).ShouldBe(Length.Px(0));
        V(style.PaddingInlineStart).ShouldBe(Length.Px(4));
        V(style.PaddingInlineEnd).ShouldBe(Length.Px(8));
        V(style.InsetBlockStart).ShouldBe(Length.Px(2));
        V(style.InsetBlockEnd).ShouldBe(Length.Px(2));
    }

    [Fact]
    public void BorderShorthand_ResetsOmittedParts()
    {
        var style = BuildStyle("border: solid #f00; border-left: 0");
        V(style.BorderTopWidth).ShouldBe(Length.Px(3), "an omitted width is medium");
        V(style.BorderTopStyle).ShouldBe(BorderStyle.Solid);
        V(style.BorderTopColor).ShouldBe(Color.Red);
        V(style.BorderLeftWidth).ShouldBe(Length.Px(0));
        V(style.BorderLeftStyle).ShouldBe(BorderStyle.None);
        style.BorderLeftColor!.Value.Keyword.ShouldBe(StyleKeyword.Initial);
    }

    [Fact]
    public void BorderShorthand_WithVar_FillsTheMissingPart()
    {
        var style = BuildStyle("border: 1px solid var(--line-color)");
        style.BorderTopColor!.Value.VarRef.Name.ShouldBe("--line-color");
        V(style.BorderTopWidth).ShouldBe(Length.Px(1));
    }

    [Fact]
    public void BorderRadius_FourCorners_AndElliptical()
    {
        var style = BuildStyle("border-radius: 1px 2px 3px 4px");
        V(style.BorderTopLeftRadius).ShouldBe(Length.Px(1));
        V(style.BorderTopRightRadius).ShouldBe(Length.Px(2));
        V(style.BorderBottomRightRadius).ShouldBe(Length.Px(3));
        V(style.BorderBottomLeftRadius).ShouldBe(Length.Px(4));
        Problems("border-radius: 10px / 20px").ShouldHaveSingleItem().ShouldStartWith("RZ10108");
    }

    [Theory]
    [InlineData("1", 1f, 1f, 0f, LengthUnit.Percent)]
    [InlineData("2 3", 2f, 3f, 0f, LengthUnit.Percent)]
    [InlineData("1 1 100px", 1f, 1f, 100f, LengthUnit.Px)]
    [InlineData("200px", 1f, 1f, 200f, LengthUnit.Px)]
    [InlineData("none", 0f, 0f, 0f, LengthUnit.Auto)]
    [InlineData("auto", 1f, 1f, 0f, LengthUnit.Auto)]
    public void FlexShorthand(string css, float grow, float shrink, float basis, LengthUnit unit)
    {
        var style = BuildStyle($"flex: {css}");
        V(style.FlexGrow).ShouldBe(grow);
        V(style.FlexShrink).ShouldBe(shrink);
        V(style.FlexBasis).ShouldBe(new Length(basis, unit));
    }

    [Fact]
    public void OtherShorthands()
    {
        var style = BuildStyle("gap: 4px 8px; overflow: hidden auto; place-items: center end; flex-flow: column wrap; " +
            "font: italic bold 14px/1.5 'Helvetica Neue', Arial; background: #fff no-repeat center; text-decoration: underline dotted red");
        V(style.RowGap).ShouldBe(Length.Px(4));
        V(style.ColumnGap).ShouldBe(Length.Px(8));
        V(style.OverflowX).ShouldBe(Overflow.Hidden);
        V(style.OverflowY).ShouldBe(Overflow.Auto);
        V(style.AlignItems).ShouldBe(AlignItems.Center);
        V(style.JustifyItems).ShouldBe(JustifyItems.FlexEnd);
        V(style.FlexDirection).ShouldBe(FlexDirection.Column);
        V(style.FlexWrap).ShouldBe(FlexWrap.Wrap);
        V(style.FontStyle).ShouldBe(FontStyle.Italic);
        V(style.FontWeight).ShouldBe(FontWeight.Bold);
        V(style.FontSize).ShouldBe(Length.Px(14));
        V(style.LineHeight).ShouldBe(Length.Number(1.5f));
        V(style.FontFamily).ShouldBe("Helvetica Neue, Arial");
        V(style.BackgroundColor).ShouldBe(Color.White);
        V(style.BackgroundRepeat).ShouldBe(BackgroundRepeat.NoRepeat);
        V(style.BackgroundPosition).ShouldBe(BackgroundPosition.Center);
        V(style.TextDecoration).ShouldBe(TextDecoration.Underline);
    }

    [Fact]
    public void Background_WithAnImage_IsReported()
    {
        Problems("background: url(a.png) red").ShouldHaveSingleItem().ShouldContain("Style.BackgroundImage");
        Problems("background-image: linear-gradient(red, blue)").ShouldHaveSingleItem().ShouldStartWith("RZ10103");
    }

    [Fact]
    public void Transform_AndOrigin()
    {
        var style = BuildStyle("transform: translate(10px, 50%) rotate(45deg) scale(2) translateZ(0); transform-origin: top left");
        var functions = V(style.Transform).Functions;
        functions.Count.ShouldBe(3);
        functions[0].ShouldBe(new TransformFunction.Translate(Length.Px(10), Length.Percent(50)));
        functions[1].ShouldBe(new TransformFunction.Rotate(45));
        functions[2].ShouldBe(new TransformFunction.Scale(2, 2));
        V(style.TransformOrigin).ShouldBe(new TransformOrigin(Length.Percent(0), Length.Percent(0)));
        V(BuildStyle("transform: rotate(.25turn)").Transform).Functions.Single().ShouldBe(new TransformFunction.Rotate(90));
        Problems("transform: rotateX(10deg)").ShouldHaveSingleItem().ShouldContain("3D");
    }

    [Fact]
    public void BoxShadow_List()
    {
        var shadows = V(BuildStyle("box-shadow: 0 2px 4px rgba(0,0,0,.2), inset 1px 1px #fff").BoxShadow);
        shadows.Count.ShouldBe(2);
        shadows[0].ShouldBe(new BoxShadow(0, 2, 4, 0, new Color(0, 0, 0, 51)));
        shadows[1].ShouldBe(new BoxShadow(1, 1, 0, 0, Color.White, true));
        V(BuildStyle("box-shadow: none").BoxShadow).ShouldBeEmpty();
    }

    [Fact]
    public void Transitions_CombineLonghandsByPosition()
    {
        var transitions = V(BuildStyle("transition: opacity .3s, background-color 200ms ease-in 50ms; transition-timing-function: linear, ease-out").Transitions);
        transitions.Select(t => (t.Property, t.Duration, t.TimingFunction, t.Delay)).ShouldBe(
        [
            (nameof(Style.Opacity), 0.3f, TimingFunction.Linear, 0f),
            (nameof(Style.BackgroundColor), 0.2f, TimingFunction.EaseOut, 0.05f),
        ]);

        var bezier = V(BuildStyle("transition: transform 1s cubic-bezier(.36,.66,.04,1)").Transitions).Single();
        bezier.TimingFunction.ShouldBe(TimingFunction.CubicBezier);
        bezier.CubicBezier.ShouldBe(new CubicBezierParams(0.36f, 0.66f, 0.04f, 1f));
        V(BuildStyle("transition: none").Transitions).ShouldBeEmpty();
    }

    [Fact]
    public void Animation_WithoutKeyframesInTheSheet_IsReported()
    {
        Problems("animation: spin 1s").ShouldHaveSingleItem().ShouldContain("no @keyframes 'spin'");
    }

    [Fact]
    public void Grid_TracksAreasAndPlacement()
    {
        var style = BuildStyle("grid-template-columns: [full-start] repeat(auto-fill, minmax(120px, 1fr)) [full-end]; " +
            "grid-template-rows: auto 1fr fit-content(200px); grid-template-areas: \"head head\" \"side main\"; " +
            "grid-column: 1 / span 2; grid-row: header; grid-auto-rows: minmax(40px, auto)");
        var columns = V(style.GridTemplateColumns);
        columns.Count.ShouldBe(3);
        columns[0].IsLineNames.ShouldBeTrue();
        columns[1].IsRepeat.ShouldBeTrue();
        columns[1].AutoRepeat.ShouldBe(GridAutoRepeat.AutoFill);
        V(style.GridTemplateRows).Count.ShouldBe(3);
        V(style.GridTemplateAreas).TryGetArea("main", out var row, out var col, out _, out _).ShouldBeTrue();
        (row, col).ShouldBe((1, 1));
        V(style.GridColumnStart).ShouldBe(GridLine.Line(1));
        V(style.GridColumnEnd).ShouldBe(GridLine.Span(2));
        V(style.GridRowStart).ShouldBe(GridLine.Named("header"));
        V(style.GridRowEnd).ShouldBe(GridLine.Named("header"), "an omitted end repeats a name");
    }

    [Theory]
    [InlineData("grid-template-areas: \"a a\" \"a b\" \"c b\"", "rectangle")]
    [InlineData("grid-template-areas: \"a b\" \"c\"", "same number of columns")]
    [InlineData("grid-template-columns: repeat(auto-fill, 1fr)", "fixed size")]
    [InlineData("grid-template-columns: minmax(1fr, 100px)", "cannot be fr")]
    [InlineData("grid-column: 0", "line 0")]
    public void InvalidGrid_IsReportedInsteadOfThrowingAtRunTime(string css, string message)
    {
        Problems(css).ShouldHaveSingleItem().ShouldContain(message);
    }

    [Fact]
    public void Expressions_InAStyleAttribute()
    {
        // As WriteInlineCss passes them: each C# expression is a placeholder.
        var p0 = Microsoft.AspNetCore.Razor.Language.Components.Css.CssPlaceholder.Encode(0);
        var p1 = Microsoft.AspNetCore.Razor.Language.Components.Css.CssPlaceholder.Encode(1);
        var p2 = Microsoft.AspNetCore.Razor.Language.Components.Css.CssPlaceholder.Encode(2);
        var (block, diagnostics) = Declarations($"width: {p0}px; color: {p1}; display: {p2}", allowDynamic: true);
        block["Width"].ShouldBe($"global::Miko.Common.Length.Px(global::Miko.Components.RenderTreeBuilder.CssNumber({p0}))");
        block["Color"].ShouldBe($"global::Miko.Components.RenderTreeBuilder.CssColor({p1})");
        diagnostics.ShouldHaveSingleItem().Descriptor.Id.ShouldBe("RZ10107");
        Problems($"width: {p0}", allowDynamic: true).ShouldHaveSingleItem().ShouldContain("needs a unit");
    }

    [Fact]
    public void Diagnostics_PointAtTheDeclaration()
    {
        const string css = "color: red; float: left; width: 10vmin";
        var diagnostics = Declarations(css).Diagnostics;
        diagnostics.Count.ShouldBe(2);
        css.Substring(diagnostics[0].Start, diagnostics[0].Length).ShouldBe("float");
        css.Substring(diagnostics[1].Start, diagnostics[1].Length).ShouldBe("width: 10vmin");
    }
}
