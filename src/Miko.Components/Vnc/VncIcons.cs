using Miko.Common;
using Miko.Graphics;

namespace Miko.Components.Vnc;

/// <summary>The viewer's Ionicons (MIT), embedded at build time, decoded once as vector images and tinted at draw time.</summary>
internal sealed class VncIcons : IDisposable
{
    private readonly Dictionary<string, Image?> _icons = new();

    public void Draw(IDrawingContext context, float x, float y, string name, float size, Color color)
    {
        if (!_icons.TryGetValue(name, out var icon))
        {
            // settings / expand / contract / refresh are the player's and alert the webcam's (one embedded copy each).
            var assembly = typeof(VncIcons).Assembly;
            using var stream = assembly.GetManifestResourceStream($"Miko.Vnc.Icons.{name}.svg")
                ?? assembly.GetManifestResourceStream($"Miko.Player.Icons.{name}.svg")
                ?? assembly.GetManifestResourceStream($"Miko.Webcam.Icons.{name}.svg");
            icon = stream == null ? null : context.Graphics.DecodeImage(stream);
            _icons.Add(name, icon);
        }
        if (icon == null) return;
        context.DrawImage(icon, new RectF(x - size / 2, y - size / 2, size, size), tint: color);
    }

    public void Draw(IDrawingContext context, RectF rect, string name, float size, Color color) =>
        Draw(context, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, name, size, color);

    public void Dispose()
    {
        foreach (var icon in _icons.Values) icon?.Dispose();
        _icons.Clear();
    }
}
