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

    // ---- 算术运算符 ----

    [Fact]
    public void Add_SameUnit_ShouldKeepUnit()
    {
        var result = Length.Rem(0.375f) + Length.Rem(1f);

        result.Value.ShouldBe(1.375f);
        result.Unit.ShouldBe(LengthUnit.Rem);
    }

    [Fact]
    public void Subtract_SameUnit_ShouldKeepUnit()
    {
        var result = Length.Px(20) - Length.Px(6);

        result.Value.ShouldBe(14);
        result.Unit.ShouldBe(LengthUnit.Px);
    }

    [Fact]
    public void AddSubtract_PercentSameUnit_ShouldKeepPercent()
    {
        var result = Length.Percent(50) + Length.Percent(10);

        result.Value.ShouldBe(60);
        result.Unit.ShouldBe(LengthUnit.Percent);
    }

    [Fact]
    public void Add_PxAndRem_ShouldNormalizeToPx()
    {
        var previous = Length.RootFontSize;
        Length.RootFontSize = 16f;
        try
        {
            var result = Length.Px(10) + Length.Rem(1f);

            result.Value.ShouldBe(26); // 10 + 1*16
            result.Unit.ShouldBe(LengthUnit.Px);
        }
        finally
        {
            Length.RootFontSize = previous;
        }
    }

    [Fact]
    public void Subtract_RemAndPx_ShouldNormalizeToPx()
    {
        var previous = Length.RootFontSize;
        Length.RootFontSize = 16f;
        try
        {
            var result = Length.Rem(2f) - Length.Px(8);

            result.Value.ShouldBe(24); // 2*16 - 8
            result.Unit.ShouldBe(LengthUnit.Px);
        }
        finally
        {
            Length.RootFontSize = previous;
        }
    }

    [Fact]
    public void Add_PxAndPercent_ShouldThrow()
    {
        Should.Throw<InvalidOperationException>(() => Length.Px(10) + Length.Percent(5));
    }

    [Fact]
    public void Add_RemAndPercent_ShouldThrow()
    {
        Should.Throw<InvalidOperationException>(() => Length.Rem(1f) + Length.Percent(5));
    }

    [Fact]
    public void Add_AutoAndPx_ShouldThrow()
    {
        Should.Throw<InvalidOperationException>(() => Length.Auto + Length.Px(5));
    }

    [Fact]
    public void UnaryNegation_ShouldNegateValueKeepUnit()
    {
        var result = -Length.Rem(1.5f);

        result.Value.ShouldBe(-1.5f);
        result.Unit.ShouldBe(LengthUnit.Rem);
    }

    [Fact]
    public void Multiply_ByScalar_ShouldScaleValueKeepUnit()
    {
        (Length.Rem(1f) * 2f).Value.ShouldBe(2f);
        (Length.Rem(1f) * 2f).Unit.ShouldBe(LengthUnit.Rem);

        // 标量在左
        (3f * Length.Px(10)).Value.ShouldBe(30);
        (3f * Length.Px(10)).Unit.ShouldBe(LengthUnit.Px);
    }

    [Fact]
    public void Divide_ByScalar_ShouldScaleValueKeepUnit()
    {
        var result = Length.Px(100) / 4f;

        result.Value.ShouldBe(25);
        result.Unit.ShouldBe(LengthUnit.Px);
    }

    [Fact]
    public void Operators_CanCompose()
    {
        // (0.375rem + 1rem) * 2 = 2.75rem
        var result = (Length.Rem(0.375f) + Length.Rem(1f)) * 2f;

        result.Value.ShouldBe(2.75f);
        result.Unit.ShouldBe(LengthUnit.Rem);
    }
}
