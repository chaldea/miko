using Miko.Common;
using Miko.Graphics;

namespace Miko.Components.Player.Danmaku;

internal sealed class DanmakuPainter
{
    private const int MaxCachedTexts = 512;
    private readonly Dictionary<string, TextBlock> _cache = new();
    private readonly Func<string, float> _measure;
    private IGraphicsFactory? _graphics;
    private float _fontSize;
    public DanmakuPainter() => _measure = text => GetText(text).Width;

    public void Paint(IDrawingContext context, RectF bounds, DanmakuTimeline timeline, TimeSpan position)
    {
        _graphics = context.Graphics;
        if (_fontSize != timeline.Settings.FontSize) { _cache.Clear(); _fontSize = timeline.Settings.FontSize; }
        timeline.Advance(position, bounds.Width, bounds.Height, _measure);
        byte alpha = (byte)(255 * timeline.Settings.Opacity);
        var outline = new Color(0, 0, 0, alpha);
        foreach (var entry in timeline.Visible)
        {
            var text = GetText(entry.Item.Text);
            var fill = entry.Item.Color ?? Color.White;
            fill.A = alpha;
            float x = bounds.X + entry.X;
            float y = bounds.Y + entry.Y + _fontSize;
            context.StrokeText(text, x, y, outline, 2);
            context.DrawText(text, x, y, fill);
        }
    }

    /// <summary>
    /// Laid out with the engine's text pipeline, so mixed scripts get their fallback fonts
    /// (a comment like <c>日本語のコメント</c> used to take one typeface for the whole string).
    /// </summary>
    private TextBlock GetText(string text)
    {
        if (_cache.TryGetValue(text, out var value)) return value;
        if (_cache.Count >= MaxCachedTexts) _cache.Clear();
        value = _graphics!.CreateText(text, new TextStyle("Arial", _fontSize));
        _cache.Add(text, value);
        return value;
    }

    public void Clear() => _cache.Clear();
}
