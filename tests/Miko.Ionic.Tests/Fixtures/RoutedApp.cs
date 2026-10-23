using Miko.Core;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Ionic.Tests.Fixtures.Routed;
using Miko.Layout;
using Miko.Platform;
using Miko.Routing;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Fixtures;

/// <summary>
/// A real app for end-to-end component tests: <see cref="MikoAppBuilder"/>, <c>AddIonic()</c>, the
/// templates' <c>&lt;IonApp&gt;@Body&lt;/IonApp&gt;</c> layout, routed pages, and taps that enter
/// through <see cref="MikoInteractionController"/> exactly as a platform host forwards them.
/// </summary>
internal sealed class RoutedApp : IDisposable
{
    public const int Width = 390;
    public const int Height = 844;

    private readonly SKBitmap _bitmap = new(Width, Height);
    private readonly SKCanvas _canvas;

    public RoutedApp(params (string Path, Type Page)[] routes)
        : this(configure: null, routes)
    {
    }

    public RoutedApp(Action<MikoAppBuilder>? configure, params (string Path, Type Page)[] routes)
    {
        _canvas = new SKCanvas(_bitmap);
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseDefaultLayout<IonAppLayoutFixture>();
        builder.UseRouter(router =>
        {
            foreach (var (path, page) in routes)
                router.MapRoute(path, page);
        });
        configure?.Invoke(builder);
        App = builder.Build();
        App.Controller.Initialize(_canvas, Width, Height);
        Render();

        Navigation = (NavigationManager)App.Services.GetService(typeof(NavigationManager))!;
        Navigation.LocationChanged += e => LastNavigation = e;
    }

    public MikoAppContext App { get; }

    public NavigationManager Navigation { get; }

    /// <summary>The last navigation's arguments (direction, transition), or null.</summary>
    public NavigationEventArgs? LastNavigation { get; private set; }

    public Element Root => App.Engine.GetRoot()!;

    /// <summary>One host frame: drains the dispatcher, re-renders and lays out.</summary>
    public void Render() => App.Controller.RenderFrame(_canvas, Width, Height, 1f / 60f, c => App.Engine.Render(c));

    /// <summary>The single element in the current tree carrying <paramref name="className"/>.</summary>
    public Element Single(string className) => Root.FindByClass(className).ShouldHaveSingleItem();

    /// <summary>
    /// Taps the center of the element carrying <paramref name="className"/> with a pointer down/up
    /// pair — hit testing, bubbling and render batching all as on a device.
    /// </summary>
    public void Tap(string className) => Tap(Single(className));

    /// <summary>Taps the center of <paramref name="element"/> (see <see cref="Tap(string)"/>).</summary>
    public void Tap(Element element)
    {
        var (x, y) = CenterOf(element);
        var hit = App.Engine.HitTest(x, y).ShouldNotBeNull();
        IsInside(hit, element).ShouldBeTrue($"a tap at the center of <{element.TagName} class=\"{element.Class}\"> must hit it, but hit <{hit.TagName} class=\"{hit.Class}\">");
        App.Controller.OnPointerDown(x, y, MouseButton.Left, PointerType.Touch);
        App.Controller.OnPointerUp(x, y, MouseButton.Left, PointerType.Touch);
    }

    /// <summary>The element's border box in the last layout.</summary>
    public Miko.Common.RectF BorderBoxOf(Element element) => BoxOf(element).BoxModel.BorderBox;

    /// <summary>The element's computed style in the last layout.</summary>
    public Miko.Styling.ComputedStyle ComputedStyleOf(Element element) => BoxOf(element).ComputedStyle;

    private LayoutBox BoxOf(Element element)
        => FindBox(App.Engine.GetCurrentLayout().ShouldNotBeNull(), element)
           ?? throw new ShouldAssertException($"<{element.TagName} class=\"{element.Class}\"> has no layout box");

    private (float X, float Y) CenterOf(Element element)
    {
        var border = BorderBoxOf(element);
        return (border.X + border.Width / 2, border.Y + border.Height / 2);
    }

    private static LayoutBox? FindBox(LayoutBox box, Element element)
    {
        if (ReferenceEquals(box.Element, element)) return box;
        foreach (var child in box.Children)
            if (FindBox(child, element) is { } found) return found;
        return null;
    }

    private static bool IsInside(Element element, Element ancestor)
    {
        for (var node = element; node is not null; node = node.Parent)
            if (ReferenceEquals(node, ancestor)) return true;
        return false;
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }
}
