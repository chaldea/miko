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
public partial class IonicTheme
{
    /// <summary>The design mode this theme was built for.</summary>
    public IonicMode Mode { get; set; } = IonicMode.Md;

    public AccordionToken Accordion { get; set; } = new();
    public ActionSheetToken ActionSheet { get; set; } = new();
    public AlertToken Alert { get; set; } = new();
    public AvatarToken Avatar { get; set; } = new();
    public BadgeToken Badge { get; set; } = new();
    public BreadcrumbToken Breadcrumb { get; set; } = new();
    public ButtonToken Button { get; set; } = new();
    public CardToken Card { get; set; } = new();
    public CheckboxToken Checkbox { get; set; } = new();
    public ChipToken Chip { get; set; } = new();
    public DatetimeToken Datetime { get; set; } = new();
    public FabToken Fab { get; set; } = new();
    public GridToken Grid { get; set; } = new();
    public HeaderToken Header { get; set; } = new();
    public InfiniteScrollToken InfiniteScroll { get; set; } = new();
    public InputToken Input { get; set; } = new();
    public ItemToken Item { get; set; } = new();
    public LabelToken Label { get; set; } = new();
    public ListToken List { get; set; } = new();
    public MenuToken Menu { get; set; } = new();
    public NoteToken Note { get; set; } = new();
    public ProgressBarToken ProgressBar { get; set; } = new();
    public PaletteToken Palette { get; set; } = new();
    public RefresherToken Refresher { get; set; } = new();
    public SearchbarToken Searchbar { get; set; } = new();
    public SegmentToken Segment { get; set; } = new();
    public SelectToken Select { get; set; } = new();
    public SpinnerToken Spinner { get; set; } = new();
    public ContentToken Content { get; set; } = new();
    public SkeletonTextToken SkeletonText { get; set; } = new();
    public SlidesToken Slides { get; set; } = new();
    public TabToken Tab { get; set; } = new();
    public ThumbnailToken Thumbnail { get; set; } = new();
    public TitleToken Title { get; set; } = new();
    public ToastToken Toast { get; set; } = new();
    public ToggleToken Toggle { get; set; } = new();
    public ToolbarToken Toolbar { get; set; } = new();

    /// <summary>
    /// Resolves this possibly partial theme against the platform mode's complete defaults.
    /// A token supplied through <see cref="Components.ConfigProvider"/> may therefore override only
    /// the properties it explicitly sets.
    /// </summary>
    internal IonicTheme ResolveForMode(IonicMode mode)
    {
        var resolved = Create(mode);
        ApplySpecifiedValuesTo(resolved);
        resolved.Mode = mode;
        return resolved;
    }

    internal void ApplySpecifiedValuesTo(IonicTheme target)
    {
        Accordion.CopySpecifiedValuesTo(target.Accordion);
        ActionSheet.CopySpecifiedValuesTo(target.ActionSheet);
        Alert.CopySpecifiedValuesTo(target.Alert);
        Avatar.CopySpecifiedValuesTo(target.Avatar);
        Badge.CopySpecifiedValuesTo(target.Badge);
        Breadcrumb.CopySpecifiedValuesTo(target.Breadcrumb);
        Button.CopySpecifiedValuesTo(target.Button);
        Card.CopySpecifiedValuesTo(target.Card);
        Checkbox.CopySpecifiedValuesTo(target.Checkbox);
        Chip.CopySpecifiedValuesTo(target.Chip);
        Datetime.CopySpecifiedValuesTo(target.Datetime);
        Fab.CopySpecifiedValuesTo(target.Fab);
        Grid.CopySpecifiedValuesTo(target.Grid);
        Header.CopySpecifiedValuesTo(target.Header);
        InfiniteScroll.CopySpecifiedValuesTo(target.InfiniteScroll);
        Input.CopySpecifiedValuesTo(target.Input);
        Item.CopySpecifiedValuesTo(target.Item);
        Label.CopySpecifiedValuesTo(target.Label);
        List.CopySpecifiedValuesTo(target.List);
        Menu.CopySpecifiedValuesTo(target.Menu);
        Note.CopySpecifiedValuesTo(target.Note);
        ProgressBar.CopySpecifiedValuesTo(target.ProgressBar);
        Palette.CopySpecifiedValuesTo(target.Palette);
        Refresher.CopySpecifiedValuesTo(target.Refresher);
        Searchbar.CopySpecifiedValuesTo(target.Searchbar);
        Segment.CopySpecifiedValuesTo(target.Segment);
        Select.CopySpecifiedValuesTo(target.Select);
        Spinner.CopySpecifiedValuesTo(target.Spinner);
        Content.CopySpecifiedValuesTo(target.Content);
        SkeletonText.CopySpecifiedValuesTo(target.SkeletonText);
        Slides.CopySpecifiedValuesTo(target.Slides);
        Tab.CopySpecifiedValuesTo(target.Tab);
        Thumbnail.CopySpecifiedValuesTo(target.Thumbnail);
        Title.CopySpecifiedValuesTo(target.Title);
        Toast.CopySpecifiedValuesTo(target.Toast);
        Toggle.CopySpecifiedValuesTo(target.Toggle);
        Toolbar.CopySpecifiedValuesTo(target.Toolbar);
    }

