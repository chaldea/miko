using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the list components (<c>ion-list</c>, <c>ion-list-header</c>, <c>ion-item</c>).
/// Ported from the Ionic source: <c>list.scss</c> / <c>list.md.scss</c> / <c>list.ios.scss</c>,
/// <c>list-header.scss</c>, <c>item.scss</c> / <c>item.md.scss</c> / <c>item.ios.scss</c>.
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>); see
/// <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class ListStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        return new CssObject
        {
            // ion-list — vertical stack of items with a small top/bottom inset.
            [$".ion-list.{mode}"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                BackgroundColor = t.ListBackground,
                PaddingTop = Length.Px(8),
                PaddingBottom = Length.Px(8),
            },

            // ion-list-header — section header above the items.
            [$".ion-list-header.{mode}"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.ItemMinHeight),
                PaddingLeft = Length.Px(t.ItemPaddingStart),
                PaddingRight = Length.Px(t.ItemPaddingStart),
                FontSize = Length.Px(t.ListHeaderFontSize),
                FontWeight = FontWeight.Medium,
                Color = t.ListHeaderColor,
            },

            // ion-item — the host: a block box that clips its native surface. The flex row lives
            // on .item-native so the host can carry the color/lines/disabled state (item.scss's
            // :host is display:block; the .item-native inside is the flex row).
            [$".ion-item.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                Width = Length.Percent(100),
                Color = t.ItemColor,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            // .item-native — the clickable/native surface: the flex row, full width, min-height,
            // side padding, and the hairline bottom divider.
            [$".ion-item.{mode} .item-native"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceBetween,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.ItemMinHeight),
                PaddingLeft = Length.Px(t.ItemPaddingStart),
                PaddingRight = Length.Px(t.ItemPaddingStart),
                BackgroundColor = Color.Transparent,
                BorderWidth = Length.Px(0),
                Color = t.ItemColor,
                Cursor = Cursor.Pointer,
                BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, t.ItemBorderColor),
            },

            // .item-inner — arranges the label/end/detail row; grows to fill the native surface.
            [$".ion-item.{mode} .item-inner"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                FlexGrow = 1,
                MinHeight = Length.Percent(100),
            },

            // .input-wrapper — wraps the default slot (the label); grows to take the free space so
            // the end slot / detail icon sit at the trailing edge.
            [$".ion-item.{mode} .input-wrapper"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                FlexGrow = 1,
            },

            // lines="none": drop the bottom divider.
            [$".ion-item.{mode}.item-lines-none .item-native"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },

            // Disabled item: dimmed and non-interactive (item.scss :host(.item-disabled)).
            [$".ion-item.{mode}.item-disabled"] = new()
            {
                Opacity = 0.3f,
                PointerEvents = PointerEvents.None,
            },

            // Leading icon (slot="start") inside an item: fixed size + gap before the label.
            [$".ion-item.{mode} .ion-icon"] = new()
            {
                Width = Length.Px(24),
                Height = Length.Px(24),
                MarginRight = Length.Px(16),
            },

            // Detail chevron at the trailing edge: a muted, slightly smaller icon.
            [$".ion-item.{mode} .item-detail-icon"] = new()
            {
                Width = Length.Px(18),
                Height = Length.Px(18),
                MarginLeft = Length.Px(8),
                Opacity = 0.25f,
            },
        };
    }
}
