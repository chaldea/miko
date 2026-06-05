using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace Miko.Tests.Rendering;

/// <summary>
/// 测试 Range 输入的伪元素样式渲染
/// </summary>
public class RangeRenderingTests
{
    private readonly LayoutEngine _layoutEngine;
    private readonly StyleResolver _styleResolver;

    public RangeRenderingTests()
    {
        _layoutEngine = new LayoutEngine();
        _styleResolver = new StyleResolver();
    }

    [Fact]
    public void RangeInput_WithPseudoElementStyles_ShouldApplyStylesToElements()
    {
        // Arrange: 创建带有伪元素样式的 range input
        var root = new DivElement();
        var range = new InputElement { Type = InputType.Range, Value = "50" };
        range.Class = "styled-range";
        root.AddChild(range);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".styled-range"] = new()
            {
                Width = Length.Px(200),
                Height = Length.Px(30),
                ["::range-track"] = new()
                {
                    Height = Length.Px(8),
                    BackgroundColor = Color.FromHex("#e0e0e0"),
                    BorderTopLeftRadius = Length.Px(4),
                },
                ["::range-thumb"] = new()
                {
                    Width = Length.Px(20),
                    Height = Length.Px(20),
                    BackgroundColor = Color.FromHex("#2196F3"),
                    BorderTopLeftRadius = Length.Px(10),
                    BorderWidth = Length.Px(2),
                    BorderTopColor = Color.FromHex("#1976D2"),
                },
                ["::range-progress"] = new()
                {
                    BackgroundColor = Color.FromHex("#64B5F6"),
                }
            }
        });

        // Act: 布局元素（这会应用伪元素样式）
        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var rangeBox = layoutRoot.Children[0];

        // Assert: 验证伪元素样式已被应用到元素上
        range.PseudoElementStyles.ShouldNotBeNull();
        range.PseudoElementStyles.Count.ShouldBe(3);

        // 验证 track 样式
        range.PseudoElementStyles.TryGetValue(PseudoElementType.RangeTrack, out var trackStyle).ShouldBeTrue();
        trackStyle.ShouldNotBeNull();
        trackStyle.Height.ShouldBe(Length.Px(8));
        trackStyle.BackgroundColor.ShouldBe(Color.FromHex("#e0e0e0"));
        trackStyle.BorderTopLeftRadius.ShouldBe(Length.Px(4));

        // 验证 thumb 样式
        range.PseudoElementStyles.TryGetValue(PseudoElementType.RangeThumb, out var thumbStyle).ShouldBeTrue();
        thumbStyle.ShouldNotBeNull();
        thumbStyle.Width.ShouldBe(Length.Px(20));
        thumbStyle.Height.ShouldBe(Length.Px(20));
        thumbStyle.BackgroundColor.ShouldBe(Color.FromHex("#2196F3"));
        thumbStyle.BorderTopLeftRadius.ShouldBe(Length.Px(10));
        thumbStyle.BorderWidth.ShouldBe(Length.Px(2));
        thumbStyle.BorderTopColor.ShouldBe(Color.FromHex("#1976D2"));

        // 验证 progress 样式
        range.PseudoElementStyles.TryGetValue(PseudoElementType.RangeProgress, out var progressStyle).ShouldBeTrue();
        progressStyle.ShouldNotBeNull();
        progressStyle.BackgroundColor.ShouldBe(Color.FromHex("#64B5F6"));
    }

    [Fact]
    public void RangeInput_WithoutPseudoElementStyles_ShouldUseDefaults()
    {
        // Arrange: range input 没有伪元素样式
        var root = new DivElement();
        var range = new InputElement { Type = InputType.Range, Value = "30" };
        root.AddChild(range);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["input[type=\"range\"]"] = new()
            {
                Width = Length.Px(200),
            }
        });

        // Act
        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        // Assert: 没有伪元素样式，或者为空
        if (range.PseudoElementStyles != null)
        {
            range.PseudoElementStyles.ContainsKey(PseudoElementType.RangeTrack).ShouldBeFalse();
            range.PseudoElementStyles.ContainsKey(PseudoElementType.RangeThumb).ShouldBeFalse();
            range.PseudoElementStyles.ContainsKey(PseudoElementType.RangeProgress).ShouldBeFalse();
        }
    }

    [Fact]
    public void RangeInput_WithPartialPseudoElementStyles_ShouldApplyOnlySpecified()
    {
        // Arrange: 只设置部分伪元素样式
        var root = new DivElement();
        var range = new InputElement { Type = InputType.Range };
        range.Class = "custom-range";
        root.AddChild(range);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".custom-range::range-thumb"] = new()
            {
                BackgroundColor = Color.Red,
            }
            // 没有设置 track 和 progress
        });

        // Act
        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        // Assert: 只有 thumb 有样式
        range.PseudoElementStyles.ShouldNotBeNull();
        range.PseudoElementStyles.ContainsKey(PseudoElementType.RangeThumb).ShouldBeTrue();
        range.PseudoElementStyles.ContainsKey(PseudoElementType.RangeTrack).ShouldBeFalse();
        range.PseudoElementStyles.ContainsKey(PseudoElementType.RangeProgress).ShouldBeFalse();

        var thumbStyle = range.PseudoElementStyles[PseudoElementType.RangeThumb];
        thumbStyle.BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void RangeInput_PseudoElementStyles_ShouldNotAffectMainElementStyle()
    {
        // Arrange: 伪元素样式不应影响主元素样式
        var root = new DivElement();
        var range = new InputElement { Type = InputType.Range };
        range.Class = "test-range";
        root.AddChild(range);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".test-range"] = new()
            {
                Width = Length.Px(300),
                BackgroundColor = Color.Transparent,
            },
            [".test-range::range-track"] = new()
            {
                BackgroundColor = Color.Blue,
            }
        });

        // Act
        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var rangeBox = layoutRoot.Children[0];

        // Assert: 主元素保持自己的样式，不受伪元素影响
        rangeBox.ComputedStyle.BackgroundColor.ShouldBe(Color.Transparent);
        rangeBox.ComputedStyle.Width.ShouldBe(Length.Px(300));

        // 伪元素有自己独立的样式
        range.PseudoElementStyles[PseudoElementType.RangeTrack].BackgroundColor.ShouldBe(Color.Blue);
    }
}
