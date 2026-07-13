using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using Xunit;

namespace Miko.Tests.Rendering;

/// <summary>
/// 测试 Range 伪元素样式中 Length 单位的解析 (rem, em, %)
/// </summary>
public class RangePseudoElementLengthTests
{
    private readonly LayoutEngine _layoutEngine = new();

    [Fact]
    public void RangeThumb_WithRemUnit_ShouldStoreRemValue()
    {
        // Arrange: 使用 rem 单位
        var root = new DivElement();
        var range = new InputElement { Type = InputType.Range };
        range.Class = "range-rem";
        root.AddChild(range);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".range-rem::range-thumb"] = new()
            {
                Width = Length.Rem(1.5f),  // 1.5rem
                Height = Length.Rem(1.5f),
                BorderTopLeftRadius = Length.Rem(0.75f),
            }
        });

        // Act
        _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        // Assert: 伪元素样式中应存储 rem 值
        range.PseudoElementStyles.ShouldNotBeNull();
        var thumbStyle = range.PseudoElementStyles[PseudoElementType.RangeThumb];

        thumbStyle.Width.ShouldBe(Length.Rem(1.5f));
        thumbStyle.Height.ShouldBe(Length.Rem(1.5f));
        thumbStyle.BorderTopLeftRadius.ShouldBe(Length.Rem(0.75f));

        // 验证单位类型
        thumbStyle.Width!.Value.Value.Unit.ShouldBe(LengthUnit.Rem);
        thumbStyle.Height!.Value.Value.Unit.ShouldBe(LengthUnit.Rem);
    }

    [Fact]
    public void RangeTrack_WithEmUnit_ShouldStoreEmValue()
    {
        // Arrange: 使用 em 单位
        var root = new DivElement();
        var range = new InputElement { Type = InputType.Range };
        range.Class = "range-em";
        root.AddChild(range);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".range-em"] = new()
            {
                FontSize = Length.Px(20),  // 设置元素字体大小
            },
            [".range-em::range-track"] = new()
            {
                Height = Length.Em(0.5f),  // 0.5em，相对于元素字体大小
                BorderTopLeftRadius = Length.Em(0.25f),
            }
        });

        // Act
        _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        // Assert
        range.PseudoElementStyles.ShouldNotBeNull();
        var trackStyle = range.PseudoElementStyles[PseudoElementType.RangeTrack];

        trackStyle.Height.ShouldBe(Length.Em(0.5f));
        trackStyle.BorderTopLeftRadius.ShouldBe(Length.Em(0.25f));

        trackStyle.Height!.Value.Value.Unit.ShouldBe(LengthUnit.Em);
    }

    [Fact]
    public void RangeThumb_WithPercentUnit_ShouldStorePercentValue()
    {
        // Arrange: 使用百分比单位
        var root = new DivElement();
        var range = new InputElement { Type = InputType.Range };
        range.Class = "range-percent";
        root.AddChild(range);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".range-percent"] = new()
            {
                Width = Length.Px(200),
                Height = Length.Px(40),
            },
            [".range-percent::range-thumb"] = new()
            {
                Width = Length.Percent(10),   // 10% 的容器宽度
                Height = Length.Percent(50),  // 50% 的容器高度
            }
        });

        // Act
        _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        // Assert
        range.PseudoElementStyles.ShouldNotBeNull();
        var thumbStyle = range.PseudoElementStyles[PseudoElementType.RangeThumb];

        thumbStyle.Width.ShouldBe(Length.Percent(10));
        thumbStyle.Height.ShouldBe(Length.Percent(50));

        thumbStyle.Width!.Value.Value.Unit.ShouldBe(LengthUnit.Percent);
        thumbStyle.Height!.Value.Value.Unit.ShouldBe(LengthUnit.Percent);
    }

    [Fact]
    public void RangePseudoElements_WithMixedUnits_ShouldStoreCorrectly()
    {
        // Arrange: 混合使用不同单位
        var root = new DivElement();
        var range = new InputElement { Type = InputType.Range };
        range.Class = "range-mixed";
        root.AddChild(range);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".range-mixed"] = new()
            {
                FontSize = Length.Px(18),
            },
            [".range-mixed::range-track"] = new()
            {
                Height = Length.Px(8),           // 像素
                BorderTopLeftRadius = Length.Rem(0.5f),  // rem
            },
            [".range-mixed::range-thumb"] = new()
            {
                Width = Length.Em(1),            // em
                Height = Length.Em(1),
                BorderWidth = Length.Px(2),      // 像素
                BorderTopLeftRadius = Length.Percent(50),  // 百分比（圆形）
            }
        });

        // Act
        _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        // Assert
        range.PseudoElementStyles.ShouldNotBeNull();

        var trackStyle = range.PseudoElementStyles[PseudoElementType.RangeTrack];
        trackStyle.Height.ShouldBe(Length.Px(8));
        trackStyle.BorderTopLeftRadius.ShouldBe(Length.Rem(0.5f));

        var thumbStyle = range.PseudoElementStyles[PseudoElementType.RangeThumb];
        thumbStyle.Width.ShouldBe(Length.Em(1));
        thumbStyle.Height.ShouldBe(Length.Em(1));
        thumbStyle.BorderWidth.ShouldBe(Length.Px(2));
        thumbStyle.BorderTopLeftRadius.ShouldBe(Length.Percent(50));
    }

    [Fact]
    public void RangeThumb_ToPixels_ShouldResolveCorrectly()
    {
        // Arrange & Act: 验证 ToPixels() 的正确解析
        var remLength = Length.Rem(1);
        var emLength = Length.Em(2);
        var percentLength = Length.Percent(50);
        var pxLength = Length.Px(10);

        // 假设 root font-size = 16px, element font-size = 20px, container = 100px
        float rootFontSize = 16;
        float elementFontSize = 20;
        float containerSize = 100;

        Length.RootFontSize = rootFontSize;

        // Assert
        remLength.ToPixels(containerSize, elementFontSize).ShouldBe(16f);      // 1rem = 16px
        emLength.ToPixels(containerSize, elementFontSize).ShouldBe(40f);       // 2em = 40px (相对元素字体)
        percentLength.ToPixels(containerSize, elementFontSize).ShouldBe(50f);  // 50% = 50px
        pxLength.ToPixels(containerSize, elementFontSize).ShouldBe(10f);       // 10px = 10px
    }

    [Fact]
    public void RangeThumb_CalcExpression_ShouldStoreCorrectly()
    {
        // Arrange: 使用 calc() 表达式（Length 支持算术运算）
        var root = new DivElement();
        var range = new InputElement { Type = InputType.Range };
        range.Class = "range-calc";
        root.AddChild(range);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".range-calc::range-thumb"] = new()
            {
                // Width = calc(1rem + 4px)
                Width = Length.Rem(1) + Length.Px(4),
            }
        });

        // Act
        _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        // Assert: calc 表达式被存储为复合 Length
        range.PseudoElementStyles.ShouldNotBeNull();
        var thumbStyle = range.PseudoElementStyles[PseudoElementType.RangeThumb];

        thumbStyle.Width.ShouldBe(Length.Rem(1) + Length.Px(4));

        // 验证计算结果（假设 root font-size = 16px）
        Length.RootFontSize = 16;
        thumbStyle.Width!.Value.Value.ToPixels(100, 16).ShouldBe(20f);  // 1rem (16px) + 4px = 20px
    }
}
