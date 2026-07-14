using Miko.Common;

namespace Miko.Bootstrap.Themes;

public static class LightTheme
{
    public static Theme Create() => new()
    {
        Blue = Color.FromHex("0d6efd"),
        Indigo = Color.FromHex("6610f2"),
        Purple = Color.FromHex("6f42c1"),
        Pink = Color.FromHex("d63384"),
        Red = Color.FromHex("dc3545"),
        Orange = Color.FromHex("fd7e14"),
        Yellow = Color.FromHex("ffc107"),
        Green = Color.FromHex("198754"),
        Teal = Color.FromHex("20c997"),
        Cyan = Color.FromHex("0dcaf0"),

        Primary = Color.FromHex("0d6efd"),
        Secondary = Color.FromHex("6c757d"),
        Success = Color.FromHex("198754"),
        Info = Color.FromHex("0dcaf0"),
        Warning = Color.FromHex("ffc107"),
        Danger = Color.FromHex("dc3545"),
        Light = Color.FromHex("f8f9fa"),
        Dark = Color.FromHex("212529"),

        Gray100 = Color.FromHex("f8f9fa"),
        Gray200 = Color.FromHex("e9ecef"),
        Gray300 = Color.FromHex("dee2e6"),
        Gray400 = Color.FromHex("ced4da"),
        Gray500 = Color.FromHex("adb5bd"),
        Gray600 = Color.FromHex("6c757d"),
        Gray700 = Color.FromHex("495057"),
        Gray800 = Color.FromHex("343a40"),
        Gray900 = Color.FromHex("212529"),

        PrimaryTextEmphasis = Color.FromHex("052c65"),
        SecondaryTextEmphasis = Color.FromHex("2b2f32"),
        SuccessTextEmphasis = Color.FromHex("0a3622"),
        InfoTextEmphasis = Color.FromHex("055160"),
        WarningTextEmphasis = Color.FromHex("664d03"),
        DangerTextEmphasis = Color.FromHex("58151c"),
        LightTextEmphasis = Color.FromHex("495057"),
        DarkTextEmphasis = Color.FromHex("495057"),

        PrimaryBgSubtle = Color.FromHex("cfe2ff"),
        SecondaryBgSubtle = Color.FromHex("e2e3e5"),
        SuccessBgSubtle = Color.FromHex("d1e7dd"),
        InfoBgSubtle = Color.FromHex("cff4fc"),
        WarningBgSubtle = Color.FromHex("fff3cd"),
        DangerBgSubtle = Color.FromHex("f8d7da"),
        LightBgSubtle = Color.FromHex("fcfcfd"),
        DarkBgSubtle = Color.FromHex("ced4da"),

        PrimaryBorderSubtle = Color.FromHex("9ec5fe"),
        SecondaryBorderSubtle = Color.FromHex("c4c8cb"),
        SuccessBorderSubtle = Color.FromHex("a3cfbb"),
        InfoBorderSubtle = Color.FromHex("9eeaf9"),
        WarningBorderSubtle = Color.FromHex("ffe69c"),
        DangerBorderSubtle = Color.FromHex("f1aeb5"),
        LightBorderSubtle = Color.FromHex("e9ecef"),
        DarkBorderSubtle = Color.FromHex("adb5bd"),

        BodyColor = Color.FromHex("212529"),
        BodyBg = Color.White,
        BodyFontFamily = "system-ui, Arial, sans-serif",
        BodyFontSize = 16f,
        BodyFontWeight = 400,
        BodyLineHeight = 1.5f,

        SecondaryColor = Color.FromRgba(33, 37, 41, 191),
        SecondaryBg = Color.FromHex("e9ecef"),
        TertiaryColor = Color.FromRgba(33, 37, 41, 128),
        TertiaryBg = Color.FromHex("f8f9fa"),
        EmphasisColor = Color.Black,

        LinkColor = Color.FromHex("0d6efd"),
        LinkHoverColor = Color.FromHex("0a58ca"),

        BorderWidth = 1f,
        BorderColor = Color.FromHex("dee2e6"),
        BorderRadius = 6f,
        BorderRadiusSm = 4f,
        BorderRadiusLg = 8f,
        BorderRadiusXl = 16f,
        BorderRadiusXxl = 32f,
        BorderRadiusPill = 800f,

        HeadingColor = Color.FromHex("212529"),

        FormValidColor = Color.FromHex("198754"),
        FormValidBorderColor = Color.FromHex("198754"),
        FormInvalidColor = Color.FromHex("dc3545"),
        FormInvalidBorderColor = Color.FromHex("dc3545"),
    };
}
