using Miko.Common;
using Shouldly;

namespace Miko.Tests.Common;

public class ColorTests
{
    [Fact]
    public void FromRgb_ShouldCreateColor()
    {
        var color = Color.FromRgb(255, 128, 64);

        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)128);
        color.B.ShouldBe((byte)64);
        color.A.ShouldBe((byte)255);
    }

    [Fact]
    public void FromRgba_ShouldCreateColorWithAlpha()
    {
        var color = Color.FromRgba(255, 128, 64, 128);

        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)128);
        color.B.ShouldBe((byte)64);
        color.A.ShouldBe((byte)128);
    }

    [Theory]
    [InlineData("#FF8040", 255, 128, 64, 255)]
    [InlineData("#FF804080", 255, 128, 64, 128)]
    public void FromHex_ShouldParseHexColor(string hex, byte r, byte g, byte b, byte a)
    {
        var color = Color.FromHex(hex);

        color.R.ShouldBe(r);
        color.G.ShouldBe(g);
        color.B.ShouldBe(b);
        color.A.ShouldBe(a);
    }

    [Fact]
    public void FromHex_WithoutHash_ShouldParseCorrectly()
    {
        var color = Color.FromHex("FF8040");

        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)128);
        color.B.ShouldBe((byte)64);
    }

    [Fact]
    public void PredefinedColors_ShouldHaveCorrectValues()
    {
        Color.White.R.ShouldBe((byte)255);
        Color.White.G.ShouldBe((byte)255);
        Color.White.B.ShouldBe((byte)255);

        Color.Black.R.ShouldBe((byte)0);
        Color.Black.G.ShouldBe((byte)0);
        Color.Black.B.ShouldBe((byte)0);

        Color.Transparent.A.ShouldBe((byte)0);
    }

    [Fact]
    public void ToSKColor_ShouldConvertCorrectly()
    {
        var color = Color.FromRgba(255, 128, 64, 200);
        var skColor = color.ToSKColor();

        skColor.Red.ShouldBe((byte)255);
        skColor.Green.ShouldBe((byte)128);
        skColor.Blue.ShouldBe((byte)64);
        skColor.Alpha.ShouldBe((byte)200);
    }

    [Fact]
    public void ToString_ShouldReturnRgbaFormat()
    {
        var color = Color.FromRgba(255, 128, 64, 200);
        var str = color.ToString();

        str.ShouldBe("rgba(255, 128, 64, 200)");
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(0.25f, 64)]
    [InlineData(0.5f, 128)]
    [InlineData(1f, 255)]
    public void FromRgba_FloatAlpha_ShouldConvertToByteAlpha(float alpha, byte expected)
    {
        var color = Color.FromRgba(13, 110, 253, alpha);

        color.R.ShouldBe((byte)13);
        color.G.ShouldBe((byte)110);
        color.B.ShouldBe((byte)253);
        color.A.ShouldBe(expected);
    }

    [Fact]
    public void FromRgba_FloatAlpha_ShouldClampAboveOne()
    {
        var color = Color.FromRgba(0, 0, 0, 1.5f);
        color.A.ShouldBe((byte)255);
    }

    [Fact]
    public void FromRgba_FloatAlpha_ShouldClampBelowZero()
    {
        var color = Color.FromRgba(0, 0, 0, -0.5f);
        color.A.ShouldBe((byte)0);
    }
}
