using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the item grouping and sliding components (<c>ion-item-group</c>,
/// <c>ion-item-divider</c>, <c>ion-item-sliding</c>, <c>ion-item-options</c>,
/// <c>ion-item-option</c>). Ported from the Ionic source: <c>item-group.scss</c>,
/// <c>item-divider.scss</c> / <c>.md.scss</c> / <c>.ios.scss</c>, <c>item-sliding.scss</c>,
/// <c>item-options.scss</c>, <c>item-option.scss</c>.
/// <para>
/// The base <c>ion-item</c> / <c>ion-list</c> / <c>ion-list-header</c> rules live in
/// <see cref="ListStyles"/>. Rules here are scoped by the active mode class (<c>md</c> /
/// <c>ios</c>); see <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class ItemStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // ion-item-group — a plain block container that groups items into a section. No
            // visual styling of its own (item-group.scss `:host` is just display:block).
            [$".ion-item-group.{mode}"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
            },

            // ion-item-divider — a section header row: flex row, vertically centered, with a
            // tinted fill, its own min-height, and a hairline bottom divider.
            [$".ion-item-divider.{mode}"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceBetween,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.ItemDividerMinHeight),
                PaddingLeft = Length.Px(t.ItemPaddingStart),
                PaddingRight = Length.Px(t.ItemPaddingStart),
                BackgroundColor = t.ItemDividerBackground,
                Color = t.ItemDividerColor,
                FontSize = Length.Px(t.ListHeaderFontSize),
                FontWeight = FontWeight.Medium,
                BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, t.ItemBorderColor),
            },

            // .item-divider-inner — arranges the divider content; grows to fill the host.
            [$".ion-item-divider.{mode} .item-divider-inner"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                FlexGrow = 1,
                MinHeight = Length.Percent(100),
            },

            // .item-divider-wrapper — wraps the default slot (the label); grows to take the space.
            [$".ion-item-divider.{mode} .item-divider-wrapper"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                FlexGrow = 1,
            },

            // Slot markers used by ion-item / ion-item-divider (the start/end RenderFragments).
            // The leading marker gets a trailing gap; the trailing marker a leading gap, so slotted
            // content (icons, notes, controls) does not butt against the label.
            [$".ion-item.{mode} .ion-slot-start"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                MarginRight = Length.Px(16),
            },
            [$".ion-item.{mode} .ion-slot-end"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                MarginLeft = Length.Px(16),
            },
            [$".ion-item-divider.{mode} .ion-slot-start"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                MarginRight = Length.Px(16),
            },
            [$".ion-item-divider.{mode} .ion-slot-end"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                MarginLeft = Length.Px(16),
            },

            // ion-item-sliding — the slide host. position:relative anchors the absolutely-placed
            // options; overflow:hidden keeps them off-screen until the row is opened.
            [$".ion-item-sliding.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                Width = Length.Percent(100),
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            // ion-item-options — the action-button container, absolutely positioned over the row
            // and pushed off the edge it is anchored to (end → right of the row, translated fully
            // to the right; start → left of the row, translated fully to the left). Revealed by
            // the open-side rules below.
            [$".ion-item-options.{mode}"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Bottom = Length.Px(0),
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Stretch,
                Transitions = new List<Transition>
                {
                    new Transition(nameof(Style.Transform), 0.2f, TimingFunction.EaseOut),
                },
            },

            // end-anchored options sit at the right edge, translated fully off to the right.
            [$".ion-item-options.{mode}.ion-item-options-end"] = new()
            {
                Right = Length.Px(0),
                Transform = new Transform(new TransformFunction.TranslateX(Length.Percent(100))),
            },

            // start-anchored options sit at the left edge, translated fully off to the left.
            [$".ion-item-options.{mode}.ion-item-options-start"] = new()
            {
                Left = Length.Px(0),
                Transform = new Transform(new TransformFunction.TranslateX(Length.Percent(-100))),
            },

            // When the row is open, slide the matching side's options back into view.
            [$".ion-item-sliding.{mode}.item-sliding-open-end .ion-item-options-end"] = new()
            {
                Transform = new Transform(new TransformFunction.TranslateX(Length.Px(0))),
            },
            [$".ion-item-sliding.{mode}.item-sliding-open-start .ion-item-options-start"] = new()
            {
                Transform = new Transform(new TransformFunction.TranslateX(Length.Px(0))),
            },

            // ion-item-option — the host: a single filled action button. White label centered on
            // the brand fill (default primary); the named-color variants below override the fill.
            [$".ion-item-option.{mode}"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Stretch,
                MinHeight = Length.Percent(100),
                BackgroundColor = t.ItemOptionBackground,
                Color = t.ItemOptionColor,
                FontSize = Length.Px(16),
                FontWeight = FontWeight.Normal,
            },

            // .button-native — the clickable surface: fills the host, transparent over its fill,
            // with the option's side padding.
            [$".ion-item-option.{mode} .button-native"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
                MinHeight = Length.Percent(100),
                PaddingLeft = Length.Px(15),
                PaddingRight = Length.Px(15),
                BackgroundColor = Color.Transparent,
                Color = t.ItemOptionColor,
                BorderWidth = Length.Px(0),
                Cursor = Cursor.Pointer,
            },

            // .button-inner — centers the option's content.
            [$".ion-item-option.{mode} .button-inner"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
            },

            // .horizontal-wrapper — wraps the default slot content, centered.
            [$".ion-item-option.{mode} .horizontal-wrapper"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
            },

            // Expandable option: grows to fill the available width and cover the other options.
            [$".ion-item-option.{mode}.item-option-expandable"] = new()
            {
                FlexGrow = 1,
            },

            // Disabled option: dimmed and non-interactive.
            [$".ion-item-option.{mode}.item-option-disabled"] = new()
            {
                Opacity = 0.4f,
                PointerEvents = PointerEvents.None,
            },

            // Named-color fills (Ionic's --ion-color-* palette). Stamped by IonItemOption from the
            // `color` attribute. White label stays from the base rule.
            [$".ion-item-option.{mode}.ion-color-primary"] = new() { BackgroundColor = t.Primary },
            [$".ion-item-option.{mode}.ion-color-secondary"] = new() { BackgroundColor = t.Secondary },
            [$".ion-item-option.{mode}.ion-color-tertiary"] = new() { BackgroundColor = t.Tertiary },
            [$".ion-item-option.{mode}.ion-color-success"] = new() { BackgroundColor = t.Success },
            [$".ion-item-option.{mode}.ion-color-warning"] = new() { BackgroundColor = t.Warning },
            [$".ion-item-option.{mode}.ion-color-danger"] = new() { BackgroundColor = t.Danger },
            [$".ion-item-option.{mode}.ion-color-light"] = new() { BackgroundColor = t.Light },
            [$".ion-item-option.{mode}.ion-color-medium"] = new() { BackgroundColor = t.Medium },
            [$".ion-item-option.{mode}.ion-color-dark"] = new() { BackgroundColor = t.Dark },
        };

        return css;
    }
}
