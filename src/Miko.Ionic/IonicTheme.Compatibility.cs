using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

public partial class IonicTheme
{
    // Compatibility facade. New code should access the component tokens on IonicTheme.
    public float TabBarHeight
    {
        get => Tab.BarHeight;
        set => Tab.BarHeight = value;
    }
    public float TabBarBorderWidth
    {
        get => Tab.BarBorderWidth;
        set => Tab.BarBorderWidth = value;
    }
    public float TabButtonFontSize
    {
        get => Tab.ButtonFontSize;
        set => Tab.ButtonFontSize = value;
    }
    public float TabButtonIconSize
    {
        get => Tab.ButtonIconSize;
        set => Tab.ButtonIconSize = value;
    }
    public float TabButtonMaxWidth
    {
        get => Tab.ButtonMaxWidth;
        set => Tab.ButtonMaxWidth = value;
    }
    public float TabButtonPaddingX
    {
        get => Tab.ButtonPaddingX;
        set => Tab.ButtonPaddingX = value;
    }
    public float TabButtonBadgeFontSize
    {
        get => Tab.ButtonBadgeFontSize;
        set => Tab.ButtonBadgeFontSize = value;
    }
    public float TabButtonBadgeMinWidth
    {
        get => Tab.ButtonBadgeMinWidth;
        set => Tab.ButtonBadgeMinWidth = value;
    }
    public float TabButtonBadgeBorderRadius
    {
        get => Tab.ButtonBadgeBorderRadius;
        set => Tab.ButtonBadgeBorderRadius = value;
    }
    public float TabButtonBadgePaddingTop
    {
        get => Tab.ButtonBadgePaddingTop;
        set => Tab.ButtonBadgePaddingTop = value;
    }
    public float TabButtonBadgePaddingEnd
    {
        get => Tab.ButtonBadgePaddingEnd;
        set => Tab.ButtonBadgePaddingEnd = value;
    }
    public float TabButtonBadgePaddingBottom
    {
        get => Tab.ButtonBadgePaddingBottom;
        set => Tab.ButtonBadgePaddingBottom = value;
    }
    public float TabButtonBadgePaddingStart
    {
        get => Tab.ButtonBadgePaddingStart;
        set => Tab.ButtonBadgePaddingStart = value;
    }
    public float TabButtonBadgeSizeEmpty
    {
        get => Tab.ButtonBadgeSizeEmpty;
        set => Tab.ButtonBadgeSizeEmpty = value;
    }
    public float ToolbarMinHeight
    {
        get => Toolbar.MinHeight;
        set => Toolbar.MinHeight = value;
    }
    public Length ToolbarPaddingTop
    {
        get => Toolbar.PaddingTop;
        set => Toolbar.PaddingTop = value;
    }
    public Length ToolbarPaddingBottom
    {
        get => Toolbar.PaddingBottom;
        set => Toolbar.PaddingBottom = value;
    }
    public Length ToolbarPaddingStart
    {
        get => Toolbar.PaddingStart;
        set => Toolbar.PaddingStart = value;
    }
    public Length ToolbarPaddingEnd
    {
        get => Toolbar.PaddingEnd;
        set => Toolbar.PaddingEnd = value;
    }
    public Length ToolbarButtonMinHeight
    {
        get => Toolbar.ButtonMinHeight;
        set => Toolbar.ButtonMinHeight = value;
    }
    public Length ToolbarButtonPaddingTop
    {
        get => Toolbar.ButtonPaddingTop;
        set => Toolbar.ButtonPaddingTop = value;
    }
    public Length ToolbarButtonPaddingBottom
    {
        get => Toolbar.ButtonPaddingBottom;
        set => Toolbar.ButtonPaddingBottom = value;
    }
    public Length ToolbarButtonPaddingStart
    {
        get => Toolbar.ButtonPaddingStart;
        set => Toolbar.ButtonPaddingStart = value;
    }
    public Length ToolbarButtonPaddingEnd
    {
        get => Toolbar.ButtonPaddingEnd;
        set => Toolbar.ButtonPaddingEnd = value;
    }
    public float ToolbarButtonMarginX
    {
        get => Toolbar.ButtonMarginX;
        set => Toolbar.ButtonMarginX = value;
    }
    public float ToolbarButtonBorderRadius
    {
        get => Toolbar.ButtonBorderRadius;
        set => Toolbar.ButtonBorderRadius = value;
    }
    public float ToolbarButtonIconFontSize
    {
        get => Toolbar.ButtonIconFontSize;
        set => Toolbar.ButtonIconFontSize = value;
    }
    public float ToolbarButtonIconOnlyFontSize
    {
        get => Toolbar.ButtonIconOnlyFontSize;
        set => Toolbar.ButtonIconOnlyFontSize = value;
    }
    public float ToolbarButtonIconOnlyClearSize
    {
        get => Toolbar.ButtonIconOnlyClearSize;
        set => Toolbar.ButtonIconOnlyClearSize = value;
    }
    public Length ToolbarButtonIconOnlyClearPadding
    {
        get => Toolbar.ButtonIconOnlyClearPadding;
        set => Toolbar.ButtonIconOnlyClearPadding = value;
    }
    public float TitleFontSize
    {
        get => Title.FontSize;
        set => Title.FontSize = value;
    }
    public FontWeight TitleFontWeight
    {
        get => Title.FontWeight;
        set => Title.FontWeight = value;
    }
    public float TitlePaddingX
    {
        get => Title.PaddingX;
        set => Title.PaddingX = value;
    }
    public TextAlign TitleTextAlign
    {
        get => Title.TextAlign;
        set => Title.TextAlign = value;
    }
    public List<BoxShadow> HeaderBoxShadow
    {
        get => Header.BoxShadow;
        set => Header.BoxShadow = value;
    }
    public float MenuWidth
    {
        get => Menu.Width;
        set => Menu.Width = value;
    }
    public List<BoxShadow> MenuBoxShadow
    {
        get => Menu.BoxShadow;
        set => Menu.BoxShadow = value;
    }
    public float MenuAnimDuration
    {
        get => Menu.AnimDuration;
        set => Menu.AnimDuration = value;
    }
    public float ListHeaderFontSize
    {
        get => List.HeaderFontSize;
        set => List.HeaderFontSize = value;
    }
    public float ItemMinHeight
    {
        get => Item.MinHeight;
        set => Item.MinHeight = value;
    }
    public float ItemPaddingStart
    {
        get => Item.PaddingStart;
        set => Item.PaddingStart = value;
    }
    public float ItemPaddingEnd
    {
        get => Item.PaddingEnd;
        set => Item.PaddingEnd = value;
    }
    public float ItemLabelMarginVertical
    {
        get => Item.LabelMarginVertical;
        set => Item.LabelMarginVertical = value;
    }
    public float ItemLabelMarginEnd
    {
        get => Item.LabelMarginEnd;
        set => Item.LabelMarginEnd = value;
    }
    public float ItemIconSlotMarginVertical
    {
        get => Item.IconSlotMarginVertical;
        set => Item.IconSlotMarginVertical = value;
    }
    public float ItemIconStartSlotMarginEnd
    {
        get => Item.IconStartSlotMarginEnd;
        set => Item.IconStartSlotMarginEnd = value;
    }
    public float ItemIconEndSlotMarginStart
    {
        get => Item.IconEndSlotMarginStart;
        set => Item.IconEndSlotMarginStart = value;
    }
    public float ItemAvatarSlotMarginVertical
    {
        get => Item.AvatarSlotMarginVertical;
        set => Item.AvatarSlotMarginVertical = value;
    }
    public float ItemAvatarStartSlotMarginEnd
    {
        get => Item.AvatarStartSlotMarginEnd;
        set => Item.AvatarStartSlotMarginEnd = value;
    }
    public float ItemAvatarEndSlotMarginStart
    {
        get => Item.AvatarEndSlotMarginStart;
        set => Item.AvatarEndSlotMarginStart = value;
    }
    public float ListInsetMargin
    {
        get => List.InsetMargin;
        set => List.InsetMargin = value;
    }
    public float ListInsetBorderRadius
    {
        get => List.InsetBorderRadius;
        set => List.InsetBorderRadius = value;
    }
    public float ItemDividerMinHeight
    {
        get => Item.DividerMinHeight;
        set => Item.DividerMinHeight = value;
    }
    public Length SegmentIndicatorHeight
    {
        get => Segment.IndicatorHeight;
        set => Segment.IndicatorHeight = value;
    }
    public List<BoxShadow> SegmentIndicatorBoxShadow
    {
        get => Segment.IndicatorBoxShadow;
        set => Segment.IndicatorBoxShadow = value;
    }
    public float SegmentButtonFontSize
    {
        get => Segment.ButtonFontSize;
        set => Segment.ButtonFontSize = value;
    }
    public float SegmentButtonMinWidth
    {
        get => Segment.ButtonMinWidth;
        set => Segment.ButtonMinWidth = value;
    }
    public float SegmentButtonMinHeight
    {
        get => Segment.ButtonMinHeight;
        set => Segment.ButtonMinHeight = value;
    }
    public float SegmentButtonLineHeight
    {
        get => Segment.ButtonLineHeight;
        set => Segment.ButtonLineHeight = value;
    }
    public Length SegmentButtonLetterSpacing
    {
        get => Segment.ButtonLetterSpacing;
        set => Segment.ButtonLetterSpacing = value;
    }
    public float SegmentButtonPaddingX
    {
        get => Segment.ButtonPaddingX;
        set => Segment.ButtonPaddingX = value;
    }
    public float SegmentButtonPaddingY
    {
        get => Segment.ButtonPaddingY;
        set => Segment.ButtonPaddingY = value;
    }
    public float ButtonBorderRadius
    {
        get => Button.BorderRadius;
        set => Button.BorderRadius = value;
    }
    public float ButtonRoundBorderRadius
    {
        get => Button.RoundBorderRadius;
        set => Button.RoundBorderRadius = value;
    }
    public Length ButtonMinHeight
    {
        get => Button.MinHeight;
        set => Button.MinHeight = value;
    }
    public Length ButtonPaddingTop
    {
        get => Button.PaddingTop;
        set => Button.PaddingTop = value;
    }
    public Length ButtonPaddingBottom
    {
        get => Button.PaddingBottom;
        set => Button.PaddingBottom = value;
    }
    public Length ButtonPaddingStart
    {
        get => Button.PaddingStart;
        set => Button.PaddingStart = value;
    }
    public Length ButtonPaddingEnd
    {
        get => Button.PaddingEnd;
        set => Button.PaddingEnd = value;
    }
    public float ButtonFontSize
    {
        get => Button.FontSize;
        set => Button.FontSize = value;
    }
    public FontWeight ButtonFontWeight
    {
        get => Button.FontWeight;
        set => Button.FontWeight = value;
    }
    public TextTransform ButtonTextTransform
    {
        get => Button.TextTransform;
        set => Button.TextTransform = value;
    }
    public Length ButtonLetterSpacing
    {
        get => Button.LetterSpacing;
        set => Button.LetterSpacing = value;
    }
    public FontWeight ButtonStrongFontWeight
    {
        get => Button.StrongFontWeight;
        set => Button.StrongFontWeight = value;
    }
    public List<BoxShadow> ButtonSolidBoxShadow
    {
        get => Button.SolidBoxShadow;
        set => Button.SolidBoxShadow = value;
    }
    public Length ButtonOutlineBorderWidth
    {
        get => Button.OutlineBorderWidth;
        set => Button.OutlineBorderWidth = value;
    }
    public Length ButtonLargeMinHeight
    {
        get => Button.LargeMinHeight;
        set => Button.LargeMinHeight = value;
    }
    public Length ButtonLargePaddingTop
    {
        get => Button.LargePaddingTop;
        set => Button.LargePaddingTop = value;
    }
    public Length ButtonLargePaddingBottom
    {
        get => Button.LargePaddingBottom;
        set => Button.LargePaddingBottom = value;
    }
    public Length ButtonLargePaddingX
    {
        get => Button.LargePaddingX;
        set => Button.LargePaddingX = value;
    }
    public float ButtonLargeFontSize
    {
        get => Button.LargeFontSize;
        set => Button.LargeFontSize = value;
    }
    public float ButtonLargeBorderRadius
    {
        get => Button.LargeBorderRadius;
        set => Button.LargeBorderRadius = value;
    }
    public Length ButtonSmallMinHeight
    {
        get => Button.SmallMinHeight;
        set => Button.SmallMinHeight = value;
    }
    public Length ButtonSmallPaddingTop
    {
        get => Button.SmallPaddingTop;
        set => Button.SmallPaddingTop = value;
    }
    public Length ButtonSmallPaddingBottom
    {
        get => Button.SmallPaddingBottom;
        set => Button.SmallPaddingBottom = value;
    }
    public Length ButtonSmallPaddingX
    {
        get => Button.SmallPaddingX;
        set => Button.SmallPaddingX = value;
    }
    public float ButtonSmallFontSize
    {
        get => Button.SmallFontSize;
        set => Button.SmallFontSize = value;
    }
    public float ButtonSmallBorderRadius
    {
        get => Button.SmallBorderRadius;
        set => Button.SmallBorderRadius = value;
    }
    public float ButtonIconOnlyMinSize
    {
        get => Button.IconOnlyMinSize;
        set => Button.IconOnlyMinSize = value;
    }
    public float ButtonIconOnlyIconSize
    {
        get => Button.IconOnlyIconSize;
        set => Button.IconOnlyIconSize = value;
    }
    public float ButtonSmallIconOnlyMinSize
    {
        get => Button.SmallIconOnlyMinSize;
        set => Button.SmallIconOnlyMinSize = value;
    }
    public float ButtonSmallIconOnlyIconSize
    {
        get => Button.SmallIconOnlyIconSize;
        set => Button.SmallIconOnlyIconSize = value;
    }
    public float ButtonLargeIconOnlyMinSize
    {
        get => Button.LargeIconOnlyMinSize;
        set => Button.LargeIconOnlyMinSize = value;
    }
    public float ButtonLargeIconOnlyIconSize
    {
        get => Button.LargeIconOnlyIconSize;
        set => Button.LargeIconOnlyIconSize = value;
    }
    public float SearchbarPaddingTop
    {
        get => Searchbar.PaddingTop;
        set => Searchbar.PaddingTop = value;
    }
    public float SearchbarPaddingEnd
    {
        get => Searchbar.PaddingEnd;
        set => Searchbar.PaddingEnd = value;
    }
    public float SearchbarPaddingBottom
    {
        get => Searchbar.PaddingBottom;
        set => Searchbar.PaddingBottom = value;
    }
    public float SearchbarPaddingStart
    {
        get => Searchbar.PaddingStart;
        set => Searchbar.PaddingStart = value;
    }
    public float SearchbarInputBorderRadius
    {
        get => Searchbar.InputBorderRadius;
        set => Searchbar.InputBorderRadius = value;
    }
    public List<BoxShadow> SearchbarInputBoxShadow
    {
        get => Searchbar.InputBoxShadow;
        set => Searchbar.InputBoxShadow = value;
    }
    public float SearchbarInputFontSize
    {
        get => Searchbar.InputFontSize;
        set => Searchbar.InputFontSize = value;
    }
    public Length SearchbarInputHeight
    {
        get => Searchbar.InputHeight;
        set => Searchbar.InputHeight = value;
    }
    public Length SearchbarInputLineHeight
    {
        get => Searchbar.InputLineHeight;
        set => Searchbar.InputLineHeight = value;
    }
    public float SearchbarSearchIconSize
    {
        get => Searchbar.SearchIconSize;
        set => Searchbar.SearchIconSize = value;
    }
    public float SearchbarClearIconSize
    {
        get => Searchbar.ClearIconSize;
        set => Searchbar.ClearIconSize = value;
    }
    public float SearchbarCancelButtonFontSize
    {
        get => Searchbar.CancelButtonFontSize;
        set => Searchbar.CancelButtonFontSize = value;
    }
    public float AvatarSize
    {
        get => Avatar.Size;
        set => Avatar.Size = value;
    }
    public float ItemAvatarSize
    {
        get => Item.AvatarSize;
        set => Item.AvatarSize = value;
    }
    public float SpinnerSize
    {
        get => Spinner.Size;
        set => Spinner.Size = value;
    }
    public float SpinnerSmallSize
    {
        get => Spinner.SmallSize;
        set => Spinner.SmallSize = value;
    }
    public float BadgeBorderRadius
    {
        get => Badge.BorderRadius;
        set => Badge.BorderRadius = value;
    }
    public float BadgeFontSize
    {
        get => Badge.FontSize;
        set => Badge.FontSize = value;
    }
    public float BadgePaddingTop
    {
        get => Badge.PaddingTop;
        set => Badge.PaddingTop = value;
    }
    public float BadgePaddingEnd
    {
        get => Badge.PaddingEnd;
        set => Badge.PaddingEnd = value;
    }
    public float BadgePaddingBottom
    {
        get => Badge.PaddingBottom;
        set => Badge.PaddingBottom = value;
    }
    public float BadgePaddingStart
    {
        get => Badge.PaddingStart;
        set => Badge.PaddingStart = value;
    }
    public float BadgeMinWidth
    {
        get => Badge.MinWidth;
        set => Badge.MinWidth = value;
    }
    public float ChipFontSize
    {
        get => Chip.FontSize;
        set => Chip.FontSize = value;
    }
    public float CardMarginTop
    {
        get => Card.MarginTop;
        set => Card.MarginTop = value;
    }
    public float CardMarginEnd
    {
        get => Card.MarginEnd;
        set => Card.MarginEnd = value;
    }
    public float CardMarginBottom
    {
        get => Card.MarginBottom;
        set => Card.MarginBottom = value;
    }
    public float CardMarginStart
    {
        get => Card.MarginStart;
        set => Card.MarginStart = value;
    }
    public float CardBorderRadius
    {
        get => Card.BorderRadius;
        set => Card.BorderRadius = value;
    }
    public float CardFontSize
    {
        get => Card.FontSize;
        set => Card.FontSize = value;
    }
    public Length CardLineHeight
    {
        get => Card.LineHeight;
        set => Card.LineHeight = value;
    }
    public List<BoxShadow> CardBoxShadow
    {
        get => Card.BoxShadow;
        set => Card.BoxShadow = value;
    }
    public float CardHeaderPaddingTop
    {
        get => Card.HeaderPaddingTop;
        set => Card.HeaderPaddingTop = value;
    }
    public float CardHeaderPaddingEnd
    {
        get => Card.HeaderPaddingEnd;
        set => Card.HeaderPaddingEnd = value;
    }
    public float CardHeaderPaddingBottom
    {
        get => Card.HeaderPaddingBottom;
        set => Card.HeaderPaddingBottom = value;
    }
    public float CardHeaderPaddingStart
    {
        get => Card.HeaderPaddingStart;
        set => Card.HeaderPaddingStart = value;
    }
    public float CardContentPaddingTop
    {
        get => Card.ContentPaddingTop;
        set => Card.ContentPaddingTop = value;
    }
    public float CardContentPaddingEnd
    {
        get => Card.ContentPaddingEnd;
        set => Card.ContentPaddingEnd = value;
    }
    public float CardContentPaddingBottom
    {
        get => Card.ContentPaddingBottom;
        set => Card.ContentPaddingBottom = value;
    }
    public float CardContentPaddingStart
    {
        get => Card.ContentPaddingStart;
        set => Card.ContentPaddingStart = value;
    }
    public float CardContentFontSize
    {
        get => Card.ContentFontSize;
        set => Card.ContentFontSize = value;
    }
    public Length CardContentLineHeight
    {
        get => Card.ContentLineHeight;
        set => Card.ContentLineHeight = value;
    }
    public float GridPadding
    {
        get => Grid.Padding;
        set => Grid.Padding = value;
    }
    public float GridColumnPadding
    {
        get => Grid.ColumnPadding;
        set => Grid.ColumnPadding = value;
    }
    public float GridFixedWidth
    {
        get => Grid.FixedWidth;
        set => Grid.FixedWidth = value;
    }
    public float InfiniteScrollContentMinHeight
    {
        get => InfiniteScroll.ContentMinHeight;
        set => InfiniteScroll.ContentMinHeight = value;
    }
    public float RefresherHeight
    {
        get => Refresher.Height;
        set => Refresher.Height = value;
    }
    public float RefresherIconFontSize
    {
        get => Refresher.IconFontSize;
        set => Refresher.IconFontSize = value;
    }
    public float RefresherTextFontSize
    {
        get => Refresher.TextFontSize;
        set => Refresher.TextFontSize = value;
    }
    public float SelectFontSize
    {
        get => Select.FontSize;
        set => Select.FontSize = value;
    }
    public float SelectMinHeight
    {
        get => Select.MinHeight;
        set => Select.MinHeight = value;
    }
    public float SelectPaddingTop
    {
        get => Select.PaddingTop;
        set => Select.PaddingTop = value;
    }
    public float SelectPaddingEnd
    {
        get => Select.PaddingEnd;
        set => Select.PaddingEnd = value;
    }
    public float SelectPaddingBottom
    {
        get => Select.PaddingBottom;
        set => Select.PaddingBottom = value;
    }
    public float SelectPaddingStart
    {
        get => Select.PaddingStart;
        set => Select.PaddingStart = value;
    }
    public float SelectBorderRadius
    {
        get => Select.BorderRadius;
        set => Select.BorderRadius = value;
    }
    public float SelectRoundBorderRadius
    {
        get => Select.RoundBorderRadius;
        set => Select.RoundBorderRadius = value;
    }
    public float CheckboxSize
    {
        get => Checkbox.Size;
        set => Checkbox.Size = value;
    }
    public float CheckboxBorderWidth
    {
        get => Checkbox.BorderWidth;
        set => Checkbox.BorderWidth = value;
    }
    public Length CheckboxBorderRadius
    {
        get => Checkbox.BorderRadius;
        set => Checkbox.BorderRadius = value;
    }
    public float CheckboxDisabledOpacity
    {
        get => Checkbox.DisabledOpacity;
        set => Checkbox.DisabledOpacity = value;
    }
    public float FabSize
    {
        get => Fab.Size;
        set => Fab.Size = value;
    }
    public float FabSmallSize
    {
        get => Fab.SmallSize;
        set => Fab.SmallSize = value;
    }
    public float FabContentMargin
    {
        get => Fab.ContentMargin;
        set => Fab.ContentMargin = value;
    }
    public float FabListMargin
    {
        get => Fab.ListMargin;
        set => Fab.ListMargin = value;
    }
    public float FabButtonSmallMargin
    {
        get => Fab.ButtonSmallMargin;
        set => Fab.ButtonSmallMargin = value;
    }
    public List<BoxShadow> FabBoxShadow
    {
        get => Fab.BoxShadow;
        set => Fab.BoxShadow = value;
    }
    public float FabIconFontSize
    {
        get => Fab.IconFontSize;
        set => Fab.IconFontSize = value;
    }
    public float FabListButtonIconSize
    {
        get => Fab.ListButtonIconSize;
        set => Fab.ListButtonIconSize = value;
    }
    public float FabTransitionDuration
    {
        get => Fab.TransitionDuration;
        set => Fab.TransitionDuration = value;
    }
    public float FabHoverOpacity
    {
        get => Fab.HoverOpacity;
        set => Fab.HoverOpacity = value;
    }
    public float InputFontSize
    {
        get => Input.FontSize;
        set => Input.FontSize = value;
    }
    public float InputMinHeight
    {
        get => Input.MinHeight;
        set => Input.MinHeight = value;
    }
    public float InputHighlightHeight
    {
        get => Input.HighlightHeight;
        set => Input.HighlightHeight = value;
    }
    public float InputBorderRadius
    {
        get => Input.BorderRadius;
        set => Input.BorderRadius = value;
    }
    public float InputPaddingStart
    {
        get => Input.PaddingStart;
        set => Input.PaddingStart = value;
    }
    public float InputPaddingEnd
    {
        get => Input.PaddingEnd;
        set => Input.PaddingEnd = value;
    }
    public float InputDisabledOpacity
    {
        get => Input.DisabledOpacity;
        set => Input.DisabledOpacity = value;
    }
    public float BreadcrumbFontSize
    {
        get => Breadcrumb.FontSize;
        set => Breadcrumb.FontSize = value;
    }
    public FontWeight BreadcrumbActiveFontWeight
    {
        get => Breadcrumb.ActiveFontWeight;
        set => Breadcrumb.ActiveFontWeight = value;
    }
    public float BreadcrumbPaddingY
    {
        get => Breadcrumb.PaddingY;
        set => Breadcrumb.PaddingY = value;
    }
    public float BreadcrumbPaddingX
    {
        get => Breadcrumb.PaddingX;
        set => Breadcrumb.PaddingX = value;
    }
    public float BreadcrumbSeparatorMarginX
    {
        get => Breadcrumb.SeparatorMarginX;
        set => Breadcrumb.SeparatorMarginX = value;
    }
    public float BreadcrumbIconFontSize
    {
        get => Breadcrumb.IconFontSize;
        set => Breadcrumb.IconFontSize = value;
    }
    public float BreadcrumbIconSlotMargin
    {
        get => Breadcrumb.IconSlotMargin;
        set => Breadcrumb.IconSlotMargin = value;
    }
    public float BreadcrumbIndicatorWidth
    {
        get => Breadcrumb.IndicatorWidth;
        set => Breadcrumb.IndicatorWidth = value;
    }
    public float BreadcrumbIndicatorHeight
    {
        get => Breadcrumb.IndicatorHeight;
        set => Breadcrumb.IndicatorHeight = value;
    }
    public float BreadcrumbIndicatorMarginX
    {
        get => Breadcrumb.IndicatorMarginX;
        set => Breadcrumb.IndicatorMarginX = value;
    }
    public float BreadcrumbIndicatorBorderRadius
    {
        get => Breadcrumb.IndicatorBorderRadius;
        set => Breadcrumb.IndicatorBorderRadius = value;
    }
    public float BreadcrumbIndicatorIconSize
    {
        get => Breadcrumb.IndicatorIconSize;
        set => Breadcrumb.IndicatorIconSize = value;
    }
    public float AccordionDisabledOpacity
    {
        get => Accordion.DisabledOpacity;
        set => Accordion.DisabledOpacity = value;
    }
    public float AccordionInsetMargin
    {
        get => Accordion.InsetMargin;
        set => Accordion.InsetMargin = value;
    }
    public float AccordionInsetBorderRadius
    {
        get => Accordion.InsetBorderRadius;
        set => Accordion.InsetBorderRadius = value;
    }
    public List<BoxShadow> AccordionInsetBoxShadow
    {
        get => Accordion.InsetBoxShadow;
        set => Accordion.InsetBoxShadow = value;
    }
    public Color ActionSheetBackdropColor
    {
        get => ActionSheet.BackdropColor;
        set => ActionSheet.BackdropColor = value;
    }
    public float ActionSheetBackdropOpacity
    {
        get => ActionSheet.BackdropOpacity;
        set => ActionSheet.BackdropOpacity = value;
    }
    public float ActionSheetMaxWidth
    {
        get => ActionSheet.MaxWidth;
        set => ActionSheet.MaxWidth = value;
    }
    public float ActionSheetTitleFontSize
    {
        get => ActionSheet.TitleFontSize;
        set => ActionSheet.TitleFontSize = value;
    }
    public float ActionSheetTitlePaddingY
    {
        get => ActionSheet.TitlePaddingY;
        set => ActionSheet.TitlePaddingY = value;
    }
    public float ActionSheetTitlePaddingX
    {
        get => ActionSheet.TitlePaddingX;
        set => ActionSheet.TitlePaddingX = value;
    }
    public float ActionSheetSubTitleFontSize
    {
        get => ActionSheet.SubTitleFontSize;
        set => ActionSheet.SubTitleFontSize = value;
    }
    public float ActionSheetButtonHeight
    {
        get => ActionSheet.ButtonHeight;
        set => ActionSheet.ButtonHeight = value;
    }
    public float ActionSheetButtonFontSize
    {
        get => ActionSheet.ButtonFontSize;
        set => ActionSheet.ButtonFontSize = value;
    }
    public float ActionSheetButtonPaddingY
    {
        get => ActionSheet.ButtonPaddingY;
        set => ActionSheet.ButtonPaddingY = value;
    }
    public float ActionSheetButtonPaddingX
    {
        get => ActionSheet.ButtonPaddingX;
        set => ActionSheet.ButtonPaddingX = value;
    }
    public float ActionSheetIconFontSize
    {
        get => ActionSheet.IconFontSize;
        set => ActionSheet.IconFontSize = value;
    }
    public JustifyContent ActionSheetButtonJustify
    {
        get => ActionSheet.ButtonJustify;
        set => ActionSheet.ButtonJustify = value;
    }
    public TextAlign ActionSheetTextAlign
    {
        get => ActionSheet.TextAlign;
        set => ActionSheet.TextAlign = value;
    }
    public FontWeight ActionSheetCancelFontWeight
    {
        get => ActionSheet.CancelFontWeight;
        set => ActionSheet.CancelFontWeight = value;
    }
    public float ActionSheetEnterDuration
    {
        get => ActionSheet.EnterDuration;
        set => ActionSheet.EnterDuration = value;
    }
    public float ActionSheetLeaveDuration
    {
        get => ActionSheet.LeaveDuration;
        set => ActionSheet.LeaveDuration = value;
    }
    public float ToastEdgeOffset
    {
        get => Toast.EdgeOffset;
        set => Toast.EdgeOffset = value;
    }
    public float ToastEnterDuration
    {
        get => Toast.EnterDuration;
        set => Toast.EnterDuration = value;
    }
    public float ToastLeaveDuration
    {
        get => Toast.LeaveDuration;
        set => Toast.LeaveDuration = value;
    }
    public Color AlertBackdropColor
    {
        get => Alert.BackdropColor;
        set => Alert.BackdropColor = value;
    }
    public float AlertBackdropOpacity
    {
        get => Alert.BackdropOpacity;
        set => Alert.BackdropOpacity = value;
    }
    public float AlertMinWidth
    {
        get => Alert.MinWidth;
        set => Alert.MinWidth = value;
    }
    public float AlertMaxWidth
    {
        get => Alert.MaxWidth;
        set => Alert.MaxWidth = value;
    }
    public float AlertBorderRadius
    {
        get => Alert.BorderRadius;
        set => Alert.BorderRadius = value;
    }
    public List<BoxShadow> AlertBoxShadow
    {
        get => Alert.BoxShadow;
        set => Alert.BoxShadow = value;
    }
    public float AlertHeadPaddingY
    {
        get => Alert.HeadPaddingY;
        set => Alert.HeadPaddingY = value;
    }
    public float AlertHeadPaddingX
    {
        get => Alert.HeadPaddingX;
        set => Alert.HeadPaddingX = value;
    }
    public TextAlign AlertHeadTextAlign
    {
        get => Alert.HeadTextAlign;
        set => Alert.HeadTextAlign = value;
    }
    public float AlertTitleFontSize
    {
        get => Alert.TitleFontSize;
        set => Alert.TitleFontSize = value;
    }
    public FontWeight AlertTitleFontWeight
    {
        get => Alert.TitleFontWeight;
        set => Alert.TitleFontWeight = value;
    }
    public float AlertSubTitleFontSize
    {
        get => Alert.SubTitleFontSize;
        set => Alert.SubTitleFontSize = value;
    }
    public float AlertMessagePaddingY
    {
        get => Alert.MessagePaddingY;
        set => Alert.MessagePaddingY = value;
    }
    public float AlertMessagePaddingX
    {
        get => Alert.MessagePaddingX;
        set => Alert.MessagePaddingX = value;
    }
    public float AlertMessageFontSize
    {
        get => Alert.MessageFontSize;
        set => Alert.MessageFontSize = value;
    }
    public float AlertButtonGroupPadding
    {
        get => Alert.ButtonGroupPadding;
        set => Alert.ButtonGroupPadding = value;
    }
    public JustifyContent AlertButtonGroupJustify
    {
        get => Alert.ButtonGroupJustify;
        set => Alert.ButtonGroupJustify = value;
    }
    public FlexWrap AlertButtonGroupFlexWrap
    {
        get => Alert.ButtonGroupFlexWrap;
        set => Alert.ButtonGroupFlexWrap = value;
    }
    public float AlertButtonFontSize
    {
        get => Alert.ButtonFontSize;
        set => Alert.ButtonFontSize = value;
    }
    public FontWeight AlertButtonFontWeight
    {
        get => Alert.ButtonFontWeight;
        set => Alert.ButtonFontWeight = value;
    }
    public float AlertButtonBorderRadius
    {
        get => Alert.ButtonBorderRadius;
        set => Alert.ButtonBorderRadius = value;
    }
    public TextTransform AlertButtonTextTransform
    {
        get => Alert.ButtonTextTransform;
        set => Alert.ButtonTextTransform = value;
    }
    public float AlertButtonPadding
    {
        get => Alert.ButtonPadding;
        set => Alert.ButtonPadding = value;
    }
    public float AlertButtonMarginX
    {
        get => Alert.ButtonMarginX;
        set => Alert.ButtonMarginX = value;
    }
    public float AlertTappableHeight
    {
        get => Alert.TappableHeight;
        set => Alert.TappableHeight = value;
    }
    public float DatetimeTitleFontSize
    {
        get => Datetime.TitleFontSize;
        set => Datetime.TitleFontSize = value;
    }
    public float DatetimeSelectedDateFontSize
    {
        get => Datetime.SelectedDateFontSize;
        set => Datetime.SelectedDateFontSize = value;
    }
    public float DatetimeDayOfWeekFontSize
    {
        get => Datetime.DayOfWeekFontSize;
        set => Datetime.DayOfWeekFontSize = value;
    }
    public float DatetimeDaySize
    {
        get => Datetime.DaySize;
        set => Datetime.DaySize = value;
    }
    public float DatetimeDayFontSize
    {
        get => Datetime.DayFontSize;
        set => Datetime.DayFontSize = value;
    }
    public Color DatetimeButtonBackground
    {
        get => Datetime.ButtonBackground;
        set => Datetime.ButtonBackground = value;
    }
    public Color DatetimeButtonColor
    {
        get => Datetime.ButtonColor;
        set => Datetime.ButtonColor = value;
    }
    public float DatetimeButtonBorderRadius
    {
        get => Datetime.ButtonBorderRadius;
        set => Datetime.ButtonBorderRadius = value;
    }
    public float DatetimeButtonPaddingY
    {
        get => Datetime.ButtonPaddingY;
        set => Datetime.ButtonPaddingY = value;
    }
    public float DatetimeButtonPaddingX
    {
        get => Datetime.ButtonPaddingX;
        set => Datetime.ButtonPaddingX = value;
    }
    public float DatetimeButtonFontSize
    {
        get => Datetime.ButtonFontSize;
        set => Datetime.ButtonFontSize = value;
    }
    public float LabelTextWrapLineHeight
    {
        get => Label.TextWrapLineHeight;
        set => Label.TextWrapLineHeight = value;
    }
    public Color NoteColor
    {
        get => Note.Color;
        set => Note.Color = value;
    }
    public float NoteFontSize
    {
        get => Note.FontSize;
        set => Note.FontSize = value;
    }
    public float ThumbnailSize
    {
        get => Thumbnail.Size;
        set => Thumbnail.Size = value;
    }
    public float ItemThumbnailSize
    {
        get => Item.ThumbnailSize;
        set => Item.ThumbnailSize = value;
    }
    public float ToggleTrackWidth
    {
        get => Toggle.TrackWidth;
        set => Toggle.TrackWidth = value;
    }
    public float ToggleTrackHeight
    {
        get => Toggle.TrackHeight;
        set => Toggle.TrackHeight = value;
    }
    public Length ToggleBorderRadius
    {
        get => Toggle.BorderRadius;
        set => Toggle.BorderRadius = value;
    }
    public float ToggleTrackCheckedAlpha
    {
        get => Toggle.TrackCheckedAlpha;
        set => Toggle.TrackCheckedAlpha = value;
    }
    public float ToggleHandleWidth
    {
        get => Toggle.HandleWidth;
        set => Toggle.HandleWidth = value;
    }
    public float ToggleHandleHeight
    {
        get => Toggle.HandleHeight;
        set => Toggle.HandleHeight = value;
    }
    public Length ToggleHandleBorderRadius
    {
        get => Toggle.HandleBorderRadius;
        set => Toggle.HandleBorderRadius = value;
    }
    public Color ToggleHandleBackground
    {
        get => Toggle.HandleBackground;
        set => Toggle.HandleBackground = value;
    }
    public Color ToggleHandleBackgroundChecked
    {
        get => Toggle.HandleBackgroundChecked;
        set => Toggle.HandleBackgroundChecked = value;
    }
    public List<BoxShadow> ToggleHandleBoxShadow
    {
        get => Toggle.HandleBoxShadow;
        set => Toggle.HandleBoxShadow = value;
    }
    public float ToggleHandleTravel
    {
        get => Toggle.HandleTravel;
        set => Toggle.HandleTravel = value;
    }
    public float ToggleTransitionDuration
    {
        get => Toggle.TransitionDuration;
        set => Toggle.TransitionDuration = value;
    }
    public float ToggleDisabledOpacity
    {
        get => Toggle.DisabledOpacity;
        set => Toggle.DisabledOpacity = value;
    }
    public float ProgressBarHeight
    {
        get => ProgressBar.Height;
        set => ProgressBar.Height = value;
    }

