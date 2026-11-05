using Miko.Common;
using Miko.Graphics;

namespace Miko.Components.Webcam;

/// <summary>The webcam's Ionicons (MIT), embedded at build time, decoded once as vector images and tinted at draw time.</summary>
internal sealed class WebcamIcons : IDisposable
{
    private readonly Dictionary<string, Image?> _icons = new();

    /// <summary>Draws icon <paramref name="name"/> at <paramref name="size"/> px, centered on (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public void Draw(IDrawingContext context, float x, float y, string name, float size, Color color)
    {
        if (!_icons.TryGetValue(name, out var icon))
        {
            // play / expand / contract are the player's resources (one embedded copy per file).
            var assembly = typeof(WebcamIcons).Assembly;
            using var stream = assembly.GetManifestResourceStream($"Miko.Webcam.Icons.{name}.svg")
                ?? assembly.GetManifestResourceStream($"Miko.Player.Icons.{name}.svg");
            icon = stream == null ? null : context.Graphics.DecodeImage(stream);
            _icons.Add(name, icon);
        }
        if (icon == null) return;
        context.DrawImage(icon, new RectF(x - size / 2, y - size / 2, size, size), tint: color);
    }

    /// <summary>Draws icon <paramref name="name"/> centered in <paramref name="rect"/>.</summary>
    public void Draw(IDrawingContext context, RectF rect, string name, float size, Color color) =>
        Draw(context, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, name, size, color);

    public void Dispose()
    {
        foreach (var icon in _icons.Values) icon?.Dispose();
        _icons.Clear();
    }
}
