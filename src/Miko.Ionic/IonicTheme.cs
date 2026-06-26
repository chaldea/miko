using Miko.Common;

namespace Miko.Ionic;

/// <summary>
/// Ionic visual mode. Ionic ships two design languages — Material Design (used on Android
/// and the web) and iOS. The active mode is supplied by the platform and defaults to
/// <see cref="Md"/>.
/// </summary>
public enum IonicMode
{
    /// <summary>Material Design mode (default).</summary>
    Md,
    /// <summary>iOS / Cupertino mode.</summary>
    Ios,
}

/// <summary>
/// Theme tokens for the Ionic component library. Carries the active <see cref="IonicMode"/>
/// and the mode-specific values needed by the ported components. Values mirror the Ionic
/// framework source (<c>ionic.theme.default.scss</c> plus the per-mode
/// <c>*.md.scss</c> / <c>*.ios.scss</c> variable files).
/// </summary>
public class IonicTheme
{
    /// <summary>The design mode this theme was built for.</summary>
    public IonicMode Mode { get; set; } = IonicMode.Md;

    // Brand colors (ionic.theme.default.scss) — shared across modes.
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

    // Tab bar
    public Color TabBarBackground { get; set; }
    public Color TabBarBorderColor { get; set; }
    public Color TabBarColor { get; set; }
    public Color TabBarColorSelected { get; set; }
    public float TabBarHeight { get; set; } = 56f;
    public float TabBarBorderWidth { get; set; } = 1f;

    // Tab button
    public float TabButtonFontSize { get; set; } = 12f;
    public float TabButtonIconSize { get; set; } = 22f;
    public float TabButtonMaxWidth { get; set; } = 168f;
    public float TabButtonPaddingX { get; set; } = 12f;

    // Toolbar
    public Color ToolbarBackground { get; set; }
    public Color ToolbarColor { get; set; }
    public float ToolbarMinHeight { get; set; } = 56f;

    // Title
    public float TitleFontSize { get; set; } = 20f;
    public FontWeight TitleFontWeight { get; set; } = FontWeight.Medium;
    public float TitlePaddingX { get; set; } = 20f;
    public TextAlign TitleTextAlign { get; set; } = TextAlign.Left;

    // Header — MD uses a 3-layer elevation shadow; iOS uses a hairline bottom border.
    public List<BoxShadow> HeaderBoxShadow { get; set; } = new();
    public Color HeaderBorderColor { get; set; }
    public float HeaderBorderWidth { get; set; }

    // Content
    public Color ContentBackground { get; set; }
    public Color ContentColor { get; set; }

    // App shell (sidemenu layout root)
    public Color AppBackground { get; set; }

    // Menu (side drawer) — MD lifts the drawer with an elevation shadow; iOS uses a
    // hairline trailing border. The backdrop dims the page behind an open overlay menu.
    public float MenuWidth { get; set; } = 304f;
    public Color MenuBackground { get; set; }
    public List<BoxShadow> MenuBoxShadow { get; set; } = new();
    public Color MenuBorderColor { get; set; }
    public float MenuBorderWidth { get; set; }
    public Color BackdropColor { get; set; }
    /// <summary>Drawer slide / backdrop fade duration in seconds (Ionic menu is ~280ms).</summary>
    public float MenuAnimDuration { get; set; } = 0.28f;

    // List / list header / item
    public Color ListBackground { get; set; }
    public Color ListHeaderColor { get; set; }
    public float ListHeaderFontSize { get; set; } = 16f;
    public float ItemMinHeight { get; set; } = 48f;
    public Color ItemColor { get; set; }
    public Color ItemBorderColor { get; set; }
    public float ItemPaddingStart { get; set; } = 16f;

