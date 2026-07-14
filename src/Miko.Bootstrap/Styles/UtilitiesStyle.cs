using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class UtilitiesStyle
{
    internal static CssObject GenStyle(Theme t)
    {
        var spacers = new (int key, Length value)[]
        {
            (0, Length.Px(0)),
            (1, Length.Rem(0.25f)),
            (2, Length.Rem(0.5f)),
            (3, Length.Rem(1f)),
            (4, Length.Rem(1.5f)),
            (5, Length.Rem(3f)),
        };

        var styles = new CssObject
        {
            // Display
            [".d-none"] = new() { Display = Display.None },
            [".d-block"] = new() { Display = Display.Block },
            [".d-flex"] = new() { Display = Display.Flex },
            [".d-inline"] = new() { Display = Display.Inline },
            [".d-inline-block"] = new() { Display = Display.InlineBlock },

            // Flex direction
            [".flex-row"] = new() { FlexDirection = FlexDirection.Row },
            [".flex-row-reverse"] = new() { FlexDirection = FlexDirection.RowReverse },
            [".flex-column"] = new() { FlexDirection = FlexDirection.Column },
            [".flex-column-reverse"] = new() { FlexDirection = FlexDirection.ColumnReverse },

            // Flex grow/shrink
            [".flex-grow-0"] = new() { FlexGrow = 0 },
            [".flex-grow-1"] = new() { FlexGrow = 1 },
            [".flex-shrink-0"] = new() { FlexShrink = 0 },
            [".flex-shrink-1"] = new() { FlexShrink = 1 },
            [".flex-fill"] = new() { FlexGrow = 1, FlexShrink = 1, FlexBasis = Length.Auto },

            // Flex wrap
            [".flex-wrap"] = new() { FlexWrap = FlexWrap.Wrap },
            [".flex-nowrap"] = new() { FlexWrap = FlexWrap.Nowrap },
            [".flex-wrap-reverse"] = new() { FlexWrap = FlexWrap.WrapReverse },

            // Justify content
            [".justify-content-start"] = new() { JustifyContent = JustifyContent.FlexStart },
            [".justify-content-end"] = new() { JustifyContent = JustifyContent.FlexEnd },
            [".justify-content-center"] = new() { JustifyContent = JustifyContent.Center },
            [".justify-content-between"] = new() { JustifyContent = JustifyContent.SpaceBetween },
            [".justify-content-around"] = new() { JustifyContent = JustifyContent.SpaceAround },
            [".justify-content-evenly"] = new() { JustifyContent = JustifyContent.SpaceEvenly },

            // Align items
            [".align-items-start"] = new() { AlignItems = AlignItems.FlexStart },
            [".align-items-end"] = new() { AlignItems = AlignItems.FlexEnd },
            [".align-items-center"] = new() { AlignItems = AlignItems.Center },
            [".align-items-baseline"] = new() { AlignItems = AlignItems.Baseline },
            [".align-items-stretch"] = new() { AlignItems = AlignItems.Stretch },

            // Align content
            [".align-content-start"] = new() { AlignContent = AlignContent.FlexStart },
            [".align-content-end"] = new() { AlignContent = AlignContent.FlexEnd },
            [".align-content-center"] = new() { AlignContent = AlignContent.Center },
            [".align-content-between"] = new() { AlignContent = AlignContent.SpaceBetween },
            [".align-content-around"] = new() { AlignContent = AlignContent.SpaceAround },
            [".align-content-stretch"] = new() { AlignContent = AlignContent.Stretch },

            // Align self
            [".align-self-auto"] = new() { AlignSelf = AlignSelf.Auto },
            [".align-self-start"] = new() { AlignSelf = AlignSelf.FlexStart },
            [".align-self-end"] = new() { AlignSelf = AlignSelf.FlexEnd },
            [".align-self-center"] = new() { AlignSelf = AlignSelf.Center },
            [".align-self-baseline"] = new() { AlignSelf = AlignSelf.Baseline },
            [".align-self-stretch"] = new() { AlignSelf = AlignSelf.Stretch },

            // Order
            // NOTE: order property not supported in CssObject

            // Vertical align
            [".align-baseline"] = new() { VerticalAlign = VerticalAlign.Baseline },
            [".align-top"] = new() { VerticalAlign = VerticalAlign.Top },
            [".align-middle"] = new() { VerticalAlign = VerticalAlign.Middle },
            [".align-bottom"] = new() { VerticalAlign = VerticalAlign.Bottom },
            [".align-text-bottom"] = new() { VerticalAlign = VerticalAlign.TextBottom },
            [".align-text-top"] = new() { VerticalAlign = VerticalAlign.TextTop },

            // Overflow
            [".overflow-auto"] = new() { Overflow = Overflow.Auto },
            [".overflow-hidden"] = new() { Overflow = Overflow.Hidden },
            [".overflow-visible"] = new() { Overflow = Overflow.Visible },
            [".overflow-scroll"] = new() { Overflow = Overflow.Scroll },
            [".overflow-x-auto"] = new() { OverflowX = Overflow.Auto },
            [".overflow-x-hidden"] = new() { OverflowX = Overflow.Hidden },
            [".overflow-x-visible"] = new() { OverflowX = Overflow.Visible },
            [".overflow-x-scroll"] = new() { OverflowX = Overflow.Scroll },
            [".overflow-y-auto"] = new() { OverflowY = Overflow.Auto },
            [".overflow-y-hidden"] = new() { OverflowY = Overflow.Hidden },
            [".overflow-y-visible"] = new() { OverflowY = Overflow.Visible },
            [".overflow-y-scroll"] = new() { OverflowY = Overflow.Scroll },

            // Opacity
            [".opacity-0"] = new() { Opacity = 0f },
            [".opacity-25"] = new() { Opacity = 0.25f },
            [".opacity-50"] = new() { Opacity = 0.5f },
            [".opacity-75"] = new() { Opacity = 0.75f },
            [".opacity-100"] = new() { Opacity = 1f },

            // Position
            [".position-static"] = new() { Position = Position.Static },
            [".position-relative"] = new() { Position = Position.Relative },
            [".position-absolute"] = new() { Position = Position.Absolute },
            [".position-fixed"] = new() { Position = Position.Fixed },
            // NOTE: position: sticky not supported in CssObject

            // Position values
            [".top-0"] = new() { Top = Length.Px(0) },
            [".top-50"] = new() { Top = Length.Percent(50) },
            [".top-100"] = new() { Top = Length.Percent(100) },
            [".bottom-0"] = new() { Bottom = Length.Px(0) },
            [".bottom-50"] = new() { Bottom = Length.Percent(50) },
            [".bottom-100"] = new() { Bottom = Length.Percent(100) },
            [".start-0"] = new() { Left = Length.Px(0) },
            [".start-50"] = new() { Left = Length.Percent(50) },
            [".start-100"] = new() { Left = Length.Percent(100) },
            [".end-0"] = new() { Right = Length.Px(0) },
            [".end-50"] = new() { Right = Length.Percent(50) },
            [".end-100"] = new() { Right = Length.Percent(100) },

            // Border
            [".border"] = new() { Border = new Border(Length.Px(t.BorderWidth), t.BorderStyle, t.BorderColor) },
            [".border-0"] = new() { Border = new Border(0) },
            [".border-top"] = new() { BorderTop = new BorderSide(Length.Px(t.BorderWidth), t.BorderStyle, t.BorderColor) },
            [".border-top-0"] = new() { BorderTop = BorderSide.None },
            [".border-end"] = new() { BorderRight = new BorderSide(Length.Px(t.BorderWidth), t.BorderStyle, t.BorderColor) },
            [".border-end-0"] = new() { BorderRight = BorderSide.None },
            [".border-bottom"] = new() { BorderBottom = new BorderSide(Length.Px(t.BorderWidth), t.BorderStyle, t.BorderColor) },
            [".border-bottom-0"] = new() { BorderBottom = BorderSide.None },
            [".border-start"] = new() { BorderLeft = new BorderSide(Length.Px(t.BorderWidth), t.BorderStyle, t.BorderColor) },
            [".border-start-0"] = new() { BorderLeft = BorderSide.None },

            // Border color
            [".border-primary"] = new() { BorderColor = t.Primary },
            [".border-secondary"] = new() { BorderColor = t.Secondary },
            [".border-success"] = new() { BorderColor = t.Success },
            [".border-danger"] = new() { BorderColor = t.Danger },
            [".border-warning"] = new() { BorderColor = t.Warning },
            [".border-info"] = new() { BorderColor = t.Info },
            [".border-light"] = new() { BorderColor = t.Light },
            [".border-dark"] = new() { BorderColor = t.Dark },
            [".border-white"] = new() { BorderColor = Color.White },

            // Border width
            [".border-1"] = new() { BorderWidth = Length.Px(1) },
            [".border-2"] = new() { BorderWidth = Length.Px(2) },
            [".border-3"] = new() { BorderWidth = Length.Px(3) },
            [".border-4"] = new() { BorderWidth = Length.Px(4) },
            [".border-5"] = new() { BorderWidth = Length.Px(5) },

            // Sizing
            [".w-25"] = new() { Width = Length.Percent(25) },
            [".w-50"] = new() { Width = Length.Percent(50) },
            [".w-75"] = new() { Width = Length.Percent(75) },
            [".w-100"] = new() { Width = Length.Percent(100) },
            [".w-auto"] = new() { Width = Length.Auto },
            [".mw-100"] = new() { MaxWidth = Length.Percent(100) },
            [".h-25"] = new() { Height = Length.Percent(25) },
            [".h-50"] = new() { Height = Length.Percent(50) },
            [".h-75"] = new() { Height = Length.Percent(75) },
            [".h-100"] = new() { Height = Length.Percent(100) },
            [".h-auto"] = new() { Height = Length.Auto },
            [".mh-100"] = new() { MaxHeight = Length.Percent(100) },
            [".min-vw-100"] = new() { MinWidth = Length.Percent(100) },
            [".min-vh-100"] = new() { MinHeight = Length.Percent(100) },

            // Text
            [".text-start"] = new() { TextAlign = TextAlign.Left },
            [".text-end"] = new() { TextAlign = TextAlign.Right },
            [".text-center"] = new() { TextAlign = TextAlign.Center },
            [".text-decoration-none"] = new() { TextDecoration = TextDecoration.None },
            [".text-decoration-underline"] = new() { TextDecoration = TextDecoration.Underline },
            [".text-decoration-line-through"] = new() { TextDecoration = TextDecoration.LineThrough },
            [".text-lowercase"] = new() { TextTransform = TextTransform.Lowercase },
            [".text-uppercase"] = new() { TextTransform = TextTransform.Uppercase },
            [".text-capitalize"] = new() { TextTransform = TextTransform.Capitalize },
            [".text-wrap"] = new() { WhiteSpace = WhiteSpace.Normal },
            [".text-nowrap"] = new() { WhiteSpace = WhiteSpace.Nowrap },

            // Font size
            [".fs-1"] = new() { FontSize = Length.Rem(2.5f) },
            [".fs-2"] = new() { FontSize = Length.Rem(2f) },
            [".fs-3"] = new() { FontSize = Length.Rem(1.75f) },
            [".fs-4"] = new() { FontSize = Length.Rem(1.5f) },
            [".fs-5"] = new() { FontSize = Length.Rem(1.25f) },
            [".fs-6"] = new() { FontSize = Length.Rem(1f) },

            // Font style
            [".fst-italic"] = new() { FontStyle = FontStyle.Italic },
            [".fst-normal"] = new() { FontStyle = FontStyle.Normal },

            // Font weight
            [".fw-lighter"] = new() { FontWeight = FontWeight.Light },
            [".fw-light"] = new() { FontWeight = FontWeight.Light },
            [".fw-normal"] = new() { FontWeight = FontWeight.Normal },
            [".fw-medium"] = new() { FontWeight = FontWeight.Medium },
            [".fw-semibold"] = new() { FontWeight = FontWeight.SemiBold },
            [".fw-bold"] = new() { FontWeight = FontWeight.Bold },
            [".fw-bolder"] = new() { FontWeight = FontWeight.Bolder },

            // Line height
            [".lh-1"] = new() { LineHeight = Length.Px(1f) },
            [".lh-sm"] = new() { LineHeight = Length.Px(1.25f) },
            [".lh-base"] = new() { LineHeight = Length.Px(1.5f) },
            [".lh-lg"] = new() { LineHeight = Length.Px(2f) },

            // Text color
            [".text-primary"] = new() { Color = t.Primary },
            [".text-secondary"] = new() { Color = t.Secondary },
            [".text-success"] = new() { Color = t.Success },
            [".text-danger"] = new() { Color = t.Danger },
            [".text-warning"] = new() { Color = t.Warning },
            [".text-info"] = new() { Color = t.Info },
            [".text-light"] = new() { Color = t.Light },
            [".text-dark"] = new() { Color = t.Dark },
            [".text-body"] = new() { Color = t.BodyColor },
            [".text-body-secondary"] = new() { Color = t.SecondaryColor },
            [".text-body-tertiary"] = new() { Color = t.TertiaryColor },
            [".text-body-emphasis"] = new() { Color = t.EmphasisColor },
            [".text-white"] = new() { Color = Color.White },
            [".text-black"] = new() { Color = Color.Black },

            // Background color
            [".bg-primary"] = new() { BackgroundColor = t.Primary },
            [".bg-secondary"] = new() { BackgroundColor = t.Secondary },
            [".bg-success"] = new() { BackgroundColor = t.Success },
            [".bg-danger"] = new() { BackgroundColor = t.Danger },
            [".bg-warning"] = new() { BackgroundColor = t.Warning },
            [".bg-info"] = new() { BackgroundColor = t.Info },
            [".bg-light"] = new() { BackgroundColor = t.Light },
            [".bg-dark"] = new() { BackgroundColor = t.Dark },
            [".bg-body"] = new() { BackgroundColor = t.BodyBg },
            [".bg-body-secondary"] = new() { BackgroundColor = t.SecondaryBg },
            [".bg-body-tertiary"] = new() { BackgroundColor = t.TertiaryBg },
            [".bg-white"] = new() { BackgroundColor = Color.White },
            [".bg-black"] = new() { BackgroundColor = Color.Black },
            [".bg-transparent"] = new() { BackgroundColor = Color.Transparent },

            // Subtle background
            [".bg-primary-subtle"] = new() { BackgroundColor = t.PrimaryBgSubtle },
            [".bg-secondary-subtle"] = new() { BackgroundColor = t.SecondaryBgSubtle },
            [".bg-success-subtle"] = new() { BackgroundColor = t.SuccessBgSubtle },
            [".bg-danger-subtle"] = new() { BackgroundColor = t.DangerBgSubtle },
            [".bg-warning-subtle"] = new() { BackgroundColor = t.WarningBgSubtle },
            [".bg-info-subtle"] = new() { BackgroundColor = t.InfoBgSubtle },
            [".bg-light-subtle"] = new() { BackgroundColor = t.LightBgSubtle },
            [".bg-dark-subtle"] = new() { BackgroundColor = t.DarkBgSubtle },

            // Shadow
            [".shadow"] = new() { BoxShadow = new List<BoxShadow> { t.BoxShadow } },
            [".shadow-sm"] = new() { BoxShadow = new List<BoxShadow> { t.BoxShadowSm } },
            [".shadow-lg"] = new() { BoxShadow = new List<BoxShadow> { t.BoxShadowLg } },
            // NOTE: .shadow-none would need BoxShadow = null, but CssObject doesn't support clearing box-shadow

            // Rounded
            [".rounded"] = new() { BorderRadius = t.BorderRadius },
            [".rounded-0"] = new() { BorderRadius = 0 },
            [".rounded-1"] = new() { BorderRadius = t.BorderRadiusSm },
            [".rounded-2"] = new() { BorderRadius = t.BorderRadius },
            [".rounded-3"] = new() { BorderRadius = t.BorderRadiusLg },
            [".rounded-4"] = new() { BorderRadius = t.BorderRadiusXl },
            [".rounded-5"] = new() { BorderRadius = t.BorderRadiusXxl },
            [".rounded-circle"] = new() { BorderRadius = new BorderRadius(Length.Percent(50)) },
            [".rounded-pill"] = new() { BorderRadius = t.BorderRadiusPill },
            [".rounded-top"] = new()
            {
                BorderTopLeftRadius = Length.Px(t.BorderRadius),
                BorderTopRightRadius = Length.Px(t.BorderRadius),
                BorderBottomLeftRadius = Length.Px(0),
                BorderBottomRightRadius = Length.Px(0)
            },
            [".rounded-end"] = new()
            {
                BorderTopRightRadius = Length.Px(t.BorderRadius),
                BorderBottomRightRadius = Length.Px(t.BorderRadius),
                BorderTopLeftRadius = Length.Px(0),
                BorderBottomLeftRadius = Length.Px(0)
            },
            [".rounded-bottom"] = new()
            {
                BorderBottomLeftRadius = Length.Px(t.BorderRadius),
                BorderBottomRightRadius = Length.Px(t.BorderRadius),
                BorderTopLeftRadius = Length.Px(0),
                BorderTopRightRadius = Length.Px(0)
            },
            [".rounded-start"] = new()
            {
                BorderTopLeftRadius = Length.Px(t.BorderRadius),
                BorderBottomLeftRadius = Length.Px(t.BorderRadius),
                BorderTopRightRadius = Length.Px(0),
                BorderBottomRightRadius = Length.Px(0)
            },

            // User select
            [".user-select-all"] = new() { UserSelect = UserSelect.All },
            [".user-select-auto"] = new() { UserSelect = UserSelect.Auto },
            [".user-select-none"] = new() { UserSelect = UserSelect.None },

            // Visibility
            [".visible"] = new() { Visibility = Visibility.Visible },
            [".invisible"] = new() { Visibility = Visibility.Hidden },

            // Z-index
            [".z-n1"] = new() { ZIndex = -1 },
            [".z-0"] = new() { ZIndex = 0 },
            [".z-1"] = new() { ZIndex = 1 },
            [".z-2"] = new() { ZIndex = 2 },
            [".z-3"] = new() { ZIndex = 3 },

            // Cursor
            // NOTE: cursor is not a standard Bootstrap utility, but commonly used
            [".pe-none"] = new() { Cursor = Cursor.Default },
            [".pe-auto"] = new() { Cursor = Cursor.Pointer },
        };

        // Spacing utilities (margin & padding)
        foreach (var (key, value) in spacers)
        {
            // Margin
            styles[$".m-{key}"] = new CssObject { Margin = new Margin(value) };
            styles[$".mx-{key}"] = new CssObject { MarginLeft = value, MarginRight = value };
            styles[$".my-{key}"] = new CssObject { MarginTop = value, MarginBottom = value };
            styles[$".mt-{key}"] = new CssObject { MarginTop = value };
            styles[$".me-{key}"] = new CssObject { MarginRight = value };
            styles[$".mb-{key}"] = new CssObject { MarginBottom = value };
            styles[$".ms-{key}"] = new CssObject { MarginLeft = value };

            // Padding
            styles[$".p-{key}"] = new CssObject { Padding = new Padding(value) };
            styles[$".px-{key}"] = new CssObject { PaddingLeft = value, PaddingRight = value };
            styles[$".py-{key}"] = new CssObject { PaddingTop = value, PaddingBottom = value };
            styles[$".pt-{key}"] = new CssObject { PaddingTop = value };
            styles[$".pe-{key}"] = new CssObject { PaddingRight = value };
            styles[$".pb-{key}"] = new CssObject { PaddingBottom = value };
            styles[$".ps-{key}"] = new CssObject { PaddingLeft = value };

            // Gap
            styles[$".gap-{key}"] = new CssObject { Gap = value };
            styles[$".row-gap-{key}"] = new CssObject { RowGap = value };
            styles[$".column-gap-{key}"] = new CssObject { ColumnGap = value };
        }

        // Margin auto
        styles[".m-auto"] = new CssObject { Margin = new Margin(Length.Auto) };
        styles[".mx-auto"] = new CssObject { MarginLeft = Length.Auto, MarginRight = Length.Auto };
        styles[".my-auto"] = new CssObject { MarginTop = Length.Auto, MarginBottom = Length.Auto };
        styles[".mt-auto"] = new CssObject { MarginTop = Length.Auto };
        styles[".me-auto"] = new CssObject { MarginRight = Length.Auto };
        styles[".mb-auto"] = new CssObject { MarginBottom = Length.Auto };
        styles[".ms-auto"] = new CssObject { MarginLeft = Length.Auto };

        return styles;
    }
}
