using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// End-to-end layout and interaction tests for the FAB family (issues/ion-fab.md), driven through a
/// full app context so the fabs sit in an <c>ion-content</c> fixed slot exactly like the demo page.
/// <para>Reported defects, all reproduced here:</para>
/// <list type="number">
/// <item><c>horizontal="end" vertical="top" edge</c> rendered wholly inside the content — the
/// <c>margin-top: -50%</c> that lifts the button half onto the header was missing.</item>
/// <item><c>vertical="center"</c> and <c>horizontal="center"</c> fabs rendered in the top-left
/// corner: with <c>width:auto</c> their pinned opposite insets stretched the host across the entire
/// content area, so the button sat at that stretched box's origin.</item>
/// <item>Tapping a fab button did nothing. Same root cause — the stretched, <c>z-index:1000</c>
/// hosts covered the whole viewport and swallowed every pointer event before it reached the fab the
/// user aimed at.</item>
/// </list>
/// Unit-level class/DOM contracts live in <see cref="IonFabTests"/>,
/// <see cref="IonFabButtonTests"/> and <see cref="IonFabListTests"/>; this file is about the pixels
/// and the pointer.
/// </summary>
public class IonFabLayoutTests : IDisposable
{
    private const float W = 390;
    private const float H = 844;
    private const float FabSize = 56;       // $fab-size
    private const float FabSmallSize = 40;  // $fab-small-size
    private const float ContentMargin = 10; // $fab-content-margin

    private readonly SKBitmap _bitmap = new((int)W, (int)H);
    private readonly SKCanvas _canvas;

    public IonFabLayoutTests()
    {
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private MikoAppContext BuildApp(Type page)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRouter(router => router.MapRoute("/", page));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        return app;
    }

    // The engine's layout boxes are internal; the painted rect of an element is reachable through
    // the public hit test, so assert geometry by probing points instead.
    private static bool HitsClass(MikoAppContext app, float x, float y, string cls)
    {
        var hit = app.Engine.HitTest(x, y);
        while (hit != null)
        {
            if (hit.HasClass(cls)) return true;
            hit = hit.Parent;
        }
        return false;
    }

    private static Element FindFabHost(Element root, string horizontal, string vertical)
        => root.FindByClass("ion-fab")
            .Single(f => f.HasClass($"fab-horizontal-{horizontal}") && f.HasClass($"fab-vertical-{vertical}"));

    // ---- positioning ----

    [Fact]
    public void EdgeTopFab_HangsHalfOutsideTheContent()
    {
        // vertical="top" edge: the button's top half sits above the content's top edge, so its
        // centre line lands ON y=0 — the point just below is inside it, the point at half the
        // button's height below is already past it.
        var app = BuildApp(typeof(DemoFabPage));

        float cx = W - ContentMargin - FabSize / 2f;
        HitsClass(app, cx, 2, "ion-fab-button").ShouldBeTrue("the lower half of the edge fab is visible");
        HitsClass(app, cx, FabSize / 2f + 2, "ion-fab-button")
            .ShouldBeFalse("only half the edge fab is inside the content");
    }

    [Fact]
    public void EdgeTopFab_WithoutEdge_SitsWhollyInsideTheContent()
    {
        // Without `edge` the same fab is pinned by the content margin and fully visible, which is
        // what the edge variant above must NOT look like.
        var app = BuildApp(typeof(NonEdgeFabPage));

        float cx = W - ContentMargin - FabSize / 2f;
        HitsClass(app, cx, ContentMargin + 2, "ion-fab-button").ShouldBeTrue();
        HitsClass(app, cx, ContentMargin + FabSize - 2, "ion-fab-button").ShouldBeTrue();
    }

    [Fact]
    public void VerticalCenterFab_SitsOnTheVerticalCentreLine()
    {
        var app = BuildApp(typeof(DemoFabPage));

        float cx = W - ContentMargin - FabSize / 2f;
        HitsClass(app, cx, H / 2f, "ion-fab-button").ShouldBeTrue("vertically centred");
        HitsClass(app, cx, 100, "ion-fab-button").ShouldBeFalse("not stretched up the whole side");
    }

    [Fact]
    public void FullyCentredFab_SitsAtTheCentreOfTheContent()
    {
        var app = BuildApp(typeof(DemoFabPage));

        HitsClass(app, W / 2f, H / 2f, "ion-fab-button").ShouldBeTrue("centred on both axes");
        // The corner the bug parked it in is now empty of fab buttons.
        HitsClass(app, 4, 4, "ion-fab-button").ShouldBeFalse();
    }

    [Fact]
    public void CentredFabHost_DoesNotCoverTheWholeContent()
    {
        // The heart of the reported click failure: a fab host at z-index 1000 that spans the
        // viewport eats every tap. Probe a spot far from all three fabs.
        var app = BuildApp(typeof(DemoFabPage));

        HitsClass(app, 40, 700, "ion-fab").ShouldBeFalse();
    }

    [Fact]
    public void EachFab_IsIndependentlyHittable()
    {
        // With the hosts shrink-wrapped, all three fabs are reachable — under the bug the last
        // full-size host covered the other two.
        var app = BuildApp(typeof(DemoFabPage));

        float rightX = W - ContentMargin - FabSize / 2f;
        HitsClass(app, rightX, 2, "ion-fab-button").ShouldBeTrue("top-right edge fab");
        HitsClass(app, rightX, H / 2f, "ion-fab-button").ShouldBeTrue("right-centre fab");
        HitsClass(app, W / 2f, H / 2f, "ion-fab-button").ShouldBeTrue("fully centred fab");
    }

    // ---- click → list reveal ----

