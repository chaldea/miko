using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Routing;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Routing;

public class NavigationStateRetentionTests
{
    private const int Width = 200;
    private const int Height = 100;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_Collect_Old_Pages_And_Layout_Boxes_While_History_Retains_Shared_State(bool animate)
    {
        using var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);
        var probe = new PageProbe();
        using var app = CreateApp(probe, canvas);
        var nav = app.Services.GetRequiredService<NavigationManager>();
        var discarded = new List<TrackedObject>();

        for (var i = 1; i <= 20; i++)
        {
            CaptureTree(app, discarded);
            NavigateFromPage(app, probe, canvas, $"/page/{i}", animate);
            if (animate)
                app.Engine.AdvanceNavigationTransition(1f);
        }

        Collect();

        AssertCollected(discarded);
        probe.Pages.Take(probe.Pages.Count - 1).ShouldAllBe(page => !page.IsAlive);
        probe.Layouts.Take(probe.Layouts.Count - 1).ShouldAllBe(layout => !layout.IsAlive);
        probe.Pages[^1].IsAlive.ShouldBeTrue();
        probe.Layouts[^1].IsAlive.ShouldBeTrue();
        probe.DisposedPages.ShouldBe(20);
        nav.History.Count.ShouldBe(21);
        nav.GetState<FormDraft>()!.SubscriberCount.ShouldBe(1);
        app.Engine.GetRoot()!.FindByClass("draft").Single().TextContent.ShouldBe("shared draft");
        GC.KeepAlive(app);
    }

    [Fact]
    public void Should_Collect_Leaving_Trees_When_Transitions_Are_Interrupted_And_Completed()
    {
        using var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);
        var probe = new PageProbe();
        using var app = CreateApp(probe, canvas);
        var first = new List<TrackedObject>();
        var second = new List<TrackedObject>();
        CaptureTree(app, first);

        NavigateFromPage(app, probe, canvas, "/page/1", animate: true);
        CaptureTree(app, second);
        Collect();
        first.ShouldAllBe(item => item.Reference.IsAlive);
        probe.DisposedPages.ShouldBe(0);

        NavigateFromPage(app, probe, canvas, "/page/2", animate: true);
        Collect();
        AssertCollected(first);
        second.ShouldAllBe(item => item.Reference.IsAlive);
        probe.DisposedPages.ShouldBe(1);

        app.Engine.AdvanceNavigationTransition(1f);
        Collect();
        AssertCollected(second);
        probe.DisposedPages.ShouldBe(2);
        app.Services.GetRequiredService<NavigationManager>().GetState<FormDraft>()!.SubscriberCount.ShouldBe(1);
        GC.KeepAlive(app);
    }

    [Fact]
    public void Should_Collect_Old_Render_Generations_While_Page_Layout_And_State_Stay_Alive()
    {
        using var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);
        var probe = new PageProbe();
        using var app = CreateApp(probe, canvas);
        NavigateFromPage(app, probe, canvas, "/page/1", animate: false);
        var discarded = new List<TrackedObject>();

        for (var i = 0; i < 30; i++)
        {
            // A root component refreshes its root element in place; its children and boxes change.
            CaptureTree(app, discarded, captureRootElement: false);
            RefreshPageAndLayout(app, probe, canvas);
        }

        Collect();

        AssertCollected(discarded);
        probe.Pages[^1].IsAlive.ShouldBeTrue();
        probe.Layouts[^1].IsAlive.ShouldBeTrue();
        app.Services.GetRequiredService<NavigationManager>().GetState<FormDraft>()!.SubscriberCount.ShouldBe(1);
        GC.KeepAlive(app);
    }

    [Theory]
    [InlineData(NavigationDirection.Back)]
    [InlineData(NavigationDirection.Root)]
    public void Should_Release_State_When_Its_History_Entry_Is_Removed(NavigationDirection direction)
    {
        var nav = new NavigationManager();
        var state = NavigateWithTemporaryState(nav);

        nav.NavigateTo("/", direction);
        Collect();

        state.IsAlive.ShouldBeFalse();
        GC.KeepAlive(nav);
    }

    [Theory]
    [InlineData(NavigationDirection.Back)]
    [InlineData(NavigationDirection.Root)]
    public void Should_Release_Removed_State_After_The_Controller_Rebuilds(NavigationDirection direction)
    {
        using var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);
        var probe = new PageProbe();
        using var app = CreateApp(probe, canvas);
        NavigateFromPage(app, probe, canvas, "/page/1", animate: false);
        var nav = app.Services.GetRequiredService<NavigationManager>();
        var state = CaptureState(nav);

        nav.NavigateTo("/", direction);
        app.Controller.Rebuild(canvas, Width, Height);
        Collect();

        state.IsAlive.ShouldBeFalse("Consumed navigation events and discarded pages must release the removed state.");
        GC.KeepAlive(app);
    }

    [Fact]
    public void Should_Release_Replaced_State_On_Back()
    {
        var nav = new NavigationManager();
        var state = NavigateWithTemporaryState(nav);
        nav.NavigateTo("/b");

        nav.NavigateTo("/a", state: null, direction: NavigationDirection.Back);
        Collect();

        state.IsAlive.ShouldBeFalse();
        GC.KeepAlive(nav);
    }

    [Fact]
    public void Should_Not_Retain_Removed_State_Through_A_Path_History_Enumerator()
    {
        var nav = new NavigationManager();
        var state = NavigateWithTemporaryState(nav);
        using var paths = nav.History.GetEnumerator();
        paths.MoveNext().ShouldBeTrue();
        paths.MoveNext().ShouldBeTrue();
        paths.Current.ShouldBe("/a");

        nav.NavigateTo("/b", NavigationDirection.Root);
        Collect();

        state.IsAlive.ShouldBeFalse("The public history enumerates paths, so its iterator must not retain state.");
        GC.KeepAlive(paths);
        GC.KeepAlive(nav);
    }

    private static MikoAppContext CreateApp(PageProbe probe, SKCanvas canvas)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.Services.AddSingleton(probe);
        builder.UseRouter(router =>
        {
            router.MapRoute<FormPage>("/");
            router.MapRoute<FormPage>("/page/{id:int}");
        });
        builder.UseDefaultLayout<FormLayout>();
        var app = builder.Build();
        app.Controller.Initialize(canvas, Width, Height);
        return app;
    }

    // Keep page/tree temporaries off the caller's stack during forced GC, including Debug builds.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void NavigateFromPage(MikoAppContext app, PageProbe probe, SKCanvas canvas, string path, bool animate)
    {
        ((FormPage)probe.Pages[^1].Target!).GoTo(path, animate ? new FadeTransition() : null);
        app.Controller.Rebuild(canvas, Width, Height);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RefreshPageAndLayout(MikoAppContext app, PageProbe probe, SKCanvas canvas)
    {
        app.Services.GetRequiredService<NavigationManager>().GetState<FormDraft>()!.NotifyChanged();
        ((FormLayout)probe.Layouts[^1].Target!).Refresh();
        app.Engine.Render(canvas);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference NavigateWithTemporaryState(NavigationManager nav)
    {
        var state = new FormDraft();
        nav.NavigateTo("/a", state);
        return new WeakReference(state);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CaptureState(NavigationManager nav) => new(nav.CurrentState!);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CaptureTree(MikoAppContext app, List<TrackedObject> objects, bool captureRootElement = true)
        => CaptureElement(app.Engine.GetRoot()!, objects, captureRootElement);

    private static void CaptureElement(Element element, List<TrackedObject> objects, bool captureElement = true)
    {
        if (captureElement)
            objects.Add(new TrackedObject("Element", new WeakReference(element)));
        if (element.LayoutBox is { } box)
            objects.Add(new TrackedObject("LayoutBox", new WeakReference(box)));
        foreach (var child in element.Children)
            CaptureElement(child, objects);
    }

    private static void Collect()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private static void AssertCollected(List<TrackedObject> objects)
        => objects.Where(item => item.Reference.IsAlive).Select(item => item.Kind).ShouldBeEmpty();

    private sealed record TrackedObject(string Kind, WeakReference Reference);

    private sealed class PageProbe
    {
        public List<WeakReference> Pages { get; } = new();
        public List<WeakReference> Layouts { get; } = new();
        public int DisposedPages { get; set; }
    }

    private sealed class FormDraft
    {
        public string Name { get; set; } = "shared draft";
        public event Action? Changed;
        public int SubscriberCount => Changed?.GetInvocationList().Length ?? 0;
        public void NotifyChanged() => Changed?.Invoke();
    }

    private sealed class FormPage : ComponentBase
    {
        private readonly PageProbe _probe;
        private FormDraft _draft = null!;

        public FormPage(PageProbe probe)
        {
            _probe = probe;
            probe.Pages.Add(new WeakReference(this));
        }

        [Parameter] public int Id { get; set; }

        protected override void OnInitialized()
        {
            _draft = NavigationManager!.GetState<FormDraft>() ?? new FormDraft();
            _draft.Changed += StateHasChanged;
        }

        public void GoTo(string path, NavigationTransition? transition)
            => NavigationManager!.NavigateTo(path, _draft, transition: transition);

        protected override void OnDispose()
        {
            _draft.Changed -= StateHasChanged;
            _probe.DisposedPages++;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>().Class = "draft";
            builder.AddContent(_draft.Name);
            builder.CloseElement();
        }
    }

    private sealed class FormLayout : LayoutComponentBase
    {
        public FormLayout(PageProbe probe) => probe.Layouts.Add(new WeakReference(this));
        public void Refresh() => StateHasChanged();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            Body?.Invoke(builder);
            builder.CloseElement();
        }
    }

    private sealed class FadeTransition : NavigationTransition
    {
        public override float Duration => 0.3f;
        public override void Apply(NavigationTransitionContext context, float progress)
            => context.EnteringOpacity = progress;
    }
}