    // Segment
    public Color SegmentBackground { get; set; }
    public Color SegmentButtonColor { get; set; }
    /// <summary>Text color of the checked button. MD turns the label the primary color; iOS keeps
    /// the default dark text (the pill behind it provides the contrast).</summary>
    public Color SegmentButtonCheckedColor { get; set; }
    /// <summary>Color of the checked indicator. MD: the 2px primary underline bar. iOS: the light
    /// elevated pill surface that slides behind the label.</summary>
    public Color SegmentIndicatorColor { get; set; }
    /// <summary>Indicator height. MD: 2px (a bottom underline bar). iOS: 100% (a full-height pill).</summary>
    public Length SegmentIndicatorHeight { get; set; } = Length.Px(2);
    /// <summary>Indicator corner radius. MD: 0 (square bar). iOS: 7px (rounded pill).</summary>
    public float SegmentIndicatorBorderRadius { get; set; }
    /// <summary>Indicator elevation. MD: none. iOS: a soft drop shadow under the pill.</summary>
    public List<BoxShadow> SegmentIndicatorBoxShadow { get; set; } = new();
    public float SegmentButtonFontSize { get; set; } = 12f;
    public float SegmentButtonMaxWidth { get; set; } = 168f;
    public float SegmentButtonMinHeight { get; set; } = 48f;
    public float SegmentButtonPaddingX { get; set; } = 8f;
    /// <summary>Top/bottom padding of a segment button. 0 in Ionic (both md and ios):
    /// <c>$segment-button-md-padding-top/-bottom</c> and the ios <c>--padding-top/-bottom</c>.
    /// The button's height comes from <see cref="SegmentButtonMinHeight"/>, not vertical padding.</summary>
    public float SegmentButtonPaddingY { get; set; } = 0f;

    // Brand palette shared by both modes (ionic.theme.default.scss).
    private static void ApplyBrandColors(IonicTheme t)
    {
        t.Primary = Color.FromHex("0054e9");
        t.Secondary = Color.FromHex("0163aa");
        t.Tertiary = Color.FromHex("6030ff");
        t.Success = Color.FromHex("2dd55b");
        t.Warning = Color.FromHex("ffc409");
        t.Danger = Color.FromHex("c5000f");
        t.Light = Color.FromHex("f4f5f8");
        t.Medium = Color.FromHex("636469");
        t.Dark = Color.FromHex("222428");
        t.BackgroundColor = Color.FromHex("ffffff");
        t.TextColor = Color.FromHex("000000");
        t.ContentColor = Color.FromHex("000000");
    }

    /// <summary>
    /// Builds the default light theme for the given mode. Defaults to Material Design.
    /// </summary>
    public static IonicTheme Create(IonicMode mode = IonicMode.Md) =>
        mode == IonicMode.Ios ? CreateIos() : CreateMd();

    /// <summary>Backwards-compatible alias for the Material Design theme.</summary>
    public static IonicTheme CreateDefault() => CreateMd();

