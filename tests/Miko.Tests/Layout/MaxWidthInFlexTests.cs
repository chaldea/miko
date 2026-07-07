using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Layout;

/// <summary>
/// Tests for max-width constraint in flex containers
/// </summary>
public class MaxWidthInFlexTests
{
    [Fact]
    public void FlexContainer_WithMaxWidth_ShouldConstrainChildWidth()
    {
        // Arrange: 根容器 600px，Flex容器 max-width: 400px，子元素 width: 100%
        var root = new DivElement { Class = "root" };
        var container = new DivElement { Class = "container" };
        var child = new DivElement { Class = "child" };

        container.AddChild(child);
        root.AddChild(container);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new()
            {
                Width = Length.Px(600),
                Height = Length.Px(600),
            },
            [".container"] = new()
            {
                Display = Display.Flex,
                MaxWidth = Length.Px(400),
                // Width 为 auto（默认）
            },
            [".child"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Px(50),
            }
        });

        using var surface = SKSurface.Create(new SKImageInfo(800, 800));
        var canvas = surface.Canvas;

        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { sheet }, canvas, 800, 800);

        // Assert
        var containerBox = root.LayoutBox!.Children[0];
        var childBox = containerBox.Children[0];

        // Container 应该被 max-width 约束到 400px
        containerBox.BoxModel.Content.Width.ShouldBeLessThanOrEqualTo(400f);

        // Child (width: 100%) 应该相对于容器的约束后宽度，不应该超过 400px
        childBox.BoxModel.Content.Width.ShouldBeLessThanOrEqualTo(400f);
    }

    [Fact]
    public void FlexContainer_WithMaxWidthAndMargin_ShouldConstrainChildWidth()
    {
        // Arrange: 类似 DebugDemo 的场景
        var root = new DivElement { Class = "root" };
        var container = new DivElement { Class = "container" };
        var child = new DivElement
        {
            Class = "child",
            TextContent = "Child content"
        };

        container.AddChild(child);
        root.AddChild(container);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["*"] = new()
            {
                BoxSizing = BoxSizing.BorderBox,
            },
            [".root"] = new()
            {
                Width = Length.Px(600),
                Height = Length.Px(600),
            },
            [".container"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Height = Length.Px(200),
                MaxWidth = Length.Px(400),
                Margin = new Margin(0, Length.Auto),
            },
            [".child"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
            }
        });

        using var surface = SKSurface.Create(new SKImageInfo(800, 800));
        var canvas = surface.Canvas;

        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { sheet }, canvas, 800, 800);

        // Assert
        var containerBox = root.LayoutBox!.Children[0];
        var childBox = containerBox.Children[0];

        // Container 的内容区应该 <= 400px
        containerBox.BoxModel.Content.Width.ShouldBeLessThanOrEqualTo(400f,
            $"Container width should be constrained by max-width: 400px, but got {containerBox.BoxModel.Content.Width}px");

        // Child 应该 <= 400px
        childBox.BoxModel.Content.Width.ShouldBeLessThanOrEqualTo(400f,
            $"Child width should not exceed container's max-width, but got {childBox.BoxModel.Content.Width}px");
    }
}
