using Miko.Components;
using Miko.Core;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Common;
using Miko.Events;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

public class IonRangeInteractionTests : IDisposable
{
    private const float Width = 320;
    private const float Height = 100;
    private readonly SKBitmap _bitmap = new((int)Width, (int)Height);
    private readonly SKCanvas _canvas;

    public IonRangeInteractionTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Fact]
    public void TrackPress_MovesKnobAndUpdatesBoundValue()
    {
        var (app, page) = BuildApp();
        var track = FindTrack(app);
        var x = track.left + track.width * 0.75f;

        app.Controller.OnPointerDown(x, track.y, MouseButton.Left);
        app.Controller.OnPointerUp(x, track.y, MouseButton.Left);

        page.Value.ShouldBe(75d, 1d);
        page.Changes.ShouldHaveSingleItem().ShouldBe(page.Value);
    }

    [Fact]
    public void Drag_ContinuesOutsideTrack_AndStopsAfterRelease()
    {
        var (app, page) = BuildApp();
        var track = FindTrack(app);

        app.Controller.OnPointerDown(track.left + track.width * 0.2f, track.y, MouseButton.Left);
        app.Controller.OnPointerMove(track.left + track.width * 0.6f, track.y);
        page.Value.ShouldBe(60d, 1d);

        app.Controller.OnPointerMove(track.left + track.width + 50, track.y);
        page.Value.ShouldBe(100d);

        app.Controller.OnPointerUp(track.left + track.width + 50, track.y, MouseButton.Left);
        app.Controller.OnPointerMove(track.left, track.y);
        page.Value.ShouldBe(100d);
    }

    [Fact]
    public void TrackPress_QuantizesValueToStep()
    {
        var (app, page) = BuildApp(p =>
        {
            p.Min = 1000;
            p.Max = 2000;
            p.Step = 100;
            p.Value = 1000;
        });
        var track = FindTrack(app);
        var x = track.left + track.width * 0.46f;

        app.Controller.OnPointerDown(x, track.y, MouseButton.Left);
        app.Controller.OnPointerUp(x, track.y, MouseButton.Left);

        page.Value.ShouldBe(1500d);
    }

    [Fact]
    public void DisabledRange_IgnoresPointerInput()
    {
        var (enabledApp, _) = BuildApp();
        var track = FindTrack(enabledApp);
        var (app, page) = BuildApp(p => p.Disabled = true);

        app.Controller.OnPointerDown(track.left + track.width * 0.8f, track.y, MouseButton.Left);
        app.Controller.OnPointerUp(track.left + track.width * 0.8f, track.y, MouseButton.Left);

        page.Value.ShouldBe(20d);
        page.Changes.ShouldBeEmpty();
    }

    private (MikoAppContext app, HostPage page) BuildApp(Action<HostPage>? configure = null)
    {
        var page = new HostPage();
        configure?.Invoke(page);
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRootComponent(page.Build);
        var app = builder.Build();
        app.Controller.Initialize(_canvas, Width, Height);
        app.Engine.Render(_canvas);
        return (app, page);
    }

    private static (float left, float width, float y) FindTrack(MikoAppContext app)
    {
        float bestLeft = 0, bestRight = 0, bestY = 0, bestWidth = 0;
        for (float y = 1; y < Height; y += 2)
        {
            float? first = null;
            float last = 0;
            for (float x = 0; x < Width; x++)
            {
                if (!IsInTrack(app.Engine.HitTest(x, y))) continue;
                first ??= x;
                last = x;
            }
            if (first is null || last - first.Value <= bestWidth) continue;
            bestLeft = first.Value;
            bestRight = last;
            bestY = y;
            bestWidth = last - first.Value;
        }
        bestWidth.ShouldBeGreaterThan(0f);
        return (bestLeft, bestRight - bestLeft, bestY);
    }

    private static bool IsInTrack(Element? element)
    {
        for (var current = element; current != null; current = current.Parent)
            if (current.HasClass("range-slider")) return true;
        return false;
    }

    private sealed class HostPage : ComponentBase
    {
        public double Value { get; set; } = 20;
        public double Min { get; set; }
        public double Max { get; set; } = 100;
        public double Step { get; set; } = 1;
        public bool Disabled { get; set; }
        public List<double> Changes { get; } = new();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<IonRange>(0);
            builder.AddComponentParameter(1, nameof(IonRange.Value), Value);
            builder.AddComponentParameter(2, nameof(IonRange.Min), Min);
            builder.AddComponentParameter(3, nameof(IonRange.Max), Max);
            builder.AddComponentParameter(4, nameof(IonRange.Step), Step);
            builder.AddComponentParameter(5, nameof(IonRange.Disabled), Disabled);
            builder.AddComponentParameter(6, nameof(IonRange.ValueChanged),
                EventCallback.Factory.Create<double>(this, value =>
                {
                    Value = value;
                    Changes.Add(value);
                }));
            builder.CloseComponent();
        }
    }
}