    /// <summary>
    /// Material Design light theme. Values from <c>*.md.scss</c> /
    /// <c>ionic.theme.default.md.scss</c>.
    /// </summary>
    public static IonicTheme CreateMd()
    {
        var t = new IonicTheme { Mode = IonicMode.Md };
        ApplyBrandColors(t);

        // Tab bar (tab-bar.md.scss): height 56, bg #fff, color step-350 (~#595959).
        t.TabBarBackground = Color.FromHex("ffffff");
        t.TabBarBorderColor = new Color(0, 0, 0, 18);          // rgba(0,0,0,.07)
        t.TabBarColor = Color.FromHex("595959");
        t.TabBarColorSelected = t.Primary;
        t.TabBarHeight = 56f;

        // Tab button (tab-button.md.vars.scss): font 12, icon 22, max 168, padding-x 12.
        t.TabButtonFontSize = 12f;
        t.TabButtonIconSize = 22f;
        t.TabButtonMaxWidth = 168f;
        t.TabButtonPaddingX = 12f;

        // Toolbar (toolbar.md.scss): min-height 56, bg #fff, color #424242.
        t.ToolbarBackground = Color.FromHex("ffffff");
        t.ToolbarColor = Color.FromHex("424242");
        t.ToolbarMinHeight = 56f;

        // Title (title.md.scss): 20px / 500, left-aligned, padding 0 20px.
        t.TitleFontSize = 20f;
        t.TitleFontWeight = FontWeight.Medium;
        t.TitlePaddingX = 20f;
        t.TitleTextAlign = TextAlign.Left;

        // Header (header.md.vars.scss): 3-layer elevation shadow, no border.
        t.HeaderBoxShadow = new List<BoxShadow>
        {
            new BoxShadow(0, 2, 4, -1, new Color(0, 0, 0, 51)),  // 0.2 * 255
            new BoxShadow(0, 4, 5, 0, new Color(0, 0, 0, 36)),   // 0.14 * 255
            new BoxShadow(0, 1, 10, 0, new Color(0, 0, 0, 31)),  // 0.12 * 255
        };
        t.HeaderBorderWidth = 0f;

        t.ContentBackground = Color.FromHex("ffffff");

        // App / sidemenu (menu.md.scss): drawer width 304, white surface, 4dp elevation
        // shadow; backdrop rgba(0,0,0,.32). List/item from list.md / item.md.
        t.AppBackground = Color.FromHex("ffffff");
        t.MenuWidth = 304f;
        t.MenuBackground = Color.FromHex("ffffff");
        t.MenuBoxShadow = new List<BoxShadow>
        {
            new BoxShadow(0, 2, 4, -1, new Color(0, 0, 0, 51)),  // 0.2 * 255
            new BoxShadow(0, 4, 5, 0, new Color(0, 0, 0, 36)),   // 0.14 * 255
            new BoxShadow(0, 1, 10, 0, new Color(0, 0, 0, 31)),  // 0.12 * 255
        };
        t.MenuBorderWidth = 0f;
        t.BackdropColor = new Color(0, 0, 0, 82);                // rgba(0,0,0,.32)

        t.ListBackground = Color.FromHex("ffffff");
        t.ListHeaderColor = Color.FromHex("595959");             // step-350
        t.ListHeaderFontSize = 16f;
        t.ItemMinHeight = 48f;
        t.ItemColor = Color.FromHex("000000");
        t.ItemBorderColor = new Color(0, 0, 0, 18);              // rgba(0,0,0,.07)
        t.ItemPaddingStart = 16f;

        // Segment (segment.md.scss / segment-button.md.scss): translucent track; the checked
        // button shows a 2px primary underline bar (the indicator) and its label turns primary —
        // the button background stays transparent (md does NOT fill the whole button).
        t.SegmentBackground = new Color(0, 0, 0, 13);            // rgba(0,0,0,.05) light track
        t.SegmentButtonColor = Color.FromHex("595959");          // step-350
        t.SegmentButtonCheckedColor = t.Primary;                 // --color-checked: primary
        t.SegmentIndicatorColor = t.Primary;                     // --indicator-color: color-checked
        t.SegmentIndicatorHeight = Length.Px(2);                 // --indicator-height: 2px
        t.SegmentIndicatorBorderRadius = 0f;                     // square underline bar
        t.SegmentIndicatorBoxShadow = new();                     // --indicator-box-shadow: none
        t.SegmentButtonFontSize = 12f;
        t.SegmentButtonMaxWidth = 168f;
        t.SegmentButtonMinHeight = 48f;                            // md $segment-button-md-min-height
        t.SegmentButtonPaddingX = 8f;
        t.SegmentButtonPaddingY = 0f;                            // md padding-top/bottom: 0

        return t;
    }

