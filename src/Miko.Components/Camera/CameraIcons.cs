using Miko.Common;
using Miko.Graphics;

namespace Miko.Components.Camera;

/// <summary>The camera components' Ionicons, decoded once per component as vector images and tinted at draw time.</summary>
internal sealed class CameraIcons : IDisposable
{
    private readonly Dictionary<string, Image?> _icons = new();

    /// <summary>Draws icon <paramref name="name"/> at <paramref name="size"/> px, centered in <paramref name="rect"/>.</summary>
    public void Draw(IDrawingContext context, RectF rect, string name, float size, Color color)
    {
        if (!_icons.TryGetValue(name, out var icon))
        {
            using var stream = typeof(CameraIcons).Assembly.GetManifestResourceStream($"Miko.Camera.Icons.{name}.svg")!;
            icon = context.Graphics.DecodeImage(stream);
            _icons.Add(name, icon);
        }
        if (icon == null) return;
        var bounds = new RectF(rect.X + (rect.Width - size) / 2, rect.Y + (rect.Height - size) / 2, size, size);
        context.DrawImage(icon, bounds, tint: color);
    }

    public void Dispose()
    {
        foreach (var icon in _icons.Values) icon?.Dispose();
        _icons.Clear();
    }
}
