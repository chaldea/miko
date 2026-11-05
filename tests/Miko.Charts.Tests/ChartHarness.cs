using System.Diagnostics;
using Miko.Charts;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Graphics;
using Miko.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Miko.Styling;

namespace Miko.Charts.Tests;

/// <summary>
/// Renders a chart component in a real engine on an offscreen surface, the way an app does: the
/// component is built under the engine's service scope, LiveCharts measures on its throttler and posts
/// the measure to the engine's dispatcher, and frames are drawn until the animations settle.
/// </summary>
internal sealed class ChartHarness : IDisposable
{
    private readonly ServiceProvider _services;

    public ChartHarness(int width = 400, int height = 300)
    {
        Width = width;
        Height = height;

        var collection = new ServiceCollection();
        collection.AddMikoEngine();
        collection.AddMikoCharts();
        _services = collection.BuildServiceProvider();

        Engine = _services.GetRequiredService<MikoEngine>();
        Surface = Engine.Graphics.CreateSurface(width, height);
        Root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                BackgroundColor = Color.White,
            }
        };
    }

    public int Width { get; }

    public int Height { get; }

    public MikoEngine Engine { get; }

    public RenderSurface Surface { get; }

    public DivElement Root { get; }

    /// <summary>The chart options registered by <c>AddMikoCharts</c>.</summary>
    public MikoChartsOptions Options => _services.GetRequiredService<MikoChartsOptions>();

    /// <summary>
    /// Builds <paramref name="component"/> as a child of the page and lays out the first frame. The services an
    /// app injects are assigned directly, as a parent render would.
    /// </summary>
    public T Mount<T>(T component) where T : ComponentBase
    {
        switch (component)
        {
            case ChartComponentBase<LiveChartsCore.MikoView.Charts.CartesianChartView> c: c.Engine = Engine; c.Options = Options; break;
            case ChartComponentBase<LiveChartsCore.MikoView.Charts.PieChartView> c: c.Engine = Engine; c.Options = Options; break;
            case ChartComponentBase<LiveChartsCore.MikoView.Charts.PolarChartView> c: c.Engine = Engine; c.Options = Options; break;
            case GeoMap m: m.Engine = Engine; break;
        }

        var element = component.Build();
        Root.Children.Add(element);
        Engine.Initialize(Root, [], Surface.Context, Width, Height);
        return component;
    }

    /// <summary>
    /// Renders frames until nothing has asked for a frame for <see cref="QuietWindow"/>, or the timeout.
    /// </summary>
    /// <remarks>
    /// LiveCharts throttles its updates on the thread pool (50 ms for charts, 100 ms for maps) and only then
    /// posts the measure to the engine's dispatcher, so between a change and its measure the engine has no
    /// pending work at all. The quiet window is several times the throttle so a frame count cannot end the
    /// wait before the measure arrives.
    /// </remarks>
    public void RenderUntilIdle(TimeSpan? timeout = null)
    {
        var limit = Stopwatch.StartNew();
        var max = timeout ?? TimeSpan.FromSeconds(10);
        var quiet = Stopwatch.StartNew();

        while (limit.Elapsed < max)
        {
            Surface.Context.Clear(Color.White);
            Engine.Render(Surface.Context);

            if (Engine.HasPendingRenderWork) quiet.Restart();
            else if (quiet.Elapsed >= QuietWindow) return;

            Thread.Sleep(15);
        }

        throw new TimeoutException("The chart did not settle.");
    }

    /// <summary>How long no frame may be requested before <see cref="RenderUntilIdle"/> returns.</summary>
    public static TimeSpan QuietWindow { get; } = TimeSpan.FromMilliseconds(400);

    /// <summary>Counts the pixels in <paramref name="area"/> that are not (almost) white.</summary>
    public int CountInk(RectF area)
    {
        var count = 0;
        for (var y = (int)area.Y; y < (int)area.Bottom; y += 2)
        {
            for (var x = (int)area.X; x < (int)area.Right; x += 2)
            {
                var c = Surface.GetPixel(x, y);
                if (c.R < 235 || c.G < 235 || c.B < 235) count++;
            }
        }

        return count;
    }

    /// <summary>Whether some pixel in <paramref name="area"/> is close to <paramref name="color"/>.</summary>
    public bool HasColor(RectF area, Color color, int tolerance = 24)
    {
        for (var y = (int)area.Y; y < (int)area.Bottom; y += 2)
        {
            for (var x = (int)area.X; x < (int)area.Right; x += 2)
            {
                var c = Surface.GetPixel(x, y);
                if (Math.Abs(c.R - color.R) <= tolerance &&
                    Math.Abs(c.G - color.G) <= tolerance &&
                    Math.Abs(c.B - color.B) <= tolerance)
                    return true;
            }
        }

        return false;
    }

    public void Dispose()
    {
        Surface.Dispose();
        _services.Dispose();
    }
}
