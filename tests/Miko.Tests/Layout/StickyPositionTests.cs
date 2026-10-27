using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Layout;

/// <summary>
/// <c>position: sticky</c>（ISSUE-159）：在常规流中占位，随最近的滚动容器滚动时被 inset 限定在
/// 可视区内、不越出包含块。滚动不触发重排，粘性偏移在每次绘制与命中测试前按当前滚动量重算，
/// 所以这些用例都走真实引擎：滚动 → 绘制 → 读几何 / 像素 / 命中。
/// </summary>
public class StickyPositionTests : IDisposable
{
    private const int W = 200;
    private const int H = 300;

    private static readonly Color Blue = Color.FromHex("0000ff");
    private static readonly Color Red = Color.FromHex("ff0000");

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;

    public StickyPositionTests()
    {
        _canvas = new SKCanvas(_bitmap);
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

    private MikoEngine Initialize(Element root, params StyleRule[] rules)
    {
        var engine = new MikoEngineBuilder().Build();
        engine.Initialize(root, [new StyleSheet { Rules = rules.ToList() }], _canvas, W, H);
        return engine;
    }

    private void Render(MikoEngine engine)
    {
        _canvas.Clear(SKColors.White);
        engine.Render(_canvas);
    }

    private bool IsBlueAt(int x, int y)
    {
        var p = _bitmap.GetPixel(x, y);
        return p.Blue > 200 && p.Red < 50;
    }

    private static float Top(Element element) => element.LayoutBox!.BoxModel.BorderBox.Top;

    private static float Left(Element element) => element.LayoutBox!.BoxModel.BorderBox.Left;

    /// <summary>
    /// <code>
    /// root (200×300)
    ///  └ scroller (overflow-y:auto, 200×100)
    ///      ├ spacer  (h 40)
    ///      ├ header  (sticky, h 20, blue)  ← 视窗内的样式由 <paramref name="header"/> 给出
    ///      └ content (h 400, red)
    /// </code>
    /// </summary>
    private static (DivElement Root, DivElement Scroller, DivElement Header, DivElement Content, StyleRule[] Rules)
        BuildList(Style header)
    {
        var headerEl = new DivElement { Class = "header" };
        var contentEl = new DivElement { Class = "content" };
        var scroller = new DivElement { Class = "scroller" };
        scroller.AddChild(new DivElement { Class = "spacer" });
        scroller.AddChild(headerEl);
        scroller.AddChild(contentEl);
        var root = new DivElement { Class = "root" };
        root.AddChild(scroller);

        header.Position ??= Position.Sticky;
        header.Height ??= Length.Px(20);
        header.BackgroundColor ??= Blue;

        return (root, scroller, headerEl, contentEl,
        [
            Rule("root", new Style { Width = Length.Px(W), Height = Length.Px(H) }),
            Rule("scroller", new Style
            {
                Width = Length.Px(W),
                Height = Length.Px(100),
                OverflowY = Overflow.Auto,
            }),
            Rule("spacer", new Style { Height = Length.Px(40) }),
            Rule("header", header),
            Rule("content", new Style { Height = Length.Px(400), BackgroundColor = Red }),
        ]);
    }

    // ---------- 常规流与基本粘附 ----------

    [Fact]
    public void Sticky_AtRest_OccupiesItsNormalFlowPosition()
    {
        var (root, _, header, content, rules) = BuildList(new Style { Top = Length.Px(0) });
        var engine = Initialize(root, rules);
        Render(engine);

        Top(header).ShouldBe(40);
        // 粘性盒在常规流中占位：后继兄弟排在它之后。
        Top(content).ShouldBe(60);
    }

    [Fact]
    public void Sticky_ScrolledPast_SticksToTheScrollportTop()
    {
        var (root, scroller, header, content, rules) = BuildList(new Style { Top = Length.Px(0) });
        var engine = Initialize(root, rules);
        engine.ScrollBy(100, 50, 0, 100).ShouldBeTrue();
        Render(engine);

        scroller.LayoutBox!.ScrollTop.ShouldBe(100);
        // 布局坐标不含滚动：可视区顶端在内容中的位置 = 100，屏幕上即滚动容器顶端。
        Top(header).ShouldBe(100);
        // 偏移不影响常规流：后继兄弟保持原位。
        Top(content).ShouldBe(60);
    }

    [Fact]
    public void Sticky_ScrolledBack_ReturnsToItsFlowPosition()
    {
        var (root, _, header, _, rules) = BuildList(new Style { Top = Length.Px(0) });
        var engine = Initialize(root, rules);
        engine.ScrollBy(100, 50, 0, 100);
        Render(engine);
        engine.ScrollBy(100, 50, 0, -70);
        Render(engine);

        // 滚动量 30：粘性视窗顶端 30 < 常规流位置 40，不粘。
        Top(header).ShouldBe(40);
    }

    [Fact]
    public void Sticky_TopInset_OffsetsTheStuckPosition()
    {
        var (root, _, header, _, rules) = BuildList(new Style { Top = Length.Px(15) });
        var engine = Initialize(root, rules);
        engine.ScrollBy(100, 50, 0, 100);
        Render(engine);

        Top(header).ShouldBe(115);
    }

    [Fact]
    public void Sticky_PercentInset_ResolvesAgainstTheScrollport()
    {
        // 10% 相对滚动容器可视区高度（100）= 10px。
        var (root, _, header, _, rules) = BuildList(new Style { Top = Length.Percent(10) });
        var engine = Initialize(root, rules);
        engine.ScrollBy(100, 50, 0, 100);
        Render(engine);

        Top(header).ShouldBe(110);
    }

    [Fact]
    public void Sticky_WithoutInsets_BehavesLikeStatic()
    {
        var (root, _, header, _, rules) = BuildList(new Style());
        var engine = Initialize(root, rules);
        engine.ScrollBy(100, 50, 0, 100);
        Render(engine);

        Top(header).ShouldBe(40);
    }

    [Fact]
    public void Sticky_Bottom_SticksToTheScrollportBottom()
    {
        // 常规流位置 140..160 在可视区（0..100）之下：bottom:0 把它拉到可视区底边。
        var (root, _, header, content, rules) = BuildList(new Style { Bottom = Length.Px(0) });
        rules[2] = Rule("spacer", new Style { Height = Length.Px(140) });
        var engine = Initialize(root, rules);
        Render(engine);

        Top(header).ShouldBe(80);
        Top(content).ShouldBe(160);

        // 滚到它的常规流位置进入可视区后，不再上移。
        engine.ScrollBy(100, 50, 0, 100);
        Render(engine);
        Top(header).ShouldBe(140);
    }

    [Fact]
    public void Sticky_Left_SticksHorizontally()
    {
        var sticky = new DivElement { Class = "sticky" };
        var scroller = new DivElement { Class = "scroller" };
        scroller.AddChild(new DivElement { Class = "spacer" });
        scroller.AddChild(sticky);
        scroller.AddChild(new DivElement { Class = "wide" });
        var root = new DivElement { Class = "root" };
        root.AddChild(scroller);

        var engine = Initialize(root,
            Rule("root", new Style { Width = Length.Px(W), Height = Length.Px(H) }),
            Rule("scroller", new Style
            {
                Display = Display.Flex,
                Width = Length.Px(100),
                Height = Length.Px(50),
                OverflowX = Overflow.Auto,
            }),
            Rule("spacer", new Style { Width = Length.Px(40), FlexShrink = 0 }),
            Rule("sticky", new Style { Position = Position.Sticky, Left = Length.Px(0), Width = Length.Px(20), FlexShrink = 0 }),
            Rule("wide", new Style { Width = Length.Px(400), FlexShrink = 0 }));
        engine.ScrollBy(50, 25, 100, 0).ShouldBeTrue();
        Render(engine);

        scroller.LayoutBox!.ScrollLeft.ShouldBe(100);
        Left(sticky).ShouldBe(100);
    }

    // ---------- 包含块约束 ----------

    [Fact]
    public void Sticky_IsPushedOutByTheEndOfItsContainingBlock()
    {
        // header 的包含块是 section（40..190）：margin box 不得越出 section 底边。
        var header = new DivElement { Class = "header" };
        var section = new DivElement { Class = "section" };
        section.AddChild(header);
        section.AddChild(new DivElement { Class = "body" });
        var scroller = new DivElement { Class = "scroller" };
        scroller.AddChild(new DivElement { Class = "spacer" });
        scroller.AddChild(section);
        scroller.AddChild(new DivElement { Class = "tail" });
        var root = new DivElement { Class = "root" };
        root.AddChild(scroller);

        var engine = Initialize(root,
            Rule("root", new Style { Width = Length.Px(W), Height = Length.Px(H) }),
            Rule("scroller", new Style { Width = Length.Px(W), Height = Length.Px(100), OverflowY = Overflow.Auto }),
            Rule("spacer", new Style { Height = Length.Px(40) }),
            Rule("header", new Style { Position = Position.Sticky, Top = Length.Px(0), Height = Length.Px(20) }),
            Rule("body", new Style { Height = Length.Px(130) }),
            Rule("tail", new Style { Height = Length.Px(400) }));

        engine.ScrollBy(100, 50, 0, 100);
        Render(engine);
        Top(header).ShouldBe(100);

        // 滚动 180：要粘到 180，但 section 底边 190 只容得下 170..190。
        engine.ScrollBy(100, 50, 0, 80);
        Render(engine);
        Top(header).ShouldBe(170);
    }

    [Fact]
    public void Sticky_WhoseContainingBlockIsTheScroller_StaysStuckThroughTheWholeScrollRange()
    {
        // 包含块就是滚动容器本身：按其可滚动区域（而不是一屏高的内容盒）约束，
        // 否则滚过一屏后标题就会被推走。
        var (root, scroller, header, _, rules) = BuildList(new Style { Top = Length.Px(0) });
        var engine = Initialize(root, rules);
        engine.ScrollBy(100, 50, 0, 350);
        Render(engine);

        scroller.LayoutBox!.ScrollTop.ShouldBe(350);
        Top(header).ShouldBe(350);
    }

    [Fact]
    public void Sticky_WithoutAScrollContainer_IsConstrainedToTheViewport()
    {
        // 视口不滚动：top:10 把位于 y=0 的盒子推到 10（包含块 root 足够高），后继兄弟仍按常规流排布。
        var sticky = new DivElement { Class = "sticky" };
        var next = new DivElement { Class = "next" };
        var root = new DivElement { Class = "root" };
        root.AddChild(sticky);
        root.AddChild(next);

        var engine = Initialize(root,
            Rule("root", new Style { Width = Length.Px(W), Height = Length.Px(H) }),
            Rule("sticky", new Style { Position = Position.Sticky, Top = Length.Px(10), Height = Length.Px(20) }),
            Rule("next", new Style { Height = Length.Px(20) }));
        Render(engine);

        Top(sticky).ShouldBe(10);
        Top(next).ShouldBe(20);
    }

    [Fact]
    public void Sticky_BelowItsInset_IsNotOffsetLikeRelative()
    {
        // top 是粘性阈值而非 relative 偏移：盒子已在阈值之下时原地不动。
        var sticky = new DivElement { Class = "sticky" };
        var root = new DivElement { Class = "root" };
        root.AddChild(new DivElement { Class = "spacer" });
        root.AddChild(sticky);

        var layout = new LayoutEngine().Layout(root, [new StyleSheet { Rules =
        [
            Rule("spacer", new Style { Height = Length.Px(50) }),
            Rule("sticky", new Style { Position = Position.Sticky, Top = Length.Px(30), Height = Length.Px(20) }),
        ] }], W, H);

        layout.Children[1].BoxModel.BorderBox.Top.ShouldBe(50);
    }

    [Fact]
    public void Sticky_UsesTheNearestScrollContainer()
    {
        // 外层滚动不影响内层滚动容器里的粘性盒：它的可视区是内层的。
        var header = new DivElement { Class = "header" };
        var inner = new DivElement { Class = "inner" };
        inner.AddChild(new DivElement { Class = "spacer" });
        inner.AddChild(header);
        inner.AddChild(new DivElement { Class = "content" });
        var outer = new DivElement { Class = "outer" };
        outer.AddChild(inner);
        outer.AddChild(new DivElement { Class = "content" });
        var root = new DivElement { Class = "root" };
        root.AddChild(outer);

        var engine = Initialize(root,
            Rule("root", new Style { Width = Length.Px(W), Height = Length.Px(H) }),
            Rule("outer", new Style { Height = Length.Px(150), OverflowY = Overflow.Auto }),
            Rule("inner", new Style { Height = Length.Px(100), OverflowY = Overflow.Auto }),
            Rule("spacer", new Style { Height = Length.Px(40) }),
            Rule("header", new Style { Position = Position.Sticky, Top = Length.Px(0), Height = Length.Px(20) }),
            Rule("content", new Style { Height = Length.Px(400) }));
        Render(engine);

        outer.LayoutBox!.ScrollTop = 80;
        Render(engine);
        Top(header).ShouldBe(40);

        inner.LayoutBox!.ScrollTop = 60;
        Render(engine);
        Top(header).ShouldBe(60);
    }

    [Fact]
    public void Sticky_AbsoluteDescendant_MovesWithTheStuckBox()
    {
        // 粘性盒是定位元素：它是绝对定位后代的包含块，粘住时整棵子树随之平移。
        var badge = new DivElement { Class = "badge" };
        var (root, _, header, _, rules) = BuildList(new Style { Top = Length.Px(0) });
        header.AddChild(badge);
        var engine = Initialize(root,
        [
            .. rules,
            Rule("badge", new Style
            {
                Position = Position.Absolute, Top = Length.Px(5), Left = Length.Px(5),
                Width = Length.Px(10), Height = Length.Px(10),
            }),
        ]);
        Render(engine);
        Top(badge).ShouldBe(45);

        engine.ScrollBy(100, 50, 0, 100);
        Render(engine);
        Top(badge).ShouldBe(105);
    }

    // ---------- 绘制与命中 ----------

    [Fact]
    public void Sticky_StuckHeader_PaintsAboveTheContentScrollingUnderIt()
    {
        // 文档序里 content 在 header 之后；粘住时 header 必须盖在滚上来的 content 之上。
        var (root, _, _, _, rules) = BuildList(new Style { Top = Length.Px(0) });
        var engine = Initialize(root, rules);
        engine.ScrollBy(100, 50, 0, 100);
        Render(engine);

        IsBlueAt(100, 10).ShouldBeTrue("the stuck header paints over the following sibling");
        IsBlueAt(100, 50).ShouldBeFalse("below the header the content shows");
    }

    [Fact]
    public void Sticky_StuckHeader_ReceivesTheHit()
    {
        var (root, _, header, _, rules) = BuildList(new Style { Top = Length.Px(0) });
        var engine = Initialize(root, rules);
        engine.ScrollBy(100, 50, 0, 100);
        Render(engine);

        engine.HitTest(100, 10).ShouldBeSameAs(header);
    }

    [Fact]
    public void Sticky_HitTest_FollowsAScrollThatHasNotBeenPaintedYet()
    {
        var (root, _, header, content, rules) = BuildList(new Style { Top = Length.Px(0) });
        var engine = Initialize(root, rules);
        Render(engine);
        engine.HitTest(100, 10).ShouldNotBeSameAs(header);

        // 滚动后尚未出帧：命中测试同样要按当前滚动量就位，而不是对着上一帧的位置测。
        engine.ScrollBy(100, 50, 0, 100);
        engine.HitTest(100, 10).ShouldBeSameAs(header);
        engine.HitTest(100, 50).ShouldBeSameAs(content);
    }
}
