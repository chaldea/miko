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

    // Item divider (item-divider.md.scss / .ios.scss) — a section header row sitting between
    // item groups. MD uses a light gray fill with a bottom border and medium text; iOS uses a
    // lighter fill (step-50) with a hairline border.
    public Color ItemDividerBackground { get; set; }
    public Color ItemDividerColor { get; set; }
    public float ItemDividerMinHeight { get; set; } = 30f;

    // Item option (item-option.scss) — a swipe-reveal action button. The label is white (it sits
    // on a filled brand surface) and the default fill is the primary color.
    public Color ItemOptionColor { get; set; }
    public Color ItemOptionBackground { get; set; }

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

    // Button (button.scss / button.md.scss / button.ios.scss)
    /// <summary>Solid button fill — Ionic's default <c>--background</c> = <c>ion-color(primary, base)</c>.</summary>
    public Color ButtonSolidBackground { get; set; }
    /// <summary>Solid button label — <c>--color</c> = <c>ion-color(primary, contrast)</c> (white).</summary>
    public Color ButtonSolidColor { get; set; }
    /// <summary>Outline/clear button label and border — <c>--color</c> = <c>ion-color(primary, base)</c>.</summary>
    public Color ButtonTextColor { get; set; }
    /// <summary>Default corner radius (md 4px, ios 14px).</summary>
    public float ButtonBorderRadius { get; set; } = 4f;
    /// <summary>Round button radius — <c>$button-round-border-radius</c> (999px, both modes).</summary>
    public float ButtonRoundBorderRadius { get; set; } = 999f;
    /// <summary>Host min-height (md 36px, ios 3.1em).</summary>
    public Length ButtonMinHeight { get; set; } = Length.Px(36);
    public Length ButtonPaddingTop { get; set; } = Length.Px(8);
    public Length ButtonPaddingBottom { get; set; } = Length.Px(8);
    public Length ButtonPaddingStart { get; set; } = Length.Em(1.1f);
    public Length ButtonPaddingEnd { get; set; } = Length.Em(1.1f);
    public float ButtonFontSize { get; set; } = 14f;
    public FontWeight ButtonFontWeight { get; set; } = FontWeight.Medium;
    /// <summary>md uppercases the label; ios leaves it as-is.</summary>
    public TextTransform ButtonTextTransform { get; set; } = TextTransform.None;
    public Length ButtonLetterSpacing { get; set; } = Length.Px(0);
    /// <summary>Strong (heavier weight) button — md bold, ios 600.</summary>
    public FontWeight ButtonStrongFontWeight { get; set; } = FontWeight.Bold;
    /// <summary>Solid button elevation — md a 3-layer shadow; ios none.</summary>
    public List<BoxShadow> ButtonSolidBoxShadow { get; set; } = new();
    /// <summary>Outline border width — md 2px, ios 1px.</summary>
    public Length ButtonOutlineBorderWidth { get; set; } = Length.Px(1);
    // Large size
    public Length ButtonLargeMinHeight { get; set; } = Length.Em(2.8f);
    public Length ButtonLargePaddingTop { get; set; } = Length.Px(14);
    public Length ButtonLargePaddingBottom { get; set; } = Length.Px(14);
    public Length ButtonLargePaddingX { get; set; } = Length.Em(1f);
    public float ButtonLargeFontSize { get; set; } = 20f;
    public float ButtonLargeBorderRadius { get; set; } = 4f;
    // Small size
    public Length ButtonSmallMinHeight { get; set; } = Length.Em(2.1f);
    public Length ButtonSmallPaddingTop { get; set; } = Length.Px(4);
    public Length ButtonSmallPaddingBottom { get; set; } = Length.Px(4);
    public Length ButtonSmallPaddingX { get; set; } = Length.Em(0.9f);
    public float ButtonSmallFontSize { get; set; } = 13f;
    public float ButtonSmallBorderRadius { get; set; } = 4f;
    /// <summary>Icon-only square side (the clamp midpoint of Ionic's min-width/min-height).</summary>
    public float ButtonIconOnlyMinSize { get; set; } = 40f;
    /// <summary>Icon box size for the <c>icon-only</c> slot at default size
    /// (Ionic's <c>::slotted(ion-icon[slot="icon-only"])</c> font-size).</summary>
    public float ButtonIconOnlyIconSize { get; set; } = 22.4f;
    /// <summary>Icon-only square side for the small size variant.</summary>
    public float ButtonSmallIconOnlyMinSize { get; set; } = 28f;
    /// <summary>Icon box size for a small icon-only button.</summary>
    public float ButtonSmallIconOnlyIconSize { get; set; } = 16f;
    /// <summary>Icon-only square side for the large size variant.</summary>
    public float ButtonLargeIconOnlyMinSize { get; set; } = 50f;
    /// <summary>Icon box size for a large icon-only button.</summary>
    public float ButtonLargeIconOnlyIconSize { get; set; } = 28f;

    // Searchbar (searchbar.scss / searchbar.md.scss / searchbar.ios.scss + their *.vars.scss).
    // The host is a full-width flex row wrapping an input container; the input is a rounded pill
    // (md: flat 2px radius with an elevation shadow + left search icon; ios: 10px radius, translucent
    // fill, centered search icon). A trailing clear button shows when there is a value; a cancel
    // button (md icon / ios text) is off by default.
    /// <summary>Host padding (md 8px all round; ios 12px all round).</summary>
    public float SearchbarPaddingTop { get; set; } = 8f;
    public float SearchbarPaddingEnd { get; set; } = 8f;
    public float SearchbarPaddingBottom { get; set; } = 8f;
    public float SearchbarPaddingStart { get; set; } = 8f;
    /// <summary>Input background (md #fff solid; ios rgba(text,.07) translucent).</summary>
    public Color SearchbarInputBackground { get; set; }
    /// <summary>Input corner radius (md 2px; ios 10px).</summary>
    public float SearchbarInputBorderRadius { get; set; } = 2f;
    /// <summary>Input elevation shadow (md 3-layer; ios none).</summary>
    public List<BoxShadow> SearchbarInputBoxShadow { get; set; } = new();
    /// <summary>Input text color (md step-150; ios text-color).</summary>
    public Color SearchbarInputTextColor { get; set; }
    /// <summary>Input font size (md 16px; ios 17px).</summary>
    public float SearchbarInputFontSize { get; set; } = 16f;
    /// <summary>Input height (md auto) / line-height (md 30px). ios uses 100% height.</summary>
    public Length SearchbarInputHeight { get; set; } = Length.Auto;
    public Length SearchbarInputLineHeight { get; set; } = Length.Px(30);
    /// <summary>iOS input min-height (36px); md has no min-height.</summary>
    public float SearchbarInputMinHeight { get; set; }
    /// <summary>Search icon color (step-400) and size (md 21px; ios 22px).</summary>
    public Color SearchbarSearchIconColor { get; set; }
    public float SearchbarSearchIconSize { get; set; } = 21f;
    /// <summary>Clear icon color (md inherits; ios step-400) and size (md 22px; ios 18px).</summary>
    public Color SearchbarClearIconColor { get; set; }
    public float SearchbarClearIconSize { get; set; } = 22f;
    /// <summary>Cancel button color (md step-100; ios primary), background, and font size.</summary>
    public Color SearchbarCancelButtonColor { get; set; }
    public Color SearchbarCancelButtonBackground { get; set; }
    public float SearchbarCancelButtonFontSize { get; set; } = 17f;

    // Slides (slides.md/ios.vars.scss). Pagination bullets, progress bar, and scroll bar colors.
    /// <summary>Pagination bullet background (text-color step-800 ≈ #333).</summary>
    public Color SlidesBulletBackground { get; set; }
    /// <summary>Active pagination bullet background (primary).</summary>
    public Color SlidesBulletBackgroundActive { get; set; }
    /// <summary>Scroll bar track background (rgba(text, .1)).</summary>
    public Color SlidesScrollBarBackground { get; set; }
    /// <summary>Scroll bar drag handle background (rgba(text, .5)).</summary>
    public Color SlidesScrollBarBackgroundActive { get; set; }

    // Avatar (avatar.md.vars.scss / avatar.ios.vars.scss). A square host clipped into a circle;
    // md is 64px, ios is 48px. Border radius is 50% in both modes.
    /// <summary>Avatar host width/height (md 64px, ios 48px).</summary>
    public float AvatarSize { get; set; } = 64f;

    // Spinner / badge / chip / card / grid / refresher / infinite-scroll / select.
    public Color SpinnerColor { get; set; }
    public Color SpinnerTrackColor { get; set; }
    public float SpinnerSize { get; set; } = 28f;
    public float SpinnerSmallSize { get; set; } = 16f;

    public Color BadgeBackground { get; set; }
    public Color BadgeColor { get; set; }
    public float BadgeBorderRadius { get; set; } = 4f;
    public float BadgeFontSize { get; set; } = 13f;
    public float BadgePaddingTop { get; set; } = 3f;
    public float BadgePaddingEnd { get; set; } = 4f;
    public float BadgePaddingBottom { get; set; } = 4f;
    public float BadgePaddingStart { get; set; } = 4f;
    public float BadgeMinWidth { get; set; } = 10f;

    public Color ChipBackground { get; set; }
    public Color ChipColor { get; set; }
    public Color ChipBorderColor { get; set; }
    public float ChipFontSize { get; set; } = 14f;

    public Color CardBackground { get; set; }
    public Color CardColor { get; set; }
    public float CardMarginTop { get; set; } = 10f;
    public float CardMarginEnd { get; set; } = 10f;
    public float CardMarginBottom { get; set; } = 10f;
    public float CardMarginStart { get; set; } = 10f;
    public float CardBorderRadius { get; set; } = 4f;
    public float CardFontSize { get; set; } = 14f;
    public Length CardLineHeight { get; set; } = Length.Number(1.5f);
    public List<BoxShadow> CardBoxShadow { get; set; } = new();
    public float CardHeaderPaddingTop { get; set; } = 16f;
    public float CardHeaderPaddingEnd { get; set; } = 16f;
    public float CardHeaderPaddingBottom { get; set; } = 16f;
    public float CardHeaderPaddingStart { get; set; } = 16f;
    public float CardContentPaddingTop { get; set; } = 13f;
    public float CardContentPaddingEnd { get; set; } = 16f;
    public float CardContentPaddingBottom { get; set; } = 13f;
    public float CardContentPaddingStart { get; set; } = 16f;
    public float CardContentFontSize { get; set; } = 14f;
    public Length CardContentLineHeight { get; set; } = Length.Number(1.5f);

    public float GridPadding { get; set; } = 5f;
    public float GridColumnPadding { get; set; } = 5f;
    public float GridFixedWidth { get; set; } = 1140f;

    public float InfiniteScrollContentMinHeight { get; set; } = 84f;
    public float RefresherHeight { get; set; } = 60f;
    public float RefresherIconFontSize { get; set; } = 30f;
    public float RefresherTextFontSize { get; set; } = 16f;

    public Color SelectTextColor { get; set; }
    public Color SelectPlaceholderColor { get; set; }
    public Color SelectLabelColor { get; set; }
    public Color SelectBackground { get; set; }
    public Color SelectBorderColor { get; set; }
    public Color SelectHighlightColor { get; set; }
    public Color SelectHelperColor { get; set; }
    public Color SelectErrorColor { get; set; }
    public float SelectFontSize { get; set; } = 16f;
    public float SelectMinHeight { get; set; } = 48f;
    public float SelectPaddingTop { get; set; } = 8f;
    public float SelectPaddingEnd { get; set; } = 0f;
    public float SelectPaddingBottom { get; set; } = 8f;
    public float SelectPaddingStart { get; set; } = 0f;
    public float SelectBorderRadius { get; set; } = 4f;
    public float SelectRoundBorderRadius { get; set; } = 999f;

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

        // Item divider (item-divider.md.scss): light gray fill (step-50 ~#f2f2f2), bottom border
        // rgba(0,0,0,.07), medium text (step-550). 30px min-height.
        t.ItemDividerBackground = Color.FromHex("f2f2f2");
        t.ItemDividerColor = Color.FromHex("404040");            // step-550
        t.ItemDividerMinHeight = 30f;

        // Item option (item-option.md.scss): white label on the primary fill.
        t.ItemOptionColor = Color.FromHex("ffffff");
        t.ItemOptionBackground = t.Primary;

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

        // Button (button.md.scss / button.md.vars.scss). Solid fill = primary, white label;
        // outline/clear text = primary. 4px radius, 36px min-height, 14px/500 uppercase label,
        // 0.06em tracking, a 3-layer elevation shadow on the solid fill, 2px outline border.
        t.ButtonSolidBackground = t.Primary;
        t.ButtonSolidColor = Color.FromHex("ffffff");            // ion-color(primary, contrast)
        t.ButtonTextColor = t.Primary;                           // ion-color(primary, base)
        t.ButtonBorderRadius = 4f;
        t.ButtonRoundBorderRadius = 999f;
        t.ButtonMinHeight = Length.Px(36);
        t.ButtonPaddingTop = Length.Px(8);
        t.ButtonPaddingBottom = Length.Px(8);
        t.ButtonPaddingStart = Length.Em(1.1f);
        t.ButtonPaddingEnd = Length.Em(1.1f);
        t.ButtonFontSize = 14f;
        t.ButtonFontWeight = FontWeight.Medium;
        t.ButtonTextTransform = TextTransform.Uppercase;         // md uppercases the label
        t.ButtonLetterSpacing = Length.Em(0.06f);
        t.ButtonStrongFontWeight = FontWeight.Bold;
        t.ButtonSolidBoxShadow = new List<BoxShadow>
        {
            new BoxShadow(0, 3, 1, -2, new Color(0, 0, 0, 51)),  // 0.2 * 255
            new BoxShadow(0, 2, 2, 0, new Color(0, 0, 0, 36)),   // 0.14 * 255
            new BoxShadow(0, 1, 5, 0, new Color(0, 0, 0, 31)),   // 0.12 * 255
        };
        t.ButtonOutlineBorderWidth = Length.Px(2);
        t.ButtonLargeMinHeight = Length.Em(2.8f);
        t.ButtonLargePaddingTop = Length.Px(14);
        t.ButtonLargePaddingBottom = Length.Px(14);
        t.ButtonLargePaddingX = Length.Em(1f);
        t.ButtonLargeFontSize = 20f;
        t.ButtonLargeBorderRadius = 4f;                          // md large keeps the default radius
        t.ButtonSmallMinHeight = Length.Em(2.1f);
        t.ButtonSmallPaddingTop = Length.Px(4);
        t.ButtonSmallPaddingBottom = Length.Px(4);
        t.ButtonSmallPaddingX = Length.Em(0.9f);
        t.ButtonSmallFontSize = 13f;
        t.ButtonSmallBorderRadius = 4f;                          // md small keeps the default radius
        t.ButtonIconOnlyMinSize = 40f;                           // clamp(30, 2.86em@14, 60) midpoint
        t.ButtonIconOnlyIconSize = 22.4f;                        // md default icon-only font size
        t.ButtonSmallIconOnlyMinSize = 28f;                      // clamp(23, 2.16em@13, 54) ≈ 28
        t.ButtonSmallIconOnlyIconSize = 16f;                     // md small icon-only font size
        t.ButtonLargeIconOnlyMinSize = 50f;                      // clamp(46, 2.5em@20, 78) ≈ 50
        t.ButtonLargeIconOnlyIconSize = 28f;                     // md large icon-only font size

        // Searchbar (searchbar.md.scss / searchbar.md.vars.scss): 8px host padding; flat 2px-radius
        // white input with a 3-layer elevation shadow and a 21px left search icon; 16px input text
        // (step-150); cancel button is an icon (arrow-back-sharp) colored step-100.
        t.SearchbarPaddingTop = 8f;
        t.SearchbarPaddingEnd = 8f;
        t.SearchbarPaddingBottom = 8f;
        t.SearchbarPaddingStart = 8f;
        t.SearchbarInputBackground = Color.FromHex("ffffff");    // $background-color
        t.SearchbarInputBorderRadius = 2f;
        t.SearchbarInputBoxShadow = new List<BoxShadow>
        {
            new BoxShadow(0, 2, 2, 0, new Color(0, 0, 0, 36)),   // rgba(0,0,0,.14)
            new BoxShadow(0, 3, 1, -2, new Color(0, 0, 0, 51)),  // rgba(0,0,0,.2)
            new BoxShadow(0, 1, 5, 0, new Color(0, 0, 0, 31)),   // rgba(0,0,0,.12)
        };
        t.SearchbarInputTextColor = Color.FromHex("262626");      // text-color-step-150
        t.SearchbarInputFontSize = 16f;
        t.SearchbarInputHeight = Length.Auto;
        t.SearchbarInputLineHeight = Length.Px(30);
        t.SearchbarInputMinHeight = 0f;                           // md has no input min-height
        t.SearchbarSearchIconColor = Color.FromHex("666666");     // text-color-step-400
        t.SearchbarSearchIconSize = 21f;
        t.SearchbarClearIconColor = Color.FromHex("666666");      // inherits icon color (step-400)
        t.SearchbarClearIconSize = 22f;
        t.SearchbarCancelButtonColor = Color.FromHex("1a1a1a");   // text-color-step-100
        t.SearchbarCancelButtonBackground = Color.Transparent;
        t.SearchbarCancelButtonFontSize = 24f;                    // 1.5em @ 16px

        // Slides (slides.md.vars.scss): bullet step-800 (~#333), active = primary; scroll bar
        // track rgba(text, .1), drag handle rgba(text, .5).
        t.SlidesBulletBackground = Color.FromHex("333333");       // text-color-step-800
        t.SlidesBulletBackgroundActive = t.Primary;
        t.SlidesScrollBarBackground = new Color(0, 0, 0, 26);     // rgba(text-color, .1)
        t.SlidesScrollBarBackgroundActive = new Color(0, 0, 0, 128); // rgba(text-color, .5)

        // Avatar (avatar.md.vars.scss): 64px square.
        t.AvatarSize = 64f;

        t.SpinnerColor = t.Primary;
        t.SpinnerTrackColor = new Color(t.Primary.R, t.Primary.G, t.Primary.B, 51);
        t.SpinnerSize = 28f;
        t.SpinnerSmallSize = 16f;

        t.BadgeBackground = t.Primary;
        t.BadgeColor = Color.FromHex("ffffff");
        t.BadgeBorderRadius = 4f;
        t.BadgeFontSize = 13f;
        t.BadgePaddingTop = 3f;
        t.BadgePaddingEnd = 4f;
        t.BadgePaddingBottom = 4f;
        t.BadgePaddingStart = 4f;

        t.ChipBackground = new Color(0, 0, 0, 31);
        t.ChipColor = new Color(0, 0, 0, 222);
        t.ChipBorderColor = new Color(0, 0, 0, 82);
        t.ChipFontSize = 14f;

        t.CardBackground = Color.FromHex("ffffff");
        t.CardColor = Color.FromHex("000000");
        t.CardMarginTop = 10f;
        t.CardMarginEnd = 10f;
        t.CardMarginBottom = 10f;
        t.CardMarginStart = 10f;
        t.CardBorderRadius = 4f;
        t.CardFontSize = 14f;
        t.CardLineHeight = Length.Number(1.5f);
        t.CardBoxShadow = new List<BoxShadow>
        {
            new BoxShadow(0, 3, 1, -2, new Color(0, 0, 0, 51)),
            new BoxShadow(0, 2, 2, 0, new Color(0, 0, 0, 36)),
            new BoxShadow(0, 1, 5, 0, new Color(0, 0, 0, 31)),
        };
        t.CardHeaderPaddingTop = 16f;
        t.CardHeaderPaddingEnd = 16f;
        t.CardHeaderPaddingBottom = 16f;
        t.CardHeaderPaddingStart = 16f;
        t.CardContentPaddingTop = 13f;
        t.CardContentPaddingEnd = 16f;
        t.CardContentPaddingBottom = 13f;
        t.CardContentPaddingStart = 16f;
        t.CardContentFontSize = 14f;
        t.CardContentLineHeight = Length.Number(1.5f);

        t.GridPadding = 5f;
        t.GridColumnPadding = 5f;
        t.GridFixedWidth = 1140f;

        t.InfiniteScrollContentMinHeight = 84f;
        t.RefresherHeight = 60f;
        t.RefresherIconFontSize = 30f;
        t.RefresherTextFontSize = 16f;

        t.SelectTextColor = Color.FromHex("000000");
        t.SelectPlaceholderColor = Color.FromHex("666666");
        t.SelectLabelColor = Color.FromHex("666666");
        t.SelectBackground = Color.Transparent;
        t.SelectBorderColor = new Color(0, 0, 0, 61);
        t.SelectHighlightColor = t.Primary;
        t.SelectHelperColor = Color.FromHex("666666");
        t.SelectErrorColor = t.Danger;
        t.SelectFontSize = 16f;
        t.SelectMinHeight = 48f;
        t.SelectPaddingTop = 8f;
        t.SelectPaddingBottom = 8f;
        t.SelectBorderRadius = 4f;
        t.SelectRoundBorderRadius = 999f;

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

        // Item divider (item-divider.ios.scss): light fill (step-50 ~#f7f7f7), hairline bottom
        // border rgba(0,0,0,.2), dark text. 28px min-height.
        t.ItemDividerBackground = Color.FromHex("f7f7f7");
        t.ItemDividerColor = Color.FromHex("000000");
        t.ItemDividerMinHeight = 28f;

        // Item option (item-option.ios.scss): white label on the primary fill.
        t.ItemOptionColor = Color.FromHex("ffffff");
        t.ItemOptionBackground = t.Primary;

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

        // Button (button.ios.scss / button.ios.vars.scss). Same solid=primary/white,
        // outline+clear text=primary semantics, but no uppercase, no letter-spacing, 14px radius,
        // 3.1em min-height, 16px/500 label, no elevation shadow, 1px outline border.
        t.ButtonSolidBackground = t.Primary;
        t.ButtonSolidColor = Color.FromHex("ffffff");            // ion-color(primary, contrast)
        t.ButtonTextColor = t.Primary;                           // ion-color(primary, base)
        t.ButtonBorderRadius = 14f;
        t.ButtonRoundBorderRadius = 999f;
        t.ButtonMinHeight = Length.Em(3.1f);
        t.ButtonPaddingTop = Length.Px(13);
        t.ButtonPaddingBottom = Length.Px(13);
        t.ButtonPaddingStart = Length.Em(1f);
        t.ButtonPaddingEnd = Length.Em(1f);
        t.ButtonFontSize = 16f;
        t.ButtonFontWeight = FontWeight.Medium;
        t.ButtonTextTransform = TextTransform.None;              // iOS keeps the label as-authored
        t.ButtonLetterSpacing = Length.Px(0);
        t.ButtonStrongFontWeight = FontWeight.SemiBold;          // ios 600
        t.ButtonSolidBoxShadow = new List<BoxShadow>();          // iOS solid has no elevation
        t.ButtonOutlineBorderWidth = Length.Px(1);
        t.ButtonLargeMinHeight = Length.Em(3.1f);
        t.ButtonLargePaddingTop = Length.Px(17);
        t.ButtonLargePaddingBottom = Length.Px(17);
        t.ButtonLargePaddingX = Length.Em(1f);
        t.ButtonLargeFontSize = 20f;
        t.ButtonLargeBorderRadius = 16f;
        t.ButtonSmallMinHeight = Length.Em(2.1f);
        t.ButtonSmallPaddingTop = Length.Px(4);
        t.ButtonSmallPaddingBottom = Length.Px(4);
        t.ButtonSmallPaddingX = Length.Em(0.9f);
        t.ButtonSmallFontSize = 13f;
        t.ButtonSmallBorderRadius = 6f;
        t.ButtonIconOnlyMinSize = 40f;                           // clamp(30, 2.125em@16, 60) ≈ 34→40
        t.ButtonIconOnlyIconSize = 18f;                          // ios default icon-only font size
        t.ButtonSmallIconOnlyMinSize = 28f;                      // clamp(23, 2.16em@13, 54) ≈ 28
        t.ButtonSmallIconOnlyIconSize = 17f;                     // ios small icon-only font size
        t.ButtonLargeIconOnlyMinSize = 50f;                      // clamp(46, 2.5em@20, 78) ≈ 50
        t.ButtonLargeIconOnlyIconSize = 18f;                     // ios large icon-only font size

        // Searchbar (searchbar.ios.scss / searchbar.ios.vars.scss): 12px host padding; 10px-radius
        // translucent input (rgba(text,.07)) with no shadow; 36px input min-height; 17px input text
        // (#000); 22px centered search icon; 18px clear icon; cancel button is "Cancel" text in
        // the primary color at 17px (does not scale with Dynamic Type).
        t.SearchbarPaddingTop = 12f;
        t.SearchbarPaddingEnd = 12f;
        t.SearchbarPaddingBottom = 12f;
        t.SearchbarPaddingStart = 12f;
        t.SearchbarInputBackground = new Color(0, 0, 0, 18);      // rgba(text-color-rgb, .07)
        t.SearchbarInputBorderRadius = 10f;
        t.SearchbarInputBoxShadow = new List<BoxShadow>();        // ios: none
        t.SearchbarInputTextColor = Color.FromHex("000000");      // $text-color
        t.SearchbarInputFontSize = 17f;
        t.SearchbarInputHeight = Length.Percent(100);
        t.SearchbarInputLineHeight = Length.Px(30);
        t.SearchbarInputMinHeight = 36f;
        t.SearchbarSearchIconColor = Color.FromHex("666666");     // text-color-step-400
        t.SearchbarSearchIconSize = 22f;
        t.SearchbarClearIconColor = Color.FromHex("666666");      // text-color-step-400
        t.SearchbarClearIconSize = 18f;
        t.SearchbarCancelButtonColor = t.Primary;                 // ion-color(primary, base)
        t.SearchbarCancelButtonBackground = Color.Transparent;
        t.SearchbarCancelButtonFontSize = 17f;

        // Slides (slides.ios.vars.scss): same token semantics as md — bullet step-800, active =
        // primary; scroll bar track rgba(text, .1), drag handle rgba(text, .5).
        t.SlidesBulletBackground = Color.FromHex("333333");       // text-color-step-800
        t.SlidesBulletBackgroundActive = t.Primary;
        t.SlidesScrollBarBackground = new Color(0, 0, 0, 26);     // rgba(text-color, .1)
        t.SlidesScrollBarBackgroundActive = new Color(0, 0, 0, 128); // rgba(text-color, .5)

        // Avatar (avatar.ios.vars.scss): 48px square.
        t.AvatarSize = 48f;

        t.SpinnerColor = t.Primary;
        t.SpinnerTrackColor = new Color(t.Primary.R, t.Primary.G, t.Primary.B, 51);
        t.SpinnerSize = 28f;
        t.SpinnerSmallSize = 16f;

        t.BadgeBackground = t.Primary;
        t.BadgeColor = Color.FromHex("ffffff");
        t.BadgeBorderRadius = 10f;
        t.BadgeFontSize = 13f;
        t.BadgePaddingTop = 3f;
        t.BadgePaddingEnd = 8f;
        t.BadgePaddingBottom = 3f;
        t.BadgePaddingStart = 8f;

        t.ChipBackground = new Color(0, 0, 0, 31);
        t.ChipColor = new Color(0, 0, 0, 222);
        t.ChipBorderColor = new Color(0, 0, 0, 82);
        t.ChipFontSize = 14f;

        t.CardBackground = Color.FromHex("ffffff");
        t.CardColor = Color.FromHex("000000");
        t.CardMarginTop = 24f;
        t.CardMarginEnd = 16f;
        t.CardMarginBottom = 24f;
        t.CardMarginStart = 16f;
        t.CardBorderRadius = 8f;
        t.CardFontSize = 14f;
        t.CardLineHeight = Length.Number(1.4f);
        t.CardBoxShadow = new List<BoxShadow>
        {
            new BoxShadow(0, 4, 16, 0, new Color(0, 0, 0, 31)),
        };
        t.CardHeaderPaddingTop = 20f;
        t.CardHeaderPaddingEnd = 20f;
        t.CardHeaderPaddingBottom = 16f;
        t.CardHeaderPaddingStart = 20f;
        t.CardContentPaddingTop = 20f;
        t.CardContentPaddingEnd = 20f;
        t.CardContentPaddingBottom = 20f;
        t.CardContentPaddingStart = 20f;
        t.CardContentFontSize = 16f;
        t.CardContentLineHeight = Length.Number(1.4f);

        t.GridPadding = 5f;
        t.GridColumnPadding = 5f;
        t.GridFixedWidth = 1140f;

        t.InfiniteScrollContentMinHeight = 84f;
        t.RefresherHeight = 60f;
        t.RefresherIconFontSize = 30f;
        t.RefresherTextFontSize = 16f;

        t.SelectTextColor = Color.FromHex("000000");
        t.SelectPlaceholderColor = Color.FromHex("666666");
        t.SelectLabelColor = Color.FromHex("666666");
        t.SelectBackground = Color.Transparent;
        t.SelectBorderColor = new Color(0, 0, 0, 51);
        t.SelectHighlightColor = t.Primary;
        t.SelectHelperColor = Color.FromHex("666666");
        t.SelectErrorColor = t.Danger;
        t.SelectFontSize = 17f;
        t.SelectMinHeight = 44f;
        t.SelectPaddingTop = 8f;
        t.SelectPaddingBottom = 8f;
        t.SelectBorderRadius = 10f;
        t.SelectRoundBorderRadius = 999f;

        return t;
    }
}
