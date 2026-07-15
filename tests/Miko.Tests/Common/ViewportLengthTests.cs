using Miko.Common;
using Shouldly;

namespace Miko.Tests.Common;

/// <summary>
/// 视窗单位 vw/vh 的 <see cref="Length"/> 层单元测试（ISSUE-091）。
/// 覆盖：工厂方法、单位/数值兼容 API、ResolveViewport 折算（基础与 calc 复合）、
/// 各类算术运算符保留视窗分量、ToString、与安全区分量共存、幂等。
/// </summary>
public class ViewportLengthTests
{
    [Fact]
    public void Vw_ShouldCreateViewportWidthLength()
    {
        var length = Length.Vw(100);

        length.Value.ShouldBe(100);
        length.Unit.ShouldBe(LengthUnit.Vw);
        length.IsAuto.ShouldBeFalse();
        length.HasViewportComponent.ShouldBeTrue();
    }

    [Fact]
    public void Vh_ShouldCreateViewportHeightLength()
    {
        var length = Length.Vh(50);

        length.Value.ShouldBe(50);
        length.Unit.ShouldBe(LengthUnit.Vh);
        length.HasViewportComponent.ShouldBeTrue();
    }

    [Fact]
    public void PlainPx_HasNoViewportComponent()
    {
        Length.Px(10).HasViewportComponent.ShouldBeFalse();
    }

    [Fact]
    public void BeforeResolve_ToPixelsIsZero()
    {
        // 未折算的 vw/vh 缺少视口上下文，ToPixels 按 0 计（与 env() 语义一致）。
        Length.Vw(100).ToPixels(500).ShouldBe(0);
        Length.Vh(100).ToPixels(500).ShouldBe(0);
    }

    [Fact]
    public void ResolveViewport_Vw_FoldsAgainstViewportWidth()
    {
        // 100vw 相对视口宽度：1000 宽 → 1000px（与传入的 containerSize 无关）。
        Length.Vw(100).ResolveViewport(1000, 800).ToPixels(500).ShouldBe(1000);
        Length.Vw(50).ResolveViewport(1000, 800).ToPixels(500).ShouldBe(500);
    }

    [Fact]
    public void ResolveViewport_Vh_FoldsAgainstViewportHeight()
    {
        Length.Vh(100).ResolveViewport(1000, 800).ToPixels(500).ShouldBe(800);
        Length.Vh(25).ResolveViewport(1000, 800).ToPixels(500).ShouldBe(200);
    }

    [Fact]
    public void ResolveViewport_IndependentOfContainerSize()
    {
        // vw/vh 不随包含块尺寸变化：不同 containerSize 得到相同像素值。
        var resolved = Length.Vw(10).ResolveViewport(1000, 800);
        resolved.ToPixels(200).ShouldBe(100);
        resolved.ToPixels(9999).ShouldBe(100);
    }

    [Fact]
    public void ResolveViewport_IssueExample_CalcVwMinusPx()
    {
        // width: calc(100vw - 240px) 在 1280 宽视口下 → 1040px。
        var len = Length.Vw(100) - Length.Px(240);
        var resolved = len.ResolveViewport(1280, 720);

        resolved.ToPixels(0).ShouldBe(1040);
        resolved.HasViewportComponent.ShouldBeFalse(); // 折算后不再是符号性长度
    }

    [Fact]
    public void ResolveViewport_MixedVwVh_FoldsBoth()
    {
        // calc(50vw + 50vh)：1000 宽 + 800 高 → 500 + 400 = 900。
        var len = Length.Vw(50) + Length.Vh(50);
        len.ResolveViewport(1000, 800).ToPixels(0).ShouldBe(900);
    }

    [Fact]
    public void ResolveViewport_PreservesPercentComponent()
    {
        // 复合 calc(100vw - 10%)：vw 折算成 px，百分比一路保留到 ToPixels 按容器解析。
        var len = Length.Vw(100) - Length.Percent(10);
        var resolved = len.ResolveViewport(1000, 800);

        // 1000 - 10% of 200 = 1000 - 20 = 980
        resolved.ToPixels(200).ShouldBe(980);
    }

