using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components.Map;
using Miko.Events;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Styling;
using Shouldly;

namespace Miko.Components.Tests.Map;

public sealed class MikoMapTests : IDisposable
{
    private readonly MikoMap _map = new() { Style = new() { Height = Length.Percent(100) } };
    private readonly Source _source = new();
    private readonly MikoAppContext _app;
    private readonly RenderSurface _surface;

    public MikoMapTests()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.Services.AddSingleton<IMapProvider>(_source);
        builder.Services.AddMikoMap();
        _app = builder.UseRootComponent(_map.Build).Build();
        _surface = _app.Engine.Graphics.CreateSurface(320, 240);
        _app.Controller.Initialize(_surface.Context, 320, 240);
    }

    private void Frame() => _app.Engine.Render(_surface.Context);

    private async Task WaitFor(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (true)
        {
            Frame();
            if (condition()) return;
            await Task.Delay(10, timeout.Token);
        }
    }

    private byte[] Image(Color color)
    {
        using var image = _app.Engine.Graphics.CreateSurface(320, 240);
        image.Context.Clear(color);
        return image.EncodePng();
    }

    [Fact]
    public async Task Should_pan_and_zoom_through_host_input_and_ignore_a_superseded_request()
    {
        var initial = await _source.Next();
        var anchor = _map.CurrentCamera.PointAt(-30, -25);
        _app.Controller.OnScroll(130, 95, 0, -40);
        _map.CurrentCamera.Zoom.ShouldBe(initial.View.Camera.Zoom + 1);
        _map.CurrentCamera.OffsetOf(anchor).X.ShouldBe(-30, .00001);
        var before = _map.CurrentCamera;
        _app.Controller.OnPointerDown(140, 140, MouseButton.Left, PointerType.Touch);
        _app.Controller.OnPointerMove(182, 158);
        _app.Controller.OnPointerUp(182, 158, MouseButton.Left, PointerType.Touch);
        _map.CurrentCamera.ShouldBe(before.PanBy(42, 18));
        Frame();
        initial.Token.IsCancellationRequested.ShouldBeTrue();
        var latest = await _source.Next();
        latest.View.Camera.ShouldBe(_map.CurrentCamera);
        var green = Color.FromHex("#449966");
        latest.Result.SetResult(Image(green));
        await WaitFor(() => _surface.GetPixel(10, 100).Equals(green));
        initial.Result.SetResult(Image(Color.Black));
        Frame();
        _surface.GetPixel(10, 100).ShouldBe(green);

        _map.MoveTo(initial.View.Camera);
        Frame();
        var pending = await _source.Next();
        _app.Dispose();
        pending.Token.IsCancellationRequested.ShouldBeTrue();
        pending.Result.SetResult([]);
    }

    [Fact]
    public async Task Should_recover_from_invalid_imagery_by_tapping_the_map()
    {
        var failed = await _source.Next();
        failed.Result.SetResult([0, 1, 2]);
        await WaitFor(() => _map.Error is InvalidDataException);
        _app.Controller.OnPointerDown(180, 110, MouseButton.Left);
        _app.Controller.OnPointerUp(180, 110, MouseButton.Left);
        Frame();
        var retry = await _source.Next();
        retry.View.ShouldBe(failed.View);
        retry.Result.SetResult(Image(Color.White));
        await WaitFor(() => _map.Error is null && _surface.GetPixel(10, 100).Equals(Color.White));

        _app.Controller.OnPointerDown(285, 30, MouseButton.Left);
        _app.Controller.OnPointerUp(285, 30, MouseButton.Left);
        _map.CurrentCamera.Zoom.ShouldBe(failed.View.Camera.Zoom + 1);
    }

    [Fact]
    public async Task Should_join_adjacent_frames_across_a_large_viewport_at_minimum_zoom()
    {
        var source = new Source();
        using var map = new MapEngine(source, _app.Engine);
        using var wide = _app.Engine.Graphics.CreateSurface(1600, 150);
        var camera = new MapCamera(new(179.9, 40), MapCamera.MinZoom);
        var bounds = new RectF(0, 0, 1600, 150);
        map.Draw(wide.Context, bounds, camera);
        var west = await source.Next();
        west.Result.SetResult(Image(Color.White));
        var east = await source.Next();
        east.View.Camera.Center.Longitude.ShouldBeLessThan(0);
        east.Result.SetResult(Image(Color.Black));
        await WaitFor(() =>
        {
            map.Draw(wide.Context, bounds, camera);
            return wide.GetPixel(799, 75).Equals(Color.White) && wide.GetPixel(800, 75).Equals(Color.Black);
        });
        wide.GetPixel(0, 75).ShouldBe(Color.White);
        wide.GetPixel(1599, 75).ShouldBe(Color.Black);
    }

    public void Dispose()
    {
        _app.Dispose();
        _surface.Dispose();
    }

    private sealed record Pending(MapImageRequest View, CancellationToken Token, TaskCompletionSource<byte[]> Result);

    private sealed class Source : IMapProvider
    {
        private readonly Channel<Pending> _requests = Channel.CreateUnbounded<Pending>();
        public string Attribution => "Test map";
        public Task<byte[]> GetImageAsync(MapImageRequest request, CancellationToken cancellationToken)
        {
            // Deliberately complete even if cancelled, to exercise stale completions after pan/removal.
            var result = new TaskCompletionSource<byte[]>();
            _requests.Writer.TryWrite(new(request, cancellationToken, result));
            return result.Task;
        }

        public Task<Pending> Next() => _requests.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
    }
}
