using Miko.Ionic.Components;
using Miko.Ionic.Styles;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>
/// Generates Ionic utility/component styles. <see cref="IonicStyleRegistry"/> uses these on
/// demand; the public aggregate factories remain available for standalone DOM/style consumers.
/// <para>
/// The stylesheet carries BOTH the <c>md</c> and <c>ios</c> mode rule sets, each scoped by the
/// matching mode class that every component stamps onto its root element. This lets the active
/// mode switch at runtime (e.g. the simulator swapping the selected device's platform) by simply
/// changing the root class — no stylesheet rebuild is required.
/// </para>
/// </summary>
public static class IonicStyleSheetFactory
{
    /// <summary>
    /// The cascade layer the Ionic stylesheet sits in (see <see cref="StyleSheet.Layer"/>).
    /// Ionic's component rules mirror the per-component shadow-tree <c>:host</c>/slotted styles
    /// of real Ionic — which always lose to outer-document rules (CSS Scoping). Placing the
    /// sheet below the application layer (0) reproduces that: an app rule like
    /// <c>.my-icon { width: 50px }</c> overrides the compound <c>.ion-icon.md</c> host rule
    /// even though its specificity is lower (ISSUE-107).
    /// </summary>
    public const int CascadeLayer = -1;

    /// <summary>Builds the Ionic utility stylesheet without any component rules.</summary>
    public static StyleSheet CreateGlobal()
    {
        var sheet = new StyleSheet { Layer = CascadeLayer };
        sheet.Add(GlobalStyle.GenStyle());
        return sheet;
    }

    /// <summary>
    /// Builds a stylesheet containing both Material Design and iOS rule sets, each from its own
    /// per-mode theme. Use this so the active mode can switch at runtime via the component mode
    /// class alone.
    /// </summary>
    public static StyleSheet CreateAllModes()
    {
        var sheet = CreateGlobal();
        AddMode(sheet, "md", IonicTheme.CreateMd());
        AddMode(sheet, "ios", IonicTheme.CreateIos());
        return sheet;
    }

    /// <summary>
    /// Builds a stylesheet for a single mode from the given theme. Both mode rule sets are still
    /// emitted when possible so a runtime switch keeps working, but the supplied theme's mode is
    /// authored from <paramref name="theme"/> (e.g. a customized theme). The other mode falls back
    /// to its default values.
    /// </summary>
    public static StyleSheet Create(IonicTheme theme)
    {
        var sheet = CreateGlobal();
        if (theme.Mode == IonicMode.Ios)
        {
            AddMode(sheet, "md", IonicTheme.CreateMd());
            AddMode(sheet, "ios", theme);
        }
        else
        {
            AddMode(sheet, "md", theme);
            AddMode(sheet, "ios", IonicTheme.CreateIos());
        }
        return sheet;
    }

    private static void AddMode(StyleSheet sheet, string mode, IonicTheme t)
    {
        sheet.Add(OverlayStyles.GenStyle(mode));
        sheet.Add(PageStyles.GenStyle(mode, t));
        sheet.Add(HeaderStyles.GenStyle(mode, t));
        sheet.Add(ContentStyles.GenStyle(mode, t));
        sheet.Add(TabStyles.GenStyle(mode, t));
        sheet.Add(IconStyles.GenStyle(mode, t));
        sheet.Add(LabelStyles.GenStyle(mode, t));
        sheet.Add(MenuStyles.GenStyle(mode, t));
        sheet.Add(ListStyles.GenStyle(mode, t));
        sheet.Add(BaseItemStyles.GenStyle(mode, t));
        sheet.Add(ItemStyles.GenStyle(mode, t));
        sheet.Add(SegmentStyles.GenStyle(mode, t));
        sheet.Add(ToolbarStyles.GenStyle(mode, t));
        sheet.Add(TitleStyles.GenStyle(mode, t));
        sheet.Add(ButtonStyles.GenStyle(mode, t));
        sheet.Add(BackButtonStyles.GenStyle(mode, t));
        sheet.Add(SearchbarStyles.GenStyle(mode, t));
        sheet.Add(FooterStyles.GenStyle(mode, t));
        sheet.Add(SlidesStyles.GenStyle(mode, t));
        sheet.Add(AvatarStyles.GenStyle(mode, t));
        sheet.Add(SpinnerStyles.GenStyle(mode, t));
        sheet.Add(BadgeStyles.GenStyle(mode, t));
        sheet.Add(NoteStyles.GenStyle(mode, t));
        sheet.Add(TextStyles.GenStyle(mode, t));
        sheet.Add(ThumbnailStyles.GenStyle(mode, t));
        sheet.Add(SkeletonTextStyles.GenStyle(mode, t));
        sheet.Add(ChipStyles.GenStyle(mode, t));
        sheet.Add(CardStyles.GenStyle(mode, t));
        sheet.Add(GridStyles.GenStyle(mode, t));
        sheet.Add(InfiniteScrollStyles.GenStyle(mode, t));
        sheet.Add(RefresherStyles.GenStyle(mode, t));
        sheet.Add(SelectStyles.GenStyle(mode, t));
        sheet.Add(CheckboxStyles.GenStyle(mode, t));
        sheet.Add(ToggleStyles.GenStyle(mode, t));
        sheet.Add(RadioStyles.GenStyle(mode, t));
        sheet.Add(RangeStyles.GenStyle(mode, t));
        sheet.Add(InputStyles.GenStyle(mode, t));
        sheet.Add(InputOtpStyles.GenStyle(mode, t));
        sheet.Add(TextareaStyles.GenStyle(mode, t));
        sheet.Add(ReorderStyles.GenStyle(mode, t));
        sheet.Add(ProgressBarStyles.GenStyle(mode, t));
        sheet.Add(FabStyles.GenStyle(mode, t));
        sheet.Add(BreadcrumbStyles.GenStyle(mode, t));
        sheet.Add(AccordionStyles.GenStyle(mode, t));
        sheet.Add(ActionSheetStyles.GenStyle(mode, t));
        sheet.Add(AlertStyles.GenStyle(mode, t));
        sheet.Add(DatetimeStyles.GenStyle(mode, t));
        sheet.Add(ModalStyles.GenStyle(mode, t));
        sheet.Add(PopoverStyles.GenStyle(mode, t));
        sheet.Add(PickerStyles.GenStyle(mode, t));
        sheet.Add(LoadingStyles.GenStyle(mode, t));
        sheet.Add(ToastStyles.GenStyle(mode, t));
    }

