using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Events;

/// <summary>
/// 滚动事件的<b>向下</b>派发（ion-infinite-scroll 无效果的根因）。
///
/// <para>冒泡只让祖先感知事件，但关心滚动的组件常常位于滚动容器<b>内部</b>：
/// <c>ion-infinite-scroll</c> 是 <c>ion-content .inner-scroll</c> 的后代。DOM 里这类组件直接
/// 在滚动元素上注册监听器，Miko 的组件拿不到祖先引用，因此 <see cref="MikoEngine.ScrollBy"/>
/// 在目标+冒泡之后额外向滚动容器子树通知一次。</para>
///
/// <para>同时校验 <see cref="ScrollEventArgs"/> 携带的滚动几何量——阈值计算依赖它们。</para>
/// </summary>
public class ScrollDescendantDispatchTests
{
    private const float ViewportW = 600f;
    private const float ViewportH = 300f;

    /// <summary>可滚动容器（300px 视口）内含 <paramref name="rows"/> 个 50px 行。</summary>
    private static DivElement BuildScroller(int rows, out DivElement marker)
    {
        var scroller = new DivElement
        {
            Class = "inner-scroll",
            Style = new Style
            {
                Width = Length.Px(ViewportW),
                Height = Length.Px(ViewportH),
                OverflowY = Overflow.Auto,
            }
        };

        for (int i = 0; i < rows; i++)
        {
            scroller.AddChild(new DivElement
            {
                Style = new Style { Height = Length.Px(50) },
                TextContent = $"Item {i}"
            });
        }

        // 列表末尾的哨兵，扮演 ion-infinite-scroll 的角色。
        marker = new DivElement
        {
            Class = "marker",
            Style = new Style { Height = Length.Px(50) }
        };
        scroller.AddChild(marker);

        return scroller;
    }

    private static MikoEngine InitEngine(Element root, SKCanvas canvas)
    {
        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet>(), canvas, ViewportW, ViewportH);
        return engine;
    }

    [Fact]
    public void ScrollBy_ShouldNotifyDescendantsOfTheScrollContainer()
    {
        // 这正是 issue 报告的场景：监听者是滚动容器的后代，不在冒泡链上。
        var root = BuildScroller(20, out var marker);
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));
        var engine = InitEngine(root, surface.Canvas);

        var received = 0;
        marker.OnScroll = _ => received++;

        engine.ScrollBy(300, 150, 0, 200).ShouldBeTrue();

        received.ShouldBe(1, "a descendant of the scroll container must receive the scroll event");
    }

    [Fact]
    public void ScrollBy_ShouldStillNotifyTheScrollContainerItself()
    {
        // 向下派发不能顶掉原有的目标阶段。
        var root = BuildScroller(20, out _);
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));
        var engine = InitEngine(root, surface.Canvas);

        var received = 0;
        root.OnScroll = _ => received++;

        engine.ScrollBy(300, 150, 0, 200);

        received.ShouldBe(1);
    }

    [Fact]
    public void ScrollEventArgs_ShouldCarryScrollGeometry()
    {
        // 20 行 * 50px + 50px 哨兵 = 1050px 内容，视口 300px。
        var root = BuildScroller(20, out var marker);
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));
        var engine = InitEngine(root, surface.Canvas);

        ScrollEventArgs? captured = null;
        marker.OnScroll = args => captured = args;

        engine.ScrollBy(300, 150, 0, 200);

        captured.ShouldNotBeNull();
        captured!.ScrollTop.ShouldBe(200f);
        captured.ClientHeight.ShouldBe(ViewportH);
        captured.ScrollHeight.ShouldBe(1050f);
    }

    [Fact]
    public void ScrollBy_ShouldNotNotifyInsideNestedScrollContainer()
    {
        // 内层滚动容器有自己的滚动位置，外层的滚动量对它的后代没有意义。
        var root = BuildScroller(20, out _);

        var innerScroller = new DivElement
        {
            Class = "nested-scroller",
            Style = new Style
            {
                Width = Length.Px(200),
                Height = Length.Px(100),
                OverflowY = Overflow.Auto,
            }
        };
        var innerChild = new DivElement { Style = new Style { Height = Length.Px(400) } };
        innerScroller.AddChild(innerChild);
        root.AddChild(innerScroller);

        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));
        var engine = InitEngine(root, surface.Canvas);

        var innerReceived = 0;
        innerChild.OnScroll = _ => innerReceived++;

        // 命中外层容器的空白处（y=10 落在第一行，属于外层）。
        engine.ScrollBy(300, 10, 0, 200);

        innerReceived.ShouldBe(0, "an outer scroll must not reach into a nested scroll container");
    }

    [Fact]
    public void ScrollBy_StopPropagationHaltsDescendantDispatch()
    {
        var root = BuildScroller(20, out var marker);

        // marker 之后再挂一个兄弟监听者，用于验证停止传播确实阻断了后续派发。
        var later = new DivElement
        {
            Class = "later",
            Style = new Style { Height = Length.Px(50) }
        };
        root.AddChild(later);

        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));
        var engine = InitEngine(root, surface.Canvas);

        var laterReceived = 0;
        marker.OnScroll = args => args.StopPropagation();
        later.OnScroll = _ => laterReceived++;

        engine.ScrollBy(300, 150, 0, 200);

        laterReceived.ShouldBe(0);
    }
}
