using Miko.Common;
using Shouldly;

namespace Miko.Tests.Common;

public class BoxShadowTests
{
    [Fact]
    public void Constructor_Float_ShouldSetAllProperties()
    {
        var shadow = new BoxShadow(1, 2, 3, 4, Color.Black);

        shadow.OffsetX.ShouldBe(1f);
        shadow.OffsetY.ShouldBe(2f);
        shadow.BlurRadius.ShouldBe(3f);
        shadow.SpreadRadius.ShouldBe(4f);
        shadow.Color.ShouldBe(Color.Black);
        shadow.Inset.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_Float_WithInset_ShouldSetInset()
    {
        var shadow = new BoxShadow(0, 0, 0, 0, Color.Black, true);

        shadow.Inset.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_Length_ShouldConvertToFloat()
    {
        var shadow = new BoxShadow(
            Length.Px(0), Length.Px(0), Length.Px(0), Length.Rem(0.25f),
            Color.FromRgba(13, 110, 253, 0.25f));

        shadow.OffsetX.ShouldBe(0f);
        shadow.OffsetY.ShouldBe(0f);
        shadow.BlurRadius.ShouldBe(0f);
        shadow.SpreadRadius.ShouldBe(4f);
        shadow.Color.A.ShouldBe((byte)64);
    }

    [Fact]
    public void Constructor_Length_WithInset_ShouldSetInset()
    {
        var shadow = new BoxShadow(
            Length.Px(0), Length.Px(0), Length.Px(0), Length.Px(0),
            Color.Black, inset: true);

        shadow.Inset.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_Length_Px_ShouldUsePixelValues()
    {
        var shadow = new BoxShadow(
            Length.Px(1), Length.Px(2), Length.Px(4), Length.Px(0),
            Color.FromRgba(0, 0, 0, 0.19f));

        shadow.OffsetX.ShouldBe(1f);
        shadow.OffsetY.ShouldBe(2f);
        shadow.BlurRadius.ShouldBe(4f);
        shadow.SpreadRadius.ShouldBe(0f);
    }
}
