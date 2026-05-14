namespace Miko.Common;

public record struct BoxShadow(float OffsetX, float OffsetY, float BlurRadius, float SpreadRadius, Color Color, bool Inset = false);
