using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Layout;

/// <summary>
/// 复现 ISSUE-094：当元素以 shrink-to-fit（auto 宽度、非 stretch 交叉轴）布局，
/// 且设置了百分比 max-width 时，百分比针对不确定包含块解析不应折算为 0，
/// 而应退化为“无约束”（与浏览器一致），使内容宽度保持为内容尺寸。
/// </summary>
public class MaxWidthPercentShrinkToFitTests
{
    [Fact]
    public void Label_WithMaxWidthPercent_InCenteredFlexColumn_ShouldNotCollapseToZero()
    {
        // Arrange：还原 issue 中的 DOM 结构
        // .root > .segment > .btn-native > .button-inner > .label("Default")
        var root = new DivElement { Class = "root" };
        var segment = new DivElement { Class = "segment" };
        var btnNative = new DivElement { Class = "btn-native" };
        var buttonInner = new DivElement { Class = "button-inner" };
        var label = new DivElement { Class = "label", TextContent = "Default" };

        buttonInner.AddChild(label);
        btnNative.AddChild(buttonInner);
        segment.AddChild(btnNative);
        root.AddChild(segment);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["*"] = new()
            {
                BoxSizing = BoxSizing.BorderBox,
            },
            [".root"] = new()
            {
                Width = Length.Px(500),
                Height = Length.Px(500),
            },
            [".segment"] = new()
            {
                Width = Length.Auto,
                Height = Length.Auto,
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.FlexStart,
                AlignItems = AlignItems.Stretch,
                AlignContent = AlignContent.FlexStart,
                MaxWidth = Length.Px(240),
            },
            [".btn-native"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Auto,
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
            },
            [".button-inner"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Auto,
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
            },
            [".label"] = new()
            {
                MaxWidth = Length.Percent(100),
                TextAlign = TextAlign.Center,
                LineHeight = Length.Px(22),
                FontSize = Length.Px(13),
            }
        });

        using var surface = SKSurface.Create(new SKImageInfo(800, 800));
        var canvas = surface.Canvas;

        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { sheet }, canvas, 800, 800);

        // Assert
        var segmentBox = root.LayoutBox!.Children[0];
        var btnNativeBox = segmentBox.Children[0];
        var buttonInnerBox = btnNativeBox.Children[0];
        var labelBox = buttonInnerBox.Children[0];

        // label 设置了 max-width:100%，其包含块（button-inner）宽度不确定（交叉轴 auto、非 stretch），
        // 百分比应退化为无约束，label 宽度应为内容宽度（约等于文本 "Default" 的宽度），而不是 0。
        labelBox.BoxModel.Content.Width.ShouldBeGreaterThan(0f,
            $"label width should be its content width, not collapsed to 0 (got {labelBox.BoxModel.Content.Width}px)");
    }

    [Fact]
    public void Label_WithMaxWidthPercent_ShouldMatchWidthWithoutConstraint()
    {
        // max-width:100% 针对不确定包含块解析应为无操作（no-op），
        // 因此与不设 max-width 的相同结构渲染出的宽度应完全一致（浏览器行为）。
        float WidthOf(bool withMaxWidth)
        {
            var buttonInner = new DivElement { Class = "button-inner" };
            var label = new DivElement { Class = "label", TextContent = "Default" };
            buttonInner.AddChild(label);

            var labelStyle = new CssObject
            {
                [".button-inner"] = new()
                {
                    Width = Length.Auto,
                    Height = Length.Auto,
                    Display = Display.Flex,
                    FlexDirection = FlexDirection.Column,
                    AlignItems = AlignItems.Center,
                },
                [".label"] = new()
                {
                    FontSize = Length.Px(13),
                    LineHeight = Length.Px(22),
                }
            };
            if (withMaxWidth)
                labelStyle[".label"].MaxWidth = Length.Percent(100);

            var sheet = new StyleSheet();
            sheet.Add(labelStyle);

            using var surface = SKSurface.Create(new SKImageInfo(800, 800));
            var engine = new MikoEngine();
            // button-inner 处于不确定宽度的父约束下（root width auto in a shrink-to-fit context）
            var host = new DivElement { Class = "host" };
            host.AddChild(buttonInner);
            var hostSheet = new StyleSheet();
            hostSheet.Add(new CssObject
            {
                [".host"] = new()
                {
                    Display = Display.Flex,
                    FlexDirection = FlexDirection.Row,
                    AlignItems = AlignItems.FlexStart,
                    JustifyContent = JustifyContent.FlexStart,
                }
            });
            hostSheet.Add(labelStyle);

            engine.Initialize(host, new List<StyleSheet> { hostSheet }, surface.Canvas, 800, 800);
            return host.LayoutBox!.Children[0].Children[0].BoxModel.Content.Width;
        }

        float withConstraint = WidthOf(withMaxWidth: true);
        float withoutConstraint = WidthOf(withMaxWidth: false);

        withoutConstraint.ShouldBeGreaterThan(0f);
        withConstraint.ShouldBe(withoutConstraint, 0.5f,
            "max-width:100% against an indefinite containing block must be a no-op");
    }
}
