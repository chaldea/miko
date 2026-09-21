using System.ComponentModel;
using System.Globalization;
using Miko.Common;
using Shouldly;

namespace Miko.Tests.Common;

/// <summary>
/// <see cref="Length"/> 的语义安全网（ISSUE-142 阶段 1）。
///
/// <para>ISSUE-142 要把 <see cref="Length"/> 的内部表示从「11 个单位分量并存」换成
/// 「单值 + 单位判别式，混合分量逃逸到旁路对象」。公开 API 一字不改，但语义面很宽：
/// <c>Value</c> / <c>Unit</c> 两个兼容门面对复合长度有一套历史的「主分量回退」规则、
/// 算术按分量累加、<c>ResolveViewport</c> / <c>ResolveSafeArea</c> 折算后清零并保持幂等。
/// 这些行为<b>没有</b>被现有测试逐条覆盖，换表示时极易漂移而不被发现。</para>
///
/// <para>本文件在改动<b>之前</b>把当前行为逐条钉住，因此断言的是「现状」而非「理想」
/// ——例如复合长度的 <c>Unit</c> 报告为 <c>Px</c>，这是刻意保留的兼容行为。</para>
/// </summary>
public class LengthSemanticsTests
{
    /// <summary>在固定的 RootFontSize 下执行——rem/em 回退都依赖它，且它是全局可变状态。</summary>
    private static void WithRootFontSize(float size, Action body)
    {
        var previous = Length.RootFontSize;
        Length.RootFontSize = size;
        try { body(); }
        finally { Length.RootFontSize = previous; }
    }

    // ---- 构造与单位门面：9 种单位逐一 ----

