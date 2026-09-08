using Miko.Common;
using Miko.Fonts;
using SkiaSharp;

namespace Miko.Components.Player.Danmaku;

internal sealed class DanmakuPainter : IDisposable
{
    private readonly Dictionary<string, (SKTextBlob Blob, float Width)> _cache = new();
    private readonly SKPaint _fill = new() { IsAntialias = true };
    private readonly SKPaint _outline = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
    private readonly Func<string, float> _measure;
    private float _fontSize;
    public DanmakuPainter() => _measure = text => GetText(text).Width;

    public void Paint(SKCanvas canvas, RectF bounds, DanmakuTimeline timeline, TimeSpan position)
    {
        if (_fontSize != timeline.Settings.FontSize) { Clear(); _fontSize = timeline.Settings.FontSize; }
        timeline.Advance(position, bounds.Width, bounds.Height, _measure);
        byte alpha = (byte)(255 * timeline.Settings.Opacity);
        _outline.Color = SKColors.Black.WithAlpha(alpha);
        foreach (var entry in timeline.Visible)
        {
            var text = GetText(entry.Item.Text);
            _fill.Color = (entry.Item.Color ?? SKColors.White).WithAlpha(alpha);
            float x = bounds.X + entry.X;
            float y = bounds.Y + entry.Y + _fontSize;
            canvas.DrawText(text.Blob, x, y, _outline);
            canvas.DrawText(text.Blob, x, y, _fill);
        }
    }

    private (SKTextBlob Blob, float Width) GetText(string text)
    {
        if (_cache.TryGetValue(text, out var value)) return value;
        if (_cache.Count >= 512) Clear();
        char sample = text.FirstOrDefault(c => c > 127);
        if (sample == default) sample = text[0];
        var typeface = FontManager.Instance.GetTypefaceForCharacter(sample, "Arial", FontWeight.Normal);
        using var font = new SKFont(typeface, _fontSize);
        value = (SKTextBlob.Create(text, font)!, font.MeasureText(text));
        _cache.Add(text, value);
        return value;
    }

    private void Clear() { foreach (var entry in _cache.Values) entry.Blob.Dispose(); _cache.Clear(); }
    public void Dispose() { Clear(); _fill.Dispose(); _outline.Dispose(); }
}
