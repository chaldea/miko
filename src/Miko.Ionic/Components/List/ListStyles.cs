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

            // ion-item — a single row: leading icon (slot="start") + label, vertically
            // centered, with a hairline bottom divider.
            [$".ion-item.{mode}"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.ItemMinHeight),
                PaddingLeft = Length.Px(t.ItemPaddingStart),
                PaddingRight = Length.Px(t.ItemPaddingStart),
                Color = t.ItemColor,
                BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, t.ItemBorderColor),
            },

            // lines="none": drop the bottom divider.
            [$".ion-item.{mode}.ion-item-lines-none"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },

            // Leading icon (slot="start") inside an item: fixed size + gap before the label.
            [$".ion-item.{mode} .ion-icon"] = new()
            {
                Width = Length.Px(24),
                Height = Length.Px(24),
                MarginRight = Length.Px(16),
            },
        };
    }
}
