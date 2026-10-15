using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-150：CSS <c>aspect-ratio</c>。width / height 至少一边为 auto 时，由另一边按首选纵横比推导
/// （CSS Box Sizing 4 §4）。覆盖块级流、inline-block、替换元素、flex（容器与项目）、grid 项目与绝对定位。
/// </summary>
public class AspectRatioLayoutTests
{
    private readonly LayoutEngine _layoutEngine = new();

    /// <summary>以固定尺寸的块级根容器包裹子元素并布局，返回根布局盒。</summary>
    private LayoutBox LayoutInRoot(Style rootStyle, params Element[] children)
    {
        var root = new DivElement { Style = rootStyle };
        foreach (var child in children) root.AddChild(child);
        return _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);
    }

    private LayoutBox LayoutInBlock(float width, params Element[] children)
        => LayoutInRoot(new Style { Display = Display.Block, Width = Length.Px(width), Height = Length.Px(600) }, children);

    private static DivElement Div(Style style, params Element[] children)
    {
        var div = new DivElement { Style = style };
        foreach (var child in children) div.AddChild(child);
        return div;
    }

    // ---- 块级流 ----

    [Fact]
    public void Should_derive_height_from_width_for_auto_height_block()
    {
        var box = Div(new Style { Display = Display.Block, AspectRatio = new AspectRatio(16, 9) });

        var root = LayoutInBlock(400, box);

        var content = root.Children[0].BoxModel.Content;
        content.Width.ShouldBe(400f, 0.01f);
        content.Height.ShouldBe(225f, 0.01f);
    }

    [Fact]
    public void Should_treat_single_number_as_ratio_over_one()
    {
        var box = Div(new Style { Display = Display.Block, Width = Length.Px(100), AspectRatio = new AspectRatio(0.5f) });

        var root = LayoutInBlock(400, box);

        root.Children[0].BoxModel.Content.Height.ShouldBe(200f, 0.01f);
    }

    [Fact]
    public void Should_derive_width_from_explicit_height_instead_of_filling_container()
    {
        var box = Div(new Style { Display = Display.Block, Height = Length.Px(100), AspectRatio = new AspectRatio(2, 1) });

        var root = LayoutInBlock(400, box);

        var content = root.Children[0].BoxModel.Content;
        content.Width.ShouldBe(200f, 0.01f);
        content.Height.ShouldBe(100f, 0.01f);
    }

    [Fact]
    public void Should_center_ratio_derived_width_with_auto_margins()
    {
        var box = Div(new Style
        {
            Display = Display.Block, Height = Length.Px(100), AspectRatio = new AspectRatio(2, 1),
            MarginLeft = Length.Auto, MarginRight = Length.Auto,
        });

        var root = LayoutInBlock(400, box);

        root.Children[0].BoxModel.Margin.Left.ShouldBe(100f, 0.01f);
    }

    [Fact]
    public void Should_ignore_ratio_when_both_sizes_are_specified()
    {
        var box = Div(new Style
        {
            Display = Display.Block, Width = Length.Px(300), Height = Length.Px(50), AspectRatio = new AspectRatio(1, 1),
        });

        var root = LayoutInBlock(400, box);

        var content = root.Children[0].BoxModel.Content;
        content.Width.ShouldBe(300f, 0.01f);
        content.Height.ShouldBe(50f, 0.01f);
    }

    [Fact]
    public void Should_apply_ratio_to_content_box_by_default()
    {
        var box = Div(new Style { Display = Display.Block, Padding = new Padding(Length.Px(10)), AspectRatio = new AspectRatio(2, 1) });

        var root = LayoutInBlock(400, box);

        // 内容宽 400 - 20 = 380 → 内容高 190。
        root.Children[0].BoxModel.Content.Height.ShouldBe(190f, 0.01f);
    }

    [Fact]
    public void Should_apply_ratio_to_border_box_when_box_sizing_is_border_box()
    {
        var box = Div(new Style
        {
            Display = Display.Block, BoxSizing = BoxSizing.BorderBox,
            Padding = new Padding(Length.Px(10)), AspectRatio = new AspectRatio(2, 1),
        });

        var root = LayoutInBlock(400, box);

        // border-box 宽 400 → border-box 高 200 → 内容高 180。
        root.Children[0].BoxModel.BorderBox.Height.ShouldBe(200f, 0.01f);
    }

    [Fact]
    public void Should_apply_auto_and_ratio_to_content_box_even_with_border_box_sizing()
    {
        var box = Div(new Style
        {
            Display = Display.Block, BoxSizing = BoxSizing.BorderBox,
            Padding = new Padding(Length.Px(10)), AspectRatio = AspectRatio.AutoOr(2, 1),
        });

        var root = LayoutInBlock(400, box);

        root.Children[0].BoxModel.Content.Height.ShouldBe(190f, 0.01f);
    }

    [Fact]
    public void Should_clamp_ratio_derived_height_by_max_height()
    {
        var box = Div(new Style { Display = Display.Block, MaxHeight = Length.Px(100), AspectRatio = new AspectRatio(1, 1) });

        var root = LayoutInBlock(400, box);

        root.Children[0].BoxModel.Content.Height.ShouldBe(100f, 0.01f);
    }

    [Fact]
    public void Should_raise_ratio_derived_height_by_min_height()
    {
        var box = Div(new Style { Display = Display.Block, MinHeight = Length.Px(300), AspectRatio = new AspectRatio(16, 9) });

        var root = LayoutInBlock(400, box);

        root.Children[0].BoxModel.Content.Height.ShouldBe(300f, 0.01f);
    }

    [Fact]
    public void Should_clamp_determining_height_before_deriving_width()
    {
        var box = Div(new Style
        {
            Display = Display.Block, Height = Length.Px(300), MaxHeight = Length.Px(100), AspectRatio = new AspectRatio(2, 1),
        });

        var root = LayoutInBlock(400, box);

        var content = root.Children[0].BoxModel.Content;
        content.Height.ShouldBe(100f, 0.01f);
        content.Width.ShouldBe(200f, 0.01f);
    }

    [Fact]
    public void Should_grow_to_fit_content_when_box_is_not_a_scroll_container()
    {
        var tall = Div(new Style { Display = Display.Block, Height = Length.Px(300) });
        var box = Div(new Style { Display = Display.Block, Width = Length.Px(100), AspectRatio = new AspectRatio(1, 1) }, tall);

        var root = LayoutInBlock(400, box);

        // CSS Box Sizing 4 §4.3：非滚动容器的块轴自动最小尺寸为内容高度。
        root.Children[0].BoxModel.Content.Height.ShouldBe(300f, 0.01f);
    }

    [Fact]
    public void Should_keep_ratio_and_overflow_when_box_is_a_scroll_container()
    {
        var tall = Div(new Style { Display = Display.Block, Height = Length.Px(300) });
        var box = Div(new Style
        {
            Display = Display.Block, Width = Length.Px(100), AspectRatio = new AspectRatio(1, 1), Overflow = Overflow.Hidden,
        }, tall);

        var root = LayoutInBlock(400, box);

        root.Children[0].BoxModel.Content.Height.ShouldBe(100f, 0.01f);
    }

    [Fact]
    public void Should_resolve_percentage_height_children_against_ratio_derived_height()
    {
        var fill = Div(new Style { Display = Display.Block, Height = Length.Percent(100) });
        var box = Div(new Style { Display = Display.Block, AspectRatio = new AspectRatio(16, 9) }, fill);

        var root = LayoutInBlock(400, box);

        // 由确定宽度推导的高度是确定尺寸，子元素 height:100% 能解析为 225（而不是退化为内容高度 0）。
        root.Children[0].Children[0].BoxModel.Content.Height.ShouldBe(225f, 0.01f);
    }

    [Fact]
    public void Should_behave_as_auto_when_ratio_is_degenerate()
    {
        var box = Div(new Style { Display = Display.Block, AspectRatio = new AspectRatio(0, 1) });

        var root = LayoutInBlock(400, box);

        root.Children[0].BoxModel.Content.Height.ShouldBe(0f, 0.01f);
    }

    [Fact]
    public void Should_not_apply_to_non_replaced_inline_boxes()
    {
        var withRatio = new SpanElement { TextContent = "Hello", Style = new Style { Display = Display.Inline, AspectRatio = new AspectRatio(1, 1) } };
        var control = new SpanElement { TextContent = "Hello", Style = new Style { Display = Display.Inline } };

        var ratioBox = LayoutInBlock(400, withRatio).Children[0].BoxModel.Content;
        var controlBox = LayoutInBlock(400, control).Children[0].BoxModel.Content;

        // 比值若生效，宽约 40px 的 span 会被撑成约 40px 高；不生效则与对照组同为一行高。
        ratioBox.Width.ShouldBeGreaterThan(20f);
        ratioBox.Height.ShouldBe(controlBox.Height, 0.01f);
    }

    [Fact]
    public void Should_reflow_when_aspect_ratio_changes()
    {
        var box = Div(new Style { Display = Display.Block, AspectRatio = new AspectRatio(2, 1) });
        var root = new DivElement { Style = new Style { Display = Display.Block, Width = Length.Px(400) } };
        root.AddChild(box);
        var engine = new LayoutEngine();
        engine.Layout(root, new List<StyleSheet>(), 800, 600).Children[0].BoxModel.Content.Height.ShouldBe(200f, 0.01f);

        box.Style = new Style { Display = Display.Block, AspectRatio = new AspectRatio(4, 1) };

        engine.Layout(root, new List<StyleSheet>(), 800, 600).Children[0].BoxModel.Content.Height.ShouldBe(100f, 0.01f);
    }

    // ---- inline-block 与替换元素 ----

    [Fact]
    public void Should_apply_to_inline_block()
    {
        var box = Div(new Style { Display = Display.InlineBlock, Width = Length.Px(120), AspectRatio = new AspectRatio(2, 1) });

        var root = LayoutInBlock(400, box);

        root.Children[0].BoxModel.Content.Height.ShouldBe(60f, 0.01f);
    }

    [Fact]
    public void Should_reserve_space_for_image_that_has_not_loaded()
    {
        var img = new ImageElement { Style = new Style { Display = Display.Block, Width = Length.Px(200), AspectRatio = new AspectRatio(4, 3) } };

        var root = LayoutInBlock(400, img);

        root.Children[0].BoxModel.Content.Height.ShouldBe(150f, 0.01f);
    }

    [Fact]
    public void Should_reserve_space_for_inline_image_that_has_not_loaded()
    {
        var img = new ImageElement { Style = new Style { Width = Length.Px(200), AspectRatio = new AspectRatio(4, 3) } };

        var root = LayoutInBlock(400, img);

        root.Children[0].BoxModel.Content.Height.ShouldBe(150f, 0.01f);
    }

    [Fact]
    public void Should_override_natural_ratio_with_declared_ratio()
    {
        var img = new ImageElement
        {
            IntrinsicWidth = 400, IntrinsicHeight = 300,
            Style = new Style { Display = Display.Block, Width = Length.Px(200), AspectRatio = new AspectRatio(2, 1) },
        };

        var root = LayoutInBlock(400, img);

        root.Children[0].BoxModel.Content.Height.ShouldBe(100f, 0.01f);
    }

    [Fact]
    public void Should_prefer_natural_ratio_when_auto_keyword_is_given()
    {
        var img = new ImageElement
        {
            IntrinsicWidth = 400, IntrinsicHeight = 300,
            Style = new Style { Display = Display.Block, Width = Length.Px(200), AspectRatio = AspectRatio.AutoOr(2, 1) },
        };

        var root = LayoutInBlock(400, img);

        root.Children[0].BoxModel.Content.Height.ShouldBe(150f, 0.01f);
    }

    [Fact]
    public void Should_derive_video_height_from_declared_ratio()
    {
        // 未加载的 video 回退到 300×150（2:1）；声明的 16/9 覆盖它。
        var video = new VideoElement { Style = new Style { Display = Display.Block, Width = Length.Px(320), AspectRatio = new AspectRatio(16, 9) } };

        var root = LayoutInBlock(400, video);

        root.Children[0].BoxModel.Content.Height.ShouldBe(180f, 0.01f);
    }

    // ---- flex ----

    [Fact]
    public void Should_size_column_flex_item_from_its_cross_size()
    {
        // MikoPlayer 的形状：flex 列容器里 width:100% 的视口按 16:9 定高，后续兄弟紧随其后。
        var viewport = Div(new Style { Display = Display.Block, Width = Length.Percent(100), AspectRatio = new AspectRatio(16, 9) });
        var controls = Div(new Style { Display = Display.Block, Height = Length.Px(48) });
        var column = Div(new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Width = Length.Percent(100) }, viewport, controls);

        var root = LayoutInBlock(360, column);

        var columnBox = root.Children[0];
        columnBox.Children[0].BoxModel.Content.Height.ShouldBe(202.5f, 0.01f);
        columnBox.Children[1].BoxModel.Content.Y.ShouldBe(202.5f, 0.01f);
        columnBox.BoxModel.Content.Height.ShouldBe(250.5f, 0.01f);
    }

    [Fact]
    public void Should_size_column_flex_item_with_auto_width_from_stretched_cross_size()
    {
        var item = Div(new Style { Display = Display.Block, AspectRatio = new AspectRatio(2, 1) });
        var column = Div(new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column }, item);

        var root = LayoutInBlock(400, column);

        root.Children[0].Children[0].BoxModel.Content.Height.ShouldBe(200f, 0.01f);
    }

    [Fact]
    public void Should_size_row_flex_item_from_explicit_width_when_not_stretched()
    {
        var item = Div(new Style { Display = Display.Block, Width = Length.Px(100), AspectRatio = new AspectRatio(1, 1) });
        var row = Div(new Style { Display = Display.Flex, AlignItems = AlignItems.FlexStart, Height = Length.Px(300) }, item);

        var root = LayoutInBlock(400, row);

        var content = root.Children[0].Children[0].BoxModel.Content;
        content.Width.ShouldBe(100f, 0.01f);
        content.Height.ShouldBe(100f, 0.01f);
    }

    [Fact]
    public void Should_let_stretch_win_over_ratio_in_definite_row()
    {
        // 与浏览器一致：stretch 拉伸到行交叉尺寸，即使这会打破比值（需 align-items 非 stretch 才保比值）。
        var item = Div(new Style { Display = Display.Block, Width = Length.Px(100), AspectRatio = new AspectRatio(1, 1) });
        var row = Div(new Style { Display = Display.Flex, Height = Length.Px(300) }, item);

        var root = LayoutInBlock(400, row);

        root.Children[0].Children[0].BoxModel.Content.Height.ShouldBe(300f, 0.01f);
    }

    [Fact]
    public void Should_size_auto_row_from_ratio_heights()
    {
        var item = Div(new Style { Display = Display.Block, Width = Length.Px(100), AspectRatio = new AspectRatio(1, 1) });
        var row = Div(new Style { Display = Display.Flex }, item);

        var root = LayoutInBlock(400, row);

        root.Children[0].BoxModel.Content.Height.ShouldBe(100f, 0.01f);
    }

    [Fact]
    public void Should_keep_main_size_of_item_stretched_in_auto_height_row()
    {
        // 容器高度 auto：交叉尺寸不确定，basis 按内容（空 → 0）。随后被同行更高的兄弟拉伸到 100 高时，
        // 主轴尺寸已由 flex 算法定型，不得再经比值改成 100 宽——否则会盖住兄弟（与浏览器一致）。
        // 带一个空子元素：叶子盒的拉伸只改写尺寸、不重排，测不到这条路径。
        var square = Div(new Style { Display = Display.Block, AspectRatio = new AspectRatio(1, 1) },
            Div(new Style { Display = Display.Block }));
        var tall = Div(new Style { Display = Display.Block, Width = Length.Px(50), Height = Length.Px(100) });
        var row = Div(new Style { Display = Display.Flex }, square, tall);

        var root = LayoutInBlock(400, row);

        var rowBox = root.Children[0];
        rowBox.Children[0].BoxModel.Content.Width.ShouldBe(0f, 0.01f);
        rowBox.Children[0].BoxModel.Content.Height.ShouldBe(100f, 0.01f);
        rowBox.Children[1].BoxModel.Content.X.ShouldBe(0f, 0.01f);
    }

    [Fact]
    public void Should_derive_row_item_main_size_from_stretched_definite_cross_size()
    {
        // CSS Flexbox §9.2 3B：单行容器交叉尺寸确定时，stretch 项的交叉尺寸确定，flex base size 由它换算。
        var item = Div(new Style { Display = Display.Block, AspectRatio = new AspectRatio(2, 1) });
        var row = Div(new Style { Display = Display.Flex, Height = Length.Px(100) }, item);

        var root = LayoutInBlock(400, row);

        var content = root.Children[0].Children[0].BoxModel.Content;
        content.Width.ShouldBe(200f, 0.01f);
        content.Height.ShouldBe(100f, 0.01f);
    }

    [Fact]
    public void Should_give_flex_container_a_definite_ratio_height_for_its_items()
    {
        var grow = Div(new Style { Display = Display.Block, FlexGrow = 1 });
        var container = Div(new Style
        {
            Display = Display.Flex, FlexDirection = FlexDirection.Column, Width = Length.Px(200), AspectRatio = new AspectRatio(2, 1),
        }, grow);

        var root = LayoutInBlock(400, container);

        var containerBox = root.Children[0];
        containerBox.BoxModel.Content.Height.ShouldBe(100f, 0.01f);
        // 比值推导的高度是确定的主轴尺寸：flex-grow 子项据此填满。
        containerBox.Children[0].BoxModel.Content.Height.ShouldBe(100f, 0.01f);
    }

    [Fact]
    public void Should_stretch_row_items_to_ratio_height_of_flex_container()
    {
        // 非滚动容器的比值高度推迟到内容排完后才定（§4.3 内容下限），比值胜出时以确定高度重排：
        // stretch 项随之拉到比值高度，而不是停留在第一遍的内容高度。
        var item = Div(new Style { Display = Display.Block, Width = Length.Px(50) });
        var container = Div(new Style { Display = Display.Flex, Width = Length.Px(200), AspectRatio = new AspectRatio(2, 1) }, item);

        var root = LayoutInBlock(400, container);

        root.Children[0].BoxModel.Content.Height.ShouldBe(100f, 0.01f);
        root.Children[0].Children[0].BoxModel.Content.Height.ShouldBe(100f, 0.01f);
    }

    [Fact]
    public void Should_center_column_items_within_ratio_height()
    {
        var item = Div(new Style { Display = Display.Block, Height = Length.Px(20) });
        var container = Div(new Style
        {
            Display = Display.Flex, FlexDirection = FlexDirection.Column, JustifyContent = JustifyContent.Center,
            Width = Length.Px(200), AspectRatio = new AspectRatio(2, 1),
        }, item);

        var root = LayoutInBlock(400, container);

        // (100 - 20) / 2 = 40。
        root.Children[0].Children[0].BoxModel.Content.Y.ShouldBe(40f, 0.01f);
    }

    [Fact]
    public void Should_derive_flex_container_width_from_explicit_height()
    {
        var container = Div(new Style { Display = Display.Flex, Height = Length.Px(50), AspectRatio = new AspectRatio(3, 1) });

        var root = LayoutInBlock(400, container);

        root.Children[0].BoxModel.Content.Width.ShouldBe(150f, 0.01f);
    }

    [Fact]
    public void Should_grow_flex_container_to_fit_content_beyond_ratio()
    {
        var tall = Div(new Style { Display = Display.Block, Height = Length.Px(200) });
        var container = Div(new Style
        {
            Display = Display.Flex, FlexDirection = FlexDirection.Column, Width = Length.Px(100), AspectRatio = new AspectRatio(1, 1),
        }, tall);

        var root = LayoutInBlock(400, container);

        root.Children[0].BoxModel.Content.Height.ShouldBe(200f, 0.01f);
    }

    // ---- grid ----

    [Fact]
    public void Should_not_stretch_grid_item_with_ratio_under_normal_alignment()
    {
        var square = Div(new Style { Display = Display.Block, AspectRatio = new AspectRatio(1, 1) });
        var tall = Div(new Style { Display = Display.Block, Height = Length.Px(300) });
        var grid = Div(new Style
        {
            Display = Display.Grid,
            GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.FromLength(Length.Px(200)), GridTrackSize.FromLength(Length.Px(200)) },
        }, square, tall);

        var root = LayoutInBlock(400, grid);

        // CSS Grid 2 §6.2：normal 对齐下有首选纵横比的项按块级规则定尺寸——填满 area 宽，高由比值推导，
        // 不被同行更高的兄弟拉伸到 300。
        var content = root.Children[0].Children[0].BoxModel.Content;
        content.Width.ShouldBe(200f, 0.01f);
        content.Height.ShouldBe(200f, 0.01f);
    }

    [Fact]
    public void Should_size_auto_grid_row_from_ratio_height()
    {
        var square = Div(new Style { Display = Display.Block, AspectRatio = new AspectRatio(1, 1) });
        var grid = Div(new Style
        {
            Display = Display.Grid,
            GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.FromLength(Length.Px(150)) },
        }, square);

        var root = LayoutInBlock(400, grid);

        root.Children[0].BoxModel.Content.Height.ShouldBe(150f, 0.01f);
    }

    [Fact]
    public void Should_stretch_grid_item_when_align_self_is_explicit_stretch()
    {
        var square = Div(new Style { Display = Display.Block, AspectRatio = new AspectRatio(1, 1), AlignSelf = AlignSelf.Stretch });
        var grid = Div(new Style
        {
            Display = Display.Grid,
            GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.FromLength(Length.Px(100)) },
            GridTemplateRows = new List<GridTrackSize> { GridTrackSize.FromLength(Length.Px(300)) },
        }, square);

        var root = LayoutInBlock(400, grid);

        root.Children[0].Children[0].BoxModel.Content.Height.ShouldBe(300f, 0.01f);
    }

    // ---- 绝对定位 ----

    [Fact]
    public void Should_derive_height_of_inset_stretched_absolute_box_from_width()
    {
        var overlay = Div(new Style
        {
            Display = Display.Block, Position = Position.Absolute,
            Top = Length.Px(0), Right = Length.Px(0), Bottom = Length.Px(0), Left = Length.Px(0),
            AspectRatio = new AspectRatio(2, 1),
        });

        var root = LayoutInRoot(new Style
        {
            Display = Display.Block, Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300),
        }, overlay);

        // CSS Position 3 §4.1：两轴都由偏移定型时块轴是 ratio-dependent 轴。
        var content = root.Children[0].BoxModel.Content;
        content.Width.ShouldBe(400f, 0.01f);
        content.Height.ShouldBe(200f, 0.01f);
    }

    [Fact]
    public void Should_derive_width_of_absolute_box_stretched_only_vertically()
    {
        var overlay = Div(new Style
        {
            Display = Display.Block, Position = Position.Absolute,
            Top = Length.Px(0), Bottom = Length.Px(0), Left = Length.Px(0),
            AspectRatio = new AspectRatio(1, 2),
        });

        var root = LayoutInRoot(new Style
        {
            Display = Display.Block, Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300),
        }, overlay);

        var content = root.Children[0].BoxModel.Content;
        content.Height.ShouldBe(300f, 0.01f);
        content.Width.ShouldBe(150f, 0.01f);
    }
}
