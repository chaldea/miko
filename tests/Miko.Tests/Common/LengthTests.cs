using Miko.Common;
using Shouldly;

namespace Miko.Tests.Common;

public class LengthTests
{
    [Fact]
    public void Px_ShouldCreatePixelLength()
    {
        var length = Length.Px(100);

        length.Value.ShouldBe(100);
        length.Unit.ShouldBe(LengthUnit.Px);
        length.IsAuto.ShouldBeFalse();
    }

    [Fact]
    public void Percent_ShouldCreatePercentLength()
    {
        var length = Length.Percent(50);

        length.Value.ShouldBe(50);
        length.Unit.ShouldBe(LengthUnit.Percent);
        length.IsAuto.ShouldBeFalse();
    }

    [Fact]
    public void Auto_ShouldCreateAutoLength()
    {
        var length = Length.Auto;

        length.Unit.ShouldBe(LengthUnit.Auto);
        length.IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void ToPixels_WithPixelUnit_ShouldReturnValue()
    {
        var length = Length.Px(100);
        var pixels = length.ToPixels(500);

        pixels.ShouldBe(100);
    }

    [Fact]
    public void ToPixels_WithPercentUnit_ShouldCalculateFromContainer()
    {
        var length = Length.Percent(50);
        var pixels = length.ToPixels(500);

        pixels.ShouldBe(250);
    }

    [Fact]
    public void ToPixels_WithAutoUnit_ShouldReturnZero()
    {
        var length = Length.Auto;
        var pixels = length.ToPixels(500);

        pixels.ShouldBe(0);
    }

    [Theory]
    [InlineData(100, LengthUnit.Px, "100px")]
    [InlineData(50, LengthUnit.Percent, "50%")]
    [InlineData(0, LengthUnit.Auto, "auto")]
    public void ToString_ShouldFormatCorrectly(float value, LengthUnit unit, string expected)
    {
        var length = new Length(value, unit);
        var str = length.ToString();

        str.ShouldBe(expected);
    }

    [Fact]
    public void ImplicitConversion_FromFloat_ShouldCreatePixelLength()
    {
        Length length = 100f;

        length.Value.ShouldBe(100);
        length.Unit.ShouldBe(LengthUnit.Px);
    }
}
