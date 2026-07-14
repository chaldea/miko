using Miko.Common;

namespace Miko.Bootstrap;

public class Theme
{
    // Base colors
    public Color Blue { get; set; }
    public Color Indigo { get; set; }
    public Color Purple { get; set; }
    public Color Pink { get; set; }
    public Color Red { get; set; }
    public Color Orange { get; set; }
    public Color Yellow { get; set; }
    public Color Green { get; set; }
    public Color Teal { get; set; }
    public Color Cyan { get; set; }
    public Color Black { get; set; }
    public Color White { get; set; }
    public Color Gray { get; set; }
    public Color GrayDark { get; set; }
    public Color Gray100 { get; set; }
    public Color Gray200 { get; set; }
    public Color Gray300 { get; set; }
    public Color Gray400 { get; set; }
    public Color Gray500 { get; set; }
    public Color Gray600 { get; set; }
    public Color Gray700 { get; set; }
    public Color Gray800 { get; set; }
    public Color Gray900 { get; set; }

    // Semantic colors
    public Color Primary { get; set; }
    public Color Secondary { get; set; }
    public Color Success { get; set; }
    public Color Info { get; set; }
    public Color Warning { get; set; }
    public Color Danger { get; set; }
    public Color Light { get; set; }
    public Color Dark { get; set; }

    // Text emphasis
    public Color PrimaryTextEmphasis { get; set; }
    public Color SecondaryTextEmphasis { get; set; }
    public Color SuccessTextEmphasis { get; set; }
    public Color InfoTextEmphasis { get; set; }
    public Color WarningTextEmphasis { get; set; }
    public Color DangerTextEmphasis { get; set; }
    public Color LightTextEmphasis { get; set; }
    public Color DarkTextEmphasis { get; set; }

    // Subtle backgrounds
    public Color PrimaryBgSubtle { get; set; }
    public Color SecondaryBgSubtle { get; set; }
    public Color SuccessBgSubtle { get; set; }
    public Color InfoBgSubtle { get; set; }
    public Color WarningBgSubtle { get; set; }
    public Color DangerBgSubtle { get; set; }
    public Color LightBgSubtle { get; set; }
    public Color DarkBgSubtle { get; set; }

    // Subtle borders
    public Color PrimaryBorderSubtle { get; set; }
    public Color SecondaryBorderSubtle { get; set; }
    public Color SuccessBorderSubtle { get; set; }
    public Color InfoBorderSubtle { get; set; }
    public Color WarningBorderSubtle { get; set; }
    public Color DangerBorderSubtle { get; set; }
    public Color LightBorderSubtle { get; set; }
    public Color DarkBorderSubtle { get; set; }

    // Body
    public Color BodyColor { get; set; }
    public Color BodyBg { get; set; }
    public string BodyFontFamily { get; set; } = "Arial";
    public float BodyFontSize { get; set; } = 16f;
    public int BodyFontWeight { get; set; } = 400;
    public float BodyLineHeight { get; set; } = 1.5f;

    // Secondary/Tertiary
    public Color EmphasisColor { get; set; }
    public Color SecondaryColor { get; set; }
    public Color SecondaryBg { get; set; }
    public Color TertiaryColor { get; set; }
    public Color TertiaryBg { get; set; }

    // Heading
    public Color HeadingColor { get; set; }

    // Links
    public Color LinkColor { get; set; }
    public TextDecoration LinkDecoration { get; set; } = TextDecoration.Underline;
    public Color LinkHoverColor { get; set; }

    // Code
    public Color CodeColor { get; set; }
    public Color HighlightColor { get; set; }
    public Color HighlightBg { get; set; }

    // Borders
    public float BorderWidth { get; set; } = 1f;
    public BorderStyle BorderStyle { get; set; } = BorderStyle.Solid;
    public Color BorderColor { get; set; }
    public Color BorderColorTranslucent { get; set; }
    public float BorderRadius { get; set; } = 6f;
    public float BorderRadiusSm { get; set; } = 4f;
    public float BorderRadiusLg { get; set; } = 8f;
    public float BorderRadiusXl { get; set; } = 16f;
    public float BorderRadiusXxl { get; set; } = 32f;
    public float BorderRadius2xl { get; set; } = 32f;
    public float BorderRadiusPill { get; set; } = 800f;

    // Box Shadow
    public BoxShadow BoxShadow { get; set; }
    public BoxShadow BoxShadowSm { get; set; }
    public BoxShadow BoxShadowLg { get; set; }
    public BoxShadow BoxShadowInset { get; set; }

    // Focus
    public float FocusRingWidth { get; set; }
    public float FocusRingOpacity { get; set; }
    public float FocusRingColor { get; set; }

    // Form validation
    public Color FormValidColor { get; set; }
    public Color FormValidBorderColor { get; set; }
    public Color FormInvalidColor { get; set; }
    public Color FormInvalidBorderColor { get; set; }
}
