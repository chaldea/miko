using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Routing;
using Shouldly;

namespace Miko.Tests.Routing;

/// <summary>
/// A layout that re-renders itself — after its own event handler, e.g. a language switch
/// (ISSUE-157) — keeps its page. The layout used to let go of <c>Body</c> after the routed build
/// (the ISSUE-142 leak fix), so <c>@Body</c> rendered nothing and the page vanished.
/// </summary>
public class LayoutRerenderTests
{
    private static ToggleLayout? LastLayout;

    /// <summary>A page that renders the layout's current "mode" and its own click count.</summary>
    private sealed class ModePage : ComponentBase
    {
        internal static ModePage? Last;
        internal static string Mode = "a";
        private int _clicks;

        public ModePage() => Last = this;

        internal void Bump()
        {
            _clicks++;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var root = builder.OpenElement<DivElement>();
            root.Class = "page";
            var text = builder.OpenElement<SpanElement>();
            text.Class = $"mode-{Mode}-clicks-{_clicks}";
            builder.AddContent("x");
            builder.CloseElement();
            builder.CloseElement();
        }
    }

    /// <summary><c>&lt;div class="root"&gt;&lt;span class="layout-{mode}"/&gt;@Body&lt;/div&gt;</c>.</summary>
    private sealed class ToggleLayout : LayoutComponentBase
    {
        public ToggleLayout() => LastLayout = this;

        internal void Toggle()
        {
            ModePage.Mode = ModePage.Mode == "a" ? "b" : "a";
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var host = builder.OpenElement<DivElement>();
            host.Class = "root";
            var marker = builder.OpenElement<SpanElement>();
            marker.Class = $"layout-{ModePage.Mode}";
            builder.CloseElement();
            Body?.Invoke(builder);
            builder.CloseElement();
        }
    }

    private static (Element Root, ToggleLayout Layout, ModePage Page) Render()
    {
        ModePage.Mode = "a";
        var router = new Router();
        router.MapRoute("/", typeof(ModePage));
        var services = new ServiceCollection().BuildServiceProvider();
        var root = new RouteView(router, new NavigationManager(), typeof(ToggleLayout), services).Render("/");
        var result = (root, LastLayout!, ModePage.Last!);
        LastLayout = null;
        ModePage.Last = null;
        return result;
    }

    private static IEnumerable<Element> Descendants(Element root)
    {
        yield return root;
        foreach (var child in root.Children)
            foreach (var descendant in Descendants(child))
                yield return descendant;
    }

    private static bool Has(Element root, string cls) => Descendants(root).Any(e => e.Class == cls);

    [Fact]
    public void Should_Keep_The_Page_When_The_Layout_Rerenders_Itself()
    {
        var (root, layout, _) = Render();

        layout.Toggle();

        Has(root, "layout-b").ShouldBeTrue();
        Has(root, "page").ShouldBeTrue("the page must still be inside the layout");
    }

    [Fact]
    public void Should_Rerender_The_Page_With_The_Layout()
    {
        // What changed for the layout (here the mode; in an app, the current culture) reaches the page.
        var (root, layout, _) = Render();

        layout.Toggle();

        Has(root, "mode-b-clicks-0").ShouldBeTrue();
        Has(root, "mode-a-clicks-0").ShouldBeFalse();
    }

    [Fact]
    public void Should_Keep_The_Page_Instance_And_Its_State()
    {
        var (root, layout, page) = Render();
        page.Bump();
        page.Bump();

        layout.Toggle();
        page.Bump();

        Has(root, "mode-b-clicks-3").ShouldBeTrue("the same page instance renders, its state intact");
        Descendants(root).Count(e => e.Class == "page").ShouldBe(1);
    }

    [Fact]
    public void Should_Not_Retain_Replaced_Page_Generations_Across_Layout_Rerenders()
    {
        var generations = new List<WeakReference>();

        int alive = RunSession(20, generations);

        alive.ShouldBeLessThanOrEqualTo(1, $"{alive}/{generations.Count} page roots survived a forced collection");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RunSession(int toggles, List<WeakReference> generations)
    {
        var (root, layout, page) = Render();
        for (int i = 0; i < toggles; i++)
        {
            layout.Toggle();
            page.Bump();
            generations.Add(new WeakReference(PageRoot(root)));
        }

        for (int i = 0; i < 3; i++)
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        int alive = generations.Count(r => r.IsAlive);

        GC.KeepAlive(root);
        GC.KeepAlive(layout);
        GC.KeepAlive(page);
        return alive;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Element PageRoot(Element root) => Descendants(root).First(e => e.Class == "page");
}