    /// <summary>
    /// iOS light theme. Values from <c>*.ios.scss</c> /
    /// <c>ionic.theme.default.ios.scss</c>.
    /// </summary>
    public static IonicTheme CreateIos()
    {
        var t = new IonicTheme { Mode = IonicMode.Ios };
        ApplyBrandColors(t);

        // Tab bar (tab-bar.ios): bg #f7f7f7 (step-50), color step-400 (~#737373),
        // hairline border rgba(0,0,0,.2). iOS tab bar is ~50px tall.
        t.TabBarBackground = Color.FromHex("f7f7f7");
        t.TabBarBorderColor = new Color(0, 0, 0, 51);           // rgba(0,0,0,.2)
        t.TabBarColor = Color.FromHex("737373");
        t.TabBarColorSelected = t.Primary;
        t.TabBarHeight = 50f;

        // Tab button (tab-button.ios.vars): font 10, icon 24, max 240, padding-x 2.
        t.TabButtonFontSize = 10f;
        t.TabButtonIconSize = 24f;
        t.TabButtonMaxWidth = 240f;
        t.TabButtonPaddingX = 2f;

        // Toolbar (toolbar.ios): min-height 44, bg #f7f7f7, color #000.
        t.ToolbarBackground = Color.FromHex("f7f7f7");
        t.ToolbarColor = Color.FromHex("000000");
        t.ToolbarMinHeight = 44f;

        // Title (title.ios): 17px / 600, centered, padding 0 90px (clamped here).
        t.TitleFontSize = 17f;
        t.TitleFontWeight = FontWeight.SemiBold;
        t.TitlePaddingX = 16f;
        t.TitleTextAlign = TextAlign.Center;

        // Header (header.ios): hairline bottom border, no elevation shadow.
        t.HeaderBoxShadow = new List<BoxShadow>();
        t.HeaderBorderColor = new Color(0, 0, 0, 51);           // rgba(0,0,0,.2)
        t.HeaderBorderWidth = 0.55f;                            // ~hairline

        t.ContentBackground = Color.FromHex("ffffff");

        // App / sidemenu (menu.ios.scss): drawer width 270, hairline trailing border
        // instead of an elevation shadow; backdrop rgba(0,0,0,.4). List/item from
        // list.ios / item.ios (44px rows).
        t.AppBackground = Color.FromHex("ffffff");
        t.MenuWidth = 270f;
        t.MenuBackground = Color.FromHex("ffffff");
        t.MenuBoxShadow = new List<BoxShadow>();
        t.MenuBorderColor = new Color(0, 0, 0, 51);             // rgba(0,0,0,.2)
        t.MenuBorderWidth = 0.55f;                              // ~hairline
        t.BackdropColor = new Color(0, 0, 0, 102);              // rgba(0,0,0,.4)

        t.ListBackground = Color.FromHex("ffffff");
        t.ListHeaderColor = Color.FromHex("737373");            // step-400
        t.ListHeaderFontSize = 17f;
        t.ItemMinHeight = 44f;
        t.ItemColor = Color.FromHex("000000");
        t.ItemBorderColor = new Color(0, 0, 0, 51);             // rgba(0,0,0,.2)
        t.ItemPaddingStart = 16f;

        // Segment (segment.ios.scss / segment-button.ios.scss): translucent track; the checked
        // button shows a full-height light rounded pill (the indicator) with a soft shadow sliding
        // behind the label. Unlike md, the label color stays the default dark text — the pill
        // provides the contrast, so the button background itself stays transparent.
        t.SegmentBackground = new Color(0, 0, 0, 13);            // rgba(0,0,0,.05)
        t.SegmentButtonColor = Color.FromHex("000000");          // iOS text color (dark)
        t.SegmentButtonCheckedColor = Color.FromHex("000000");   // checked label stays dark
        t.SegmentIndicatorColor = Color.FromHex("ffffff");       // light elevated pill surface
        t.SegmentIndicatorHeight = Length.Percent(100);          // --indicator-height: 100%
        t.SegmentIndicatorBorderRadius = 7f;                     // --border-radius: 7px (rounded pill)
        t.SegmentIndicatorBoxShadow = new()                      // 0 0 5px rgba(0,0,0,.16)
        {
            new BoxShadow(Length.Px(0), Length.Px(0), Length.Px(5), Length.Px(0), new Color(0, 0, 0, 41)),
        };
        t.SegmentButtonFontSize = 13f;                           // iOS slightly larger
        t.SegmentButtonMaxWidth = 240f;                          // iOS wider max
        t.SegmentButtonMinHeight = 28f;                          // iOS slightly shorter
        t.SegmentButtonPaddingX = 6f;
        t.SegmentButtonPaddingY = 0f;                            // iOS padding-top/bottom: 0

        return t;
    }
}