    [Fact]
    public void ClickingTheMainButton_OpensTheList()
    {
        var app = BuildApp(typeof(DemoFabPage));
        var root = app.Engine.GetRoot()!;

        var list = root.FindByClass("ion-fab-list").Single();
        list.ShouldNotHaveClass("fab-list-active");

        Click(app, W - ContentMargin - FabSize / 2f, 2);

        var opened = app.Engine.GetRoot()!.FindByClass("ion-fab-list").Single();
        opened.ShouldHaveClass("fab-list-active");
        // The main button swaps to its close icon, and the list's buttons are told to show.
        FindFabHost(app.Engine.GetRoot()!, "end", "top")
            .FindByClass("ion-fab-button")
            .First(b => !b.HasClass("fab-button-in-list"))
            .ShouldHaveClass("fab-button-close-active");
        opened.FindByClass("fab-button-in-list").Single().ShouldHaveClass("fab-button-show");
    }

    [Fact]
    public void ClickingTheMainButtonAgain_ClosesTheList()
    {
        var app = BuildApp(typeof(DemoFabPage));

        float cx = W - ContentMargin - FabSize / 2f;
        Click(app, cx, 2);
        Click(app, cx, 2);

        app.Engine.GetRoot()!.FindByClass("ion-fab-list").Single()
            .ShouldNotHaveClass("fab-list-active");
    }

    [Fact]
    public void OpenedList_LaysOutItsButtonsBelowTheMainButton()
    {
        // The list only gets a box once it is active (display:none until then), so this also proves
        // the toggle actually reaches layout rather than just flipping a class.
        var app = BuildApp(typeof(DemoFabPage));

        float cx = W - ContentMargin - FabSize / 2f;
        Click(app, cx, 2);
        app.Engine.Render(_canvas);

        // The main button's bottom edge is at FabSize/2 (it hangs half above the content); the list
        // starts one list-margin below that, and its 40px button is centred on the same column.
        HitsClass(app, cx, FabSize / 2f + 10 + FabSmallSize / 2f, "fab-button-in-list")
            .ShouldBeTrue("the list button is laid out just under the main button");
    }

    [Fact]
    public void ClosedList_HasNoHittableButtons()
    {
        var app = BuildApp(typeof(DemoFabPage));

        float cx = W - ContentMargin - FabSize / 2f;
        HitsClass(app, cx, FabSize / 2f + 10 + FabSmallSize / 2f, "fab-button-in-list")
            .ShouldBeFalse("a closed list is display:none");
    }

    /// <summary>Taps a point, then renders a frame so the subtree the click rebuilt is laid out
    /// before the next probe (this is what the platform host's render loop does each frame).</summary>
    private void Click(MikoAppContext app, float x, float y)
    {
        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);
        app.Engine.Render(_canvas);
    }

    // ---- pages ----

    /// <summary>The three fabs from the reported demo: the edge one (with a list), the
    /// right-centre one, and the fully centred one.</summary>
    private sealed class DemoFabPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => FabPageBuilder.Build(builder, edge: true, includeCentred: true);
    }

    /// <summary>The same top-right fab without <c>edge</c>, as the contrast case.</summary>
    private sealed class NonEdgeFabPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => FabPageBuilder.Build(builder, edge: false, includeCentred: false);
    }

    private static class FabPageBuilder
    {
        public static void Build(RenderTreeBuilder builder, bool edge, bool includeCentred)
        {
            builder.OpenComponent<IonPage>(0);
            builder.AddComponentParameter(1, nameof(IonPage.ChildContent), (RenderFragment)(page =>
            {
                page.OpenComponent<IonContent>(0);
                page.AddComponentParameter(1, nameof(IonContent.Fixed), (RenderFragment)(slot =>
                {
                    // end / top (+ edge), carrying a bottom fab-list.
                    slot.OpenComponent<IonFab>(0);
                    slot.AddComponentParameter(1, nameof(IonFab.Horizontal), "end");
                    slot.AddComponentParameter(2, nameof(IonFab.Vertical), "top");
                    slot.AddComponentParameter(3, nameof(IonFab.Edge), edge);
                    slot.AddComponentParameter(4, nameof(IonFab.ChildContent), (RenderFragment)(fab =>
                    {
                        fab.OpenComponent<IonFabButton>(0);
                        fab.CloseComponent();
                        fab.OpenComponent<IonFabList>(1);
                        fab.AddComponentParameter(2, nameof(IonFabList.ChildContent), (RenderFragment)(l =>
                        {
                            l.OpenComponent<IonFabButton>(0);
                            l.CloseComponent();
                        }));
                        fab.CloseComponent();
                    }));
                    slot.CloseComponent();

                    if (!includeCentred) return;

                    // end / center
                    slot.OpenComponent<IonFab>(10);
                    slot.AddComponentParameter(11, nameof(IonFab.Horizontal), "end");
                    slot.AddComponentParameter(12, nameof(IonFab.Vertical), "center");
                    slot.AddComponentParameter(13, nameof(IonFab.ChildContent), (RenderFragment)(fab =>
                    {
                        fab.OpenComponent<IonFabButton>(0);
                        fab.CloseComponent();
                    }));
                    slot.CloseComponent();

                    // center / center
                    slot.OpenComponent<IonFab>(20);
                    slot.AddComponentParameter(21, nameof(IonFab.Horizontal), "center");
                    slot.AddComponentParameter(22, nameof(IonFab.Vertical), "center");
                    slot.AddComponentParameter(23, nameof(IonFab.ChildContent), (RenderFragment)(fab =>
                    {
                        fab.OpenComponent<IonFabButton>(0);
                        fab.CloseComponent();
                    }));
                    slot.CloseComponent();
                }));
                page.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
