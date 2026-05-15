using Miko.Common;

namespace Miko.Animation;

public abstract record TransformFunction
{
    public record Translate(Length X, Length Y) : TransformFunction;
    public record TranslateX(Length X) : TransformFunction;
    public record TranslateY(Length Y) : TransformFunction;
    public record Scale(float X, float Y) : TransformFunction;
    public record ScaleX(float X) : TransformFunction;
    public record ScaleY(float Y) : TransformFunction;
    public record Rotate(float Degrees) : TransformFunction;
    public record SkewX(float Degrees) : TransformFunction;
    public record SkewY(float Degrees) : TransformFunction;
    public record Skew(float DegreesX, float DegreesY) : TransformFunction;
    public record Matrix(float A, float B, float C, float D, float Tx, float Ty) : TransformFunction;
}

public record struct TransformOrigin(Length X, Length Y)
{
    public static TransformOrigin Center => new(Length.Percent(50), Length.Percent(50));
    public static TransformOrigin TopLeft => new(Length.Percent(0), Length.Percent(0));
    public static TransformOrigin TopRight => new(Length.Percent(100), Length.Percent(0));
    public static TransformOrigin BottomLeft => new(Length.Percent(0), Length.Percent(100));
    public static TransformOrigin BottomRight => new(Length.Percent(100), Length.Percent(100));
}

public class Transform
{
    public List<TransformFunction> Functions { get; set; } = new();

    public Transform() { }

    public Transform(params TransformFunction[] functions)
    {
        Functions.AddRange(functions);
    }

    public static Transform None => new();

    public static Transform FromTranslate(Length x, Length y)
        => new(new TransformFunction.Translate(x, y));

    public static Transform FromScale(float x, float y)
        => new(new TransformFunction.Scale(x, y));

    public static Transform FromRotate(float degrees)
        => new(new TransformFunction.Rotate(degrees));
}
