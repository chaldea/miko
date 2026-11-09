using Miko.Common;
using Miko.Components.Vnc.Protocol;
using Miko.Core.DomElements;
using Miko.Graphics;

namespace Miko.Components.Vnc;

/// <summary>
/// The remote screen element. A canvas that paints the framebuffer; it is also the keyboard target: as a
/// text editor that never holds text, it receives key events, typed text and the soft keyboard's input
/// (IME composition included) and hands them to the viewer.
/// </summary>
internal sealed class VncScreen : CanvasElement, ITextEditable
{
    /// <summary>Text typed into the remote screen (one commit: a character, an IME result, a paste).</summary>
    public Action<string>? TextTyped { get; set; }

    public string? Value { get => string.Empty; set { } }

    public int CursorPosition { get => 0; set { } }

    public bool IsEditable => true;

    // Multiline keeps "\n" from soft keyboards (iOS commits Return as text): it is sent as Return.
    public bool IsMultiline => true;

    public void InsertText(string text) => TextTyped?.Invoke(text);

    public bool Backspace() => false;

    public bool Delete() => false;

    public void MoveCursorToEnd() { }
}

/// <summary>
/// Maps the framebuffer onto the screen element (fit, zoom, pan) and paints it: the framebuffer is cut into
/// tiles, each an image recreated only when its pixels changed, so a typing cursor re-uploads 64 KB, not the
/// whole desktop. Render thread only.
/// </summary>
internal sealed class VncView : IDisposable
{
    public const float MaxZoom = 8;

    private VncFramebuffer? _framebuffer;
    private Image?[] _tiles = [];
    private int[] _tileVersions = [];
    private readonly byte[] _scratch = new byte[VncFramebuffer.TileSize * VncFramebuffer.TileSize * 4];
    private VncCursorShape? _cursorShape;
    private Image? _cursorImage;
    private bool _cursorDirty;

    /// <summary>Size of the screen element's content box (from the last paint).</summary>
    public float ViewWidth { get; private set; }

    public float ViewHeight { get; private set; }

    /// <summary>1 = the whole framebuffer fits; above zooms in.</summary>
    public float Zoom { get; private set; } = 1;

    /// <summary>The framebuffer point shown at the centre of the view.</summary>
    public float CenterX { get; private set; }

    public float CenterY { get; private set; }

    /// <summary>Where the remote pointer is, in framebuffer pixels (null before the first input).</summary>
    public (float X, float Y)? Pointer { get; set; }

    /// <summary>Draw <see cref="Pointer"/> with the server's cursor shape (it then stays out of the framebuffer).</summary>
    public bool DrawCursor { get; set; } = true;

    /// <summary>A press is held long enough to be a right click: a ring around the pointer.</summary>
    public bool ShowHoldRing { get; set; }

    /// <summary>Paints dimmed (the session ended; the last picture stays up).</summary>
    public bool Dimmed { get; set; }

    public VncFramebuffer? Framebuffer => _framebuffer;

    public bool HasCursorShape => _cursorShape is { IsEmpty: false };

    public void SetFramebuffer(VncFramebuffer? framebuffer)
    {
        if (ReferenceEquals(framebuffer, _framebuffer)) return;
        bool sameSize = framebuffer != null && _framebuffer != null
            && framebuffer.Width == _framebuffer.Width && framebuffer.Height == _framebuffer.Height;
        _framebuffer = framebuffer;
        DisposeTiles();
        if (!sameSize)
        {
            Zoom = 1;
            CenterX = (framebuffer?.Width ?? 0) / 2f;
            CenterY = (framebuffer?.Height ?? 0) / 2f;
        }
    }

    /// <summary>The server's pointer shape; may come from any thread (applied at the next paint).</summary>
    public void SetCursor(VncCursorShape shape)
    {
        Volatile.Write(ref _cursorShape, shape);
        _cursorDirty = true;
    }

    // ---- geometry ------------------------------------------------------------------

    public float FitScale => _framebuffer is { } fb && ViewWidth > 0 && ViewHeight > 0
        ? Math.Min(ViewWidth / fb.Width, ViewHeight / fb.Height) : 1;

    public float Scale => FitScale * Zoom;

