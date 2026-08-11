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
    private readonly LayoutEngine _layoutEngine = new();

    [Fact]
    public void Should_CenterNestedFlexItemsCorrectly()
    {
        // ISSUE-124: 嵌套 flex 容器的 align-items:center 应该各自相对自己的高度居中
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
        });
        sheet.AddRule(new ClassSelector("toggle-inner"), new Style
        {
            Height = Length.Px(20),
            Width = Length.Px(20),
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

        // .toggle-inner: 20×20
        var innerBox = wrapperBox.Children[0];
        innerBox.BoxModel.Content.Width.ShouldBe(20f);
        innerBox.BoxModel.Content.Height.ShouldBe(20f);

        // 关键测试：inner 应该在 wrapper (14px 高) 中垂直居中
        // inner 的高度 (20px) 大于 wrapper 的高度 (14px)，居中后应该上溢出 3px
        // 相对于 wrapper 的 Y 偏移 = (14 - 20) / 2 = -3
        // 绝对 Y 位置 = wrapper.Y + 相对偏移 = 13 + (-3) = 10
        float expectedInnerY = wrapperBox.BoxModel.Content.Y + (14f - 20f) / 2f;
        innerBox.BoxModel.Content.Y.ShouldBe(expectedInnerY, tolerance: 0.01f);
    }
}
