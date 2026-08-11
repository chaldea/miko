using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using Xunit;

namespace Miko.Tests.Layout.LayoutAlgorithms;

public partial class FlexLayoutTests
{
    [Fact]
    public void Should_CenterAbsolutePositionedItemCorrectly()
    {
        // ISSUE-125: 当 flex 子项为 absolute 定位时，align-items:center 应该仍然相对容器居中
        var root = new DivElement { Class = "root" };
        var toggle = new DivElement { Class = "toggle" };
        var wrapper = new DivElement { Class = "native-wrapper" };
        var inner = new DivElement { Class = "toggle-inner" };

        root.AddChild(toggle);
        toggle.AddChild(wrapper);
        wrapper.AddChild(inner);

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("toggle"), new Style
        {
            Height = Length.Px(40),
            Width = Length.Px(100),
            Display = Display.Flex,
            AlignItems = AlignItems.Center,
        });
        sheet.AddRule(new ClassSelector("native-wrapper"), new Style
        {
            Height = Length.Px(14),
            Width = Length.Px(36),
            Display = Display.Flex,
            AlignItems = AlignItems.Center,
            BackgroundColor = Color.FromRgba(0, 84, 233, 0.5f),
        });
        sheet.AddRule(new ClassSelector("toggle-inner"), new Style
        {
            Width = Length.Px(20),
            Height = Length.Px(20),
            Display = Display.Block,
            Position = Position.Absolute,
            Left = Length.Px(0),
            BackgroundColor = Color.FromRgb(0, 84, 233),
        });

        var box = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        // .toggle: 100×40，内容从 (0,0) 开始
        var toggleBox = box.Children[0];
        toggleBox.BoxModel.Content.Width.ShouldBe(100f);
        toggleBox.BoxModel.Content.Height.ShouldBe(40f);

        // .native-wrapper: 36×14，在 .toggle 中垂直居中
        var wrapperBox = toggleBox.Children[0];
        wrapperBox.BoxModel.Content.Width.ShouldBe(36f);
        wrapperBox.BoxModel.Content.Height.ShouldBe(14f);

        // wrapper 在 toggle 中垂直居中：Y = (40 - 14) / 2 = 13
        wrapperBox.BoxModel.Content.Y.ShouldBe(13f, tolerance: 0.01f);

        // .toggle-inner: 20×20，absolute 定位
        var innerBox = wrapperBox.Children[0];
        innerBox.BoxModel.Content.Width.ShouldBe(20f);
        innerBox.BoxModel.Content.Height.ShouldBe(20f);

