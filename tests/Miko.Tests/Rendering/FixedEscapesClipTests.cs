using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// <c>position: fixed</c> 的包含块是视口，所以它既不该被祖先的 <c>overflow</c> 裁掉，也不该跟着
/// 祖先滚动——即便它挂在一个又矮又裁剪的祖先里面。
/// <para>
/// 由 Ionic 的 <c>ion-select</c> 暴露（issues/ion-select.md 问题 4）：覆盖层（alert / popover /
/// modal / action-sheet）挂在 <c>.ion-select</c> 宿主内部，而外层 <c>.ion-item</c> 是
/// <c>overflow: hidden</c> + <c>min-height: 48px</c>。布局层早已把全屏覆盖层解析到正确的视口坐标
/// （<c>LayoutEngine</c> 对 fixed 换用视口作包含块），但绘制层照旧把它关在祖先的裁剪里，于是
/// 整个覆盖层被切成 48px 高的一条，界面上等于看不见。
/// </para>
/// <para>
/// 修复是 <c>RenderEngine</c> 的顶层 pass：遇到 fixed 就地收集、跳过，等回到画布根状态后由
/// <c>FlushFixed</c> 按 z-index 统一绘制。这些用例守住那条语义，同时守住「absolute 仍然被裁剪」
/// 这条不能误伤的既有行为。
/// </para>
/// </summary>
public class FixedEscapesClipTests : IDisposable
{
    private const int W = 200;
    private const int H = 200;

    /// <summary>裁剪祖先的高度：远小于视口，才能让「逃逸 / 被裁」在像素上区分得开。</summary>
    private const int ClipperHeight = 50;

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;

    private static readonly Color Blue = Color.FromHex("0000ff");

