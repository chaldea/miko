using Miko.Common;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Styling;

public class BorderStyleTests
{
    [Fact]
    public void Style_BorderShorthand_ShouldSetAllUniformProperties()
    {
        var style = new Style();
        style.Border = new Border(Length.Px(2), BorderStyle.Solid, Color.Red);

        style.BorderWidth.ShouldBe(Length.Px(2));
        style.BorderStyle.ShouldBe(BorderStyle.Solid);
        style.BorderColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void Style_BorderShorthand_ShouldClearPerSideProperties()
    {
        var style = new Style
        {
            BorderTopWidth = Length.Px(5),
            BorderTopColor = Color.Blue
        };

        // Setting Border shorthand should clear per-side overrides
        style.Border = new Border(Length.Px(2), Color.Red);

        style.BorderTopWidth.ShouldBeNull();
        style.BorderTopColor.ShouldBeNull();
    }

    [Fact]
    public void Style_BorderTop_ShouldSetTopProperties()
    {
        var style = new Style();
        style.BorderTop = new BorderSide(Length.Px(3), BorderStyle.Dashed, Color.Green);

        style.BorderTopWidth.ShouldBe(Length.Px(3));
        style.BorderTopStyle.ShouldBe(BorderStyle.Dashed);
        style.BorderTopColor.ShouldBe(Color.Green);
    }

    [Fact]
    public void Style_BorderTop_Get_ShouldFallbackToUniformProperties()
    {
        var style = new Style
        {
            BorderWidth = Length.Px(1),
            BorderColor = Color.Black,
            BorderStyle = BorderStyle.Solid
        };

        var top = style.BorderTop;

        top.Width.ShouldBe(Length.Px(1));
        top.Color.ShouldBe(Color.Black);
        top.Style.ShouldBe(BorderStyle.Solid);
    }

    [Fact]
    public void Style_BorderTop_Get_ShouldPreferPerSideOverUniform()
    {
        var style = new Style
        {
            BorderWidth = Length.Px(1),
            BorderColor = Color.Black,
            BorderStyle = BorderStyle.Solid,
            BorderTopWidth = Length.Px(5),
            BorderTopColor = Color.Red
        };

        var top = style.BorderTop;

        top.Width.ShouldBe(Length.Px(5));
        top.Color.ShouldBe(Color.Red);
        top.Style.ShouldBe(BorderStyle.Solid); // Falls back to uniform
    }

    [Fact]
    public void Style_AllBorderSides_ShouldBeIndependent()
    {
        var style = new Style
        {
            BorderTop = new BorderSide(Length.Px(1), BorderStyle.Solid, Color.Red),
            BorderRight = new BorderSide(Length.Px(2), BorderStyle.Dashed, Color.Green),
            BorderBottom = new BorderSide(Length.Px(3), BorderStyle.Dotted, Color.Blue),
            BorderLeft = new BorderSide(Length.Px(4), BorderStyle.Solid, Color.Yellow)
        };

        style.BorderTop.Width.ShouldBe(Length.Px(1));
        style.BorderRight.Width.ShouldBe(Length.Px(2));
        style.BorderBottom.Width.ShouldBe(Length.Px(3));
        style.BorderLeft.Width.ShouldBe(Length.Px(4));

        style.BorderTop.Color.ShouldBe(Color.Red);
        style.BorderRight.Color.ShouldBe(Color.Green);
        style.BorderBottom.Color.ShouldBe(Color.Blue);
        style.BorderLeft.Color.ShouldBe(Color.Yellow);
    }

    [Fact]
    public void Style_Merge_ShouldMergePerSideProperties()
    {
        var style1 = new Style { BorderTopWidth = Length.Px(2) };
        var style2 = new Style { BorderBottomWidth = Length.Px(3) };

        style1.Merge(style2);

        style1.BorderTopWidth.ShouldBe(Length.Px(2));
        style1.BorderBottomWidth.ShouldBe(Length.Px(3));
    }

    [Fact]
    public void Style_Merge_ShouldNotOverrideExistingPerSideProperties()
    {
        var style1 = new Style { BorderTopWidth = Length.Px(2) };
        var style2 = new Style { BorderTopWidth = Length.Px(5) };

        style1.Merge(style2);

        style1.BorderTopWidth.ShouldBe(Length.Px(2)); // Original value preserved
    }

    [Fact]
    public void ComputedStyle_FromStyle_ShouldResolvePerSideProperties()
    {
        var style = new Style
        {
            BorderTopWidth = Length.Px(1),
            BorderRightWidth = Length.Px(2),
            BorderBottomWidth = Length.Px(3),
            BorderLeftWidth = Length.Px(4),
            BorderTopColor = Color.Red,
            BorderRightColor = Color.Green,
            BorderBottomColor = Color.Blue,
            BorderLeftColor = Color.Yellow,
            BorderTopStyle = BorderStyle.Solid,
            BorderRightStyle = BorderStyle.Dashed,
            BorderBottomStyle = BorderStyle.Dotted,
            BorderLeftStyle = BorderStyle.Solid
        };

        var computed = ComputedStyle.FromStyle(style);

        computed.BorderTopWidth.Value.ShouldBe(1);
        computed.BorderRightWidth.Value.ShouldBe(2);
        computed.BorderBottomWidth.Value.ShouldBe(3);
        computed.BorderLeftWidth.Value.ShouldBe(4);

        computed.BorderTopColor.ShouldBe(Color.Red);
        computed.BorderRightColor.ShouldBe(Color.Green);
        computed.BorderBottomColor.ShouldBe(Color.Blue);
        computed.BorderLeftColor.ShouldBe(Color.Yellow);

        computed.BorderTopStyle.ShouldBe(BorderStyle.Solid);
        computed.BorderRightStyle.ShouldBe(BorderStyle.Dashed);
        computed.BorderBottomStyle.ShouldBe(BorderStyle.Dotted);
        computed.BorderLeftStyle.ShouldBe(BorderStyle.Solid);
    }

    [Fact]
    public void ComputedStyle_FromStyle_PerSideShouldTakePrecedenceOverUniform()
    {
        var style = new Style
        {
            BorderWidth = Length.Px(1),
            BorderColor = Color.Black,
            BorderStyle = BorderStyle.Solid,
            BorderTopWidth = Length.Px(5),
            BorderTopColor = Color.Red
        };

        var computed = ComputedStyle.FromStyle(style);

        // Top should use per-side values
        computed.BorderTopWidth.Value.ShouldBe(5);
        computed.BorderTopColor.ShouldBe(Color.Red);
        computed.BorderTopStyle.ShouldBe(BorderStyle.Solid); // Falls back to uniform

        // Other sides should use uniform values
        computed.BorderRightWidth.Value.ShouldBe(1);
        computed.BorderBottomWidth.Value.ShouldBe(1);
        computed.BorderLeftWidth.Value.ShouldBe(1);
        computed.BorderRightColor.ShouldBe(Color.Black);
        computed.BorderBottomColor.ShouldBe(Color.Black);
        computed.BorderLeftColor.ShouldBe(Color.Black);
    }

    [Fact]
    public void ComputedStyle_FromStyle_ShouldUseDefaultsWhenNoStyleSet()
    {
        var computed = ComputedStyle.FromStyle(null);

        computed.BorderTopWidth.Value.ShouldBe(0);
        computed.BorderRightWidth.Value.ShouldBe(0);
        computed.BorderBottomWidth.Value.ShouldBe(0);
        computed.BorderLeftWidth.Value.ShouldBe(0);

        computed.BorderTopStyle.ShouldBe(BorderStyle.None);
        computed.BorderRightStyle.ShouldBe(BorderStyle.None);
        computed.BorderBottomStyle.ShouldBe(BorderStyle.None);
        computed.BorderLeftStyle.ShouldBe(BorderStyle.None);
    }

    [Fact]
    public void ComputedStyle_ComputedBorderSides_ShouldReturnCorrectBorderSide()
    {
        var style = new Style
        {
            BorderTopWidth = Length.Px(2),
            BorderTopColor = Color.Red,
            BorderTopStyle = BorderStyle.Solid
        };

        var computed = ComputedStyle.FromStyle(style);
        var topBorder = computed.ComputedBorderTop;

        topBorder.Width.Value.ShouldBe(2);
        topBorder.Color.ShouldBe(Color.Red);
        topBorder.Style.ShouldBe(BorderStyle.Solid);
        topBorder.IsVisible.ShouldBeTrue();
    }

    [Fact]
    public void Border_ToBorderSide_ShouldConvertCorrectly()
    {
        var border = new Border(Length.Px(3), BorderStyle.Dashed, Color.Blue);
        var side = border.ToBorderSide();

        side.Width.ShouldBe(Length.Px(3));
        side.Style.ShouldBe(BorderStyle.Dashed);
        side.Color.ShouldBe(Color.Blue);
    }
}