        // 关键测试：即使 inner 是 absolute 定位，它应该在 wrapper (14px 高) 中垂直居中
        // 相对于 wrapper 的 Y 偏移 = (14 - 20) / 2 = -3
        // 绝对 Y 位置 = wrapper.Y + 相对偏移 = 13 + (-3) = 10
        float expectedInnerY = wrapperBox.BoxModel.Content.Y + (14f - 20f) / 2f;
        innerBox.BoxModel.Content.Y.ShouldBe(expectedInnerY, tolerance: 0.01f);
    }

    [Fact]
    public void Should_AlignAbsoluteItem_FlexEnd()
    {
        // absolute 元素在 flex 容器中应用 align-items:flex-end
        var root = new DivElement { Class = "root" };
        var container = new DivElement { Class = "container" };
        var item = new DivElement { Class = "item" };

        root.AddChild(container);
        container.AddChild(item);

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("container"), new Style
        {
            Display = Display.Flex,
            AlignItems = AlignItems.FlexEnd,
            Width = Length.Px(100),
            Height = Length.Px(100),
        });
        sheet.AddRule(new ClassSelector("item"), new Style
        {
            Position = Position.Absolute,
            Width = Length.Px(30),
            Height = Length.Px(30),
        });

        var box = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        var containerBox = box.Children[0];
        var itemBox = containerBox.Children[0];

        // absolute 元素应该对齐到容器底部
        float expectedY = containerBox.BoxModel.Content.Y + 100f - 30f;
        itemBox.BoxModel.Content.Y.ShouldBe(expectedY, tolerance: 0.01f);
    }

    [Fact]
    public void Should_AlignAbsoluteItem_FlexStart()
    {
        // absolute 元素在 flex 容器中应用 align-items:flex-start（默认）
        var root = new DivElement { Class = "root" };
        var container = new DivElement { Class = "container" };
        var item = new DivElement { Class = "item" };

        root.AddChild(container);
        container.AddChild(item);

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("container"), new Style
        {
            Display = Display.Flex,
            AlignItems = AlignItems.FlexStart,
            Width = Length.Px(100),
            Height = Length.Px(100),
        });
        sheet.AddRule(new ClassSelector("item"), new Style
        {
            Position = Position.Absolute,
            Width = Length.Px(30),
            Height = Length.Px(30),
        });

        var box = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        var containerBox = box.Children[0];
        var itemBox = containerBox.Children[0];

        // absolute 元素应该对齐到容器顶部
        itemBox.BoxModel.Content.Y.ShouldBe(containerBox.BoxModel.Content.Y, tolerance: 0.01f);
    }

    [Fact]
    public void Should_NotAlignAbsoluteItem_WhenTopIsSpecified()
    {
        // 当 absolute 元素指定了 top 时，不应用 align-items
        var root = new DivElement { Class = "root" };
        var container = new DivElement { Class = "container" };
        var item = new DivElement { Class = "item" };

        root.AddChild(container);
        container.AddChild(item);

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("container"), new Style
        {
            Display = Display.Flex,
            AlignItems = AlignItems.Center,
            Width = Length.Px(100),
            Height = Length.Px(100),
        });
        sheet.AddRule(new ClassSelector("item"), new Style
        {
            Position = Position.Absolute,
            Top = Length.Px(10),
            Width = Length.Px(30),
            Height = Length.Px(30),
        });

        var box = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        var containerBox = box.Children[0];
        var itemBox = containerBox.Children[0];

        // absolute 元素应该按 top:10px 定位，而不是居中
        float expectedY = containerBox.BoxModel.Content.Y + 10f;
        itemBox.BoxModel.Content.Y.ShouldBe(expectedY, tolerance: 0.01f);
    }

    [Fact]
    public void Should_AlignAbsoluteItem_InColumnDirection()
    {
        // 列方向的 flex 容器中，absolute 元素应用水平对齐
        var root = new DivElement { Class = "root" };
        var container = new DivElement { Class = "container" };
        var item = new DivElement { Class = "item" };

        root.AddChild(container);
        container.AddChild(item);

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("container"), new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Column,
            AlignItems = AlignItems.Center,
            Width = Length.Px(100),
            Height = Length.Px(100),
        });
        sheet.AddRule(new ClassSelector("item"), new Style
        {
            Position = Position.Absolute,
            Width = Length.Px(30),
            Height = Length.Px(30),
        });

        var box = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        var containerBox = box.Children[0];
        var itemBox = containerBox.Children[0];

        // 列方向：交叉轴是水平，absolute 元素应该水平居中
        float expectedX = containerBox.BoxModel.Content.X + (100f - 30f) / 2f;
        itemBox.BoxModel.Content.X.ShouldBe(expectedX, tolerance: 0.01f);
    }

    [Fact]
    public void Should_RespectAlignSelf_OnAbsoluteItem()
    {
        // absolute 元素的 align-self 应该覆盖容器的 align-items
        var root = new DivElement { Class = "root" };
        var container = new DivElement { Class = "container" };
        var item = new DivElement { Class = "item" };

        root.AddChild(container);
        container.AddChild(item);

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("container"), new Style
        {
            Display = Display.Flex,
            AlignItems = AlignItems.FlexStart,
            Width = Length.Px(100),
            Height = Length.Px(100),
        });
        sheet.AddRule(new ClassSelector("item"), new Style
        {
            Position = Position.Absolute,
            AlignSelf = AlignSelf.FlexEnd,
            Width = Length.Px(30),
            Height = Length.Px(30),
        });

        var box = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        var containerBox = box.Children[0];
        var itemBox = containerBox.Children[0];

        // absolute 元素应该按 align-self:flex-end 对齐到底部，而不是 align-items:flex-start
        float expectedY = containerBox.BoxModel.Content.Y + 100f - 30f;
        itemBox.BoxModel.Content.Y.ShouldBe(expectedY, tolerance: 0.01f);
    }
}
