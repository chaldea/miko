using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-accordion</c> / <c>ion-accordion-group</c>. Ported from the Ionic source:
/// <c>accordion.scss</c> / <c>.md.scss</c> / <c>.ios.scss</c> and
/// <c>accordion-group.scss</c> / <c>.md.scss</c> / <c>.ios.scss</c> (+ their <c>*.vars.scss</c>).
/// <para>
/// An accordion is a block panel with a clickable header row and a content region shown only when
/// the panel is expanded (the enclosing group owns which value(s) are expanded). The toggle icon
/// rotates 180° when expanded. Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>);
/// see <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class AccordionStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // ion-accordion-group — a plain block container.
            [$".ion-accordion-group.{mode}"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
            },

            // Inset groups get an all-round margin (accordion-group.scss).
            [$".ion-accordion-group.{mode}.accordion-group-expand-inset"] = new()
            {
                MarginTop = Length.Px(t.AccordionInsetMargin),
                MarginRight = Length.Px(t.AccordionInsetMargin),
                MarginBottom = Length.Px(t.AccordionInsetMargin),
                MarginLeft = Length.Px(t.AccordionInsetMargin),
            },

            // Disabled group dims and blocks the whole subtree.
            [$".ion-accordion-group.{mode}.accordion-group-disabled"] = new()
            {
                Opacity = t.AccordionDisabledOpacity,
                PointerEvents = PointerEvents.None,
            },

            // ion-accordion host — a block panel. position:relative + overflow:hidden + z-index:0
            // establishes a stacking context so the (inset) border radius clips its header/content.
            [$".ion-accordion.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                Width = Length.Percent(100),
                BackgroundColor = t.AccordionBackground,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
                ZIndex = 0,
            },

            // .accordion-header — the clickable header row. Ionic makes it a flex row so the slotted
            // ion-item fills it and the toggle icon sits at the chosen end.
            [$".ion-accordion.{mode} .accordion-header"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Width = Length.Percent(100),
                Cursor = Cursor.Pointer,
            },

            // Hover wash across the header row. In Ionic the wash is not the accordion's own:
            // accordion.tsx setItemDefaults() sets `ionItem.button = true`, so the slotted header
            // item becomes ion-activatable and paints ITS hover overlay
            // (.ion-item.ion-activatable:hover .item-native — see ListStyles). Ionic also slots the
            // toggle icon INSIDE that item, so the overlay covers the whole row including the
            // chevron. This port renders the icon as a sibling of the item inside .accordion-header,
            // so putting the wash on the header (rather than stamping the item activatable) is what
            // actually covers the same area. Same value as the item's overlay: the row's text color
            // at 4%.
            [$".ion-accordion.{mode} .accordion-header:hover"] = new()
            {
                BackgroundColor = WithAlpha(t.ItemColor, 10), // --background-hover: currentColor @ .04
            },

            // The slotted header item fills the header row (so the toggle icon can sit beside it).
            [$".ion-accordion.{mode} .accordion-header .ion-item"] = new()
            {
                FlexGrow = 1,
            },

            // Collapsed accordion: the header item's divider spans the WHOLE row. Ionic's
            // accordion.tsx setItemDefaults() defaults the slotted header item to lines="full"
            // (`if (ionItem.lines === undefined) ionItem.lines = 'full'`), which moves the border
            // off .item-inner (inset, indented by the leading padding) onto the full-width
            // .item-native. Retargeting only .item-lines-default mirrors the `=== undefined`
            // guard — an explicit `Lines` on the header item still wins (same pattern as the
            // list-level lines rules in ListStyles).
            [$".ion-accordion.{mode}.accordion-collapsed .accordion-header .ion-item.item-lines-default .item-native"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, t.ItemBorderColor),
            },
            [$".ion-accordion.{mode}.accordion-collapsed .accordion-header .ion-item.item-lines-default .item-inner"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },

            // Expanded/expanding accordion: the header item shows NO border at all. Ionic's
            // accordion.scss :host(.accordion-expanding/.accordion-expanded) ::slotted(ion-item[slot="header"])
            // zeroes --border-width (on native) AND --inner-border-width.
            [$".ion-accordion.{mode}.accordion-expanded .accordion-header .ion-item .item-native"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },
            [$".ion-accordion.{mode}.accordion-expanded .accordion-header .ion-item .item-inner"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },
            [$".ion-accordion.{mode}.accordion-expanding .accordion-header .ion-item .item-native"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },
            [$".ion-accordion.{mode}.accordion-expanding .accordion-header .ion-item .item-inner"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },

            // .accordion-content — the collapsible region; hidden entirely when collapsed.
            [$".ion-accordion.{mode} .accordion-content"] = new()
            {
                Display = Display.Block,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },
            [$".ion-accordion.{mode}.accordion-collapsed .accordion-content"] = new()
            {
                Display = Display.None,
            },

            // .ion-accordion-toggle-icon — the chevron; rotates 180° when the panel is expanded.
            // Ionic injects the icon into ion-item's end slot, so it benefits from .item-inner's
            // padding-right (16px = --inner-padding-end). In Miko the icon wrapper sits outside
            // .item-inner in the flex row, so we replicate that spacing with an explicit right margin.
            [$".ion-accordion.{mode} .ion-accordion-toggle-icon"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                MarginRight = Length.Px(t.ItemPaddingEnd), // mirrors --inner-padding-end (16px)
            },
            [$".ion-accordion.{mode} .ion-accordion-toggle-icon .ion-icon"] = new()
            {
                FontSize = Length.Px(20),
                Color = t.Medium,
            },
            [$".ion-accordion.{mode}.accordion-expanded .ion-accordion-toggle-icon .ion-icon"] = new()
            {
                Transform = Transform.FromRotate(180),
            },

            // The toggle icon in the start slot orders before the item; end slot after (default).
            [$".ion-accordion.{mode} .accordion-toggle-icon-start"] = new()
            {
                Order = -1,
            },

            // Disabled/readonly panels block interaction on the header/content.
            [$".ion-accordion.{mode}.accordion-disabled .accordion-header"] = new()
            {
                Opacity = t.AccordionDisabledOpacity,
                PointerEvents = PointerEvents.None,
            },
            [$".ion-accordion.{mode}.accordion-disabled .accordion-content"] = new()
            {
                Opacity = t.AccordionDisabledOpacity,
                PointerEvents = PointerEvents.None,
            },
            [$".ion-accordion.{mode}.accordion-readonly .accordion-header"] = new()
            {
                PointerEvents = PointerEvents.None,
            },
            [$".ion-accordion.{mode}.accordion-readonly .accordion-content"] = new()
            {
                PointerEvents = PointerEvents.None,
            },
        };

        // Inset panels get a rounded, elevated card look (md a 3-layer shadow; ios none).
        if (t.AccordionInsetBoxShadow.Count > 0)
        {
            css[$".ion-accordion-group.{mode}.accordion-group-expand-inset .ion-accordion"] = new()
            {
                BoxShadow = t.AccordionInsetBoxShadow,
                BorderRadius = new BorderRadius(Length.Px(t.AccordionInsetBorderRadius)),
            };
        }
        else
        {
            css[$".ion-accordion-group.{mode}.accordion-group-expand-inset .ion-accordion"] = new()
            {
                BorderRadius = new BorderRadius(Length.Px(t.AccordionInsetBorderRadius)),
            };
        }

        return css;
    }

    private static Color WithAlpha(Color c, byte alpha) => new(c.R, c.G, c.B, alpha);
}