    public FixedEscapesClipTests()
    {
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private static StyleRule Rule(string cls, Style style) => new()
    {
        Selector = new ClassSelector(cls),
        Style = style,
    };

    private MikoEngine Render(Element root, params StyleRule[] rules)
    {
        _canvas.Clear(SKColors.White);
        var engine = new MikoEngine();
        engine.Initialize(root, [new StyleSheet { Rules = rules.ToList() }], _canvas, W, H);
        engine.Render(_canvas);
        return engine;
    }

    private bool IsBlueAt(int x, int y)
    {
        var p = _bitmap.GetPixel(x, y);
        return p.Blue > p.Red;
    }

    /// <summary>
    /// ion-item &gt; ion-select &gt; 覆盖层 的最小复现：
    /// <code>
    /// root
    ///  └ clipper (overflow:hidden, height 50)          — 「ion-item」
    ///      └ overlay (fixed, inset:0, blue)            — 「ion-alert」全屏覆盖层
    /// </code>
    /// 覆盖层铺满视口，(100, 180) 远在裁剪盒之外——修复前这里是白的。
    /// </summary>
    private static (Element Root, StyleRule[] Rules) BuildClippedOverlay(Position overlayPosition)
    {
        var overlay = new DivElement { Class = "overlay" };
        var clipper = new DivElement { Class = "clipper" };
        clipper.AddChild(overlay);
        var root = new DivElement { Class = "root" };
        root.AddChild(clipper);

        return (root,
        [
            Rule("root", new Style { Width = Length.Px(W), Height = Length.Px(H) }),
            Rule("clipper", new Style
            {
                Position = Position.Relative,
                Width = Length.Px(W),
                Height = Length.Px(ClipperHeight),
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            }),
            Rule("overlay", new Style
            {
                Position = overlayPosition,
                Top = Length.Px(0), Right = Length.Px(0),
                Bottom = Length.Px(0), Left = Length.Px(0),
                BackgroundColor = Blue,
            }),
        ]);
    }

    [Fact]
    public void FixedOverlay_EscapesAClippingAncestor()
    {
        var (root, rules) = BuildClippedOverlay(Position.Fixed);
        Render(root, rules);

        IsBlueAt(100, 180).ShouldBeTrue(
            "a fixed overlay must cover the viewport, not be clipped to its overflow:hidden ancestor");
        IsBlueAt(100, 25).ShouldBeTrue("the part inside the clipper is painted too");
    }

    [Fact]
    public void AbsoluteOverlay_IsStillClipped()
    {
        // 回归护栏：修复只能放行 fixed。absolute 仍然要老老实实被裁剪，否则滚动容器里的
        // 定位子元素会漏出裁剪框（RenderEngine.CollectZOrderedDescendants 的既有取舍）。
        var (root, rules) = BuildClippedOverlay(Position.Absolute);
        Render(root, rules);

        IsBlueAt(100, 180).ShouldBeFalse("absolute descendants must still be clipped by the ancestor");
        IsBlueAt(100, 25).ShouldBeTrue("...while the part inside the clipper still paints");
    }

    [Fact]
    public void FixedOverlay_DoesNotScrollWithItsAncestor()
    {
        // fixed 相对视口固定：祖先滚动时它不能跟着走。
        var overlay = new DivElement { Class = "overlay" };
        var spacer = new DivElement { Class = "spacer" };
        var scroller = new DivElement { Class = "scroller" };
        scroller.AddChild(overlay);
        scroller.AddChild(spacer);
        var root = new DivElement { Class = "root" };
        root.AddChild(scroller);

        var engine = Render(root,
            Rule("root", new Style { Width = Length.Px(W), Height = Length.Px(H) }),
            Rule("scroller", new Style
            {
                Position = Position.Relative,
                Width = Length.Px(W),
                Height = Length.Px(ClipperHeight),
                OverflowY = Overflow.Auto,
            }),
            // 撑出可滚动高度。
            Rule("spacer", new Style { Width = Length.Px(W), Height = Length.Px(1000) }),
            Rule("overlay", new Style
            {
                Position = Position.Fixed,
                Top = Length.Px(150), Left = Length.Px(0),
                Width = Length.Px(W), Height = Length.Px(50),
                BackgroundColor = Blue,
            }));

        IsBlueAt(100, 175).ShouldBeTrue("the fixed box sits at y 150..200 before scrolling");

        engine.ScrollBy(100, 25, 0, 300);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(0f, 0.01f,
            "sanity: the root itself must not be the scroller in this fixture");

        _canvas.Clear(SKColors.White);
        engine.Render(_canvas);

        IsBlueAt(100, 175).ShouldBeTrue("a fixed box must stay put when an ancestor scrolls");
    }

    [Fact]
    public void FixedOverlays_PaintInZIndexOrder()
    {
        // 覆盖层之间仍按 z-index 排序（IonMenu/IonModal/IonToast 互相层叠依赖这一点）。
        var under = new DivElement { Class = "under" };
        var over = new DivElement { Class = "over" };
        var root = new DivElement { Class = "root" };
        // 文档序里高 z-index 的在前，只有真的按 z-index 排序才会让它压在上面。
        root.AddChild(over);
        root.AddChild(under);

        Render(root,
            Rule("root", new Style { Width = Length.Px(W), Height = Length.Px(H) }),
            Rule("over", new Style
            {
                Position = Position.Fixed,
                ZIndex = 20,
                Top = Length.Px(0), Left = Length.Px(0),
                Width = Length.Px(W), Height = Length.Px(H),
                BackgroundColor = Blue,
            }),
            Rule("under", new Style
            {
                Position = Position.Fixed,
                ZIndex = 10,
                Top = Length.Px(0), Left = Length.Px(0),
                Width = Length.Px(W), Height = Length.Px(H),
                BackgroundColor = Color.FromHex("ff0000"),
            }));

        IsBlueAt(100, 100).ShouldBeTrue("z 20 must paint over z 10 regardless of document order");
    }

    [Fact]
    public void FixedOverlay_IsHitTestableOutsideAClippingAncestor()
    {
        // 「看得见但点不到」是同一个 bug 的另一半：命中测试必须和绘制顺序镜像，否则覆盖层上的
        // 按钮点不动（ion-select 的选项就是这样）。
        var (root, rules) = BuildClippedOverlay(Position.Fixed);
        var engine = Render(root, rules);

        var hit = engine.HitTest(100, 180);

        hit.ShouldNotBeNull();
        hit.HasClass("overlay").ShouldBeTrue(
            "the point is outside the clipping ancestor, but a fixed overlay still covers it");
    }

    [Fact]
    public void AbsoluteOverlay_IsStillNotHitTestableOutsideItsClippingAncestor()
    {
        // 与绘制侧对称的回归护栏。
        var (root, rules) = BuildClippedOverlay(Position.Absolute);
        var engine = Render(root, rules);

        var hit = engine.HitTest(100, 180);

        (hit?.HasClass("overlay") ?? false).ShouldBeFalse(
            "a clipped absolute descendant must not be hit outside its ancestor");
    }
}
