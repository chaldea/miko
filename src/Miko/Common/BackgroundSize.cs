namespace Miko.Common;

public readonly struct BackgroundSize : IEquatable<BackgroundSize>
{
    public BackgroundSizeMode Mode { get; }
    public Length Width { get; }
    public Length Height { get; }

    private BackgroundSize(BackgroundSizeMode mode, Length width = default, Length height = default)
    {
        Mode = mode;
        Width = width;
        Height = height;
    }

    public static BackgroundSize Auto => new(BackgroundSizeMode.Auto);
    public static BackgroundSize Cover => new(BackgroundSizeMode.Cover);
    public static BackgroundSize Contain => new(BackgroundSizeMode.Contain);
    public static BackgroundSize Px(float width, float height) => new(BackgroundSizeMode.Explicit, Length.Px(width), Length.Px(height));
    public static BackgroundSize Px(float size) => new(BackgroundSizeMode.Explicit, Length.Px(size), Length.Px(size));
    public static BackgroundSize From(Length width, Length height) => new(BackgroundSizeMode.Explicit, width, height);
    public static BackgroundSize From(Length size) => new(BackgroundSizeMode.Explicit, size, size);

    public float ResolveWidth(float containerWidth, float imageWidth) =>
        Mode == BackgroundSizeMode.Explicit ? Width.ToPixels(containerWidth) : imageWidth;

    public float ResolveHeight(float containerHeight, float imageHeight) =>
        Mode == BackgroundSizeMode.Explicit ? Height.ToPixels(containerHeight) : imageHeight;

    public bool Equals(BackgroundSize other) =>
        Mode == other.Mode && Width.Equals(other.Width) && Height.Equals(other.Height);

    public override bool Equals(object? obj) => obj is BackgroundSize other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Mode, Width, Height);
    public static bool operator ==(BackgroundSize left, BackgroundSize right) => left.Equals(right);
    public static bool operator !=(BackgroundSize left, BackgroundSize right) => !left.Equals(right);
}
