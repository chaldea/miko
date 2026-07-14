using Miko.Common;

namespace Miko.Bootstrap.Themes;

public static class DarkTheme
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

        PrimaryTextEmphasis = Color.FromHex("6ea8fe"),
        SecondaryTextEmphasis = Color.FromHex("a7acb1"),
        SuccessTextEmphasis = Color.FromHex("75b798"),
        InfoTextEmphasis = Color.FromHex("6edff6"),
        WarningTextEmphasis = Color.FromHex("ffda6a"),
        DangerTextEmphasis = Color.FromHex("ea868f"),
        LightTextEmphasis = Color.FromHex("f8f9fa"),
        DarkTextEmphasis = Color.FromHex("dee2e6"),

        PrimaryBgSubtle = Color.FromHex("031633"),
        SecondaryBgSubtle = Color.FromHex("161719"),
        SuccessBgSubtle = Color.FromHex("051b11"),
        InfoBgSubtle = Color.FromHex("032830"),
        WarningBgSubtle = Color.FromHex("332701"),
        DangerBgSubtle = Color.FromHex("2c0b0e"),
        LightBgSubtle = Color.FromHex("343a40"),
        DarkBgSubtle = Color.FromHex("1a1d20"),

        PrimaryBorderSubtle = Color.FromHex("084298"),
        SecondaryBorderSubtle = Color.FromHex("41464b"),
        SuccessBorderSubtle = Color.FromHex("0f5132"),
        InfoBorderSubtle = Color.FromHex("087990"),
        WarningBorderSubtle = Color.FromHex("997404"),
        DangerBorderSubtle = Color.FromHex("842029"),
        LightBorderSubtle = Color.FromHex("495057"),
        DarkBorderSubtle = Color.FromHex("343a40"),

        BodyColor = Color.FromHex("dee2e6"),
        BodyBg = Color.FromHex("212529"),
        BodyFontFamily = "system-ui, Arial, sans-serif",
        BodyFontSize = 16f,
        BodyFontWeight = 400,
        BodyLineHeight = 1.5f,

        SecondaryColor = Color.FromRgba(222, 226, 230, 191),
        SecondaryBg = Color.FromHex("343a40"),
        TertiaryColor = Color.FromRgba(222, 226, 230, 128),
        TertiaryBg = Color.FromHex("2b3035"),
        EmphasisColor = Color.White,

        LinkColor = Color.FromHex("6ea8fe"),
        LinkHoverColor = Color.FromHex("8bb9fe"),

        BorderWidth = 1f,
        BorderColor = Color.FromHex("495057"),
        BorderRadius = 6f,
        BorderRadiusSm = 4f,
        BorderRadiusLg = 8f,
        BorderRadiusXl = 16f,
        BorderRadiusXxl = 32f,
        BorderRadiusPill = 800f,

        HeadingColor = Color.FromHex("dee2e6"),

        FormValidColor = Color.FromHex("75b798"),
        FormValidBorderColor = Color.FromHex("75b798"),
        FormInvalidColor = Color.FromHex("ea868f"),
        FormInvalidBorderColor = Color.FromHex("ea868f"),
    };
}
