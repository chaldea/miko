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

    // 以上两例的容器宽度是"确定"的（块级子元素取包含块宽度），max-width 在子元素布局前就已夹取
    // 生效。以下用例覆盖容器宽度"不确定"的情形：容器作为 flex 项且交叉轴 auto，走 shrink-to-fit，
    // 布局子元素时 contentWidth 仅为 0 占位，提前夹取（Math.Min(0, max)）被跳过（见 ISSUE-115）。

    /// <summary>
    /// shrink-to-fit 的 flex 容器被 max-width 夹取后，子元素必须重排到夹取后的宽度内。
    /// 这是 IonAlert 的形状：居中的 alert-wrapper（max-width:280）内含 auto 宽度的 alert-head，
    /// head 的文本一度把自己撑到 max-content 宽度并溢出 wrapper。
    /// </summary>
    [Fact]
    public void ShrinkToFitFlexContainer_ClampedByMaxWidth_ShouldReflowChildren()
    {
        var root = new DivElement { Class = "root" };
        var host = new DivElement { Class = "host" };
        var wrapper = new DivElement { Class = "wrapper" };
        var head = new DivElement { Class = "head", TextContent = "A Short Title Is Best that is quite long indeed here" };

        wrapper.AddChild(head);
        host.AddChild(wrapper);
        root.AddChild(host);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(800), Height = Length.Px(600) },
            // 行向居中：wrapper 的交叉轴（宽）为 auto 且不 stretch → shrink-to-fit → 宽度不确定。
            [".host"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Px(800),
                Height = Length.Px(600),
            },
            [".wrapper"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                MaxWidth = Length.Px(280),
            },
            [".head"] = new() { PaddingLeft = Length.Px(23), PaddingRight = Length.Px(23) },
        });

        using var surface = SKSurface.Create(new SKImageInfo(800, 800));
        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { sheet }, surface.Canvas, 800, 800);

        var wrapperBox = root.LayoutBox!.Children[0].Children[0];
        var headBox = wrapperBox.Children[0];

        wrapperBox.BoxModel.Content.Width.ShouldBe(280f, 0.5f);
        // 修复前 head 为 max-content 宽度（约 397px），右缘溢出 wrapper。
        headBox.BoxModel.BorderBox.Width.ShouldBe(280f, 0.5f);
        headBox.BoxModel.BorderBox.Right.ShouldBeLessThanOrEqualTo(
            wrapperBox.BoxModel.BorderBox.Right + 0.5f);
    }

    /// <summary>
    /// 同一场景下 <c>width:100%</c> 的子元素也应解析到夹取后的宽度，从而让其自身的
    /// justify-content 有剩余空间可分配。这是 IonAlert 的 alert-button-group：修复前它退化为
    /// 内容宽度，<c>justify-content:flex-end</c> 无空间可分，按钮贴左而非靠右（ISSUE-115 问题2）。
    /// </summary>
    [Fact]
    public void ShrinkToFitFlexContainer_ClampedByMaxWidth_ShouldResolvePercentChildAndJustify()
    {
        var root = new DivElement { Class = "root" };
        var host = new DivElement { Class = "host" };
        var wrapper = new DivElement { Class = "wrapper" };
        var group = new DivElement { Class = "group" };
        var button = new DivElement { Class = "button", TextContent = "OK" };

        group.AddChild(button);
        wrapper.AddChild(group);
        host.AddChild(wrapper);
        root.AddChild(host);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(800), Height = Length.Px(600) },
            [".host"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Px(800),
                Height = Length.Px(600),
            },
            [".wrapper"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                MinWidth = Length.Px(250),
                MaxWidth = Length.Px(280),
            },
            [".group"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                JustifyContent = JustifyContent.FlexEnd,
                Width = Length.Percent(100),
            },
            [".button"] = new() { Display = Display.Block },
        });

        using var surface = SKSurface.Create(new SKImageInfo(800, 800));
        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { sheet }, surface.Canvas, 800, 800);

        var wrapperBox = root.LayoutBox!.Children[0].Children[0];
        var groupBox = wrapperBox.Children[0];
        var buttonBox = groupBox.Children[0];

        // 内容（"OK"）远小于 min-width，故容器定型在 min-width 的 250px（max-width 未触及）。
        // 关键点与 max-width 相同：这是一个夹取后才确定的宽度，子元素必须基于它重排。
        wrapperBox.BoxModel.Content.Width.ShouldBe(250f, 0.5f);
        // width:100% 现在相对夹取后的 250px 解析（修复前退化为按钮的内容宽度）。
        groupBox.BoxModel.Content.Width.ShouldBe(250f, 0.5f);
        // flex-end：按钮右缘贴住组的右缘，而不是从左缘开始。
        buttonBox.BoxModel.BorderBox.Right.ShouldBe(groupBox.BoxModel.Content.Right, 0.5f);
        buttonBox.BoxModel.BorderBox.X.ShouldBeGreaterThan(groupBox.BoxModel.Content.X + 1f);
    }

    /// <summary>
    /// 对称用例：min-width 抬升 shrink-to-fit 容器宽度后，<c>width:100%</c> 的子元素同样应基于
    /// 抬升后的宽度解析（而非内容宽度）。
    /// </summary>
    [Fact]
    public void ShrinkToFitFlexContainer_RaisedByMinWidth_ShouldResolvePercentChild()
    {
        var root = new DivElement { Class = "root" };
        var host = new DivElement { Class = "host" };
        var wrapper = new DivElement { Class = "wrapper" };
        var child = new DivElement { Class = "child", TextContent = "Hi" };

        wrapper.AddChild(child);
        host.AddChild(wrapper);
        root.AddChild(host);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(800), Height = Length.Px(600) },
            [".host"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Width = Length.Px(800),
                Height = Length.Px(600),
            },
            [".wrapper"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                MinWidth = Length.Px(300),
            },
            [".child"] = new() { Display = Display.Block, Width = Length.Percent(100) },
        });

        using var surface = SKSurface.Create(new SKImageInfo(800, 800));
        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { sheet }, surface.Canvas, 800, 800);

        var wrapperBox = root.LayoutBox!.Children[0].Children[0];
        var childBox = wrapperBox.Children[0];

        wrapperBox.BoxModel.Content.Width.ShouldBe(300f, 0.5f);
        childBox.BoxModel.Content.Width.ShouldBe(300f, 0.5f);
    }

    /// <summary>
    /// 未触发夹取时不应重排：内容宽度已在 [min, max] 区间内，容器按 shrink-to-fit 收缩包裹，
    /// 子元素保持内容宽度（守住 ISSUE-097 的收缩包裹行为，确认重排是条件触发而非无条件执行）。
    /// </summary>
    [Fact]
    public void ShrinkToFitFlexContainer_WithinMaxWidth_ShouldStayContentSized()
    {
        var root = new DivElement { Class = "root" };
        var host = new DivElement { Class = "host" };
        var wrapper = new DivElement { Class = "wrapper" };
        var child = new DivElement { Class = "child" };

        wrapper.AddChild(child);
        host.AddChild(wrapper);
        root.AddChild(host);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(800), Height = Length.Px(600) },
            [".host"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Width = Length.Px(800),
                Height = Length.Px(600),
            },
            [".wrapper"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                MaxWidth = Length.Px(500),
            },
            [".child"] = new() { Width = Length.Px(120), Height = Length.Px(40) },
        });

        using var surface = SKSurface.Create(new SKImageInfo(800, 800));
        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { sheet }, surface.Canvas, 800, 800);

        var wrapperBox = root.LayoutBox!.Children[0].Children[0];
        var childBox = wrapperBox.Children[0];

        // 120px 内容未触及 500px 上限：容器仍收缩到内容宽度，不被拉到 max-width。
        wrapperBox.BoxModel.Content.Width.ShouldBe(120f, 0.5f);
        childBox.BoxModel.Content.Width.ShouldBe(120f, 0.5f);
    }
}
