using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Layout;

/// <summary>
/// 平台滚动条样式对布局的影响（ISSUE-156）：只有 classic 滚动条占用布局空间；overlay 盖在内容之上，
/// none 不画——两者都不缩小内容宽度，容器也照样可以滚动。<c>scrollbar-width</c> 仍只管粗细与隐藏。
/// </summary>
public class ScrollbarStyleLayoutTests
{
    private static DivElement ScrollContainer(ScrollbarWidth? width = null, float contentHeight = 300) => new()
    {
        Style = new Style
        {
            Display = Display.Block,
            Width = Length.Px(200),
            Height = Length.Px(100),
            OverflowY = Overflow.Scroll,
            ScrollbarWidth = width,
        },
        Children = { new DivElement { Style = new Style { Height = Length.Px(contentHeight) } } }
    };

    private static LayoutBox Layout(Element root, ScrollbarStyle style)
    {
        var layoutEngine = new LayoutEngine { ScrollbarStyle = style };
        return layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);
    }

    [Fact]
    public void Should_stamp_the_engine_style_on_computed_styles()
    {
        var layout = Layout(ScrollContainer(), ScrollbarStyle.Overlay);

        layout.ComputedStyle.ScrollbarStyle.ShouldBe(ScrollbarStyle.Overlay);
        layout.Children[0].ComputedStyle.ScrollbarStyle.ShouldBe(ScrollbarStyle.Overlay);
        layout.UsesOverlayScrollbars.ShouldBeTrue();
    }

    [Fact]
    public void Should_reserve_the_gutter_for_classic_scrollbars()
    {
        var layout = Layout(ScrollContainer(), ScrollbarStyle.Classic);

        layout.ShowsVerticalScrollbar.ShouldBeTrue();
        layout.VerticalScrollbarThickness.ShouldBe(LayoutBox.ScrollbarThickness);
        layout.VerticalScrollbarGutter.ShouldBe(LayoutBox.ScrollbarThickness);
        layout.Children[0].BoxModel.Content.Width.ShouldBe(200 - LayoutBox.ScrollbarThickness);
    }

    [Theory]
    [InlineData(null, LayoutBox.OverlayScrollbarThickness)]
    [InlineData(ScrollbarWidth.Thin, LayoutBox.ThinOverlayScrollbarThickness)]
    public void Should_draw_overlay_scrollbars_over_the_content_without_a_gutter(ScrollbarWidth? width, float thickness)
    {
        var layout = Layout(ScrollContainer(width), ScrollbarStyle.Overlay);

        layout.ShowsVerticalScrollbar.ShouldBeTrue();
        layout.VerticalScrollbarThickness.ShouldBe(thickness);
        layout.VerticalScrollbarGutter.ShouldBe(0f);
        layout.Children[0].BoxModel.Content.Width.ShouldBe(200f);
    }

    [Fact]
    public void Should_hide_scrollbars_without_a_gutter_when_the_platform_has_none()
    {
        var layout = Layout(ScrollContainer(), ScrollbarStyle.None);

        layout.HasVerticalScrollbar.ShouldBeTrue("the container still scrolls");
        layout.ShowsVerticalScrollbar.ShouldBeFalse();
        layout.VerticalScrollbarThickness.ShouldBe(0f);
        layout.Children[0].BoxModel.Content.Width.ShouldBe(200f);
    }

    [Theory]
    [InlineData(ScrollbarStyle.Classic)]
    [InlineData(ScrollbarStyle.Overlay)]
    public void Should_still_hide_scrollbars_with_scrollbar_width_none(ScrollbarStyle style)
    {
        var layout = Layout(ScrollContainer(ScrollbarWidth.None), style);

        layout.ShowsVerticalScrollbar.ShouldBeFalse();
        layout.Children[0].BoxModel.Content.Width.ShouldBe(200f);
    }

    [Fact]
    public void Should_not_relayout_for_an_auto_overflow_overlay_scrollbar()
    {
        // overflow:auto 一旦溢出，classic 要预留宽度再排一遍子项；overlay 不占位，宽度保持不变。
        var root = ScrollContainer();
        root.Style!.OverflowY = Overflow.Auto;

        var classic = Layout(root, ScrollbarStyle.Classic);
        classic.Children[0].BoxModel.Content.Width.ShouldBe(200 - LayoutBox.ScrollbarThickness);

        var overlay = Layout(root, ScrollbarStyle.Overlay);
        overlay.HasVerticalScrollbar.ShouldBeTrue();
        overlay.Children[0].BoxModel.Content.Width.ShouldBe(200f);
    }

    [Fact]
    public void Should_relayout_when_the_engine_style_changes()
    {
        var root = ScrollContainer();
        var engine = new MikoEngineBuilder().Build();
        using var surface = SKSurface.Create(new SKImageInfo(400, 300));
        engine.Initialize(root, new List<StyleSheet>(), surface.Canvas, 400, 300);
        var content = root.Children[0];
        content.LayoutBox!.BoxModel.Content.Width.ShouldBe(200 - LayoutBox.ScrollbarThickness);
        engine.HasPendingVisualWork.ShouldBeFalse();

        engine.ScrollbarStyle = ScrollbarStyle.Overlay;

        // 布局缓存必须失效：否则快速路径会沿用 classic 的布局，内容一直缺一条滚动条宽。
        engine.HasPendingVisualWork.ShouldBeTrue();
        engine.Render(surface.Canvas);
        content.LayoutBox!.BoxModel.Content.Width.ShouldBe(200f);
        content.LayoutBox!.ComputedStyle.ScrollbarStyle.ShouldBe(ScrollbarStyle.Overlay);
        engine.HasPendingVisualWork.ShouldBeFalse();

        // 值不变时赋值是空操作（控制器每帧都会赋一次）。
        engine.ScrollbarStyle = ScrollbarStyle.Overlay;
        engine.HasPendingVisualWork.ShouldBeFalse();
    }
}
