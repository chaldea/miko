using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Hosting;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Layout;
using Miko.Platform;
using Miko.Styling;
using Miko.Testing;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

public class IonSegmentScrollableTests : IDisposable
{
    private const float ViewportWidth = 300f;
    private const float ViewportHeight = 160f;
    private static readonly string[] Values =
        ["Call", "Favorite", "Map", "Watch", "Account", "Settings", "Profile"];

    private readonly SKSurface _surface =
        SKSurface.Create(new SKImageInfo((int)ViewportWidth, (int)ViewportHeight));

    public void Dispose() => _surface.Dispose();

    private (Element Root, MikoEngine Engine) BuildAndInitialize(HostPlatform platform = HostPlatform.Ios)
    {
        var engine = new MikoEngine();
        using var context = new TestContext
        {
            ViewportWidth = ViewportWidth,
            ViewportHeight = ViewportHeight,
        };
        context.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(platform));
        context.Services.AddSingleton(engine);
        var styles = IonicStyleSheetFactory.CreateAllModes();
        context.AddStyleSheet(styles);

        var cut = context.Render<IonSegment>(parameters =>
        {
            parameters.Add(nameof(IonSegment.Value), "Call");
            parameters.Add(nameof(IonSegment.Scrollable), true);
            parameters.AddChildContent(builder =>
            {
                var sequence = 0;
                foreach (var value in Values)
                {
                    var captured = value;
                    builder.OpenComponent<IonSegmentButton>(sequence++);
                    builder.AddComponentParameter(sequence++, nameof(IonSegmentButton.Value), captured);
                    builder.AddComponentParameter(sequence++, nameof(IonSegmentButton.ChildContent),
                        (RenderFragment)(content => content.AddContent(0, captured)));
                    builder.CloseComponent();
                }
            });
        });

