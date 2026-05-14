using SkiaSharp;

namespace Miko.Common;

/// <summary>
/// 颜色类
/// </summary>
public struct Color
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; }

    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public SKColor ToSKColor() => new SKColor(R, G, B, A);

    public static Color FromRgb(byte r, byte g, byte b) => new Color(r, g, b);
    public static Color FromRgba(byte r, byte g, byte b, byte a) => new Color(r, g, b, a);
    public static Color FromHex(string hex)
    {
        hex = hex.TrimStart('#');

        if (hex.Length == 3)
            hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
        else if (hex.Length == 4)
            hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}{hex[3]}{hex[3]}";

        return hex.Length switch
        {
            6 => new Color(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16)),
            8 => new Color(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16),
                Convert.ToByte(hex.Substring(6, 2), 16)),
            _ => throw new ArgumentException("Invalid hex color format")
        };
    }

    // 预定义颜色
    public static Color Transparent => new Color(0, 0, 0, 0);
    public static Color White => new Color(255, 255, 255);
    public static Color Black => new Color(0, 0, 0);
    public static Color Red => new Color(255, 0, 0);
    public static Color Green => new Color(0, 255, 0);
    public static Color Blue => new Color(0, 0, 255);
    public static Color Yellow => new Color(255, 255, 0);
    public static Color Cyan => new Color(0, 255, 255);
    public static Color Magenta => new Color(255, 0, 255);
    public static Color Gray => new Color(128, 128, 128);
    public static Color LightGray => new Color(211, 211, 211);
    public static Color DarkGray => new Color(169, 169, 169);

    public override string ToString() => $"rgba({R}, {G}, {B}, {A})";
}
