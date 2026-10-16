using Miko.Common;
using Shouldly;

namespace Miko.Tests.Common;

/// <summary>ISSUE-150：<see cref="AspectRatio"/> 值类型——比值、退化、关键词与书写形式。</summary>
public class AspectRatioTests
{
    [Fact]
    public void Should_default_to_auto()
    {
        default(AspectRatio).ShouldBe(AspectRatio.Auto);
        AspectRatio.Auto.HasRatio.ShouldBeFalse();
        AspectRatio.Auto.HasAutoKeyword.ShouldBeTrue();
        AspectRatio.Auto.ToString().ShouldBe("auto");
    }

    [Theory]
    [InlineData(16f, 9f, 16f / 9f)]
    [InlineData(1f, 1f, 1f)]
    [InlineData(3f, 4f, 0.75f)]
    public void Should_compute_width_over_height(float width, float height, float expected)
    {
        var ratio = new AspectRatio(width, height);

        ratio.HasRatio.ShouldBeTrue();
        ratio.HasAutoKeyword.ShouldBeFalse();
        ratio.Value.ShouldBe(expected, 0.0001f);
    }

    [Fact]
    public void Should_treat_single_number_as_number_over_one()
    {
        new AspectRatio(0.5f).ShouldBe(new AspectRatio(0.5f, 1f));
        new AspectRatio(0.5f).ToString().ShouldBe("0.5 / 1");
    }

    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(1f, 0f)]
    [InlineData(0f, 0f)]
    [InlineData(float.PositiveInfinity, 1f)]
    [InlineData(1f, float.PositiveInfinity)]
    public void Should_behave_as_auto_when_degenerate(float width, float height)
    {
        var ratio = new AspectRatio(width, height);

        ratio.HasRatio.ShouldBeFalse();
        ratio.HasAutoKeyword.ShouldBeTrue();
        ratio.Value.ShouldBe(0f);
    }

    [Theory]
    [InlineData(-1f, 1f)]
    [InlineData(1f, -1f)]
    [InlineData(float.NaN, 1f)]
    public void Should_reject_invalid_components(float width, float height)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new AspectRatio(width, height));
    }

    [Fact]
    public void Should_keep_auto_keyword_with_ratio()
    {
        var ratio = AspectRatio.AutoOr(16, 9);

        ratio.HasRatio.ShouldBeTrue();
        ratio.HasAutoKeyword.ShouldBeTrue();
        ratio.ShouldNotBe(new AspectRatio(16, 9));
        ratio.ToString().ShouldBe("auto 16 / 9");
    }

    [Fact]
    public void Should_format_ratio_in_css_syntax()
    {
        new AspectRatio(16, 9).ToString().ShouldBe("16 / 9");
    }
}
