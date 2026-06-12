using Miko.Common;

namespace Miko.Ionic;

/// <summary>
/// Theme tokens for the Ionic component library. This is a focused subset matching the
/// Ionic Material Design (MD) defaults required for the tab layout; it will grow as more
/// components are ported. Values mirror <c>ionic.theme.default.scss</c> /
/// <c>ionic.theme.default.md.scss</c> from the Ionic framework source.
/// </summary>
public class IonicTheme
{
    // Brand colors (ionic.theme.default.scss)
    public Color Primary { get; set; }
    public Color Secondary { get; set; }
    public Color Tertiary { get; set; }
    public Color Success { get; set; }
    public Color Warning { get; set; }
    public Color Danger { get; set; }
    public Color Light { get; set; }
    public Color Medium { get; set; }
    public Color Dark { get; set; }

    // Surface / text
    public Color BackgroundColor { get; set; }
    public Color TextColor { get; set; }

    // Tab bar (ionic.theme.default.md.scss)
    public Color TabBarBackground { get; set; }
    public Color TabBarBorderColor { get; set; }
    public Color TabBarColor { get; set; }
    public Color TabBarColorSelected { get; set; }
    public float TabBarHeight { get; set; } = 56f;
    public float TabBarBorderWidth { get; set; } = 1f;

    // Tab button (tab-button.md.vars.scss)
    public float TabButtonFontSize { get; set; } = 12f;
    public float TabButtonIconSize { get; set; } = 22f;
    public float TabButtonMaxWidth { get; set; } = 168f;
    public float TabButtonPaddingX { get; set; } = 12f;

    /// <summary>
    /// Creates the default Ionic light theme (Material Design mode).
    /// </summary>
    public static IonicTheme CreateDefault() => new()
    {
        Primary = Color.FromHex("0054e9"),
        Secondary = Color.FromHex("0163aa"),
        Tertiary = Color.FromHex("6030ff"),
        Success = Color.FromHex("2dd55b"),
        Warning = Color.FromHex("ffc409"),
        Danger = Color.FromHex("c5000f"),
        Light = Color.FromHex("f4f5f8"),
        Medium = Color.FromHex("636469"),
        Dark = Color.FromHex("222428"),

        BackgroundColor = Color.FromHex("ffffff"),
        TextColor = Color.FromHex("000000"),

        // $tabbar-md-background: $background-color (#fff)
        TabBarBackground = Color.FromHex("ffffff"),
        // fallback rgba(0, 0, 0, .07)
        TabBarBorderColor = new Color(0, 0, 0, 18),
        // $tabbar-md-color: $text-color-step-350 (~#595959 in light mode)
        TabBarColor = Color.FromHex("595959"),
        // $tabbar-md-color-selected: ion-color(primary, base)
        TabBarColorSelected = Color.FromHex("0054e9"),
    };
}
