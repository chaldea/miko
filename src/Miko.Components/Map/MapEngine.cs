using Miko.Common;
using Miko.Core;
using Miko.Graphics;

namespace Miko.Components.Map;

/// <summary>Draws georeferenced imagery and markers on a Miko canvas. Call Draw, Retry and Dispose on the UI thread.</summary>
public sealed class MapEngine(IMapProvider provider, MikoEngine engine) : IDisposable
{
    private static readonly Color Paper = Color.FromHex("#ecefe9");
    private static readonly Color Ink = Color.FromHex("#263b41");
    private static readonly Color Pin = Color.FromHex("#b34d32");
    private MapImageRequest[] _requested = [], _displayed = [];
    private Image[] _images = [];
    private MapCamera? _lastCamera;
    private float _width, _height;
    private CancellationTokenSource? _pending;
    private bool _disposed;

    public Exception? Error { get; private set; }

    public void Draw(IDrawingContext context, RectF bounds, MapCamera camera, IReadOnlyList<MapMarker>? markers = null)
    {
        if (_disposed || bounds.Width <= 0 || bounds.Height <= 0) return;
        if (_lastCamera != camera || _width != bounds.Width || _height != bounds.Height)
        {
            _lastCamera = camera;
            _width = bounds.Width;
            _height = bounds.Height;
            Request(MapImageRequest.CoverViewport(camera, _width, _height));
        }

        int save = context.Save();
        try
        {
            context.ClipRect(bounds);
            context.FillRect(bounds, Paper);
            DrawBaseMap(context, bounds, camera);
            if (markers is not null) DrawMarkers(context, bounds, camera, markers);
            if (Error is not null || _images.Length == 0)
                DrawLabel(context, Error is null ? "Loading map…" : "Map unavailable · Tap to retry", bounds.X + 10, bounds.Y + 10);
            var credit = context.Graphics.CreateText(provider.Attribution, new("sans-serif", 11));
            float creditX = bounds.Right - credit.Width - 12;
            context.FillRect(new(creditX, bounds.Bottom - credit.Height - 6, credit.Width + 12, credit.Height + 6), Color.White);
            context.DrawText(credit, creditX + 6, bounds.Bottom - credit.Descent - 3, Ink);
        }
        finally { context.RestoreToCount(save); }
    }

    private void DrawBaseMap(IDrawingContext context, RectF bounds, MapCamera camera)
    {
        for (int i = 0; i < _displayed.Length; i++)
        {
            var frame = _displayed[i];
            var (dx, dy) = camera.OffsetOf(frame.Camera.Center);
            float scale = (float)camera.WorldSize / frame.Camera.WorldSize;
            float w = frame.Width * scale, h = frame.Height * scale;
            float left = bounds.X + (bounds.Width - w) / 2 + (float)dx;
            float top = bounds.Y + (bounds.Height - h) / 2 + (float)dy;
            // Repeat horizontally when the viewport straddles the antimeridian.
            left += (float)Math.Floor((bounds.Left - left) / camera.WorldSize) * camera.WorldSize;
            for (float x = left; x < bounds.Right; x += camera.WorldSize)
                if (x + w > bounds.Left) context.DrawImage(_images[i], new(x, top, w, h));
        }
    }

    private static void DrawMarkers(IDrawingContext context, RectF bounds, MapCamera camera, IReadOnlyList<MapMarker> markers)
    {
        foreach (var marker in markers)
        {
            var (dx, dy) = camera.OffsetOf(marker.Position);
            float y = bounds.Y + bounds.Height / 2 + (float)dy;
            if (y < bounds.Top - 6 || y > bounds.Bottom + 6) continue;
            float x = bounds.X + bounds.Width / 2 + (float)dx;
            x += (float)Math.Ceiling((bounds.Left - 6 - x) / camera.WorldSize) * camera.WorldSize;
            for (; x <= bounds.Right + 6; x += camera.WorldSize)
            {
                context.FillCircle(x, y, 6, Color.White);
                context.FillCircle(x, y, 4, Pin);
                if (!string.IsNullOrEmpty(marker.Label)) DrawLabel(context, marker.Label, x + 9, y - 10);
            }
        }
    }

    private static void DrawLabel(IDrawingContext context, string value, float left, float top)
    {
        var text = context.Graphics.CreateText(value, new("sans-serif", 13));
        context.FillRoundRect(new(left, top, text.Width + 12, text.Height + 6), 4, Color.White);
        context.DrawText(text, left + 6, top + text.Ascent + 3, Ink);
    }

    private void Request(MapImageRequest[] requests)
    {
        if (_requested.SequenceEqual(requests)) return;
        bool debounce = _requested.Length > 0;
        CancelPending();
        Error = null;
        _requested = requests;
        if (_displayed.SequenceEqual(requests)) return;
        _pending = new();
        _ = LoadAsync(requests, debounce, _pending.Token);
    }

    private async Task LoadAsync(MapImageRequest[] requests, bool debounce, CancellationToken token)
    {
        try
        {
            // Continue moving the previous image during drags and coalesce rapid camera changes.
            if (debounce) await Task.Delay(120, token).ConfigureAwait(false);
            var data = new byte[requests.Length][];
            for (int i = 0; i < requests.Length; i++)
            {
                data[i] = await provider.GetImageAsync(requests[i], token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
            }
            engine.Dispatcher.Post(() =>
            {
                if (token.IsCancellationRequested) return;
                try { InstallImages(requests, data); }
                catch (Exception error) { Error = error; }
                engine.RequestRepaint();
            });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error)
        {
            engine.Dispatcher.Post(() =>
            {
                if (token.IsCancellationRequested) return;
                Error = error;
                engine.RequestRepaint();
            });
        }
    }

    private void InstallImages(MapImageRequest[] requests, byte[][] data)
    {
        var images = new Image[data.Length];
        try
        {
            for (int i = 0; i < data.Length; i++)
                images[i] = engine.Graphics.DecodeImage(data[i]) ?? throw new InvalidDataException("Unreadable map image.");
        }
        catch
        {
            foreach (var image in images) image?.Dispose();
            throw;
        }

        foreach (var oldImage in _images) oldImage.Dispose();
        _images = images;
        _displayed = requests;
    }

    public void Retry()
    {
        if (Error is null || _disposed) return;
        Error = null;
        _lastCamera = null;
        _requested = [];
        engine.RequestRepaint();
    }

    private void CancelPending()
    {
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = null;
    }

    public void Dispose()
    {
        _disposed = true;
        CancelPending();
        foreach (var image in _images) image.Dispose();
        _images = [];
    }
}