    /// <summary>Returns a deterministic fingerprint used to reuse an equivalent rule set.</summary>
    internal string GetStyleKey(string component, IonicMode mode)
    {
        var builder = new System.Text.StringBuilder(component).Append('|').Append(mode);
        IonicToken[] dependencies = component switch
        {
            "Accordion" => [Accordion, Item, Palette],
            "ActionSheet" => [ActionSheet],
            "Alert" => [Alert, Checkbox],
            "Avatar" => [Avatar],
            "BackButton" => [Palette],
            "Badge" => [Badge, Palette],
            "Breadcrumb" => [Breadcrumb, Palette],
            "Button" => [Button, Palette, Toolbar],
            "Card" => [Card, Palette],
            "Checkbox" => [Checkbox, Item, Palette, Select],
            "Chip" => [Chip, Palette],
            "Content" => [Content, Palette],
            "Datetime" => [Datetime, Palette],
            "Fab" => [Fab, Palette],
            "Footer" => [Header],
            "Grid" => [Grid],
            "Header" => [Header],
            "Icon" => [Palette, Tab],
            "InfiniteScroll" => [InfiniteScroll],
            "Input" => [Input, Palette],
            "InputOtp" => [Input, Item, Palette],
            "Item" => [Item, List, Palette],
            "Label" => [Label, Palette, Tab],
            "List" => [Item, List, Palette],
            "Loading" => [ActionSheet, Palette],
            "Menu" => [Menu, Palette, Toolbar],
            "Modal" => [Alert, Palette],
            "Note" => [Note, Palette],
            "Overlay" => [],
            "Page" => [],
            "Picker" => [Palette],
            "Popover" => [Alert, Palette],
            "ProgressBar" => [Palette, ProgressBar],
            "Radio" => [Checkbox, Item, Palette, Select],
            "Range" => [Item, Palette],
            "Refresher" => [Refresher],
            "Reorder" => [],
            "Searchbar" => [Palette, Searchbar],
            "Segment" => [Palette, Segment, Toolbar],
            "Select" => [Palette, Select],
            "SkeletonText" => [SkeletonText],
            "Slides" => [Slides],
            "Spinner" => [Palette, Spinner],
            "Tab" => [Tab],
            "Text" => [Palette],
            "Textarea" => [Input, Palette],
            "Thumbnail" => [Thumbnail],
            "Title" => [Title, Toolbar],
            "Toast" => [Palette, Toast],
            "Toggle" => [Item, Palette, Select, Toggle],
            "Toolbar" => [Toolbar],
            _ => [],
        };
        foreach (var token in dependencies)
            token.AppendFingerprint(builder);
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes.AsSpan(0, 8)).ToLowerInvariant();
    }
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

        // Tab button badge (tab-button.md.vars.scss): 8px font, 3/2/2/2 padding, min-width 12,
        // radius 8; an empty badge collapses to an 8x8 dot.
        t.TabButtonBadgeFontSize = 8f;
        t.TabButtonBadgeMinWidth = 12f;
        t.TabButtonBadgeBorderRadius = 8f;
        t.TabButtonBadgePaddingTop = 3f;
        t.TabButtonBadgePaddingEnd = 2f;
        t.TabButtonBadgePaddingBottom = 2f;
        t.TabButtonBadgePaddingStart = 2f;
        t.TabButtonBadgeSizeEmpty = 8f;

        // Toolbar (toolbar.md.scss): min-height 56, bg #fff, color #424242.
        t.ToolbarBackground = Color.FromHex("ffffff");
        t.ToolbarColor = Color.FromHex("424242");
        t.ToolbarBorderColor = Color.FromHex("b2b2b2");
        t.ToolbarMinHeight = 56f;
        t.ToolbarPaddingTop = Length.Px(0);
        t.ToolbarPaddingBottom = Length.Px(0);
        t.ToolbarPaddingStart = Length.Px(0);
        t.ToolbarPaddingEnd = Length.Px(0);

        // Toolbar buttons (buttons.md.scss `::slotted(*) ion-button`): 32px tall, 3px/8px padding,
        // 2px radius ($toolbar-md-button-border-radius), 1.4em start/end icons, 1.8em icon-only.
        t.ToolbarButtonMinHeight = Length.Px(32);
        t.ToolbarButtonPaddingTop = Length.Px(3);
        t.ToolbarButtonPaddingBottom = Length.Px(3);
        t.ToolbarButtonPaddingStart = Length.Px(8);
        t.ToolbarButtonPaddingEnd = Length.Px(8);
        t.ToolbarButtonMarginX = 2f;
        t.ToolbarButtonBorderRadius = 2f;
        t.ToolbarButtonIconFontSize = 1.4f;
        t.ToolbarButtonIconOnlyFontSize = 1.8f;
        t.ToolbarButtonIconOnlyClearSize = 48f;   // 3rem
        t.ToolbarButtonIconOnlyClearPadding = Length.Px(12);

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
        t.ItemPaddingEnd = 16f;                                  // $item-md-padding-end → --inner-padding-end
        t.ItemLabelMarginVertical = 10f;                         // $item-md-label-margin-top/bottom
        t.ItemLabelMarginEnd = 0f;                               // $item-md-label-margin-end
        t.ItemIconSlotMarginVertical = 12f;                      // $item-md-icon-slot-margin-top/bottom
        t.ItemIconStartSlotMarginEnd = 32f;                      // $item-md-icon-start-slot-margin-end
        t.ItemIconEndSlotMarginStart = 16f;                      // $item-md-icon-end-slot-margin-start
        t.ItemAvatarSlotMarginVertical = 8f;                     // $item-md-media-slot-margin-top/bottom
        t.ItemAvatarStartSlotMarginEnd = 16f;                    // $item-md-media-start-slot-margin-end
        t.ItemAvatarEndSlotMarginStart = 16f;                    // $item-md-media-end-slot-margin-start
        t.ListInsetMargin = 16f;                                 // $list-inset-md-margin-*
        t.ListInsetBorderRadius = 2f;                            // $list-inset-md-border-radius

        // Item divider (item-divider.md.scss): light gray fill (step-50 ~#f2f2f2), bottom border
        // rgba(0,0,0,.07), medium text (step-550). 30px min-height.
        t.ItemDividerBackground = Color.FromHex("f2f2f2");
        t.ItemDividerColor = Color.FromHex("404040");            // step-550
        t.ItemDividerMinHeight = 30f;

        // Item option (item-option.md.scss): white label on the primary fill.
        t.ItemOptionColor = Color.FromHex("ffffff");
        t.ItemOptionBackground = t.Primary;

        // Segment (segment.md.scss / segment-button.md.scss): transparent track; the checked
        // button shows a 2px primary underline bar (the indicator) and its label turns primary —
        // the button background stays transparent (md does NOT fill the whole button).
        t.SegmentBackground = Color.Transparent;                 // --background: transparent
        t.SegmentBorderRadius = 0f;
        t.SegmentButtonColor = new Color(0, 0, 0, 153);         // rgba(text-color-rgb, .6)
        t.SegmentButtonCheckedColor = t.Primary;                 // --color-checked: primary
        t.SegmentIndicatorColor = t.Primary;                     // --indicator-color: color-checked
        t.SegmentIndicatorHeight = Length.Px(2);                 // --indicator-height: 2px
        t.SegmentIndicatorBorderRadius = 0f;                     // square underline bar
        t.SegmentIndicatorBoxShadow = new();                     // --indicator-box-shadow: none
        t.SegmentButtonFontSize = 14f;
        t.SegmentButtonMinWidth = 90f;
        t.SegmentButtonMinHeight = 48f;
        t.SegmentButtonLineHeight = 40f;
        t.SegmentButtonLetterSpacing = Length.Em(0.06f);
        t.SegmentButtonPaddingX = 16f;
        t.SegmentButtonMarginY = 0f;
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
        t.SlidesNavigationColor = t.Primary;                      // --swiper-theme-color

        // Avatar (avatar.md.vars.scss): 64px square.
        t.AvatarSize = 64f;
        // Item avatar (item.md.vars.scss $item-md-avatar-width): 40px when slotted inside ion-item.
        t.ItemAvatarSize = 40f;

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
        t.SelectPaddingTop = 0f;
        t.SelectPaddingBottom = 0f;
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
        t.FabHoverOpacity = 0.08f;                                // --background-hover-opacity (md)

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
        t.BreadcrumbIndicatorBorderRadius = 2f;                   // md indicator radius

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
        t.ActionSheetEnterDuration = 0.4f;                        // md.enter.ts .duration(400)
        t.ActionSheetLeaveDuration = 0.45f;                       // md.leave.ts .duration(450)

        // Toast (toast.md.scss / .md.vars.scss): the md enter/leave animations only cross-fade the
        // wrapper; the ±8px edge offset comes from getAnimationPosition (mode === 'md' → 8).
        t.ToastEdgeOffset = 8f;                                   // animations/utils.ts md offset
        t.ToastEnterDuration = 0.4f;                              // md.enter.ts .duration(400)
        t.ToastLeaveDuration = 0.3f;                              // md.leave.ts .duration(300)

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
        t.AlertButtonGroupFlexWrap = FlexWrap.WrapReverse;        // overflow wraps upward
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

        // Label (label.md.vars.scss): wrapped text only loosens line-height; stacked labels zero
        // their margins and keep the inherited font size.
        t.LabelTextWrapLineHeight = 1.5f;                         // $label-md-text-wrap-line-height
        t.LabelTextWrapFontSize = null;                           // md has no font-size override
        t.LabelStackedMarginBottom = 0f;                          // label.md.scss @include margin(0,0,0,0)
        t.LabelStackedFontSize = null;                            // md has no font-size override

        // Note (note.md.vars.scss): muted gray text (text-color-step-400 ≈ #666666), 14px.
        t.NoteColor = Color.FromHex("666666");                    // $text-color-step-400
        t.NoteFontSize = 14f;                                     // dynamic-font(14px)

        // Thumbnail (thumbnail.scss): 48px square, no per-mode difference.
        t.ThumbnailSize = 48f;
        // Item thumbnail (item.md.vars.scss $item-md-thumbnail-size): 56px when slotted in an item.
        t.ItemThumbnailSize = 56f;

        // Skeleton text (skeleton-text.vars.scss): rgba(text,.065) fill; animated pulses to rgba(text,.135).
        t.SkeletonTextBackground = new Color(0, 0, 0, 17);        // rgba(0,0,0,.065)
        t.SkeletonTextBackgroundAnimated = new Color(0, 0, 0, 34); // rgba(0,0,0,.135)

        // Toggle (toggle.md.scss / toggle.md.vars.scss): 36x14 track, radius = track-height (14px);
        // off track rgba(text,.39); on track primary @ .5 alpha; 20px round white knob (radius 10 =
        // 50%) with a 3-layer Material shadow, no inset, sliding 16px (track-width - handle-width);
        // 160ms slide; disabled dims the host to .38.
        t.ToggleTrackWidth = 36f;
        t.ToggleTrackHeight = 14f;
        t.ToggleBorderRadius = Length.Px(14);                     // = $toggle-md-track-height
        t.ToggleTrackBackgroundOff = new Color(0, 0, 0, 99);      // rgba(text,.39)
        t.ToggleTrackBackgroundOn = new Color(t.Primary.R, t.Primary.G, t.Primary.B, 128); // primary @ .5
        t.ToggleTrackCheckedAlpha = 0.5f;                         // $toggle-md-track-background-color-alpha-on
        t.ToggleHandleWidth = 20f;
        t.ToggleHandleHeight = 20f;
        t.ToggleHandleBorderRadius = Length.Px(10);               // 50% of a 20px knob → a circle
        t.ToggleHandleBackground = Color.White;
        t.ToggleHandleBackgroundChecked = t.Primary;              // --handle-background-checked
        t.ToggleHandleBoxShadow = new List<BoxShadow>
        {
            new BoxShadow(0, 3, 1, -2, new Color(0, 0, 0, 51)),   // rgba(0,0,0,.2)
            new BoxShadow(0, 2, 2, 0, new Color(0, 0, 0, 36)),    // rgba(0,0,0,.14)
            new BoxShadow(0, 1, 5, 0, new Color(0, 0, 0, 31)),    // rgba(0,0,0,.12)
        };
        t.ToggleHandleSpacing = 0f;                               // --handle-spacing: 0
        t.ToggleHandleTravel = 36f - 20f;                         // track-width - handle-width = 16px
        t.ToggleTransitionDuration = 0.16f;                       // $toggle-md-transition-duration
        t.ToggleDisabledOpacity = 0.38f;                          // $form-control-md-disabled-opacity

        // ProgressBar (progress-bar.md.scss / .md.vars.scss): 4px tall, square corners; track/buffer
        // primary @ .3 alpha; fill solid primary.
        t.ProgressBarHeight = 4f;
        t.ProgressBarBorderRadius = 0f;                           // md has no radius
        t.ProgressBarBackground = new Color(t.Primary.R, t.Primary.G, t.Primary.B, 77);   // primary @ .3
        t.ProgressBarProgressBackground = t.Primary;

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

        // Tab button badge (tab-button.ios.scss): 12px font, 1/6 padding. No radius/min-width
        // override there, so the base badge values (radius 10, min-width 10) carry over.
        t.TabButtonBadgeFontSize = 12f;
        t.TabButtonBadgeMinWidth = 10f;
        t.TabButtonBadgeBorderRadius = 10f;
        t.TabButtonBadgePaddingTop = 1f;
        t.TabButtonBadgePaddingEnd = 6f;
        t.TabButtonBadgePaddingBottom = 1f;
        t.TabButtonBadgePaddingStart = 6f;

        // Toolbar (toolbar.ios): min-height 44, bg #f7f7f7, color #000.
        t.ToolbarBackground = Color.FromHex("f7f7f7");
        t.ToolbarColor = Color.FromHex("000000");
        t.ToolbarBorderColor = Color.FromHex("c8c7cc");
        t.ToolbarMinHeight = 44f;
        t.ToolbarPaddingTop = Length.Px(0);
        t.ToolbarPaddingBottom = Length.Px(0);
        t.ToolbarPaddingStart = Length.Px(0);
        t.ToolbarPaddingEnd = Length.Px(0);

        // Toolbar buttons (buttons.ios.scss `::slotted(*) ion-button`): 32px tall, 3px/5px padding,
        // 4px radius ($toolbar-ios-button-border-radius). Icon sizes are expressed against the 17px
        // iOS button font so they land on Ionic's intended pixel sizes: 1.41em ≈ 24px start/end,
        // 1.65em ≈ 28px icon-only. iOS has no circular clear icon-only treatment (MD-only rule).
        t.ToolbarButtonMinHeight = Length.Px(32);
        t.ToolbarButtonPaddingTop = Length.Px(3);
        t.ToolbarButtonPaddingBottom = Length.Px(3);
        t.ToolbarButtonPaddingStart = Length.Px(5);
        t.ToolbarButtonPaddingEnd = Length.Px(5);
        t.ToolbarButtonMarginX = 2f;
        t.ToolbarButtonBorderRadius = 4f;
        t.ToolbarButtonIconFontSize = 1.41f;
        t.ToolbarButtonIconOnlyFontSize = 1.65f;
        t.ToolbarButtonIconOnlyClearSize = 0f;    // no iOS equivalent
        t.ToolbarButtonIconOnlyClearPadding = Length.Px(0);

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
        t.ItemPaddingEnd = 16f;                                 // $item-ios-padding-end → --inner-padding-end
        t.ItemLabelMarginVertical = 10f;                        // item.ios.scss ::slotted(ion-label)
        t.ItemLabelMarginEnd = 8f;                              // item.ios.scss ::slotted(ion-label)
        t.ItemIconSlotMarginVertical = 7f;                      // $item-ios-icon-slot-margin-top/bottom
        t.ItemIconStartSlotMarginEnd = 16f;                     // $item-ios-slot-start-margin-end survives the icon rule
        t.ItemIconEndSlotMarginStart = 0f;                      // iOS gives slotted icons no horizontal margin
        t.ItemAvatarSlotMarginVertical = 0f;                    // iOS sizes slotted avatars only
        t.ItemAvatarStartSlotMarginEnd = 0f;                    // iOS gives start avatars no margin
        t.ItemAvatarEndSlotMarginStart = 8f;                    // item.ios.scss ::slotted(ion-avatar[slot="end"])
        t.ListInsetMargin = 16f;                                // $list-inset-ios-margin-*
        t.ListInsetBorderRadius = 10f;                          // $list-inset-ios-border-radius

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
        t.SegmentBackground = new Color(0, 0, 0, 17);            // rgba(0,0,0,.065)
        t.SegmentBorderRadius = 8f;
        t.SegmentButtonColor = Color.FromHex("000000");          // iOS text color (dark)
        t.SegmentButtonCheckedColor = Color.FromHex("000000");   // checked label stays dark
        t.SegmentIndicatorColor = Color.FromHex("ffffff");       // light elevated pill surface
        t.SegmentIndicatorHeight = Length.Percent(100);          // --indicator-height: 100%
        t.SegmentIndicatorBorderRadius = 7f;                     // --border-radius: 7px (rounded pill)
        t.SegmentIndicatorBoxShadow = new()                      // 0 0 5px rgba(0,0,0,.16)
        {
            new BoxShadow(Length.Px(0), Length.Px(0), Length.Px(5), Length.Px(0), new Color(0, 0, 0, 41)),
        };
        t.SegmentButtonFontSize = 13f;
        t.SegmentButtonMinWidth = 70f;
        t.SegmentButtonMinHeight = 28f;                          // iOS slightly shorter
        t.SegmentButtonLineHeight = 37f;
        t.SegmentButtonLetterSpacing = Length.Px(0);
        t.SegmentButtonPaddingX = 13f;
        t.SegmentButtonMarginY = 2f;
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
        t.SlidesNavigationColor = t.Primary;                      // --swiper-theme-color

        // Avatar (avatar.ios.vars.scss): 48px square.
        t.AvatarSize = 48f;
        // Item avatar (item.ios.vars.scss $item-ios-avatar-width): 36px when slotted inside ion-item.
        t.ItemAvatarSize = 36f;

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
        t.SelectPaddingTop = 0f;
        t.SelectPaddingBottom = 0f;
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
        // ios sets --background-hover-opacity: 1 with --background-hover: ion-color(primary, tint),
        // i.e. a full swap to a lighter tint rather than a wash. Without tint/shade in the palette
        // this port approximates it with a stronger white wash over the fill.
        t.FabHoverOpacity = 0.16f;

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
        t.BreadcrumbIndicatorBorderRadius = 4f;                   // ios indicator radius

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
        t.ActionSheetEnterDuration = 0.4f;                        // ios.enter.ts .duration(400)
        t.ActionSheetLeaveDuration = 0.45f;                       // ios.leave.ts .duration(450)

        // Toast (toast.ios.scss / .ios.vars.scss): the ios enter/leave animations slide the wrapper
        // in from off-screen; the ±10px edge offset comes from getAnimationPosition (else → 10).
        t.ToastEdgeOffset = 10f;                                  // animations/utils.ts ios offset
        t.ToastEnterDuration = 0.4f;                              // ios.enter.ts .duration(400)
        t.ToastLeaveDuration = 0.3f;                              // ios.leave.ts .duration(300)

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
        t.AlertButtonGroupFlexWrap = FlexWrap.Wrap;               // ios wraps downward
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

        // Label (label.ios.vars.scss / label.ios.scss): wrapped text drops to 14px at line-height
        // 1.5; a stacked label gets a 4px bottom margin and a 14px font.
        t.LabelTextWrapLineHeight = 1.5f;                       // $label-ios-text-wrap-line-height
        t.LabelTextWrapFontSize = 14f;                          // $label-ios-text-wrap-font-size
        t.LabelStackedMarginBottom = 4f;                        // label.ios.scss @include margin(null,null,4px,null)
        t.LabelStackedFontSize = 14f;                           // label.ios.scss dynamic-font(14px)

        // Note (note.ios.vars.scss): lighter gray text (text-color-step-650 ≈ #a6a6a6), 14px
        // (dynamic-font-min(0.875, 16px) → 0.875 * 16).
        t.NoteColor = Color.FromHex("a6a6a6");                    // $text-color-step-650
        t.NoteFontSize = 14f;                                     // 0.875 * 16px

        // Thumbnail (thumbnail.scss): 48px square, no per-mode difference.
        t.ThumbnailSize = 48f;
        // Item thumbnail (item.ios.vars.scss $item-ios-thumbnail-size): 56px when slotted in an item.
        t.ItemThumbnailSize = 56f;

        // Skeleton text (skeleton-text.vars.scss): rgba(text,.065) fill; animated pulses to rgba(text,.135).
        t.SkeletonTextBackground = new Color(0, 0, 0, 17);        // rgba(0,0,0,.065)
        t.SkeletonTextBackgroundAnimated = new Color(0, 0, 0, 34); // rgba(0,0,0,.135)

        // Toggle (toggle.ios.scss / toggle.ios.vars.scss): 51x31 track, radius = height*.5 (15.5px,
        // a full pill); off track rgba(text,.088); on track solid primary; 27px round white knob
        // (= height - border*2; radius = width*.5 = 25.5px) with a soft 2-layer shadow, inset 2px,
        // sliding 24px (track-width - handle-width); 300ms slide; disabled dims the host to .3.
        t.ToggleTrackWidth = 51f;
        t.ToggleTrackHeight = 31f;
        t.ToggleBorderRadius = Length.Px(15.5f);                  // $toggle-ios-height * 0.5
        t.ToggleTrackBackgroundOff = new Color(0, 0, 0, 22);      // rgba(text,.088)
        t.ToggleTrackBackgroundOn = t.Primary;                    // ion-color(primary, base)
        t.ToggleTrackCheckedAlpha = 1f;                           // ios paints a solid base color
        t.ToggleHandleWidth = 27f;                                // height - border*2 = 31 - 4
        t.ToggleHandleHeight = 27f;
        t.ToggleHandleBorderRadius = Length.Px(25.5f);            // $toggle-ios-width * 0.5
        t.ToggleHandleBackground = Color.White;
        t.ToggleHandleBackgroundChecked = Color.White;            // --handle-background-checked (unchanged)
        t.ToggleHandleBoxShadow = new List<BoxShadow>
        {
            new BoxShadow(0, 3, 4, 0, new Color(0, 0, 0, 15)),    // 0 3px 4px rgba(0,0,0,.06)
            new BoxShadow(0, 3, 8, 0, new Color(0, 0, 0, 15)),    // 0 3px 8px rgba(0,0,0,.06)
        };
        t.ToggleHandleSpacing = 2f;                               // = $toggle-ios-border-width
        t.ToggleHandleTravel = 51f - 27f;                         // track-width - handle-width = 24px
        t.ToggleTransitionDuration = 0.30f;                       // $toggle-ios-transition-duration
        t.ToggleDisabledOpacity = 0.3f;                           // $toggle-ios-disabled-opacity

        // ProgressBar (progress-bar.ios.scss / .ios.vars.scss): 4px tall, fully-rounded (9999px);
        // track/buffer primary @ .3 alpha; fill solid primary.
        t.ProgressBarHeight = 4f;
        t.ProgressBarBorderRadius = 9999f;                        // $progress-bar-ios-border-radius
        t.ProgressBarBackground = new Color(t.Primary.R, t.Primary.G, t.Primary.B, 77);   // primary @ .3
        t.ProgressBarProgressBackground = t.Primary;

        return t;
    }
}
