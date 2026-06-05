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

    [Fact]
    public void Em_ShouldCreateEmLength()
    {
        var length = Length.Em(1.5f);

        length.Value.ShouldBe(1.5f);
        length.Unit.ShouldBe(LengthUnit.Em);
        length.IsAuto.ShouldBeFalse();
    }

    [Fact]
    public void ToPixels_WithEmUnit_ShouldResolveAgainstFontSize()
    {
        var length = Length.Em(2f);
        var pixels = length.ToPixels(500, fontSize: 20f);

        pixels.ShouldBe(40); // 2 * 20
    }

    [Fact]
    public void ToPixels_WithEmUnit_NoFontSize_ShouldFallBackToRootFontSize()
    {
        var previous = Length.RootFontSize;
        Length.RootFontSize = 16f;
        try
        {
            var length = Length.Em(2f);
            var pixels = length.ToPixels(500);

            pixels.ShouldBe(32); // 2 * 16 (root fallback)
        }
        finally
        {
            Length.RootFontSize = previous;
        }
    }

    [Theory]
    [InlineData(100, LengthUnit.Px, "100px")]
    [InlineData(50, LengthUnit.Percent, "50%")]
    [InlineData(0, LengthUnit.Auto, "auto")]
    [InlineData(1.5f, LengthUnit.Em, "1.5em")]
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

    // 复合长度：混合单位不再提前折算，而是保留各分量，待 ToPixels 时按上下文解析。

    [Fact]
    public void Add_PxAndRem_ShouldDeferAndResolveViaRoot()
    {
        var previous = Length.RootFontSize;
        Length.RootFontSize = 16f;
        try
        {
            var result = Length.Px(10) + Length.Rem(1f);

            // 不再提前折算；按 px+rem 分量在 ToPixels 时解析：10 + 1*16 = 26
            result.ToPixels(0).ShouldBe(26);
        }
        finally
        {
            Length.RootFontSize = previous;
        }
    }

    [Fact]
    public void Subtract_RemAndPx_ShouldDeferAndResolveViaRoot()
    {
        var previous = Length.RootFontSize;
        Length.RootFontSize = 16f;
        try
        {
            var result = Length.Rem(2f) - Length.Px(8);

            result.ToPixels(0).ShouldBe(24); // 2*16 - 8
        }
        finally
        {
            Length.RootFontSize = previous;
        }
    }

    [Fact]
    public void Add_PxAndEm_ShouldResolveAgainstSuppliedFontSize()
    {
        var previous = Length.RootFontSize;
        Length.RootFontSize = 16f;
        try
        {
            var result = Length.Px(10) + Length.Em(1f);

            // em 推迟到 ToPixels 时按元素字体大小解析，而非提前用 RootFontSize 折算
            result.ToPixels(0, fontSize: 20f).ShouldBe(30); // 10 + 1*20
            result.ToPixels(0).ShouldBe(26);                // 无 fontSize 时回退 root：10 + 1*16
        }
        finally
        {
            Length.RootFontSize = previous;
        }
    }

    [Fact]
    public void Add_PxAndPercent_ShouldComposeAndResolveAgainstContainer()
    {
        // 旧实现抛异常；复合长度应组合并在 ToPixels 时按容器尺寸解析百分比
        var result = Length.Px(10) + Length.Percent(50);

        result.ToPixels(200).ShouldBe(110); // 10 + 50% of 200
    }

    [Fact]
    public void Add_RemAndPercent_ShouldCompose()
    {
        var previous = Length.RootFontSize;
        Length.RootFontSize = 16f;
        try
        {
            var result = Length.Rem(1f) + Length.Percent(10);

            result.ToPixels(200).ShouldBe(36); // 1*16 + 10% of 200
        }
        finally
        {
            Length.RootFontSize = previous;
        }
    }

    [Fact]
    public void Add_AutoAndPx_ShouldStayAuto()
    {
        // auto 不与具体长度混合：含 auto 的算术结果仍为 auto
        var result = Length.Auto + Length.Px(5);

        result.IsAuto.ShouldBeTrue();
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

    // ---- 复合长度 / em 延迟解析（ISSUE-040 后续：Bootstrap form-control min-height）----

    /// <summary>
    /// Bootstrap 用 <c>calc(1.5em + 0.5rem + 2px)</c> 设置 .form-control-sm 的 min-height，
    /// 其中 em 相对元素自身字体大小。验证复合长度按元素字体大小解析 em，而非提前用 root 折算。
    /// </summary>
    [Fact]
    public void Composite_EmRemPx_ResolvesEmAgainstElementFontSize()
    {
        var previous = Length.RootFontSize;
        Length.RootFontSize = 16f;
        try
        {
            // 1.5em + 0.5rem + 2px
            var len = Length.Em(1.5f) + Length.Rem(0.5f) + Length.Px(2);

            // form-control-lg：font-size 20px → 1.5em=30, 0.5rem=8, +2 = 40
            len.ToPixels(0, fontSize: 20f).ShouldBe(40);
            // form-control-sm：font-size 14px → 1.5em=21, 0.5rem=8, +2 = 31
            len.ToPixels(0, fontSize: 14f).ShouldBe(31);
            // 不同字体大小得到不同结果，证明 em 未被提前折算
            len.ToPixels(0, fontSize: 20f).ShouldNotBe(len.ToPixels(0, fontSize: 14f));
        }
        finally
        {
            Length.RootFontSize = previous;
        }
    }

    [Fact]
    public void Composite_PreservesAllComponents()
    {
        var previous = Length.RootFontSize;
        Length.RootFontSize = 16f;
        try
        {
            // 1em + 1rem + 1px + 10% ，分量独立保留
            var len = Length.Em(1f) + Length.Rem(1f) + Length.Px(1) + Length.Percent(10);

            // fontSize=10, container=200 → 10 + 16 + 1 + 20 = 47
            len.ToPixels(200, fontSize: 10f).ShouldBe(47);
        }
        finally
        {
            Length.RootFontSize = previous;
        }
    }

    [Fact]
    public void Composite_Scaling_ScalesAllComponents()
    {
        var previous = Length.RootFontSize;
        Length.RootFontSize = 16f;
        try
        {
            // (1em + 2px) * 3 = 3em + 6px
            var len = (Length.Em(1f) + Length.Px(2)) * 3f;

            len.ToPixels(0, fontSize: 10f).ShouldBe(36); // 3*10 + 6
        }
        finally
        {
            Length.RootFontSize = previous;
        }
    }

    [Fact]
    public void Composite_ToString_ListsComponents()
    {
        var len = Length.Em(1.5f) + Length.Rem(0.5f) + Length.Px(2);

        // 复合长度列出各非零分量（顺序：em, rem, px, %）
        len.ToString().ShouldBe("1.5em + 0.5rem + 2px");
    }
}