    /// <summary>Top-left of the framebuffer in view coordinates.</summary>
    public (float X, float Y) Origin => (ViewWidth / 2 - CenterX * Scale, ViewHeight / 2 - CenterY * Scale);

    /// <summary>Whether the framebuffer is larger than the view on either axis (panning moves it).</summary>
    public bool CanPan => _framebuffer is { } fb && (fb.Width * Scale > ViewWidth + 0.5f || fb.Height * Scale > ViewHeight + 0.5f);

    public (float X, float Y) ToRemote(float viewX, float viewY)
    {
        var (ox, oy) = Origin;
        float s = Scale;
        return ((viewX - ox) / s, (viewY - oy) / s);
    }

    public (float X, float Y) ToView(float remoteX, float remoteY)
    {
        var (ox, oy) = Origin;
        float s = Scale;
        return (ox + remoteX * s, oy + remoteY * s);
    }

    public void SetViewSize(float width, float height)
    {
        if (width == ViewWidth && height == ViewHeight) return;
        ViewWidth = width;
        ViewHeight = height;
        Clamp();
    }

    /// <summary>Sets the zoom, keeping the framebuffer point under (<paramref name="viewX"/>, <paramref name="viewY"/>) in place.</summary>
    public void SetZoom(float zoom, float? viewX = null, float? viewY = null)
    {
        zoom = Math.Clamp(zoom, 1, MaxZoom);
        float vx = viewX ?? ViewWidth / 2, vy = viewY ?? ViewHeight / 2;
        var (rx, ry) = ToRemote(vx, vy);
        Zoom = zoom;
        float s = Scale;
        CenterX = rx - (vx - ViewWidth / 2) / s;
        CenterY = ry - (vy - ViewHeight / 2) / s;
        Clamp();
    }

    /// <summary>The zoom at which one framebuffer pixel is one logical pixel.</summary>
    public float ActualSizeZoom => FitScale > 0 ? Math.Clamp(1 / FitScale, 1, MaxZoom) : 1;

    /// <summary>Moves the picture with a drag of (<paramref name="dx"/>, <paramref name="dy"/>) view pixels.</summary>
    public void Pan(float dx, float dy)
    {
        float s = Scale;
        CenterX -= dx / s;
        CenterY -= dy / s;
        Clamp();
    }

    /// <summary>Pans so that a framebuffer point stays visible, <paramref name="margin"/> view pixels from the edges.</summary>
    public void Follow(float remoteX, float remoteY, float margin = 48)
    {
        if (!CanPan) return;
        var (vx, vy) = ToView(remoteX, remoteY);
        float s = Scale;
        margin = Math.Min(margin, Math.Min(ViewWidth, ViewHeight) / 4);
        if (vx < margin) CenterX -= (margin - vx) / s;
        else if (vx > ViewWidth - margin) CenterX += (vx - (ViewWidth - margin)) / s;
        if (vy < margin) CenterY -= (margin - vy) / s;
        else if (vy > ViewHeight - margin) CenterY += (vy - (ViewHeight - margin)) / s;
        Clamp();
    }

    /// <summary>
    /// Pans so the pointer's place in the view matches its place on the remote screen (a pointer at the view's
    /// right edge shows the desktop's right edge): with a mouse, moving it sweeps the whole zoomed desktop.
    /// </summary>
    public void Track(float viewX, float viewY)
    {
        if (_framebuffer is not { } fb || !CanPan) return;
        float s = Scale;
        float w = fb.Width * s, h = fb.Height * s;
        if (w > ViewWidth) CenterX = (fb.Width - ViewWidth / s) * Math.Clamp(viewX / ViewWidth, 0, 1) + ViewWidth / s / 2;
        if (h > ViewHeight) CenterY = (fb.Height - ViewHeight / s) * Math.Clamp(viewY / ViewHeight, 0, 1) + ViewHeight / s / 2;
        Clamp();
    }

    private void Clamp()
    {
        if (_framebuffer is not { } fb) return;
        float s = Scale;
        float halfW = ViewWidth / s / 2, halfH = ViewHeight / s / 2;
        CenterX = fb.Width <= halfW * 2 ? fb.Width / 2f : Math.Clamp(CenterX, halfW, fb.Width - halfW);
        CenterY = fb.Height <= halfH * 2 ? fb.Height / 2f : Math.Clamp(CenterY, halfH, fb.Height - halfH);
    }

