using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-103：逻辑属性在布局中的端到端行为（经计算样式映射为物理边后走既有布局路径）。
/// </summary>
public class LogicalPropertyLayoutTests
{
    private readonly LayoutEngine _layoutEngine = new();

    [Fact]
    public void BlockLayout_MarginInlineStart_OffsetsContentBox()
    {
        var container = new DivElement();
        var child = new DivElement
        {
            Style = new Style { MarginInlineStart = Length.Px(20), Height = Length.Px(40) },
        };
        container.AddChild(child);

        var root = _layoutEngine.Layout(container, [], 400, 600);

        // 默认 horizontal-tb + ltr：margin-inline-start 即左边距。
        root.Children[0].BoxModel.Content.X.ShouldBe(20);
        root.Children[0].BoxModel.Content.Width.ShouldBe(380);
    }

    [Fact]
    public void BlockLayout_MarginInlineStart_AppliesRightMargin_WhenRtl()
    {
        var container = new DivElement();
        var child = new DivElement
        {
            Style = new Style
            {
                Direction = Direction.Rtl,
                MarginInlineStart = Length.Px(20),
                Height = Length.Px(40),
            },
        };
        container.AddChild(child);

        var root = _layoutEngine.Layout(container, [], 400, 600);

        // rtl：margin-inline-start 映射为右边距，内容宽被右边距压缩，左侧无偏移。
        var childBox = root.Children[0];
        childBox.ComputedStyle.MarginRight.Value.ShouldBe(20f);
        childBox.BoxModel.Content.X.ShouldBe(0);
        childBox.BoxModel.Content.Width.ShouldBe(380);
    }

    [Fact]
    public void BlockLayout_PaddingInlineAndBlock_ApplyToBoxModel()
    {
        var container = new DivElement
        {
            Style = new Style
            {
                PaddingInline = Length.Px(10),
                PaddingBlock = Length.Px(5),
            },
        };
        var child = new DivElement { Style = new Style { Height = Length.Px(40) } };
        container.AddChild(child);

        var root = _layoutEngine.Layout(container, [], 400, 600);

        root.BoxModel.Padding.Left.ShouldBe(10);
        root.BoxModel.Padding.Right.ShouldBe(10);
        root.BoxModel.Padding.Top.ShouldBe(5);
        root.BoxModel.Padding.Bottom.ShouldBe(5);
        root.Children[0].BoxModel.Content.X.ShouldBe(10);
        root.Children[0].BoxModel.Content.Y.ShouldBe(5);
    }

    [Fact]
    public void BlockLayout_InlineSize_SetsWidth()
    {
        var container = new DivElement();
        var child = new DivElement
        {
            Style = new Style { InlineSize = Length.Px(120), BlockSize = Length.Px(40) },
        };
        container.AddChild(child);

        var root = _layoutEngine.Layout(container, [], 400, 600);

        root.Children[0].BoxModel.Content.Width.ShouldBe(120);
        root.Children[0].BoxModel.Content.Height.ShouldBe(40);
    }
}