    public float? LabelTextWrapFontSize
    {
        get => Label.TextWrapFontSize;
        set => Label.TextWrapFontSize = value;
    }

    public float? LabelStackedFontSize
    {
        get => Label.StackedFontSize;
        set => Label.StackedFontSize = value;
    }

    public Color Primary
    {
        get => Palette.Primary;
        set => Palette.Primary = value;
    }
    public Color Secondary
    {
        get => Palette.Secondary;
        set => Palette.Secondary = value;
    }
    public Color Tertiary
    {
        get => Palette.Tertiary;
        set => Palette.Tertiary = value;
    }
    public Color Success
    {
        get => Palette.Success;
        set => Palette.Success = value;
    }
    public Color Warning
    {
        get => Palette.Warning;
        set => Palette.Warning = value;
    }
    public Color Danger
    {
        get => Palette.Danger;
        set => Palette.Danger = value;
    }
    public Color Light
    {
        get => Palette.Light;
        set => Palette.Light = value;
    }
    public Color Medium
    {
        get => Palette.Medium;
        set => Palette.Medium = value;
    }
    public Color Dark
    {
        get => Palette.Dark;
        set => Palette.Dark = value;
    }
    public Color BackgroundColor
    {
        get => Palette.BackgroundColor;
        set => Palette.BackgroundColor = value;
    }
    public Color TextColor
    {
        get => Palette.TextColor;
        set => Palette.TextColor = value;
    }
    public Color TabBarBackground
    {
        get => Tab.BarBackground;
        set => Tab.BarBackground = value;
    }
    public Color TabBarBorderColor
    {
        get => Tab.BarBorderColor;
        set => Tab.BarBorderColor = value;
    }
    public Color TabBarColor
    {
        get => Tab.BarColor;
        set => Tab.BarColor = value;
    }
    public Color TabBarColorSelected
    {
        get => Tab.BarColorSelected;
        set => Tab.BarColorSelected = value;
    }
    public Color ToolbarBackground
    {
        get => Toolbar.Background;
        set => Toolbar.Background = value;
    }
    public Color ToolbarColor
    {
        get => Toolbar.Color;
        set => Toolbar.Color = value;
    }
    public Color ToolbarBorderColor
    {
        get => Toolbar.BorderColor;
        set => Toolbar.BorderColor = value;
    }
    public Color HeaderBorderColor
    {
        get => Header.BorderColor;
        set => Header.BorderColor = value;
    }
    public float HeaderBorderWidth
    {
        get => Header.BorderWidth;
        set => Header.BorderWidth = value;
    }
    public Color ContentBackground
    {
        get => Content.Background;
        set => Content.Background = value;
    }
    public Color ContentColor
    {
        get => Content.Color;
        set => Content.Color = value;
    }
    public Color AppBackground
    {
        get => Menu.AppBackground;
        set => Menu.AppBackground = value;
    }
    public Color MenuBackground
    {
        get => Menu.Background;
        set => Menu.Background = value;
    }
    public Color MenuBorderColor
    {
        get => Menu.BorderColor;
        set => Menu.BorderColor = value;
    }
    public float MenuBorderWidth
    {
        get => Menu.BorderWidth;
        set => Menu.BorderWidth = value;
    }
    public Color BackdropColor
    {
        get => Palette.BackdropColor;
        set => Palette.BackdropColor = value;
    }
    public Color ListBackground
    {
        get => List.Background;
        set => List.Background = value;
    }
    public Color ListHeaderColor
    {
        get => List.HeaderColor;
        set => List.HeaderColor = value;
    }
    public Color ItemColor
    {
        get => Item.Color;
        set => Item.Color = value;
    }
    public Color ItemBorderColor
    {
        get => Item.BorderColor;
        set => Item.BorderColor = value;
    }
    public Color ItemDividerBackground
    {
        get => Item.DividerBackground;
        set => Item.DividerBackground = value;
    }
    public Color ItemDividerColor
    {
        get => Item.DividerColor;
        set => Item.DividerColor = value;
    }
    public Color ItemOptionColor
    {
        get => Item.OptionColor;
        set => Item.OptionColor = value;
    }
    public Color ItemOptionBackground
    {
        get => Item.OptionBackground;
        set => Item.OptionBackground = value;
    }
    public Color SegmentBackground
    {
        get => Segment.Background;
        set => Segment.Background = value;
    }
    public float SegmentBorderRadius
    {
        get => Segment.BorderRadius;
        set => Segment.BorderRadius = value;
    }
    public Color SegmentButtonColor
    {
        get => Segment.ButtonColor;
        set => Segment.ButtonColor = value;
    }
    public Color SegmentButtonCheckedColor
    {
        get => Segment.ButtonCheckedColor;
        set => Segment.ButtonCheckedColor = value;
    }
    public Color SegmentIndicatorColor
    {
        get => Segment.IndicatorColor;
        set => Segment.IndicatorColor = value;
    }
    public float SegmentIndicatorBorderRadius
    {
        get => Segment.IndicatorBorderRadius;
        set => Segment.IndicatorBorderRadius = value;
    }
    public float SegmentButtonMarginY
    {
        get => Segment.ButtonMarginY;
        set => Segment.ButtonMarginY = value;
    }
    public Color ButtonSolidBackground
    {
        get => Button.SolidBackground;
        set => Button.SolidBackground = value;
    }
    public Color ButtonSolidColor
    {
        get => Button.SolidColor;
        set => Button.SolidColor = value;
    }
    public Color ButtonTextColor
    {
        get => Button.TextColor;
        set => Button.TextColor = value;
    }
    public Color SearchbarInputBackground
    {
        get => Searchbar.InputBackground;
        set => Searchbar.InputBackground = value;
    }
    public Color SearchbarInputTextColor
    {
        get => Searchbar.InputTextColor;
        set => Searchbar.InputTextColor = value;
    }
    public float SearchbarInputMinHeight
    {
        get => Searchbar.InputMinHeight;
        set => Searchbar.InputMinHeight = value;
    }
    public Color SearchbarSearchIconColor
    {
        get => Searchbar.SearchIconColor;
        set => Searchbar.SearchIconColor = value;
    }
    public Color SearchbarClearIconColor
    {
        get => Searchbar.ClearIconColor;
        set => Searchbar.ClearIconColor = value;
    }
    public Color SearchbarCancelButtonColor
    {
        get => Searchbar.CancelButtonColor;
        set => Searchbar.CancelButtonColor = value;
    }
    public Color SearchbarCancelButtonBackground
    {
        get => Searchbar.CancelButtonBackground;
        set => Searchbar.CancelButtonBackground = value;
    }
    public Color SlidesBulletBackground
    {
        get => Slides.BulletBackground;
        set => Slides.BulletBackground = value;
    }
    public Color SlidesBulletBackgroundActive
    {
        get => Slides.BulletBackgroundActive;
        set => Slides.BulletBackgroundActive = value;
    }
    public Color SlidesScrollBarBackground
    {
        get => Slides.ScrollBarBackground;
        set => Slides.ScrollBarBackground = value;
    }
    public Color SlidesScrollBarBackgroundActive
    {
        get => Slides.ScrollBarBackgroundActive;
        set => Slides.ScrollBarBackgroundActive = value;
    }
    public Color SlidesNavigationColor
    {
        get => Slides.NavigationColor;
        set => Slides.NavigationColor = value;
    }
    public Color SpinnerColor
    {
        get => Spinner.Color;
        set => Spinner.Color = value;
    }
    public Color SpinnerTrackColor
    {
        get => Spinner.TrackColor;
        set => Spinner.TrackColor = value;
    }
    public Color BadgeBackground
    {
        get => Badge.Background;
        set => Badge.Background = value;
    }
    public Color BadgeColor
    {
        get => Badge.Color;
        set => Badge.Color = value;
    }
    public Color ChipBackground
    {
        get => Chip.Background;
        set => Chip.Background = value;
    }
    public Color ChipColor
    {
        get => Chip.Color;
        set => Chip.Color = value;
    }
    public Color ChipBorderColor
    {
        get => Chip.BorderColor;
        set => Chip.BorderColor = value;
    }
    public Color CardBackground
    {
        get => Card.Background;
        set => Card.Background = value;
    }
    public Color CardColor
    {
        get => Card.Color;
        set => Card.Color = value;
    }
    public Color SelectTextColor
    {
        get => Select.TextColor;
        set => Select.TextColor = value;
    }
    public Color SelectPlaceholderColor
    {
        get => Select.PlaceholderColor;
        set => Select.PlaceholderColor = value;
    }
    public Color SelectLabelColor
    {
        get => Select.LabelColor;
        set => Select.LabelColor = value;
    }
    public Color SelectBackground
    {
        get => Select.Background;
        set => Select.Background = value;
    }
    public Color SelectBorderColor
    {
        get => Select.BorderColor;
        set => Select.BorderColor = value;
    }
    public Color SelectHighlightColor
    {
        get => Select.HighlightColor;
        set => Select.HighlightColor = value;
    }
    public Color SelectHelperColor
    {
        get => Select.HelperColor;
        set => Select.HelperColor = value;
    }
    public Color SelectErrorColor
    {
        get => Select.ErrorColor;
        set => Select.ErrorColor = value;
    }
    public Color CheckboxBorderColorOff
    {
        get => Checkbox.BorderColorOff;
        set => Checkbox.BorderColorOff = value;
    }
    public Color CheckboxBackgroundOff
    {
        get => Checkbox.BackgroundOff;
        set => Checkbox.BackgroundOff = value;
    }
    public Color CheckboxBackgroundChecked
    {
        get => Checkbox.BackgroundChecked;
        set => Checkbox.BackgroundChecked = value;
    }
    public Color CheckboxCheckmarkColor
    {
        get => Checkbox.CheckmarkColor;
        set => Checkbox.CheckmarkColor = value;
    }
    public Color FabBackground
    {
        get => Fab.Background;
        set => Fab.Background = value;
    }
    public Color FabColor
    {
        get => Fab.Color;
        set => Fab.Color = value;
    }
    public Color FabListButtonBackground
    {
        get => Fab.ListButtonBackground;
        set => Fab.ListButtonBackground = value;
    }
    public Color FabListButtonColor
    {
        get => Fab.ListButtonColor;
        set => Fab.ListButtonColor = value;
    }
    public Color InputTextColor
    {
        get => Input.TextColor;
        set => Input.TextColor = value;
    }
    public Color InputPlaceholderColor
    {
        get => Input.PlaceholderColor;
        set => Input.PlaceholderColor = value;
    }
    public Color InputLabelColor
    {
        get => Input.LabelColor;
        set => Input.LabelColor = value;
    }
    public Color InputBackground
    {
        get => Input.Background;
        set => Input.Background = value;
    }
    public Color InputBorderColor
    {
        get => Input.BorderColor;
        set => Input.BorderColor = value;
    }
    public Color InputHighlightColor
    {
        get => Input.HighlightColor;
        set => Input.HighlightColor = value;
    }
    public Color InputHelperColor
    {
        get => Input.HelperColor;
        set => Input.HelperColor = value;
    }
    public Color InputErrorColor
    {
        get => Input.ErrorColor;
        set => Input.ErrorColor = value;
    }
    public Color InputClearIconColor
    {
        get => Input.ClearIconColor;
        set => Input.ClearIconColor = value;
    }
    public Color BreadcrumbColor
    {
        get => Breadcrumb.Color;
        set => Breadcrumb.Color = value;
    }
    public Color BreadcrumbColorActive
    {
        get => Breadcrumb.ColorActive;
        set => Breadcrumb.ColorActive = value;
    }
    public float BreadcrumbBorderRadius
    {
        get => Breadcrumb.BorderRadius;
        set => Breadcrumb.BorderRadius = value;
    }
    public Color BreadcrumbSeparatorColor
    {
        get => Breadcrumb.SeparatorColor;
        set => Breadcrumb.SeparatorColor = value;
    }
    public Color BreadcrumbIconColor
    {
        get => Breadcrumb.IconColor;
        set => Breadcrumb.IconColor = value;
    }
    public Color BreadcrumbIconColorActive
    {
        get => Breadcrumb.IconColorActive;
        set => Breadcrumb.IconColorActive = value;
    }
    public Color BreadcrumbIndicatorBackground
    {
        get => Breadcrumb.IndicatorBackground;
        set => Breadcrumb.IndicatorBackground = value;
    }
    public Color BreadcrumbIndicatorColor
    {
        get => Breadcrumb.IndicatorColor;
        set => Breadcrumb.IndicatorColor = value;
    }
    public Color AccordionBackground
    {
        get => Accordion.Background;
        set => Accordion.Background = value;
    }
    public Color ActionSheetBackground
    {
        get => ActionSheet.Background;
        set => ActionSheet.Background = value;
    }
    public float ActionSheetBorderRadius
    {
        get => ActionSheet.BorderRadius;
        set => ActionSheet.BorderRadius = value;
    }
    public float ActionSheetContainerPaddingX
    {
        get => ActionSheet.ContainerPaddingX;
        set => ActionSheet.ContainerPaddingX = value;
    }
    public float ActionSheetGroupMarginTop
    {
        get => ActionSheet.GroupMarginTop;
        set => ActionSheet.GroupMarginTop = value;
    }
    public float ActionSheetGroupMarginBottom
    {
        get => ActionSheet.GroupMarginBottom;
        set => ActionSheet.GroupMarginBottom = value;
    }
    public Color ActionSheetTitleColor
    {
        get => ActionSheet.TitleColor;
        set => ActionSheet.TitleColor = value;
    }
    public Color ActionSheetButtonColor
    {
        get => ActionSheet.ButtonColor;
        set => ActionSheet.ButtonColor = value;
    }
    public Color ActionSheetDestructiveColor
    {
        get => ActionSheet.DestructiveColor;
        set => ActionSheet.DestructiveColor = value;
    }
    public Color ActionSheetButtonBorderColor
    {
        get => ActionSheet.ButtonBorderColor;
        set => ActionSheet.ButtonBorderColor = value;
    }
    public Color AlertBackground
    {
        get => Alert.Background;
        set => Alert.Background = value;
    }
    public Color AlertTitleColor
    {
        get => Alert.TitleColor;
        set => Alert.TitleColor = value;
    }
    public Color AlertSubTitleColor
    {
        get => Alert.SubTitleColor;
        set => Alert.SubTitleColor = value;
    }
    public Color AlertMessageColor
    {
        get => Alert.MessageColor;
        set => Alert.MessageColor = value;
    }
    public Color AlertButtonColor
    {
        get => Alert.ButtonColor;
        set => Alert.ButtonColor = value;
    }
    public Color AlertListBorderColor
    {
        get => Alert.ListBorderColor;
        set => Alert.ListBorderColor = value;
    }
    public Color AlertControlBorderColorOff
    {
        get => Alert.ControlBorderColorOff;
        set => Alert.ControlBorderColorOff = value;
    }
    public Color AlertControlAccent
    {
        get => Alert.ControlAccent;
        set => Alert.ControlAccent = value;
    }
    public Color DatetimeBackground
    {
        get => Datetime.Background;
        set => Datetime.Background = value;
    }
    public Color DatetimeHeaderBackground
    {
        get => Datetime.HeaderBackground;
        set => Datetime.HeaderBackground = value;
    }
    public Color DatetimeHeaderColor
    {
        get => Datetime.HeaderColor;
        set => Datetime.HeaderColor = value;
    }
    public Color DatetimeDayOfWeekColor
    {
        get => Datetime.DayOfWeekColor;
        set => Datetime.DayOfWeekColor = value;
    }
    public Color DatetimeMonthYearColor
    {
        get => Datetime.MonthYearColor;
        set => Datetime.MonthYearColor = value;
    }
    public Color DatetimeDayColor
    {
        get => Datetime.DayColor;
        set => Datetime.DayColor = value;
    }
    public Color DatetimeDayActiveBackground
    {
        get => Datetime.DayActiveBackground;
        set => Datetime.DayActiveBackground = value;
    }
    public Color DatetimeDayActiveColor
    {
        get => Datetime.DayActiveColor;
        set => Datetime.DayActiveColor = value;
    }
    public Color DatetimeTodayColor
    {
        get => Datetime.TodayColor;
        set => Datetime.TodayColor = value;
    }
    public Color DatetimeButtonActiveColor
    {
        get => Datetime.ButtonActiveColor;
        set => Datetime.ButtonActiveColor = value;
    }
    public float LabelStackedMarginBottom
    {
        get => Label.StackedMarginBottom;
        set => Label.StackedMarginBottom = value;
    }
    public Color SkeletonTextBackground
    {
        get => SkeletonText.Background;
        set => SkeletonText.Background = value;
    }
    public Color SkeletonTextBackgroundAnimated
    {
        get => SkeletonText.BackgroundAnimated;
        set => SkeletonText.BackgroundAnimated = value;
    }
    public Color ToggleTrackBackgroundOff
    {
        get => Toggle.TrackBackgroundOff;
        set => Toggle.TrackBackgroundOff = value;
    }
    public Color ToggleTrackBackgroundOn
    {
        get => Toggle.TrackBackgroundOn;
        set => Toggle.TrackBackgroundOn = value;
    }
    public float ToggleHandleSpacing
    {
        get => Toggle.HandleSpacing;
        set => Toggle.HandleSpacing = value;
    }
    public float ProgressBarBorderRadius
    {
        get => ProgressBar.BorderRadius;
        set => ProgressBar.BorderRadius = value;
    }
    public Color ProgressBarBackground
    {
        get => ProgressBar.Background;
        set => ProgressBar.Background = value;
    }
    public Color ProgressBarProgressBackground
    {
        get => ProgressBar.ProgressBackground;
        set => ProgressBar.ProgressBackground = value;
    }
}