    // ---- paint ---------------------------------------------------------------------

    public void Paint(IDrawingContext context, RectF bounds, VncIcons icons)
    {
        SetViewSize(bounds.Width, bounds.Height);
        context.FillRect(bounds, Color.Black);
        if (_framebuffer is not { } fb) return;

        var (ox, oy) = Origin;
        float s = Scale;
        // 1:1 copies pixels; otherwise bilinear (tiles are sampled independently, so no wide kernels at their seams).
        var sampling = Math.Abs(s * context.DeviceScale - 1) < 0.01f ? ImageSampling.Nearest : ImageSampling.Linear;
        int ts = VncFramebuffer.TileSize;
        if (_tiles.Length != fb.TilesX * fb.TilesY)
        {
            DisposeTiles();
            _tiles = new Image?[fb.TilesX * fb.TilesY];
            _tileVersions = new int[_tiles.Length];
            Array.Fill(_tileVersions, -1);
        }

        // Only tiles in view are copied and drawn.
        int tx0 = Math.Max(0, (int)((0 - ox) / s / ts)), ty0 = Math.Max(0, (int)((0 - oy) / s / ts));
        int tx1 = Math.Min(fb.TilesX - 1, (int)((bounds.Width - ox) / s / ts)), ty1 = Math.Min(fb.TilesY - 1, (int)((bounds.Height - oy) / s / ts));
        for (int ty = ty0; ty <= ty1; ty++)
        {
            for (int tx = tx0; tx <= tx1; tx++)
            {
                int index = ty * fb.TilesX + tx;
                int x = tx * ts, y = ty * ts;
                int w = Math.Min(ts, fb.Width - x), h = Math.Min(ts, fb.Height - y);
                int version = fb.TileVersion(tx, ty);
                if (version != _tileVersions[index] || _tiles[index] == null)
                {
                    fb.CopyTo(x, y, w, h, _scratch);
                    var image = context.Graphics.CreateImage(w, h, ImagePixelFormat.Bgrx8888, _scratch.AsSpan(0, w * h * 4));
                    _tiles[index]?.Dispose();
                    _tiles[index] = image;
                    _tileVersions[index] = version;
                }
                if (_tiles[index] is { } tile)
                    context.DrawImage(tile, new RectF(bounds.X + ox + x * s, bounds.Y + oy + y * s, w * s, h * s), sampling);
            }
        }

        if (Dimmed) context.FillRect(bounds, Color.FromRgba(0, 0, 0, .55f));
        else PaintPointer(context, bounds, ox, oy, s);
    }

    private void PaintPointer(IDrawingContext context, RectF bounds, float ox, float oy, float s)
    {
        if (Pointer is not { } p) return;
        float px = bounds.X + ox + p.X * s, py = bounds.Y + oy + p.Y * s;
        if (ShowHoldRing)
        {
            context.StrokeArc(new RectF(px - 22, py - 22, 44, 44), 0, 360, Color.FromRgba(255, 255, 255, .9f), 2.5f);
            context.StrokeArc(new RectF(px - 24, py - 24, 48, 48), 0, 360, Color.FromRgba(0, 0, 0, .35f), 1.5f);
        }
        if (!DrawCursor) return;
        var shape = Volatile.Read(ref _cursorShape);
        if (shape == null || shape.IsEmpty) return;
        if (_cursorDirty || _cursorImage == null)
        {
            _cursorDirty = false;
            _cursorImage?.Dispose();
            _cursorImage = context.Graphics.CreateImage(shape.Width, shape.Height, ImagePixelFormat.Bgra8888, shape.Pixels);
        }
        if (_cursorImage == null) return;
        // The pointer scales with the picture, but never so small it cannot be seen or so large it covers it.
        float cs = Math.Clamp(s, .75f, 1.5f);
        context.DrawImage(_cursorImage, new RectF(px - shape.HotspotX * cs, py - shape.HotspotY * cs, shape.Width * cs, shape.Height * cs),
            ImageSampling.Linear);
    }

    private void DisposeTiles()
    {
        foreach (var tile in _tiles) tile?.Dispose();
        _tiles = [];
        _tileVersions = [];
    }

    public void Dispose()
    {
        DisposeTiles();
        _cursorImage?.Dispose();
        _cursorImage = null;
    }
}