    [Theory]
    [InlineData(LengthUnit.Px, 12f)]
    [InlineData(LengthUnit.Percent, 12f)]
    [InlineData(LengthUnit.Rem, 12f)]
    [InlineData(LengthUnit.Em, 12f)]
    [InlineData(LengthUnit.Vw, 12f)]
    [InlineData(LengthUnit.Vh, 12f)]
    [InlineData(LengthUnit.Number, 12f)]
    public void Constructor_SingleUnit_RoundTripsThroughValueAndUnit(LengthUnit unit, float value)
    {
        var length = new Length(value, unit);

        length.Unit.ShouldBe(unit);
        length.Value.ShouldBe(value);
        length.IsAuto.ShouldBeFalse();
        length.IsFitContent.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_Auto_ReportsAutoWithZeroValue()
    {
        var length = new Length(99f, LengthUnit.Auto);

        length.IsAuto.ShouldBeTrue();
        length.IsFitContent.ShouldBeFalse();
        length.Unit.ShouldBe(LengthUnit.Auto);
        length.Value.ShouldBe(0);   // auto 丢弃传入的数值
    }

    /// <summary>
    /// fit-content 与 auto 走同一条「按内容测量」的布局路径，因此 <c>IsAuto</c> 亦为 true；
    /// 区分点只在脱离文档流的定型规则（见 <see cref="Length.IsFitContent"/>）。
    /// </summary>
    [Fact]
    public void FitContent_IsAlsoAuto()
    {
        Length.FitContent.IsFitContent.ShouldBeTrue();
        Length.FitContent.IsAuto.ShouldBeTrue();
        Length.FitContent.Unit.ShouldBe(LengthUnit.FitContent);

        Length.Auto.IsFitContent.ShouldBeFalse();
    }

    [Fact]
    public void Default_IsZeroPixels()
    {
        // default(Length) 必须等价于 0px——它是 Style 里所有未赋值槽的取值。
        default(Length).Unit.ShouldBe(LengthUnit.Px);
        default(Length).Value.ShouldBe(0);
        default(Length).IsAuto.ShouldBeFalse();
        default(Length).ToPixels(500).ShouldBe(0);
        default(Length).ToString().ShouldBe("0px");
    }

    [Fact]
    public void ZeroOfAnyUnit_ReportsPxUnit()
    {
        // 「主分量」判定靠非零：全零长度没有可识别的单位，一律回落 Px。
        // AnimationManager.UnitsCompatible 正是依赖这条放宽（0 与任意单位可插值）。
        new Length(0, LengthUnit.Rem).Unit.ShouldBe(LengthUnit.Px);
        new Length(0, LengthUnit.Percent).Unit.ShouldBe(LengthUnit.Px);
        new Length(0, LengthUnit.Vh).Unit.ShouldBe(LengthUnit.Px);
    }

    // ---- Value / Unit 的「主分量回退」：复合长度统一报告 px ----

    [Fact]
    public void Composite_ValueAndUnit_FallBackToPxComponent()
    {
        var len = Length.Em(1.5f) + Length.Px(2);

        // 复合长度无单一单位可报：Unit 报 Px、Value 取 px 分量（仅作兼容，应改用 ToPixels）。
        len.Unit.ShouldBe(LengthUnit.Px);
        len.Value.ShouldBe(2);
    }

    [Fact]
    public void Composite_WithoutPxComponent_StillReportsPxUnitAndZeroValue()
    {
        var len = Length.Em(1f) + Length.Rem(1f);

        len.Unit.ShouldBe(LengthUnit.Px);
        len.Value.ShouldBe(0);   // px 分量为 0
    }

    [Fact]
    public void SafeAreaOnlyComponent_ReportsPxUnit()
    {
        // safe-area 系数不在 Unit 的判别链里：符号性长度统一报告 Px（历史行为）。
        Length.SafeAreaInsetTop.Unit.ShouldBe(LengthUnit.Px);
        Length.SafeAreaInsetTop.Value.ShouldBe(0);
    }

    // ---- 谓词 ----

    [Fact]
    public void HasPercentComponent_OnlyWhenNonZeroPercentAndNotAuto()
    {
        Length.Percent(50).HasPercentComponent.ShouldBeTrue();
        (Length.Percent(50) + Length.Px(10)).HasPercentComponent.ShouldBeTrue();
        Length.Percent(0).HasPercentComponent.ShouldBeFalse();
        Length.Px(50).HasPercentComponent.ShouldBeFalse();
        Length.Auto.HasPercentComponent.ShouldBeFalse();
    }

    [Fact]
    public void HasSafeAreaComponent_CoversAllFourSides()
    {
        Length.SafeAreaInsetTop.HasSafeAreaComponent.ShouldBeTrue();
        Length.SafeAreaInsetRight.HasSafeAreaComponent.ShouldBeTrue();
        Length.SafeAreaInsetBottom.HasSafeAreaComponent.ShouldBeTrue();
        Length.SafeAreaInsetLeft.HasSafeAreaComponent.ShouldBeTrue();
        Length.Px(1).HasSafeAreaComponent.ShouldBeFalse();
        Length.Auto.HasSafeAreaComponent.ShouldBeFalse();
    }

    [Fact]
    public void HasViewportComponent_CoversBothAxes()
    {
        Length.Vw(1).HasViewportComponent.ShouldBeTrue();
        Length.Vh(1).HasViewportComponent.ShouldBeTrue();
        Length.Px(1).HasViewportComponent.ShouldBeFalse();
    }

    // ---- ToPixels 求和公式 ----

    [Fact]
    public void ToPixels_SumsAllResolvableComponents()
    {
        WithRootFontSize(16f, () =>
        {
            var len = Length.Px(1) + Length.Rem(1f) + Length.Em(1f) + Length.Percent(10);

            // 1 + 1*16 + 1*10 + 10% of 200 = 1 + 16 + 10 + 20 = 47
            len.ToPixels(200, fontSize: 10f).ShouldBe(47);
        });
    }

    [Fact]
    public void ToPixels_NumberComponent_ScalesByFontSize()
    {
        // 无单位数值（无单位 line-height）：像素 = 系数 × 字体大小。
        Length.Number(1.5f).ToPixels(0, fontSize: 20f).ShouldBe(30);
    }

    [Fact]
    public void ToPixels_NumberComponent_NoFontSize_FallsBackToRoot()
    {
        WithRootFontSize(16f, () => Length.Number(2f).ToPixels(0).ShouldBe(32));
    }

    [Fact]
    public void ToPixels_UnresolvedViewportAndSafeArea_CountAsZero()
    {
        // 缺上下文时 vw/vh/safe 按 0 计，但同一长度里的 px 分量照常计入。
        (Length.Vw(100) + Length.Px(7)).ToPixels(500).ShouldBe(7);
        (Length.SafeAreaInsetTop + Length.Px(7)).ToPixels(500).ShouldBe(7);
    }

    [Fact]
    public void ToPixels_Auto_IsAlwaysZero()
    {
        Length.Auto.ToPixels(500, fontSize: 20f).ShouldBe(0);
        Length.FitContent.ToPixels(500, fontSize: 20f).ShouldBe(0);
    }

    // ---- 折算：清零 + 幂等 + 互不吞噬 ----

    [Fact]
    public void ResolveViewport_FoldsBothAxesAndClearsCoefficients()
    {
        var resolved = (Length.Vw(50) + Length.Vh(25)).ResolveViewport(1000, 800);

        resolved.HasViewportComponent.ShouldBeFalse();
        resolved.ToPixels(0).ShouldBe(500 + 200);
        // 再折算一次不得重复计入。
        resolved.ResolveViewport(1000, 800).ToPixels(0).ShouldBe(700);
    }

    [Fact]
    public void ResolveSafeArea_FoldsAllFourSidesAndClearsCoefficients()
    {
        var insets = new SafeAreaInsets(Left: 1, Top: 2, Right: 4, Bottom: 8);
        var len = Length.SafeAreaInsetTop + Length.SafeAreaInsetRight
                + Length.SafeAreaInsetBottom + Length.SafeAreaInsetLeft;

        var resolved = len.ResolveSafeArea(insets);

        resolved.HasSafeAreaComponent.ShouldBeFalse();
        resolved.ToPixels(0).ShouldBe(1 + 2 + 4 + 8);
        resolved.ResolveSafeArea(insets).ToPixels(0).ShouldBe(15);   // 幂等
    }

    [Fact]
    public void ResolveSafeArea_ScaledCoefficient_MultipliesInset()
    {
        // IonModal 的真实写法：env(safe-area-inset-top) * breakpoint。
        var len = Length.SafeAreaInsetTop * 0.5f;

        len.ResolveSafeArea(new SafeAreaInsets(0, 40, 0, 0)).ToPixels(0).ShouldBe(20);
    }

    [Fact]
    public void Resolve_Auto_StaysAutoOnBothPaths()
    {
        Length.Auto.ResolveViewport(1000, 800).IsAuto.ShouldBeTrue();
        Length.Auto.ResolveSafeArea(new SafeAreaInsets(9, 9, 9, 9)).IsAuto.ShouldBeTrue();
        Length.FitContent.ResolveViewport(1000, 800).IsFitContent.ShouldBeTrue();
        Length.FitContent.ResolveSafeArea(new SafeAreaInsets(9, 9, 9, 9)).IsFitContent.ShouldBeTrue();
    }

    [Fact]
    public void Resolve_NoRelevantComponent_ReturnsUnchanged()
    {
        WithRootFontSize(16f, () =>
        {
            var len = Length.Em(1f) + Length.Percent(10) + Length.Px(3);

            len.ResolveViewport(1000, 800).ToPixels(100, 10f).ShouldBe(len.ToPixels(100, 10f));
            len.ResolveSafeArea(new SafeAreaInsets(5, 5, 5, 5)).ToPixels(100, 10f).ShouldBe(len.ToPixels(100, 10f));
        });
    }

    /// <summary>
    /// 两条折算路径顺序无关：各自只折自己的分量，绝不吞掉对方的
    /// （<c>calc(100vh - env(safe-area-inset-bottom))</c> 是 ion-modal 的真实写法）。
    /// </summary>
    [Fact]
    public void Resolve_OrderIndependent()
    {
        var len = Length.Vh(100) - Length.SafeAreaInsetBottom;
        var insets = new SafeAreaInsets(0, 0, 0, 24);

        len.ResolveViewport(1000, 800).ResolveSafeArea(insets).ToPixels(0).ShouldBe(776);
        len.ResolveSafeArea(insets).ResolveViewport(1000, 800).ToPixels(0).ShouldBe(776);
    }

    // ---- 算术：分量累加 + auto 短路 ----

    [Fact]
    public void Arithmetic_AccumulatesEveryComponent()
    {
        WithRootFontSize(16f, () =>
        {
            var a = Length.Px(1) + Length.Em(2f) + Length.Rem(3f) + Length.Percent(4)
                  + Length.Number(5f) + Length.Vw(6) + Length.Vh(7)
                  + Length.SafeAreaInsetTop + Length.SafeAreaInsetLeft;
            var doubled = a + a;

            var insets = new SafeAreaInsets(Left: 10, Top: 20, Right: 0, Bottom: 0);
            float single = a.ResolveViewport(1000, 800).ResolveSafeArea(insets).ToPixels(200, 10f);
            float twice = doubled.ResolveViewport(1000, 800).ResolveSafeArea(insets).ToPixels(200, 10f);

            twice.ShouldBe(single * 2, tolerance: 0.001f);
        });
    }

    [Fact]
    public void Subtract_ProducesNegativeComponents()
    {
        WithRootFontSize(16f, () =>
        {
            var len = Length.Px(0) - (Length.Em(1f) + Length.Rem(1f) + Length.Percent(10));

            len.ToPixels(200, fontSize: 10f).ShouldBe(-(10 + 16 + 20));
        });
    }

    [Fact]
    public void UnaryNegation_NegatesEveryComponent()
    {
        var len = -(Length.Vw(10) + Length.SafeAreaInsetTop + Length.Px(5));
        var resolved = len.ResolveViewport(1000, 800).ResolveSafeArea(new SafeAreaInsets(0, 30, 0, 0));

        resolved.ToPixels(0).ShouldBe(-(100 + 30 + 5));
    }

    [Fact]
    public void Divide_ScalesEveryComponent()
    {
        var len = (Length.Vw(100) + Length.SafeAreaInsetTop * 4f) / 2f;
        var resolved = len.ResolveViewport(1000, 800).ResolveSafeArea(new SafeAreaInsets(0, 10, 0, 0));

        resolved.ToPixels(0).ShouldBe(500 + 20);
    }

    /// <summary>auto 不与具体长度混合：参与任何运算都直接返回 auto。</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Auto_ShortCircuitsEveryOperator(bool autoOnLeft)
    {
        var auto = Length.Auto;
        var px = Length.Px(5);
        var (x, y) = autoOnLeft ? (auto, px) : (px, auto);

        (x + y).IsAuto.ShouldBeTrue();
        (x - y).IsAuto.ShouldBeTrue();
        (-auto).IsAuto.ShouldBeTrue();
        (auto * 3f).IsAuto.ShouldBeTrue();
        (3f * auto).IsAuto.ShouldBeTrue();
        (auto / 3f).IsAuto.ShouldBeTrue();
    }

    /// <summary>
    /// fit-content 参与运算退化为<b>普通</b> auto：短路返回的是 <see cref="Length.Auto"/>，
    /// 不保留 fit-content 标志。
    /// </summary>
    [Fact]
    public void FitContent_InArithmetic_DegradesToPlainAuto()
    {
        var result = Length.FitContent + Length.Px(5);

        result.IsAuto.ShouldBeTrue();
        result.IsFitContent.ShouldBeFalse();
    }

    [Fact]
    public void ImplicitFromFloat_IsPixels()
    {
        Length len = 12.5f;

        len.Unit.ShouldBe(LengthUnit.Px);
        len.Value.ShouldBe(12.5f);
    }

    // ---- ToString：单分量简洁写法 vs 复合写法 ----

    [Theory]
    [InlineData(16f, LengthUnit.Px, "16px")]
    [InlineData(1.5f, LengthUnit.Em, "1.5em")]
    [InlineData(0.5f, LengthUnit.Rem, "0.5rem")]
    [InlineData(50f, LengthUnit.Percent, "50%")]
    [InlineData(100f, LengthUnit.Vw, "100vw")]
    [InlineData(80f, LengthUnit.Vh, "80vh")]
    [InlineData(1.5f, LengthUnit.Number, "1.5")]
    [InlineData(0f, LengthUnit.Auto, "auto")]
    [InlineData(0f, LengthUnit.FitContent, "fit-content")]
    public void ToString_SingleComponent_UsesConciseForm(float value, LengthUnit unit, string expected)
    {
        new Length(value, unit).ToString().ShouldBe(expected);
    }

    [Fact]
    public void ToString_SafeArea_RendersEnvFunction()
    {
        Length.SafeAreaInsetTop.ToString().ShouldBe("env(safe-area-inset-top)");
        Length.SafeAreaInsetRight.ToString().ShouldBe("env(safe-area-inset-right)");
        Length.SafeAreaInsetBottom.ToString().ShouldBe("env(safe-area-inset-bottom)");
        Length.SafeAreaInsetLeft.ToString().ShouldBe("env(safe-area-inset-left)");
    }

    [Fact]
    public void ToString_ScaledSafeArea_RendersCoefficient()
    {
        (Length.SafeAreaInsetTop * 2f).ToString().ShouldBe("2 * env(safe-area-inset-top)");
    }

    /// <summary>复合写法按固定顺序列出各非零分量：em, rem, number, px, %, vw, vh, safe*。</summary>
    [Fact]
    public void ToString_Composite_ListsComponentsInDeclaredOrder()
    {
        var len = Length.Vh(1) + Length.Vw(2) + Length.Percent(3) + Length.Px(4)
                + Length.Number(5f) + Length.Rem(6f) + Length.Em(7f) + Length.SafeAreaInsetTop;

        len.ToString().ShouldBe("7em + 6rem + 5 + 4px + 3% + 2vw + 1vh + env(safe-area-inset-top)");
    }

    [Fact]
    public void ToString_CompositeCancellingToZero_RendersZeroPixels()
    {
        // 分量互相抵消后没有可列出的项：回落 "0px"。
        (Length.Px(5) - Length.Px(5)).ToString().ShouldBe("0px");
    }

    // ---- 相等语义（ISSUE-142 风险表第 1 行）----
    // 大量测试写 `style.Width.ShouldBe(Length.Px(1))`，依赖 Length 的相等比较。

    [Fact]
    public void Equality_SameSingleComponent_AreEqual()
    {
        Length.Px(12).ShouldBe(Length.Px(12));
        Length.Rem(1.5f).ShouldBe(Length.Rem(1.5f));
        Length.Auto.ShouldBe(Length.Auto);
        Length.FitContent.ShouldBe(Length.FitContent);
        Length.SafeAreaInsetTop.ShouldBe(Length.SafeAreaInsetTop);
    }

    [Fact]
    public void Equality_DifferentUnitSameNumber_AreNotEqual()
    {
        Length.Px(12).ShouldNotBe(Length.Rem(12));
        Length.Percent(50).ShouldNotBe(Length.Px(50));
        Length.Vw(1).ShouldNotBe(Length.Vh(1));
        Length.SafeAreaInsetTop.ShouldNotBe(Length.SafeAreaInsetLeft);
        Length.Auto.ShouldNotBe(Length.FitContent);
        Length.Auto.ShouldNotBe(Length.Px(0));
    }

    /// <summary>
    /// 混合分量必须按<b>值</b>相等。改用旁路对象存储后，两个分量相同的混合长度持有的是
    /// 不同的旁路实例；若相等退化成引用比较，所有断言混合长度的测试都会悄悄变红。
    /// </summary>
    [Fact]
    public void Equality_MixedComponents_ComparesByValue()
    {
        var a = Length.Percent(50) + Length.Px(10);
        var b = Length.Percent(50) + Length.Px(10);

        a.ShouldBe(b);
        a.GetHashCode().ShouldBe(b.GetHashCode());
        a.ShouldNotBe(Length.Percent(50) + Length.Px(11));
        a.ShouldNotBe(Length.Percent(50));
    }

    [Fact]
    public void Equality_MixedVersusSingle_AreNotEqual()
    {
        // px 分量相同但多了一个百分比分量：不得相等。
        (Length.Px(10) + Length.Percent(50)).ShouldNotBe(Length.Px(10));
    }

    /// <summary>
    /// 混合分量相互抵消后只剩一个分量时，必须与该单分量长度相等——否则
    /// <c>calc(100vw - 240px)</c> 折算后的长度会与等值的 px 长度不等，级联/插值的
    /// 「值未变」判定就会漏。
    /// </summary>
    [Fact]
    public void Equality_MixedCollapsingToSingle_EqualsThatSingle()
    {
        (Length.Px(10) + Length.Px(5) - Length.Percent(0)).ShouldBe(Length.Px(15));
        Length.Vw(100).ResolveViewport(1000, 800).ShouldBe(Length.Px(1000));
        (Length.SafeAreaInsetTop + Length.Px(8))
            .ResolveSafeArea(new SafeAreaInsets(0, 24, 0, 0))
            .ShouldBe(Length.Px(32));
    }

    [Fact]
    public void Equality_BoxedComparison_Works()
    {
        // 反射路径（ComputedStylePoolingTests 用 object.Equals 逐属性比较）依赖装箱相等。
        object boxed = Length.Rem(2f);
        boxed.Equals(Length.Rem(2f)).ShouldBeTrue();
        boxed.Equals(Length.Rem(3f)).ShouldBeFalse();
        boxed.Equals("not a length").ShouldBeFalse();
    }

    // ---- 实测发现的两种真实混合形状（ISSUE-142 §2.2）----

    /// <summary>
    /// <c>FabStyles.cs:110</c> 的真实写法：<c>(-100% + 2 * smallMargin) / 2</c>。
    /// 这是组件库里唯一的 px+pct 混合形状（22 条规则）。
    /// </summary>
    [Fact]
    public void RealShape_FabMargin_PercentPlusPx()
    {
        var smallMargin = Length.Px(10);
        var len = (Length.Percent(-100) + smallMargin * 2f) / 2f;

        // -50% + 10px：容器 200 → -100 + 10 = -90
        len.ToPixels(200).ShouldBe(-90);
        len.HasPercentComponent.ShouldBeTrue();
        len.ToString().ShouldBe("10px + -50%");
    }

    /// <summary>
    /// <c>IonModal.razor:239</c> 的真实写法：3 个分量（px + pct + safeTop），
    /// 是组件库里分量最多的长度。
    /// </summary>
    [Fact]
    public void RealShape_ModalSheetTop_PercentPlusPxPlusSafeTop()
    {
        const float breakpoint = 0.5f;
        var len = Length.Percent((1 - breakpoint) * 100)
                + Length.SafeAreaInsetTop * breakpoint
                + Length.Px(10 * breakpoint);

        len.HasSafeAreaComponent.ShouldBeTrue();
        len.HasPercentComponent.ShouldBeTrue();

        var resolved = len.ResolveSafeArea(new SafeAreaInsets(0, 44, 0, 0));
        resolved.HasSafeAreaComponent.ShouldBeFalse();
        // 50% of 800 + 44*0.5 + 5 = 400 + 22 + 5 = 427
        resolved.ToPixels(800).ShouldBe(427);
    }

    [Fact]
    public void RealShape_ModalHeight_PercentMinusSafeTopMinusPx()
    {
        // ModalStyles.cs:101 —— 100% - env(safe-area-inset-top) - 10px
        var len = Length.Percent(100) - Length.SafeAreaInsetTop - Length.Px(10);
        var resolved = len.ResolveSafeArea(new SafeAreaInsets(0, 44, 0, 0));

        resolved.ToPixels(800).ShouldBe(800 - 44 - 10);
    }

    // ---- LengthConverter 字符串解析 ----

    [Theory]
    [InlineData("12px", LengthUnit.Px, 12f)]
    [InlineData("1.5rem", LengthUnit.Rem, 1.5f)]
    [InlineData("2em", LengthUnit.Em, 2f)]
    [InlineData("50%", LengthUnit.Percent, 50f)]
    [InlineData("100vw", LengthUnit.Vw, 100f)]
    [InlineData("80vh", LengthUnit.Vh, 80f)]
    [InlineData("1.5", LengthUnit.Number, 1.5f)]
    [InlineData("  8px  ", LengthUnit.Px, 8f)]
    [InlineData("-4px", LengthUnit.Px, -4f)]
    public void Converter_ParsesSuffix(string text, LengthUnit expectedUnit, float expectedValue)
    {
        var converter = TypeDescriptor.GetConverter(typeof(Length));
        var length = (Length)converter.ConvertFrom(null, CultureInfo.InvariantCulture, text)!;

        length.Unit.ShouldBe(expectedUnit);
        length.Value.ShouldBe(expectedValue);
    }

    /// <summary>
    /// 后缀匹配顺序敏感：<c>"rem"</c> 必须先于 <c>"em"</c> 试探，否则 <c>"1.5rem"</c> 会被
    /// 当成以 <c>"em"</c> 结尾、解析 <c>"1.5r"</c> 而抛异常。
    /// </summary>
    [Fact]
    public void Converter_RemIsTriedBeforeEm()
    {
        var converter = TypeDescriptor.GetConverter(typeof(Length));

        ((Length)converter.ConvertFrom(null, CultureInfo.InvariantCulture, "1.5rem")!)
            .Unit.ShouldBe(LengthUnit.Rem);
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("AUTO")]
    public void Converter_ParsesAuto(string text)
    {
        var converter = TypeDescriptor.GetConverter(typeof(Length));
        ((Length)converter.ConvertFrom(null, CultureInfo.InvariantCulture, text)!).IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void Converter_ParsesFitContent()
    {
        var converter = TypeDescriptor.GetConverter(typeof(Length));
        ((Length)converter.ConvertFrom(null, CultureInfo.InvariantCulture, "fit-content")!)
            .IsFitContent.ShouldBeTrue();
    }
}
