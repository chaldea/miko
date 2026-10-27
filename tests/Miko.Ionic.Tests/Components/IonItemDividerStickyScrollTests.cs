using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Core;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Styling;
using Miko.Testing;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// <c>IonItemDivider Sticky</c>（ISSUE-159）在真实页面结构里滚动：
/// <code>
/// IonPage > IonContent (.inner-scroll 是滚动容器)
///   └ IonList
///       ├ IonItemGroup ── IonItemDivider "A" (sticky) + IonItem × 15
///       └ IonItemGroup ── IonItemDivider "B" (sticky) + IonItem × 15
/// </code>
/// 分组标题在本组滚过时粘在可视区顶端；本组滚完时被组的底边推走，由下一组的标题接替——
/// 这依赖包含块（ion-item-group）约束，所以必须用真实的分组嵌套。
/// </summary>
public class IonItemDividerStickyScrollTests : IDisposable
{
    private const float ViewportW = 390f;
    private const float ViewportH = 600f;
    private const int RowsPerGroup = 15;

    private readonly SKSurface _surface =
        SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

    public void Dispose() => _surface.Dispose();

    private sealed class GroupedListPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            var page = b.OpenComponent<IonPage>();
            page.ChildContent = (RenderFragment)(p =>
            {
                var content = p.OpenComponent<IonContent>();
                content.ChildContent = (RenderFragment)(c =>
                {
                    var list = c.OpenComponent<IonList>();
                    list.ChildContent = (RenderFragment)(l =>
                    {
                        Group(l, 100, "A", "divider-a");
                        Group(l, 200, "B", "divider-b");
                    });
                    c.CloseComponent();
                });
                p.CloseComponent();
            });
            b.CloseComponent();
        }

        private static void Group(RenderTreeBuilder l, int seq, string title, string dividerClass)
        {
            l.OpenComponent<IonItemGroup>(seq);
            l.AddComponentParameter(seq + 1, nameof(IonItemGroup.ChildContent), (RenderFragment)(g =>
            {
                g.OpenComponent<IonItemDivider>(seq + 2);
                g.AddComponentParameter(seq + 3, nameof(IonItemDivider.Sticky), true);
                g.AddComponentParameter(seq + 4, nameof(IonItemDivider.Class), dividerClass);
                g.AddComponentParameter(seq + 5, nameof(IonItemDivider.ChildContent), (RenderFragment)(d => d.AddContent(title)));
                g.CloseComponent();

                for (int i = 0; i < RowsPerGroup; i++)
                {
                    g.OpenComponent<IonItem>(seq + 10 + i * 2);
                    g.AddComponentParameter(seq + 11 + i * 2, nameof(IonItem.ChildContent), (RenderFragment)(item =>
                    {
                        var label = item.OpenComponent<IonLabel>();
                        label.ChildContent = (RenderFragment)(lab => lab.AddContent($"{title} row"));
                        item.CloseComponent();
                    }));
                    g.CloseComponent();
                }
            }));
            l.CloseComponent();
        }
    }

    private (MikoEngine Engine, Element Root) Initialize()
    {
        using var context = new TestContext { ViewportWidth = ViewportW, ViewportHeight = ViewportH };
        context.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(HostPlatform.Android));
        var root = context.Render<GroupedListPage>().Root;

        var engine = new MikoEngineBuilder().Build();
        engine.Initialize(root, [IonicStyleSheetFactory.CreateAllModes()], _surface.Canvas, ViewportW, ViewportH);
        engine.Render(_surface.Canvas);
        return (engine, root);
    }

    private static Element Find(Element root, string cls) => root.FindByClass(cls).Single();

    /// <summary>元素在屏幕上的顶边：布局位置减去所有祖先的滚动量。</summary>
    private static float ScreenTop(Element element)
    {
        float top = element.LayoutBox!.BoxModel.BorderBox.Top;
        for (var p = element.Parent; p != null; p = p.Parent)
            top -= p.LayoutBox?.ScrollTop ?? 0;
        return top;
    }

    private static float ScreenBottom(Element element) => ScreenTop(element) + element.LayoutBox!.BoxModel.BorderBox.Height;

    [Fact]
    public void StickyDivider_StaysAtTheTopWhileItsGroupScrollsBy()
    {
        var (engine, root) = Initialize();
        var scroller = Find(root, "inner-scroll");
        var dividerA = Find(root, "divider-a");
        float viewportTop = ScreenTop(scroller);
        // 静止时在常规流位置（ion-list 的上内边距之下）。
        float restTop = ScreenTop(dividerA);
        restTop.ShouldBeGreaterThan(viewportTop);

        engine.ScrollBy(ViewportW / 2, ViewportH / 2, 0, 120).ShouldBeTrue();
        engine.Render(_surface.Canvas);

        scroller.LayoutBox!.ScrollTop.ShouldBe(120, 0.5f);
        ScreenTop(dividerA).ShouldBe(viewportTop, 0.5f);
        // 粘住的标题盖在滚到它下面的行之上，也接收点击。
        var hit = engine.HitTest(ViewportW / 2, viewportTop + 5);
        hit.ShouldNotBeNull();
        IsInside(hit!, dividerA).ShouldBeTrue();
    }

    [Fact]
    public void StickyDivider_IsPushedOutByTheNextGroup()
    {
        var (engine, root) = Initialize();
        var scroller = Find(root, "inner-scroll");
        var dividerA = Find(root, "divider-a");
        var dividerB = Find(root, "divider-b");
        var groupA = dividerA.Parent!;
        float viewportTop = ScreenTop(scroller);

        // 滚到 B 组标题刚好越过可视区顶端。
        float distance = dividerB.LayoutBox!.BoxModel.BorderBox.Top - scroller.LayoutBox!.BoxModel.PaddingBox.Top + 10;
        engine.ScrollBy(ViewportW / 2, ViewportH / 2, 0, distance).ShouldBeTrue();
        engine.Render(_surface.Canvas);

        ScreenTop(dividerB).ShouldBe(viewportTop, 0.5f);
        // A 组已滚完：它的标题被组底边推出可视区，不再与 B 重叠。
        ScreenBottom(dividerA).ShouldBeLessThanOrEqualTo(ScreenBottom(groupA) + 0.5f);
        ScreenBottom(dividerA).ShouldBeLessThanOrEqualTo(viewportTop + 0.5f);
    }

    private static bool IsInside(Element element, Element ancestor)
    {
        for (var e = element; e != null; e = e.Parent)
            if (ReferenceEquals(e, ancestor)) return true;
        return false;
    }
}
