using Miko.Common;
using Miko.Graphics;

namespace Miko.Components.Reader;

/// <summary>Only the visible spread is decoded. Every decoded image is released when it leaves the view.</summary>
internal sealed class ReaderImageView : IDisposable
{
    private IReadOnlyList<ReaderContent> _content = [];
    private readonly List<Image> _images = [];
    private bool _decoded;
    private bool _startAtTop = true;
    private float _panX, _panY, _overflowX, _overflowY;
    public float Zoom { get; private set; } = 1;
    public bool CanPan => _overflowX > 0 || _overflowY > 0;
    public string? Error { get; private set; }

    public void SetContent(IReadOnlyList<ReaderContent> content)
    {
        if (ReferenceEquals(_content, content)) return;
        Dispose();
        _content = content; _decoded = false; Error = null;
        ResetZoom();
    }

    public void ResetZoom() { Zoom = 1; _panX = _panY = _overflowX = _overflowY = 0; _startAtTop = true; }
    public void ToggleZoom(float amount) { Zoom = Zoom > 1 ? 1 : Math.Clamp(amount, 1, 8); _panX = _panY = 0; }
    public void Pan(float dx, float dy)
    {
        _panX = Math.Clamp(_panX + dx, -_overflowX, _overflowX);
        _panY = Math.Clamp(_panY + dy, -_overflowY, _overflowY);
    }

    public void Paint(IDrawingContext context, RectF box, bool fitWidth, ReaderDirection direction, ReaderOrientation orientation)
    {
        if (!_decoded)
        {
            _decoded = true;
            try
            {
                foreach (var content in _content)
                {
                    if (content is not ReaderImageContent data) throw new InvalidDataException("The comic source must return images.");
                    var image = context.Graphics.DecodeImage(data.Data.Span) ?? throw new InvalidDataException("Cannot decode this page image.");
                    _images.Add(image);
                }
            }
            catch (Exception error) { DisposeImages(); Error = error.Message; }
        }
        if (_images.Count == 0) return;
        bool horizontal = orientation == ReaderOrientation.Horizontal;
        float gap = _images.Count > 1 ? 8 : 0;
        float slotWidth = horizontal ? Math.Max(1, (box.Width - gap) / _images.Count) : box.Width;
        float slotHeight = horizontal ? box.Height : Math.Max(1, (box.Height - gap) / _images.Count);
        var rects = new RectF[_images.Count];
        float groupWidth = 0, groupHeight = 0;
        for (int i = 0; i < _images.Count; i++)
        {
            var image = _images[i];
            float scale = fitWidth ? slotWidth / image.Width : Math.Min(slotWidth / image.Width, slotHeight / image.Height);
            float w = image.Width * scale, h = image.Height * scale;
            rects[i] = new(0, 0, w, h);
            groupWidth = horizontal ? groupWidth + w : Math.Max(groupWidth, w);
            groupHeight = horizontal ? Math.Max(groupHeight, h) : groupHeight + h;
        }
        if (horizontal) groupWidth += gap; else groupHeight += gap;
        _overflowX = Math.Max(0, (groupWidth * Zoom - box.Width) / 2);
        _overflowY = Math.Max(0, (groupHeight * Zoom - box.Height) / 2);
        if (_startAtTop) { if (fitWidth) _panY = _overflowY; _startAtTop = false; }
        Pan(0, 0);
        float cursor = 0;
        for (int slot = 0; slot < _images.Count; slot++)
        {
            int i = horizontal && direction == ReaderDirection.RightToLeft ? _images.Count - 1 - slot : slot;
            var r = rects[i];
            float x = horizontal ? cursor : (groupWidth - r.Width) / 2;
            float y = horizontal ? (groupHeight - r.Height) / 2 : cursor;
            context.DrawImage(_images[i], new(
                box.X + box.Width / 2 + (x - groupWidth / 2) * Zoom + _panX,
                box.Y + box.Height / 2 + (y - groupHeight / 2) * Zoom + _panY,
                r.Width * Zoom, r.Height * Zoom));
            cursor += (horizontal ? r.Width : r.Height) + gap;
        }
    }

    private void DisposeImages() { foreach (var image in _images) image.Dispose(); _images.Clear(); }
    public void Dispose() { DisposeImages(); _content = []; }
}
