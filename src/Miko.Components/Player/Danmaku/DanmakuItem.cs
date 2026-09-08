using SkiaSharp;

namespace Miko.Components.Player.Danmaku;

public enum DanmakuMode { Scroll, Top, Bottom }

public sealed record DanmakuItem(TimeSpan Time, string Text, DanmakuMode Mode = DanmakuMode.Scroll, SKColor? Color = null);

public sealed record DanmakuSettings
{
    public bool Enabled { get; init; } = true;
    public float Opacity { get; init; } = .9f;
    public float FontSize { get; init; } = 22;
    public float Duration { get; init; } = 8;
    public float Area { get; init; } = .7f;
    public int MaxVisible { get; init; } = 80;

    internal DanmakuSettings Validate()
    {
        if (!float.IsFinite(Opacity) || Opacity is < 0 or > 1 ||
            !float.IsFinite(FontSize) || FontSize is < 12 or > 48 ||
            !float.IsFinite(Duration) || Duration is < 2 or > 20 ||
            !float.IsFinite(Area) || Area is < .2f or > 1 || MaxVisible is < 1 or > 500)
            throw new ArgumentOutOfRangeException(nameof(DanmakuSettings));
        return this;
    }
}

public readonly record struct VisibleDanmaku(DanmakuItem Item, float X, float Y, float Width);
