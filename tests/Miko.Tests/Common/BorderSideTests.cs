using Miko.Common;
using Shouldly;

namespace Miko.Tests.Common;

public class BorderSideTests
{
    [Fact]
    public void BorderSide_None_ShouldHaveZeroWidthAndNoneStyle()
    {
        var side = BorderSide.None;

        side.Width.Value.ShouldBe(0);
        side.Style.ShouldBe(BorderStyle.None);
        side.Color.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void BorderSide_None_ShouldNotBeVisible()
    {
        var side = BorderSide.None;

        side.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithWidthOnly_ShouldDefaultToSolidBlack()
    {
        var side = new BorderSide(Length.Px(2));

        side.Width.Value.ShouldBe(2);
        side.Style.ShouldBe(BorderStyle.Solid);
        side.Color.ShouldBe(Color.Black);
    }

    [Fact]
    public void Constructor_WithWidthAndColor_ShouldDefaultToSolid()
    {
        var side = new BorderSide(Length.Px(3), Color.Red);

        side.Width.Value.ShouldBe(3);
        side.Style.ShouldBe(BorderStyle.Solid);
        side.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetAllProperties()
    {
        var side = new BorderSide(Length.Px(5), BorderStyle.Dashed, Color.Blue);

        side.Width.Value.ShouldBe(5);
        side.Style.ShouldBe(BorderStyle.Dashed);
        side.Color.ShouldBe(Color.Blue);
    }

    [Fact]
    public void IsVisible_WithValidBorder_ShouldReturnTrue()
    {
        var side = new BorderSide(Length.Px(2), BorderStyle.Solid, Color.Red);

        side.IsVisible.ShouldBeTrue();
    }

    [Fact]
    public void IsVisible_WithZeroWidth_ShouldReturnFalse()
    {
        var side = new BorderSide(Length.Px(0), BorderStyle.Solid, Color.Red);

        side.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public void IsVisible_WithNoneStyle_ShouldReturnFalse()
    {
        var side = new BorderSide(Length.Px(2), BorderStyle.None, Color.Red);

        side.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public void IsVisible_WithTransparentColor_ShouldReturnFalse()
    {
        var side = new BorderSide(Length.Px(2), BorderStyle.Solid, Color.Transparent);

        side.IsVisible.ShouldBeFalse();
    }

    [Theory]
    [InlineData(BorderStyle.Solid)]
    [InlineData(BorderStyle.Dashed)]
    [InlineData(BorderStyle.Dotted)]
    public void IsVisible_WithDifferentStyles_ShouldReturnTrue(BorderStyle style)
    {
        var side = new BorderSide(Length.Px(1), style, Color.Black);

        side.IsVisible.ShouldBeTrue();
    }
}
