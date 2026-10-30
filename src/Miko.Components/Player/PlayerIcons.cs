using Miko.Common;
using Miko.Graphics;

namespace Miko.Components.Player;

internal sealed class PlayerIcons : IDisposable
{
    private const float IconSize = 22;
    private readonly Dictionary<string, Image?> _icons = new();

    /// <summary>Draws the white icon <paramref name="name"/>, 22px, centered in <paramref name="rect"/>.</summary>
    public void Draw(IDrawingContext context, RectF rect, string name)
    {
        if (!_icons.TryGetValue(name, out var icon))
        {
            using var stream = typeof(PlayerIcons).Assembly.GetManifestResourceStream($"Miko.Player.Icons.{name}.svg")!;
            icon = context.Graphics.DecodeImage(stream);
            _icons.Add(name, icon);
        }
        if (icon == null) return;
        var bounds = new RectF(rect.X + (rect.Width - IconSize) / 2, rect.Y + (rect.Height - IconSize) / 2, IconSize, IconSize);
        context.DrawImage(icon, bounds, tint: Color.White);
    }

    public void Dispose()
    {
        foreach (var icon in _icons.Values) icon?.Dispose();
        _icons.Clear();
    }
}