    internal static CssObject? CreateComponentStyle(string component, string mode, IonicTheme theme) => component switch
    {
        "Accordion" => AccordionStyles.GenStyle(mode, theme),
        "ActionSheet" => ActionSheetStyles.GenStyle(mode, theme),
        "Alert" => AlertStyles.GenStyle(mode, theme),
        "Avatar" => AvatarStyles.GenStyle(mode, theme),
        "BackButton" => BackButtonStyles.GenStyle(mode, theme),
        "Badge" => BadgeStyles.GenStyle(mode, theme),
        "Breadcrumb" => BreadcrumbStyles.GenStyle(mode, theme),
        "Button" => ButtonStyles.GenStyle(mode, theme),
        "Card" => CardStyles.GenStyle(mode, theme),
        "Checkbox" => CheckboxStyles.GenStyle(mode, theme),
        "Chip" => ChipStyles.GenStyle(mode, theme),
        "Content" => ContentStyles.GenStyle(mode, theme),
        "Datetime" => DatetimeStyles.GenStyle(mode, theme),
        "Fab" => FabStyles.GenStyle(mode, theme),
        "Footer" => FooterStyles.GenStyle(mode, theme),
        "Grid" => GridStyles.GenStyle(mode, theme),
        "Header" => HeaderStyles.GenStyle(mode, theme),
        "Icon" => IconStyles.GenStyle(mode, theme),
        "InfiniteScroll" => InfiniteScrollStyles.GenStyle(mode, theme),
        "Input" => InputStyles.GenStyle(mode, theme),
        "InputOtp" => InputOtpStyles.GenStyle(mode, theme),
        "Item" => ItemStyles.GenStyle(mode, theme),
        "Label" => LabelStyles.GenStyle(mode, theme),
        "List" => ListStyles.GenStyle(mode, theme),
        "Loading" => LoadingStyles.GenStyle(mode, theme),
        "Menu" => MenuStyles.GenStyle(mode, theme),
        "Modal" => ModalStyles.GenStyle(mode, theme),
        "Note" => NoteStyles.GenStyle(mode, theme),
        "Overlay" => OverlayStyles.GenStyle(mode),
        "Page" => PageStyles.GenStyle(mode, theme),
        "Picker" => PickerStyles.GenStyle(mode, theme),
        "Popover" => PopoverStyles.GenStyle(mode, theme),
        "ProgressBar" => ProgressBarStyles.GenStyle(mode, theme),
        "Radio" => RadioStyles.GenStyle(mode, theme),
        "Range" => RangeStyles.GenStyle(mode, theme),
        "Refresher" => RefresherStyles.GenStyle(mode, theme),
        "Reorder" => ReorderStyles.GenStyle(mode, theme),
        "Searchbar" => SearchbarStyles.GenStyle(mode, theme),
        "Segment" => SegmentStyles.GenStyle(mode, theme),
        "Select" => SelectStyles.GenStyle(mode, theme),
        "SkeletonText" => SkeletonTextStyles.GenStyle(mode, theme),
        "Slides" => SlidesStyles.GenStyle(mode, theme),
        "Spinner" => SpinnerStyles.GenStyle(mode, theme),
        "Tab" => TabStyles.GenStyle(mode, theme),
        "Text" => TextStyles.GenStyle(mode, theme),
        "Textarea" => TextareaStyles.GenStyle(mode, theme),
        "Thumbnail" => ThumbnailStyles.GenStyle(mode, theme),
        "Title" => TitleStyles.GenStyle(mode, theme),
        "Toast" => ToastStyles.GenStyle(mode, theme),
        "Toggle" => ToggleStyles.GenStyle(mode, theme),
        "Toolbar" => ToolbarStyles.GenStyle(mode, theme),
        _ => null,
    };
}
