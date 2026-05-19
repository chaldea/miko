namespace Miko.Common;

public record struct BoxShadow(float OffsetX, float OffsetY, float BlurRadius, float SpreadRadius, Color Color, bool Inset = false)
{
    public BoxShadow(Length offsetX, Length offsetY, Length blurRadius, Length spreadRadius, Color color, bool inset = false)
        : this(offsetX.ToPixels(0), offsetY.ToPixels(0), blurRadius.ToPixels(0), spreadRadius.ToPixels(0), color, inset) { }
}
