using Miko.Common;
using SkiaSharp;
using Svg.Skia;

namespace Miko.Components.Player;

internal sealed class PlayerIcons : IDisposable
{
    private readonly Dictionary<string, SKSvg> _icons = new();
    private readonly SKPaint _paint = new() { ColorFilter = SKColorFilter.CreateBlendMode(SKColors.White, SKBlendMode.SrcIn) };

    public void Draw(SKCanvas canvas, RectF rect, string name)
    {
        if (!_icons.TryGetValue(name, out var svg))
        {
            using var stream = typeof(PlayerIcons).Assembly.GetManifestResourceStream($"Miko.Player.Icons.{name}.svg")!;
            svg = new SKSvg();
            svg.Load(stream);
            _icons.Add(name, svg);
        }
        if (svg.Picture == null) return;
        var bounds = svg.Picture.CullRect;
        canvas.Save();
        canvas.Translate(rect.X + (rect.Width - 22) / 2, rect.Y + (rect.Height - 22) / 2);
        canvas.Scale(22 / bounds.Width, 22 / bounds.Height);
        canvas.DrawPicture(svg.Picture, _paint);
        canvas.Restore();
    }
    public void Dispose()
    {
        foreach (var icon in _icons.Values) icon.Dispose();
        _icons.Clear();
        _paint.ColorFilter?.Dispose();
        _paint.Dispose();
    }
}