    [Fact]
    public void ResolveViewport_Auto_StaysAuto()
    {
        Length.Auto.ResolveViewport(1000, 800).IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void ResolveViewport_NoViewportComponent_ReturnsUnchanged()
    {
        var len = Length.Px(42);
        len.ResolveViewport(1000, 800).ToPixels(0).ShouldBe(42);
    }

    [Fact]
    public void ResolveViewport_Idempotent()
    {
        var once = Length.Vw(100).ResolveViewport(1000, 800);
        var twice = once.ResolveViewport(1000, 800);

        // 折算后视窗系数已清零，重复折算不会重复计入。
        twice.ToPixels(0).ShouldBe(1000);
    }

    // ---- 算术运算符保留视窗分量 ----

    [Fact]
    public void Add_VwAndVw_KeepsUnit()
    {
        var result = Length.Vw(30) + Length.Vw(20);

        result.Value.ShouldBe(50);
        result.Unit.ShouldBe(LengthUnit.Vw);
    }

    [Fact]
    public void Subtract_VhAndVh_KeepsUnit()
    {
        var result = Length.Vh(80) - Length.Vh(30);

        result.Value.ShouldBe(50);
        result.Unit.ShouldBe(LengthUnit.Vh);
    }

    [Fact]
    public void Negate_Vw_NegatesValue()
    {
        var result = -Length.Vw(25);

        result.ResolveViewport(1000, 800).ToPixels(0).ShouldBe(-250);
    }

    [Fact]
    public void Multiply_Vw_ScalesComponent()
    {
        (Length.Vw(10) * 3f).ResolveViewport(1000, 800).ToPixels(0).ShouldBe(300);
        (2f * Length.Vh(10)).ResolveViewport(1000, 800).ToPixels(0).ShouldBe(160);
    }

    [Fact]
    public void Divide_Vw_ScalesComponent()
    {
        (Length.Vw(100) / 4f).ResolveViewport(1000, 800).ToPixels(0).ShouldBe(250);
    }

    // ---- ToString ----

    [Theory]
    [InlineData(100f, LengthUnit.Vw, "100vw")]
    [InlineData(50f, LengthUnit.Vh, "50vh")]
    public void ToString_SingleUnit_FormatsCorrectly(float value, LengthUnit unit, string expected)
    {
        new Length(value, unit).ToString().ShouldBe(expected);
    }

    [Fact]
    public void ToString_Composite_ListsViewportComponent()
    {
        (Length.Vw(100) - Length.Px(240)).ToString().ShouldBe("-240px + 100vw");
    }

    // ---- 与安全区分量共存（ResolveSafeArea 不应吞掉 vw/vh，反之亦然）----

    [Fact]
    public void ResolveSafeArea_PreservesViewportComponent()
    {
        // calc(100vh - env(safe-area-inset-bottom))：先折算安全区，vw/vh 分量保留。
        var len = Length.Vh(100) - Length.SafeAreaInsetBottom;
        var afterSafe = len.ResolveSafeArea(new SafeAreaInsets(0, 0, 0, 24));

        afterSafe.HasViewportComponent.ShouldBeTrue();
        // 再折算视口：800 高 - 24 安全区 = 776。
        afterSafe.ResolveViewport(1000, 800).ToPixels(0).ShouldBe(776);
    }

    [Fact]
    public void ResolveViewport_PreservesSafeAreaComponent()
    {
        // 折算顺序无关：先折算视口，安全区分量仍应保留待后续折算。
        var len = Length.Vh(100) - Length.SafeAreaInsetBottom;
        var afterViewport = len.ResolveViewport(1000, 800);

        afterViewport.HasSafeAreaComponent.ShouldBeTrue();
        afterViewport.ResolveSafeArea(new SafeAreaInsets(0, 0, 0, 24)).ToPixels(0).ShouldBe(776);
    }
}