        engine.Initialize(cut.Root, [styles], _surface.Canvas, ViewportWidth, ViewportHeight);
        return (cut.Root, engine);
    }

    [Theory]
    [InlineData(HostPlatform.Ios)]
    [InlineData(HostPlatform.Android)]
    public void Scrollable_ClickingWatch_CentersButtonInViewport(HostPlatform platform)
    {
        var (root, engine) = BuildAndInitialize(platform);
        var segment = engine.GetCurrentLayout().ShouldNotBeNull();

        root.ShouldHaveClass("segment-scrollable");
        segment.ComputedStyle.OverflowX.ShouldBe(Overflow.Auto);
        segment.ComputedStyle.ScrollbarWidth.ShouldBe(ScrollbarWidth.None);
        segment.ScrollableContentWidth.ShouldBeGreaterThan(segment.BoxModel.PaddingBox.Width);
        segment.ScrollLeft.ShouldBe(0f);

        var watch = root.FindByClass("ion-segment-button")[3];
        var watchBox = FindBox(segment, watch).ShouldNotBeNull();
        watch.FindByClass("button-native").Single().OnClick!
            .Invoke(new Miko.Events.MouseEventArgs { Target = watch });
        engine.Render(_surface.Canvas);

        segment = engine.GetCurrentLayout().ShouldNotBeNull();
        watch = segment.Element.FindByClass("ion-segment-button")[3];
        watchBox = FindBox(segment, watch).ShouldNotBeNull();

        segment.ScrollLeft.ShouldBeGreaterThan(0f);
        var visibleCenter = watchBox.BoxModel.BorderBox.Left
            + watchBox.BoxModel.BorderBox.Width / 2f
            - segment.ScrollLeft;
        var viewportCenter = segment.BoxModel.PaddingBox.Left
            + segment.BoxModel.PaddingBox.Width / 2f;
        visibleCenter.ShouldBe(viewportCenter, 0.01f);
    }

    [Fact]
    public void Scrollable_ClickingLastButton_StopsAtMaximumScroll()
    {
        var (root, engine) = BuildAndInitialize();
        var segment = engine.GetCurrentLayout().ShouldNotBeNull();
        var profile = root.FindByClass("ion-segment-button").Last();

        profile.FindByClass("button-native").Single().OnClick!
            .Invoke(new Miko.Events.MouseEventArgs { Target = profile });
        engine.Render(_surface.Canvas);

        segment = engine.GetCurrentLayout().ShouldNotBeNull();

        var max = segment.ScrollableContentWidth - segment.BoxModel.PaddingBox.Width;
        segment.ScrollLeft.ShouldBe(max, 0.01f);
    }

    private static LayoutBox? FindBox(LayoutBox box, Element element)
    {
        if (box.Element == element) return box;
        foreach (var child in box.Children)
        {
            var found = FindBox(child, element);
            if (found is not null) return found;
        }
        return null;
    }

    [Fact]
    public void Ios_DebugDemoStructure_ClickingWatch_CentersButtonInViewport()
    {
        var engine = new MikoEngine();
        using var context = new TestContext
        {
            ViewportWidth = 400f,
            ViewportHeight = 800f,
        };
        context.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(HostPlatform.Ios));
        context.Services.AddSingleton(engine);
        var styles = IonicStyleSheetFactory.CreateAllModes();
        context.AddStyleSheet(styles);

        var cut = context.Render<DebugDemoSegmentHost>();
        engine.Initialize(cut.Root, [styles], _surface.Canvas, 400f, 800f);

        var segmentElement = cut.Root.FindByClass("ion-segment").Single();
        var segment = FindBox(engine.GetCurrentLayout().ShouldNotBeNull(), segmentElement).ShouldNotBeNull();
        segment.BoxModel.PaddingBox.Width.ShouldBe(360f, 0.01f);
        segment.ScrollableContentWidth.ShouldBeGreaterThan(segment.BoxModel.PaddingBox.Width);

        var watch = segmentElement.FindByClass("ion-segment-button")[3];
        watch.FindByClass("button-native").Single().OnClick!
            .Invoke(new Miko.Events.MouseEventArgs { Target = watch });
        engine.Render(_surface.Canvas);

        segmentElement = cut.Root.FindByClass("ion-segment").Single();
        segment = FindBox(engine.GetCurrentLayout().ShouldNotBeNull(), segmentElement).ShouldNotBeNull();
        watch = segmentElement.FindByClass("ion-segment-button")[3];
        var watchBox = FindBox(segment, watch).ShouldNotBeNull();
        var visibleCenter = watchBox.BoxModel.BorderBox.Left
            + watchBox.BoxModel.BorderBox.Width / 2f
            - segment.ScrollLeft;
        var viewportCenter = segment.BoxModel.PaddingBox.Left
            + segment.BoxModel.PaddingBox.Width / 2f;

        segment.ScrollLeft.ShouldBeGreaterThan(0f);
        visibleCenter.ShouldBe(viewportCenter, 0.01f);
    }

    [Fact]
    public void Ios_DebugDemoStructure_PointerClickCentersWatch()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(configuration => configuration.Platform = HostPlatform.Ios);
        builder.UseRouter(router => router.MapRoute("/", typeof(DebugDemoSegmentHost)));
        var app = builder.Build();
        app.Controller.Initialize(_surface.Canvas, 400f, 800f);

        var root = app.Engine.GetRoot().ShouldNotBeNull();
        var segmentElement = root.FindByClass("ion-segment").Single();
        var segment = FindBox(app.Engine.GetCurrentLayout().ShouldNotBeNull(), segmentElement).ShouldNotBeNull();
        var watch = segmentElement.FindByClass("ion-segment-button")[3];
        var watchBox = FindBox(segment, watch).ShouldNotBeNull();
        var clickX = watchBox.BoxModel.BorderBox.Left + watchBox.BoxModel.BorderBox.Width / 2f;
        var clickY = watchBox.BoxModel.BorderBox.Top + watchBox.BoxModel.BorderBox.Height / 2f;

        app.Controller.OnPointerDown(clickX, clickY, Miko.Events.MouseButton.Left);
        app.Controller.OnPointerUp(clickX, clickY, Miko.Events.MouseButton.Left);
        for (var frame = 0; frame < 30; frame++)
        {
            app.Controller.RenderFrame(_surface.Canvas, 400f, 800f, 1f / 60f,
                canvas => app.Engine.Render(canvas));
        }

        root = app.Engine.GetRoot().ShouldNotBeNull();
        segmentElement = root.FindByClass("ion-segment").Single();
        segment = FindBox(app.Engine.GetCurrentLayout().ShouldNotBeNull(), segmentElement).ShouldNotBeNull();
        watch = segmentElement.FindByClass("ion-segment-button")[3];
        watch.ShouldHaveClass("segment-button-checked");
        watchBox = FindBox(segment, watch).ShouldNotBeNull();
        var visibleCenter = watchBox.BoxModel.BorderBox.Left
            + watchBox.BoxModel.BorderBox.Width / 2f
            - segment.ScrollLeft;
        var viewportCenter = segment.BoxModel.PaddingBox.Left
            + segment.BoxModel.PaddingBox.Width / 2f;

        segment.ScrollLeft.ShouldBeGreaterThan(0f);
        visibleCenter.ShouldBe(viewportCenter, 0.01f);
    }

    private sealed class DebugDemoSegmentHost : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "debug-root");
            builder.AddAttribute(2, "style", new Style
            {
                Width = Length.Percent(100),
                Height = Length.Px(60),
                Padding = new Padding(Length.Px(20), Length.Px(20)),
            });
            builder.OpenComponent<IonSegment>(3);
            builder.AddComponentParameter(4, nameof(IonSegment.Value), "call");
            builder.AddComponentParameter(5, nameof(IonSegment.Color), "success");
            builder.AddComponentParameter(6, nameof(IonSegment.Scrollable), true);
            builder.AddComponentParameter(7, nameof(IonSegment.ChildContent), (RenderFragment)(content =>
            {
                var sequence = 0;
                foreach (var value in Values)
                {
                    var captured = value.ToLowerInvariant();
                    content.OpenComponent<IonSegmentButton>(sequence++);
                    content.AddComponentParameter(sequence++, nameof(IonSegmentButton.Value), captured);
                    content.AddComponentParameter(sequence++, nameof(IonSegmentButton.ChildContent),
                        (RenderFragment)(icon =>
                        {
                            icon.OpenComponent<IonIcon>(0);
                            icon.AddComponentParameter(1, nameof(IonIcon.Icon), Ionicons.Call);
                            icon.CloseComponent();
                        }));
                    content.CloseComponent();
                }
            }));
            builder.CloseComponent();
            builder.CloseElement();
        }
    }
}
