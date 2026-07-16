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

    // Checkbox (checkbox.scss / checkbox.md.scss / checkbox.ios.scss + their *.vars.scss).
    // The visual box (.checkbox-icon) is a bordered square that fills with the checked color and
    // fades in the checkmark when checked/indeterminate. md is a small square (18px, ~2px radius);
    // ios is a larger circle (22px, 50% radius).
    /// <summary>Box side length (md 18px, ios 22px).</summary>
    public float CheckboxSize { get; set; } = 18f;
    /// <summary>Box border width (2px both modes).</summary>
    public float CheckboxBorderWidth { get; set; } = 2f;
    /// <summary>Box corner radius (md size*.125 ≈ 2.25px, ios 50% → half the size for a circle).</summary>
    public Length CheckboxBorderRadius { get; set; } = Length.Px(2.25f);
    /// <summary>Box border color when unchecked (md rgba(text,.60), ios rgba(text,.23)).</summary>
    public Color CheckboxBorderColorOff { get; set; }
    /// <summary>Box background when unchecked (item background — white).</summary>
    public Color CheckboxBackgroundOff { get; set; }
    /// <summary>Box background / border color when checked (primary).</summary>
    public Color CheckboxBackgroundChecked { get; set; }
    /// <summary>Checkmark tint when checked (primary contrast — white).</summary>
    public Color CheckboxCheckmarkColor { get; set; }
    /// <summary>Opacity of the whole disabled checkbox host (ios) / its label (md).</summary>
    public float CheckboxDisabledOpacity { get; set; } = 0.38f;

    // Fab (fab.scss / fab-button.scss / fab-button.md.scss / fab-button.ios.scss / fab-list.scss +
    // their *.vars.scss). A floating action button: an absolutely-positioned container holding a
    // round main button (56px) and optional fab-lists of mini buttons (40px). The main button fills
    // with the primary color and carries an elevation shadow; list buttons use the light surface.
    /// <summary>Main FAB button width/height (<c>$fab-size</c>, 56px both modes).</summary>
    public float FabSize { get; set; } = 56f;
    /// <summary>Mini FAB button width/height (<c>$fab-small-size</c>, 40px both modes).</summary>
    public float FabSmallSize { get; set; } = 40f;
    /// <summary>Margin of the FAB container from the viewport edge (<c>$fab-content-margin</c>, 10px).</summary>
    public float FabContentMargin { get; set; } = 10f;
    /// <summary>Margin between the main button and a fab-list (<c>$fab-list-margin</c>, 10px).</summary>
    public float FabListMargin { get; set; } = 10f;
    /// <summary>Margin applied to a mini FAB button (<c>$fab-button-small-margin</c>, 8px).</summary>
    public float FabButtonSmallMargin { get; set; } = 8f;
    /// <summary>Main button fill (<c>--background</c> = <c>ion-color(primary, base)</c>).</summary>
    public Color FabBackground { get; set; }
    /// <summary>Main button icon/text color (<c>--color</c> = <c>ion-color(primary, contrast)</c> — white).</summary>
    public Color FabColor { get; set; }
    /// <summary>Main button elevation (<c>--box-shadow</c>: md 3-layer; ios <c>0 4px 16px rgba(0,0,0,.12)</c>).</summary>
    public List<BoxShadow> FabBoxShadow { get; set; } = new();
    /// <summary>Slotted icon font size for the main button (md 24px, ios 28px).</summary>
    public float FabIconFontSize { get; set; } = 24f;
    /// <summary>Fill of a button inside a fab-list (<c>ion-color(light, base)</c>).</summary>
    public Color FabListButtonBackground { get; set; }
    /// <summary>Text/icon color of a button inside a fab-list (<c>ion-color(light, contrast)</c> — dark).</summary>
    public Color FabListButtonColor { get; set; }
    /// <summary>Slotted icon font size for a button inside a fab-list (18px both modes).</summary>
    public float FabListButtonIconSize { get; set; } = 18f;

    // Input (input.scss / input.md.scss / input.ios.scss + their *.vars.scss). A full-width text
    // field that lives inside an ion-item; the label sits beside/above the native input, an
    // optional bottom row carries helper/error text and a character counter, and md draws a 2px
    // focus highlight bar under the field. Tokens mirror the input's SCSS custom properties and the
    // per-mode item border/font values.
    /// <summary>Input font size (md 16px, ios 17px — SCSS uses <c>inherit</c>; we resolve to the item font).</summary>
    public float InputFontSize { get; set; } = 16f;
    /// <summary>Host min-height (44px both modes; grows to 56px for floating/stacked labels — handled in CSS).</summary>
    public float InputMinHeight { get; set; } = 44f;
    /// <summary>Input text color (<c>--color</c>, resolves to the item text color — black).</summary>
    public Color InputTextColor { get; set; }
    /// <summary>Placeholder text color (<c>--placeholder-color</c>).</summary>
    public Color InputPlaceholderColor { get; set; }
    /// <summary>Label text color (the neutral form-control label color).</summary>
    public Color InputLabelColor { get; set; }
    /// <summary>Field background (<c>--background</c>, transparent by default).</summary>
    public Color InputBackground { get; set; }
    /// <summary>Bottom border color below the field when helper/error/counter is shown
    /// (<c>--border-color</c>: md <c>$item-md-border-color</c>, ios <c>$item-ios-border-color</c>).</summary>
    public Color InputBorderColor { get; set; }
    /// <summary>Focus highlight / caret color (<c>--highlight-color-focused</c> = primary).</summary>
    public Color InputHighlightColor { get; set; }
    /// <summary>Helper text color (neutral step-300).</summary>
    public Color InputHelperColor { get; set; }
    /// <summary>Error text color (<c>--highlight-color-invalid</c> = danger).</summary>
    public Color InputErrorColor { get; set; }
    /// <summary>Highlight bar height (<c>--highlight-height</c>: md 2px, ios 0px — ios has no bar).</summary>
    public float InputHighlightHeight { get; set; } = 2f;
    /// <summary>Field corner radius (<c>--border-radius</c>: used by fill/shape variants; round = 16px).</summary>
    public float InputBorderRadius { get; set; } = 4f;
    /// <summary>Field start padding (<c>--padding-start</c>, 0 by default; 16px for solid/outline fills).</summary>
    public float InputPaddingStart { get; set; } = 0f;
    /// <summary>Field end padding (<c>--padding-end</c>, 0 by default; 16px for solid/outline fills).</summary>
    public float InputPaddingEnd { get; set; } = 0f;
    /// <summary>Clear button icon color (text-color step-400).</summary>
    public Color InputClearIconColor { get; set; }
    /// <summary>Opacity of the whole disabled input host (md .38, ios .3).</summary>
    public float InputDisabledOpacity { get; set; } = 0.38f;

    // Breadcrumb (breadcrumb.scss / breadcrumb.md.scss / breadcrumb.ios.scss + their *.vars.scss).
    // A flex row of crumbs, each a native anchor/span followed by a separator ("/" on md, a forward
    // chevron on ios). The last crumb is active (no separator). Tokens mirror the per-mode SCSS vars.
    /// <summary>Crumb text color (<c>--color</c>).</summary>
    public Color BreadcrumbColor { get; set; }
    /// <summary>Active crumb text color (<c>--color-active</c>).</summary>
    public Color BreadcrumbColorActive { get; set; }
    /// <summary>Crumb font size (<c>$breadcrumb-font-size</c>, 16px both modes).</summary>
    public float BreadcrumbFontSize { get; set; } = 16f;
    /// <summary>Active crumb font weight (md 500, ios 600).</summary>
    public FontWeight BreadcrumbActiveFontWeight { get; set; } = FontWeight.Medium;
    /// <summary>Native element top/bottom padding (md 6px, ios 5px).</summary>
    public float BreadcrumbPaddingY { get; set; } = 6f;
    /// <summary>Native element start/end padding (12px both modes).</summary>
    public float BreadcrumbPaddingX { get; set; } = 12f;
    /// <summary>Native element corner radius (md 0, ios 4px).</summary>
    public float BreadcrumbBorderRadius { get; set; }
    /// <summary>Separator glyph color (<c>#73849a</c> both modes).</summary>
    public Color BreadcrumbSeparatorColor { get; set; }
    /// <summary>Separator start/end margin (10px both modes; md also lifts it -1px, elided here).</summary>
    public float BreadcrumbSeparatorMarginX { get; set; } = 10f;
    /// <summary>Slotted icon color (<c>--icon-color</c>).</summary>
    public Color BreadcrumbIconColor { get; set; }
    /// <summary>Slotted icon color when the crumb is active.</summary>
    public Color BreadcrumbIconColorActive { get; set; }
    /// <summary>Slotted icon font size (18px both modes).</summary>
    public float BreadcrumbIconFontSize { get; set; } = 18f;
    /// <summary>Collapsed-indicator background (md <c>#eef1f3</c>, ios <c>#e9edf3</c>).</summary>
    public Color BreadcrumbIndicatorBackground { get; set; }
    /// <summary>Collapsed-indicator icon/text color (<c>#73849a</c> both modes).</summary>
    public Color BreadcrumbIndicatorColor { get; set; }

    // Accordion (accordion.scss / accordion-group.scss + their *.vars.scss). A vertical group of
    // collapsible panels: each accordion has a header row (an ion-item) and a content region that
    // shows/hides on the group's selected value. Tokens mirror the shared accordion SCSS vars.
    /// <summary>Accordion panel background (<c>$accordion-background-color</c>, white).</summary>
    public Color AccordionBackground { get; set; }
    /// <summary>Opacity of a disabled accordion's header/content (<c>0.4</c>).</summary>
    public float AccordionDisabledOpacity { get; set; } = 0.4f;
    /// <summary>Inset-group margin and inset expanded-panel radius (16px / md 6px).</summary>
    public float AccordionInsetMargin { get; set; } = 16f;
    /// <summary>Inset expanded-panel corner radius (md 6px).</summary>
    public float AccordionInsetBorderRadius { get; set; } = 6f;
    /// <summary>Inset panel elevation shadow (md 3-layer; ios none).</summary>
    public List<BoxShadow> AccordionInsetBoxShadow { get; set; } = new();

    // ActionSheet (action-sheet.scss / .md.scss / .ios.scss + their *.vars.scss). A bottom-anchored
    // overlay: a scrollable group of full-width buttons under an optional title, with cancel buttons
    // pulled into a separate group. md fills to the bottom edge with a flat surface; ios floats a
    // rounded group with side margins and a separate rounded cancel group.
    /// <summary>Backdrop dim color (black) — opacity applied separately per mode.</summary>
    public Color ActionSheetBackdropColor { get; set; } = Color.FromHex("000000");
    /// <summary>Backdrop opacity (md 0.32, ios 0.4).</summary>
    public float ActionSheetBackdropOpacity { get; set; } = 0.32f;
    /// <summary>Group surface background (md white, ios <c>#f9f9f9</c>).</summary>
    public Color ActionSheetBackground { get; set; }
    /// <summary>Group corner radius (md 0, ios 13px).</summary>
    public float ActionSheetBorderRadius { get; set; }
    /// <summary>Max width of the sheet (500px both modes).</summary>
    public float ActionSheetMaxWidth { get; set; } = 500f;
    /// <summary>Container side padding (md 0, ios 8px) — the ios float inset.</summary>
    public float ActionSheetContainerPaddingX { get; set; }
    /// <summary>Group top margin (ios 10px; md 0).</summary>
    public float ActionSheetGroupMarginTop { get; set; }
    /// <summary>Group bottom margin (ios 10px; md 0).</summary>
    public float ActionSheetGroupMarginBottom { get; set; }
    /// <summary>Title text color (md <c>rgba(0,0,0,.54)</c>, ios <c>#999999</c> step-600).</summary>
    public Color ActionSheetTitleColor { get; set; }
    /// <summary>Title font size (md 16px, ios 13px).</summary>
    public float ActionSheetTitleFontSize { get; set; } = 16f;
    /// <summary>Title vertical/side padding (md 20/16, ios 14/10).</summary>
    public float ActionSheetTitlePaddingY { get; set; } = 20f;
    public float ActionSheetTitlePaddingX { get; set; } = 16f;
    /// <summary>Sub-title font size (md 14px, ios 13px).</summary>
    public float ActionSheetSubTitleFontSize { get; set; } = 14f;
    /// <summary>Button label color (md step-150 <c>#262626</c>, ios primary).</summary>
    public Color ActionSheetButtonColor { get; set; }
    /// <summary>Button min-height (md 52px, ios 56px).</summary>
    public float ActionSheetButtonHeight { get; set; } = 52f;
    /// <summary>Button font size (md 16px, ios 20px).</summary>
    public float ActionSheetButtonFontSize { get; set; } = 16f;
    /// <summary>Button vertical/side padding (md 12/16, ios 14/14).</summary>
    public float ActionSheetButtonPaddingY { get; set; } = 12f;
    public float ActionSheetButtonPaddingX { get; set; } = 16f;
    /// <summary>Button icon size (md 24px, ios 28px).</summary>
    public float ActionSheetIconFontSize { get; set; } = 24f;
    /// <summary>Button content alignment: md left (flex-start), ios center.</summary>
    public JustifyContent ActionSheetButtonJustify { get; set; } = JustifyContent.FlexStart;
    /// <summary>Text alignment of title/buttons: md start, ios center.</summary>
    public TextAlign ActionSheetTextAlign { get; set; } = TextAlign.Left;
    /// <summary>Destructive button label color (danger).</summary>
    public Color ActionSheetDestructiveColor { get; set; }
    /// <summary>Divider between ios buttons (a hairline; md has none).</summary>
    public Color ActionSheetButtonBorderColor { get; set; }
    /// <summary>Cancel button font weight (ios 600; md normal).</summary>
    public FontWeight ActionSheetCancelFontWeight { get; set; } = FontWeight.Normal;

    // Alert (alert.scss / .md.scss / .ios.scss + their *.vars.scss). A centered modal card: a head
    // (title + sub-title), an optional message, an optional inputs group (text / radio / checkbox),
    // and a button group (row; column when >2 buttons). md left-aligns the head and right-aligns
    // uppercase buttons; ios centers everything with hairline-divided buttons.
    /// <summary>Backdrop dim color (black) — opacity applied separately.</summary>
    public Color AlertBackdropColor { get; set; } = Color.FromHex("000000");
    /// <summary>Backdrop opacity (md 0.32, ios 0.4).</summary>
    public float AlertBackdropOpacity { get; set; } = 0.32f;
    /// <summary>Card background (md white, ios <c>#f9f9f9</c>).</summary>
    public Color AlertBackground { get; set; }
    /// <summary>Card min width (md 250px, ios 250px).</summary>
    public float AlertMinWidth { get; set; } = 250f;
    /// <summary>Card max width (md 280px, ios 270px).</summary>
    public float AlertMaxWidth { get; set; } = 280f;
    /// <summary>Card corner radius (md 4px, ios 13px).</summary>
    public float AlertBorderRadius { get; set; } = 4f;
    /// <summary>Card elevation shadow (md 3-layer; ios none).</summary>
    public List<BoxShadow> AlertBoxShadow { get; set; } = new();
    /// <summary>Head vertical/side padding (md 20/23, ios 12/16).</summary>
    public float AlertHeadPaddingY { get; set; } = 20f;
    public float AlertHeadPaddingX { get; set; } = 23f;
    /// <summary>Head text alignment (md start, ios center).</summary>
    public TextAlign AlertHeadTextAlign { get; set; } = TextAlign.Left;
    /// <summary>Title color (text color) / font-size (md 20, ios 17) / weight (md 500, ios 600).</summary>
    public Color AlertTitleColor { get; set; }
    public float AlertTitleFontSize { get; set; } = 20f;
    public FontWeight AlertTitleFontWeight { get; set; } = FontWeight.Medium;
    /// <summary>Sub-title color / font-size (md 16, ios 14).</summary>
    public Color AlertSubTitleColor { get; set; }
    public float AlertSubTitleFontSize { get; set; } = 16f;
    /// <summary>Message vertical/side padding (md 20/24, ios 0-21/16) and color / font-size.</summary>
    public float AlertMessagePaddingY { get; set; } = 20f;
    public float AlertMessagePaddingX { get; set; } = 24f;
    public Color AlertMessageColor { get; set; }
    public float AlertMessageFontSize { get; set; } = 16f;
    /// <summary>Button group padding (md 8px; ios 0) and its content justification.</summary>
    public float AlertButtonGroupPadding { get; set; } = 8f;
    public JustifyContent AlertButtonGroupJustify { get; set; } = JustifyContent.FlexEnd;
    /// <summary>Button label color (primary), background (transparent), font size / weight.</summary>
    public Color AlertButtonColor { get; set; }
    public float AlertButtonFontSize { get; set; } = 14f;
    public FontWeight AlertButtonFontWeight { get; set; } = FontWeight.Medium;
    /// <summary>Button corner radius (md 2px, ios 0) and text transform (md uppercase, ios none).</summary>
    public float AlertButtonBorderRadius { get; set; } = 2f;
    public TextTransform AlertButtonTextTransform { get; set; } = TextTransform.None;
    /// <summary>Button padding (md 10px, ios 8px) and side margin (md 8px between buttons).</summary>
    public float AlertButtonPadding { get; set; } = 10f;
    public float AlertButtonMarginX { get; set; } = 8f;
    /// <summary>Divider between ios buttons (hairline; md none) and radio/checkbox group borders.</summary>
    public Color AlertListBorderColor { get; set; }
    /// <summary>Radio/checkbox unchecked circle border color and checked accent (primary).</summary>
    public Color AlertControlBorderColorOff { get; set; }
    public Color AlertControlAccent { get; set; }
    /// <summary>Tappable radio/checkbox row min-height (md 48, ios 44).</summary>
    public float AlertTappableHeight { get; set; } = 48f;

    // Datetime (datetime.scss / .md.scss / .ios.scss + their *.vars.scss). The default calendar
    // (date) view: a header (title + selected date), a calendar (month/year toggle + weekday row +
    // a 7-column month grid of round day buttons), and an optional footer. md fills the header with
    // the primary color and marks the active day with a solid primary circle; ios keeps a light
    // header and marks the active day with a translucent primary circle.
    /// <summary>Component background surface (md <c>#ffffff</c>, ios light <c>#f4f5f8</c>).</summary>
    public Color DatetimeBackground { get; set; }
    /// <summary>Header background (md primary; ios matches the surface).</summary>
    public Color DatetimeHeaderBackground { get; set; }
    /// <summary>Header title/selected-date color (md white on primary; ios dark).</summary>
    public Color DatetimeHeaderColor { get; set; }
    /// <summary>Title font size (md 12px, ios 14px).</summary>
    public float DatetimeTitleFontSize { get; set; } = 12f;
    /// <summary>Selected-date font size (md 34px, ios 16px).</summary>
    public float DatetimeSelectedDateFontSize { get; set; } = 34f;
    /// <summary>Weekday-row text color (a neutral gray).</summary>
    public Color DatetimeDayOfWeekColor { get; set; }
    /// <summary>Weekday-row font size (md 14px, ios 12px).</summary>
    public float DatetimeDayOfWeekFontSize { get; set; } = 14f;
    /// <summary>Month/year toggle text color (a neutral dark gray).</summary>
    public Color DatetimeMonthYearColor { get; set; }
    /// <summary>Day cell text color (near-black).</summary>
    public Color DatetimeDayColor { get; set; }
    /// <summary>Day cell width/height (md 42px, ios 40px).</summary>
    public float DatetimeDaySize { get; set; } = 42f;
    /// <summary>Day cell font size (md 14px, ios 20px).</summary>
    public float DatetimeDayFontSize { get; set; } = 14f;
    /// <summary>Active day background (md solid primary; ios translucent primary).</summary>
    public Color DatetimeDayActiveBackground { get; set; }
    /// <summary>Active day text color (md white contrast; ios primary).</summary>
    public Color DatetimeDayActiveColor { get; set; }
    /// <summary>Today text/border accent (primary).</summary>
    public Color DatetimeTodayColor { get; set; }

    // Datetime button (datetime-button.scss / .md.scss / .ios.scss). A pill button pair (date/time)
    // with an 8px-radius light-gray fill and dark text; the active button turns the primary color.
    /// <summary>Button fill (<c>#edeef0</c> both modes).</summary>
    public Color DatetimeButtonBackground { get; set; } = Color.FromHex("edeef0");
    /// <summary>Button text color (near-black).</summary>
    public Color DatetimeButtonColor { get; set; } = Color.FromHex("000000");
    /// <summary>Active button text color (primary).</summary>
    public Color DatetimeButtonActiveColor { get; set; }
    /// <summary>Button corner radius (8px both modes).</summary>
    public float DatetimeButtonBorderRadius { get; set; } = 8f;
    /// <summary>Button vertical/side padding (md 6/12, ios 7/13).</summary>
    public float DatetimeButtonPaddingY { get; set; } = 6f;
    public float DatetimeButtonPaddingX { get; set; } = 12f;
    /// <summary>Button font size (16px both modes).</summary>
    public float DatetimeButtonFontSize { get; set; } = 16f;

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

        // Checkbox (checkbox.md.scss / checkbox.md.vars.scss): 18px square, 2px border,
        // radius size*.125 (2.25px), unchecked border rgba(text,.60), white fill; checked fills
        // with primary and shows a white checkmark. Disabled dims the label to .38.
        t.CheckboxSize = 18f;
        t.CheckboxBorderWidth = 2f;
        t.CheckboxBorderRadius = Length.Px(18f * 0.125f);        // calc(var(--size) * .125)
        t.CheckboxBorderColorOff = new Color(0, 0, 0, 153);      // rgba(text,.60)
        t.CheckboxBackgroundOff = Color.FromHex("ffffff");        // $item-md-background
        t.CheckboxBackgroundChecked = t.Primary;                  // --checkbox-background-checked
        t.CheckboxCheckmarkColor = Color.FromHex("ffffff");       // ion-color(primary, contrast)
        t.CheckboxDisabledOpacity = 0.38f;                        // $form-control-md-disabled-opacity

        // Fab (fab-button.md.scss / fab-button.md.vars.scss): 56px round primary button with a
        // 3-layer Material elevation shadow and a 24px slotted icon; mini button 40px. List buttons
        // use the light surface with a dark icon at 18px.
        t.FabSize = 56f;
        t.FabSmallSize = 40f;
        t.FabContentMargin = 10f;
        t.FabListMargin = 10f;
        t.FabButtonSmallMargin = 8f;
        t.FabBackground = t.Primary;                              // ion-color(primary, base)
        t.FabColor = Color.FromHex("ffffff");                     // ion-color(primary, contrast)
        t.FabBoxShadow = new List<BoxShadow>
        {
            new BoxShadow(0, 3, 5, -1, new Color(0, 0, 0, 51)),   // 0.2 * 255
            new BoxShadow(0, 6, 10, 0, new Color(0, 0, 0, 36)),   // 0.14 * 255
            new BoxShadow(0, 1, 18, 0, new Color(0, 0, 0, 31)),   // 0.12 * 255
        };
        t.FabIconFontSize = 24f;                                  // $fab-md-icon-font-size
        t.FabListButtonBackground = t.Light;                      // ion-color(light, base)
        t.FabListButtonColor = Color.FromHex("000000");           // ion-color(light, contrast)
        t.FabListButtonIconSize = 18f;                            // $fab-md-list-button-icon-font-size

        // Input (input.md.scss / input.md.vars.scss): 16px text; 44px min-height; 1px item bottom
        // border rgba(0,0,0,.07); 2px primary highlight bar; radius 4 (used by fills/round). Text and
        // background inherit the item's black-on-transparent; disabled dims the host to .38.
        t.InputFontSize = 16f;
        t.InputMinHeight = 44f;
        t.InputTextColor = Color.FromHex("000000");               // --color → item text
        t.InputPlaceholderColor = Color.FromHex("666666");        // step-400 neutral placeholder
        t.InputLabelColor = Color.FromHex("666666");              // neutral form-control label
        t.InputBackground = Color.Transparent;                    // --background: transparent
        t.InputBorderColor = new Color(0, 0, 0, 18);              // $item-md-border-color rgba(0,0,0,.07)
        t.InputHighlightColor = t.Primary;                        // --highlight-color-focused: primary
        t.InputHelperColor = Color.FromHex("666666");             // $text-color-step-300
        t.InputErrorColor = t.Danger;                             // --highlight-color-invalid: danger
        t.InputHighlightHeight = 2f;                              // --highlight-height: 2px
        t.InputBorderRadius = 4f;
        t.InputPaddingStart = 0f;
        t.InputPaddingEnd = 0f;
        t.InputClearIconColor = Color.FromHex("666666");          // $text-color-step-400
        t.InputDisabledOpacity = 0.38f;                           // $input-md-disabled-opacity

        // Breadcrumb (breadcrumb.md.scss / breadcrumb.md.vars.scss): step-600 text, near-black active
        // at weight 500, 6px/12px native padding (square corners), a "/" separator in step-550 gray
        // with 10px side margins, step-550 icons.
        t.BreadcrumbColor = Color.FromHex("677483");              // $breadcrumb-md-color
        t.BreadcrumbColorActive = Color.FromHex("03060b");        // $breadcrumb-md-color-active
        t.BreadcrumbFontSize = 16f;
        t.BreadcrumbActiveFontWeight = FontWeight.Medium;         // md active weight 500
        t.BreadcrumbPaddingY = 6f;
        t.BreadcrumbPaddingX = 12f;
        t.BreadcrumbBorderRadius = 0f;                            // md native has no radius
        t.BreadcrumbSeparatorColor = Color.FromHex("73849a");     // $breadcrumb-separator-color
        t.BreadcrumbSeparatorMarginX = 10f;
        t.BreadcrumbIconColor = Color.FromHex("7d8894");          // $breadcrumb-md-icon-color
        t.BreadcrumbIconColorActive = Color.FromHex("222d3a");    // $breadcrumb-md-icon-color-active
        t.BreadcrumbIconFontSize = 18f;
        t.BreadcrumbIndicatorBackground = Color.FromHex("eef1f3"); // $breadcrumb-md-indicator-background
        t.BreadcrumbIndicatorColor = Color.FromHex("73849a");     // step-550

        // Accordion (accordion.md.vars.scss): white panels, 0.4 disabled opacity, 16px inset margin,
        // 6px inset radius with a 3-layer Material elevation shadow on inset panels.
        t.AccordionBackground = Color.FromHex("ffffff");
        t.AccordionDisabledOpacity = 0.4f;
        t.AccordionInsetMargin = 16f;
        t.AccordionInsetBorderRadius = 6f;
        t.AccordionInsetBoxShadow = new List<BoxShadow>
        {
            new BoxShadow(0, 3, 1, -2, new Color(0, 0, 0, 51)),  // 0.2 * 255
            new BoxShadow(0, 2, 2, 0, new Color(0, 0, 0, 36)),   // 0.14 * 255
            new BoxShadow(0, 1, 5, 0, new Color(0, 0, 0, 31)),   // 0.12 * 255
        };

        // ActionSheet (action-sheet.md.scss / .md.vars.scss): a flat white group filling to the
        // bottom edge; left-aligned title in rgba(0,0,0,.54) and left-aligned buttons in step-150
        // dark text. No group radius/margins; a subtle backdrop (0.32).
        t.ActionSheetBackdropColor = Color.FromHex("000000");
        t.ActionSheetBackdropOpacity = 0.32f;
        t.ActionSheetBackground = Color.FromHex("ffffff");        // $overlay-md-background-color
        t.ActionSheetBorderRadius = 0f;
        t.ActionSheetMaxWidth = 500f;
        t.ActionSheetContainerPaddingX = 0f;
        t.ActionSheetGroupMarginTop = 0f;
        t.ActionSheetGroupMarginBottom = 0f;
        t.ActionSheetTitleColor = new Color(0, 0, 0, 138);        // rgba(text,.54)
        t.ActionSheetTitleFontSize = 16f;
        t.ActionSheetTitlePaddingY = 20f;                         // top 20 / bottom 17 → use 20 top
        t.ActionSheetTitlePaddingX = 16f;
        t.ActionSheetSubTitleFontSize = 14f;
        t.ActionSheetButtonColor = Color.FromHex("262626");        // $text-color-step-150
        t.ActionSheetButtonHeight = 52f;
        t.ActionSheetButtonFontSize = 16f;
        t.ActionSheetButtonPaddingY = 12f;
        t.ActionSheetButtonPaddingX = 16f;
        t.ActionSheetIconFontSize = 24f;
        t.ActionSheetButtonJustify = JustifyContent.FlexStart;    // md left-aligns content
        t.ActionSheetTextAlign = TextAlign.Left;
        t.ActionSheetDestructiveColor = t.Danger;
        t.ActionSheetButtonBorderColor = Color.Transparent;       // md has no button divider
        t.ActionSheetCancelFontWeight = FontWeight.Normal;

        // Alert (alert.md.scss / .md.vars.scss): a 4px-radius white card with a 3-layer Material
        // shadow; left-aligned head (20px/500 title, 16px sub-title), step-450 message; a right-
        // aligned button row with 2px-radius, uppercase, primary-colored buttons. Radio/checkbox
        // controls use a step-550 unchecked border and the primary accent.
        t.AlertBackdropColor = Color.FromHex("000000");
        t.AlertBackdropOpacity = 0.32f;
        t.AlertBackground = Color.FromHex("ffffff");              // $overlay-md-background-color
        t.AlertMinWidth = 250f;
        t.AlertMaxWidth = 280f;
        t.AlertBorderRadius = 4f;
        t.AlertBoxShadow = new List<BoxShadow>
        {
            new BoxShadow(0, 11, 15, -7, new Color(0, 0, 0, 51)), // rgba(0,0,0,.2)
            new BoxShadow(0, 24, 38, 3, new Color(0, 0, 0, 36)),  // rgba(0,0,0,.14)
            new BoxShadow(0, 9, 46, 8, new Color(0, 0, 0, 31)),   // rgba(0,0,0,.12)
        };
        t.AlertHeadPaddingY = 20f;
        t.AlertHeadPaddingX = 23f;
        t.AlertHeadTextAlign = TextAlign.Left;
        t.AlertTitleColor = Color.FromHex("000000");              // $text-color
        t.AlertTitleFontSize = 20f;
        t.AlertTitleFontWeight = FontWeight.Medium;               // 500
        t.AlertSubTitleColor = Color.FromHex("000000");
        t.AlertSubTitleFontSize = 16f;
        t.AlertMessagePaddingY = 20f;
        t.AlertMessagePaddingX = 24f;
        t.AlertMessageColor = Color.FromHex("737373");            // $text-color-step-450
        t.AlertMessageFontSize = 16f;
        t.AlertButtonGroupPadding = 8f;
        t.AlertButtonGroupJustify = JustifyContent.FlexEnd;       // right-aligned row
        t.AlertButtonColor = t.Primary;                           // ion-color(primary, base)
        t.AlertButtonFontSize = 14f;
        t.AlertButtonFontWeight = FontWeight.Medium;              // 500
        t.AlertButtonBorderRadius = 2f;
        t.AlertButtonTextTransform = TextTransform.Uppercase;     // md uppercases labels
        t.AlertButtonPadding = 10f;
        t.AlertButtonMarginX = 8f;
        t.AlertListBorderColor = Color.FromHex("d9d9d9");         // $background-color-step-150
        t.AlertControlBorderColorOff = Color.FromHex("737373");   // $background-color-step-550
        t.AlertControlAccent = t.Primary;
        t.AlertTappableHeight = 48f;                              // $item-md-min-height

        // Datetime (datetime.md.scss / .md.vars.scss): white surface, a primary-filled header with a
        // white 12px title and a 34px selected date; step-500 weekdays at 14px; 42px round day cells;
        // the active day is a solid primary circle with white text; today shows a primary border/text.
        t.DatetimeBackground = Color.FromHex("ffffff");           // step-100 → white
        t.DatetimeHeaderBackground = t.Primary;                   // md header is the brand color
        t.DatetimeHeaderColor = Color.FromHex("ffffff");          // primary contrast
        t.DatetimeTitleFontSize = 12f;
        t.DatetimeSelectedDateFontSize = 34f;
        t.DatetimeDayOfWeekColor = Color.FromHex("808080");       // $text-color-step-500
        t.DatetimeDayOfWeekFontSize = 14f;
        t.DatetimeMonthYearColor = Color.FromHex("595959");       // $text-color-step-350
        t.DatetimeDayColor = Color.FromHex("000000");
        t.DatetimeDaySize = 42f;
        t.DatetimeDayFontSize = 14f;
        t.DatetimeDayActiveBackground = t.Primary;                // solid primary circle
        t.DatetimeDayActiveColor = Color.FromHex("ffffff");       // white on primary
        t.DatetimeTodayColor = t.Primary;

        // Datetime button (datetime-button.md.scss): 6px/12px padding, light-gray pill, primary when active.
        t.DatetimeButtonBackground = Color.FromHex("edeef0");     // step-300 fallback
        t.DatetimeButtonColor = Color.FromHex("000000");
        t.DatetimeButtonActiveColor = t.Primary;
        t.DatetimeButtonBorderRadius = 8f;
        t.DatetimeButtonPaddingY = 6f;
        t.DatetimeButtonPaddingX = 12f;
        t.DatetimeButtonFontSize = 16f;

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

        // Checkbox (checkbox.ios.scss / checkbox.ios.vars.scss): 22px circle, 2px border, 50% radius
        // (a circle), unchecked border rgba(text,.23), white fill; checked fills with primary and
        // shows a white checkmark. Disabled dims the whole host to .3.
        t.CheckboxSize = 22f;
        t.CheckboxBorderWidth = 2f;
        t.CheckboxBorderRadius = Length.Px(11f);                  // 50% of 22px → a circle
        t.CheckboxBorderColorOff = new Color(0, 0, 0, 59);       // rgba(text,.23)
        t.CheckboxBackgroundOff = Color.FromHex("ffffff");        // $item-ios-background
        t.CheckboxBackgroundChecked = t.Primary;                  // --checkbox-background-checked
        t.CheckboxCheckmarkColor = Color.FromHex("ffffff");       // ion-color(primary, contrast)
        t.CheckboxDisabledOpacity = 0.3f;                         // $form-control-ios-disabled-opacity

        // Fab (fab-button.ios.scss / fab-button.ios.vars.scss): 56px round primary button with a
        // soft single-layer shadow and a larger 28px slotted icon; mini button 40px. List buttons
        // use the light surface with a dark icon at 18px.
        t.FabSize = 56f;
        t.FabSmallSize = 40f;
        t.FabContentMargin = 10f;
        t.FabListMargin = 10f;
        t.FabButtonSmallMargin = 8f;
        t.FabBackground = t.Primary;                              // ion-color(primary, base)
        t.FabColor = Color.FromHex("ffffff");                     // ion-color(primary, contrast)
        t.FabBoxShadow = new List<BoxShadow>
        {
            new BoxShadow(0, 4, 16, 0, new Color(0, 0, 0, 31)),   // 0 4px 16px rgba(0,0,0,.12)
        };
        t.FabIconFontSize = 28f;                                  // $fab-ios-icon-font-size
        t.FabListButtonBackground = t.Light;                      // ion-color(light, base)
        t.FabListButtonColor = Color.FromHex("000000");           // ion-color(light, contrast)
        t.FabListButtonIconSize = 18f;                            // $fab-ios-list-button-icon-font-size

        // Input (input.ios.scss / input.ios.vars.scss): 17px text; 44px min-height; hairline item
        // bottom border rgba(0,0,0,.2); NO highlight bar (ios --highlight-height: 0); radius 4.
        // Disabled dims the host to .3.
        t.InputFontSize = 17f;
        t.InputMinHeight = 44f;
        t.InputTextColor = Color.FromHex("000000");               // --color → item text
        t.InputPlaceholderColor = Color.FromHex("666666");        // step-400 neutral placeholder
        t.InputLabelColor = Color.FromHex("666666");              // neutral form-control label
        t.InputBackground = Color.Transparent;                    // --background: transparent
        t.InputBorderColor = new Color(0, 0, 0, 51);              // $item-ios-border-color rgba(0,0,0,.2)
        t.InputHighlightColor = t.Primary;                        // --highlight-color-focused: primary
        t.InputHelperColor = Color.FromHex("666666");             // $text-color-step-300
        t.InputErrorColor = t.Danger;                             // --highlight-color-invalid: danger
        t.InputHighlightHeight = 0f;                              // ios --highlight-height: 0px
        t.InputBorderRadius = 4f;
        t.InputPaddingStart = 0f;
        t.InputPaddingEnd = 0f;
        t.InputClearIconColor = Color.FromHex("666666");          // $text-color-step-400
        t.InputDisabledOpacity = 0.3f;                            // $input-ios-disabled-opacity

        // Breadcrumb (breadcrumb.ios.scss / breadcrumb.ios.vars.scss): darker step-850 text, near-black
        // active at weight 600, 5px/12px native padding with a 4px radius, a chevron separator in
        // step-550 gray, step-400 icons. Collapsed indicator uses a slightly bluer light fill.
        t.BreadcrumbColor = Color.FromHex("2d4665");              // $breadcrumb-ios-color
        t.BreadcrumbColorActive = Color.FromHex("03060b");        // $breadcrumb-ios-color-active
        t.BreadcrumbFontSize = 16f;
        t.BreadcrumbActiveFontWeight = FontWeight.SemiBold;       // ios active weight 600
        t.BreadcrumbPaddingY = 5f;
        t.BreadcrumbPaddingX = 12f;
        t.BreadcrumbBorderRadius = 4f;                            // ios native has a 4px radius
        t.BreadcrumbSeparatorColor = Color.FromHex("73849a");     // $breadcrumb-separator-color
        t.BreadcrumbSeparatorMarginX = 10f;
        t.BreadcrumbIconColor = Color.FromHex("92a0b3");          // $breadcrumb-ios-icon-color
        t.BreadcrumbIconColorActive = Color.FromHex("242d39");    // $breadcrumb-ios-icon-color-active
        t.BreadcrumbIconFontSize = 18f;
        t.BreadcrumbIndicatorBackground = Color.FromHex("e9edf3"); // $breadcrumb-ios-indicator-background
        t.BreadcrumbIndicatorColor = Color.FromHex("73849a");     // step-550

        // Accordion (accordion.ios / accordion.vars.scss): white panels, 0.4 disabled opacity, 16px
        // inset margin. iOS inset panels have no elevation shadow (they use item hairline borders).
        t.AccordionBackground = Color.FromHex("ffffff");
        t.AccordionDisabledOpacity = 0.4f;
        t.AccordionInsetMargin = 16f;
        t.AccordionInsetBorderRadius = 6f;
        t.AccordionInsetBoxShadow = new List<BoxShadow>();       // ios: no elevation on inset panels

        // ActionSheet (action-sheet.ios.scss / .ios.vars.scss): a floating rounded group with 8px
        // side padding and 10px top/bottom margins; centered title in step-600 gray and centered
        // buttons in the primary color, split by hairline dividers. Destructive = danger, cancel is
        // its own bold rounded group. Backdrop is heavier (0.4).
        t.ActionSheetBackdropColor = Color.FromHex("000000");
        t.ActionSheetBackdropOpacity = 0.4f;
        t.ActionSheetBackground = Color.FromHex("f9f9f9");        // $overlay-ios-background-color
        t.ActionSheetBorderRadius = 13f;
        t.ActionSheetMaxWidth = 500f;
        t.ActionSheetContainerPaddingX = 8f;
        t.ActionSheetGroupMarginTop = 10f;
        t.ActionSheetGroupMarginBottom = 10f;
        t.ActionSheetTitleColor = Color.FromHex("999999");        // $text-color-step-600
        t.ActionSheetTitleFontSize = 13f;
        t.ActionSheetTitlePaddingY = 14f;
        t.ActionSheetTitlePaddingX = 10f;
        t.ActionSheetSubTitleFontSize = 13f;
        t.ActionSheetButtonColor = t.Primary;                     // ion-color(primary, base)
        t.ActionSheetButtonHeight = 56f;
        t.ActionSheetButtonFontSize = 20f;
        t.ActionSheetButtonPaddingY = 14f;
        t.ActionSheetButtonPaddingX = 14f;
        t.ActionSheetIconFontSize = 28f;
        t.ActionSheetButtonJustify = JustifyContent.Center;       // ios centers content
        t.ActionSheetTextAlign = TextAlign.Center;
        t.ActionSheetDestructiveColor = t.Danger;
        t.ActionSheetButtonBorderColor = new Color(0, 0, 0, 20);  // rgba(text,.08) hairline divider
        t.ActionSheetCancelFontWeight = FontWeight.SemiBold;      // ios cancel 600

        // Alert (alert.ios.scss / .ios.vars.scss): a 13px-radius #f9f9f9 card with no shadow;
        // centered head (17px/600 title, 14px step-400 sub-title), 13px message; a button row split
        // by hairline dividers, primary-colored, not uppercased. Backdrop is heavier (0.4).
        t.AlertBackdropColor = Color.FromHex("000000");
        t.AlertBackdropOpacity = 0.4f;
        t.AlertBackground = Color.FromHex("f9f9f9");              // $overlay-ios-background-color
        t.AlertMinWidth = 250f;
        t.AlertMaxWidth = 270f;
        t.AlertBorderRadius = 13f;
        t.AlertBoxShadow = new List<BoxShadow>();                 // ios alert has no elevation
        t.AlertHeadPaddingY = 12f;
        t.AlertHeadPaddingX = 16f;
        t.AlertHeadTextAlign = TextAlign.Center;
        t.AlertTitleColor = Color.FromHex("000000");              // $text-color
        t.AlertTitleFontSize = 17f;
        t.AlertTitleFontWeight = FontWeight.SemiBold;             // 600
        t.AlertSubTitleColor = Color.FromHex("666666");           // $text-color-step-400
        t.AlertSubTitleFontSize = 14f;
        t.AlertMessagePaddingY = 12f;                             // top 0 / bottom 21 → use 12 avg
        t.AlertMessagePaddingX = 16f;
        t.AlertMessageColor = Color.FromHex("000000");            // $text-color
        t.AlertMessageFontSize = 13f;
        t.AlertButtonGroupPadding = 0f;                           // ios group has no padding
        t.AlertButtonGroupJustify = JustifyContent.Center;
        t.AlertButtonColor = t.Primary;                          // ion-color(primary, base)
        t.AlertButtonFontSize = 17f;
        t.AlertButtonFontWeight = FontWeight.Normal;
        t.AlertButtonBorderRadius = 0f;                          // ios buttons are square, divided
        t.AlertButtonTextTransform = TextTransform.None;         // ios keeps label case
        t.AlertButtonPadding = 8f;
        t.AlertButtonMarginX = 0f;
        t.AlertListBorderColor = new Color(0, 0, 0, 51);         // rgba(text,.2) hairline
        t.AlertControlBorderColorOff = Color.FromHex("bfbfbf");   // $background-color-step-250
        t.AlertControlAccent = t.Primary;
        t.AlertTappableHeight = 44f;                             // $item-ios-min-height

        // Datetime (datetime.ios.scss / .ios.vars.scss): a light (ion-color light) surface with a
        // matching (non-primary) header, dark title; gray weekdays at 12px; 40px round day cells;
        // the active day is a translucent-primary circle with primary text; today shows primary text.
        t.DatetimeBackground = t.Light;                          // ion-color(light) surface
        t.DatetimeHeaderBackground = t.Light;                    // ios header matches the surface
        t.DatetimeHeaderColor = Color.FromHex("000000");         // dark title on the light header
        t.DatetimeTitleFontSize = 14f;
        t.DatetimeSelectedDateFontSize = 16f;
        t.DatetimeDayOfWeekColor = Color.FromHex("808080");      // neutral gray
        t.DatetimeDayOfWeekFontSize = 12f;
        t.DatetimeMonthYearColor = Color.FromHex("000000");
        t.DatetimeDayColor = Color.FromHex("000000");
        t.DatetimeDaySize = 40f;
        t.DatetimeDayFontSize = 20f;
        t.DatetimeDayActiveBackground = new Color(t.Primary.R, t.Primary.G, t.Primary.B, 51); // primary @ .2
        t.DatetimeDayActiveColor = t.Primary;                    // primary text on the translucent circle
        t.DatetimeTodayColor = t.Primary;

        // Datetime button (datetime-button.ios.scss): 7px/13px padding, light-gray pill, primary when active.
        t.DatetimeButtonBackground = Color.FromHex("edeef0");
        t.DatetimeButtonColor = Color.FromHex("000000");
        t.DatetimeButtonActiveColor = t.Primary;
        t.DatetimeButtonBorderRadius = 8f;
        t.DatetimeButtonPaddingY = 7f;
        t.DatetimeButtonPaddingX = 13f;
        t.DatetimeButtonFontSize = 16f;

        return t;
    }
}
