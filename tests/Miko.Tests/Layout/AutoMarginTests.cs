using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Layout;

/// <summary>
/// Tests for auto margin centering behavior
/// </summary>
public class AutoMarginTests
{
    [Fact]
    public void BlockElement_WithExplicitWidthAndAutoMargin_ShouldCenter()
    {
        // Arrange: Container 600px, child explicit width 400px, margin 0 auto
        var root = new DivElement { Class = "container" };
        var child = new DivElement { Class = "card", TextContent = "test" };
        root.AddChild(child);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".container"] = new()
            {
                Width = Length.Px(600),
                Display = Display.Block
            },
            [".card"] = new()
            {
                Width = Length.Px(400),
                Margin = new Margin(0, Length.Auto),
                TextAlign = TextAlign.Center,
            }
        });

        using var surface = SKSurface.Create(new SKImageInfo(800, 800));
        var canvas = surface.Canvas;

        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { sheet }, canvas, 800, 800);

        // Assert
        var childBox = root.LayoutBox!.Children[0];

        // Width should be 400px
        childBox.BoxModel.Content.Width.ShouldBe(400f);

        // Auto margins should each be 100px (600 - 400 = 200, divided by 2)
        childBox.BoxModel.Margin.Left.ShouldBe(100f);
        childBox.BoxModel.Margin.Right.ShouldBe(100f);
    }

    [Fact]
    public void BlockElement_WithMaxWidthAndAutoMargin_ShouldCenter()
    {
        // Arrange: Container 600px, child max-width 400px (no explicit width), margin 0 auto
        var root = new DivElement { Class = "container" };
        var child = new DivElement { Class = "card", TextContent = "test" };
        root.AddChild(child);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["*"] = new()
            {
                BoxSizing = BoxSizing.BorderBox,
            },
            [".container"] = new()
            {
                Width = Length.Px(600),
                Display = Display.Block
            },
            [".card"] = new()
            {
                MaxWidth = Length.Px(400),
                Margin = new Margin(0, Length.Auto),
                TextAlign = TextAlign.Center,
            }
        });

        using var surface = SKSurface.Create(new SKImageInfo(800, 800));
        var canvas = surface.Canvas;

        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { sheet }, canvas, 800, 800);

        // Assert
        var childBox = root.LayoutBox!.Children[0];

        // Width should be constrained to 400px by max-width
        childBox.BoxModel.Content.Width.ShouldBe(400f,
            "Width should be constrained by max-width: 400px");

        // Auto margins should each be 100px (600 - 400 = 200, divided by 2)
        // This is the bug: currently margins are 0
        childBox.BoxModel.Margin.Left.ShouldBe(100f,
            "Left auto margin should be 100px to center the element");
        childBox.BoxModel.Margin.Right.ShouldBe(100f,
            "Right auto margin should be 100px to center the element");
    }

    [Fact]
    public void FlexElement_WithMaxWidthAndAutoMargin_ShouldCenter()
    {
        // Arrange: Container 600px, flex child max-width 400px, margin 0 auto (ISSUE-084 followup)
        var root = new DivElement { Class = "container" };
        var child = new DivElement { Class = "card", TextContent = "test" };
        root.AddChild(child);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["*"] = new()
            {
                BoxSizing = BoxSizing.BorderBox,
            },
            [".container"] = new()
            {
                Width = Length.Px(600),
                Display = Display.Block
            },
            [".card"] = new()
            {
                MaxWidth = Length.Px(400),
                Margin = new Margin(0, Length.Auto),
                Display = Display.Flex,
                JustifyContent = JustifyContent.Center,
            }
        });

        using var surface = SKSurface.Create(new SKImageInfo(800, 800));
        var canvas = surface.Canvas;

        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { sheet }, canvas, 800, 800);

        // Assert
        var childBox = root.LayoutBox!.Children[0];

        // Width should be constrained to 400px by max-width
        childBox.BoxModel.Content.Width.ShouldBe(400f,
            "Flex container width should be constrained by max-width: 400px");

        // Auto margins should each be 100px (600 - 400 = 200, divided by 2)
        childBox.BoxModel.Margin.Left.ShouldBe(100f,
            "Left auto margin should center the flex container");
        childBox.BoxModel.Margin.Right.ShouldBe(100f,
            "Right auto margin should center the flex container");

        // Content should actually start at 100px offset from container's left edge
        childBox.BoxModel.Content.X.ShouldBe(100f,
            "Flex container content should be offset by the left auto margin");
    }

    [Fact]
    public void BlockElement_WithMaxWidthLargerThanContent_ShouldNotCenter()
    {
        // Arrange: Container 600px, child max-width 800px (no constraint), margin 0 auto
        var root = new DivElement { Class = "container" };
        var child = new DivElement { Class = "card", TextContent = "test" };
        root.AddChild(child);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".container"] = new()
            {
                Width = Length.Px(600),
                Display = Display.Block
            },
            [".card"] = new()
            {
                MaxWidth = Length.Px(800),  // Larger than container
                Margin = new Margin(0, Length.Auto),
            }
        });

        using var surface = SKSurface.Create(new SKImageInfo(800, 800));
        var canvas = surface.Canvas;

        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { sheet }, canvas, 800, 800);

        // Assert
        var childBox = root.LayoutBox!.Children[0];

        // Width should fill container (600px) since max-width doesn't constrain it
        childBox.BoxModel.Content.Width.ShouldBe(600f);

        // Margins should be 0 (no space left for auto margins)
        childBox.BoxModel.Margin.Left.ShouldBe(0f);
        childBox.BoxModel.Margin.Right.ShouldBe(0f);
    }
}
