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

            // The slotted header item fills the header row (so the toggle icon can sit beside it).
            [$".ion-accordion.{mode} .accordion-header .ion-item"] = new()
            {
                FlexGrow = 1,
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
            [$".ion-accordion.{mode} .ion-accordion-toggle-icon"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
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
}
