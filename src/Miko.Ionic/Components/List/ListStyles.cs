using Miko.Common;
using Miko.Styling;
using static Miko.Ionic.Styles.IonicMixins;

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
        var css = new CssObject
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

            // list-inset (list.md.scss / list.ios.scss .list-{mode}.list-inset): margin around
            // the list, rounded corners, clipped content (list.scss: overflow hidden).
            [$".ion-list.{mode}.list-inset"] = new()
            {
                MarginTop = Length.Px(t.ListInsetMargin),
                MarginBottom = Length.Px(t.ListInsetMargin),
                MarginLeft = Length.Px(t.ListInsetMargin),
                MarginRight = Length.Px(t.ListInsetMargin),
                BorderRadius = new BorderRadius(Length.Px(t.ListInsetBorderRadius)),
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            // Two adjacent inset lists collapse the gap between them
            // (list.md.scss / list.ios.scss: .list-{mode}.list-inset + ion-list.list-inset).
            [$".ion-list.{mode}.list-inset + .ion-list.list-inset"] = new()
            {
                MarginTop = Length.Px(0),
            },

            // ion-list-header — section header above the items.
            [$".ion-list-header.{mode}"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceBetween,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.ItemMinHeight),
                PaddingLeft = Length.Px(t.ItemPaddingStart),
                PaddingRight = Length.Px(t.ItemPaddingStart),
                BackgroundColor = Color.Transparent,
                FontSize = Length.Px(t.ListHeaderFontSize),
                FontWeight = FontWeight.Medium,
                Color = t.ListHeaderColor,
                Overflow = Overflow.Hidden,
            },

            // list-header.scss .list-header-inner — the inner row carries inset lines and keeps
            // the default slot laid out as a single flex row.
            [$".ion-list-header.{mode} .list-header-inner"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                Flex = 1,
                AlignItems = AlignItems.Center,
                AlignSelf = AlignSelf.Stretch,
                MinHeight = Inherit,
                Overflow = Overflow.Hidden,
            },

            // list-header.scss ::slotted(ion-label) { flex: 1 1 auto; }. Only a label in the
            // header's own default slot receives this, not labels nested inside child controls.
            [$".ion-list-header.{mode} .list-header-inner > .ion-label"] = new()
            {
                Flex = Flex.Auto,
            },

            // `lines="full"` draws across the host; `lines="inset"` starts after the host's
            // leading padding by drawing on the inner row; `none` keeps both surfaces borderless.
            [$".ion-list-header.{mode}.list-header-lines-full"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, t.ItemBorderColor),
            },
            [$".ion-list-header.{mode}.list-header-lines-full .list-header-inner"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },
            [$".ion-list-header.{mode}.list-header-lines-inset"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },
            [$".ion-list-header.{mode}.list-header-lines-inset .list-header-inner"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, t.ItemBorderColor),
            },
            [$".ion-list-header.{mode}.list-header-lines-none"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },
            [$".ion-list-header.{mode}.list-header-lines-none .list-header-inner"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
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
            // and leading padding. Ionic's --padding-end defaults to 0 (only --padding-start is
            // 16px) — the trailing inset lives on .item-inner's --inner-padding-end instead, so an
            // inset divider on .item-inner reaches the item's right edge. The default hairline
            // divider is NOT here: :host's --border-width defaults to 0 (item.md.scss /
            // item.ios.scss) and the default divider sits on .item-inner below (inset look);
            // only lines="full" moves it onto this surface (rules further down).
            [$".ion-item.{mode} .item-native"] = new()
            {
                // item.scss .item-native { @include text-inherit(); } — the native can be a
                // <button>/<a>, whose UA defaults (text-align: center on button, underline on a)
                // would otherwise leak through and, say, center a button item's content.
                // Directly written properties below (e.g. Color) still win over the mixin.
                ["..."] = TextInherit(),
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceBetween,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.ItemMinHeight),
                // Ionic's --padding-top/--padding-bottom default to 0; zero them explicitly so the
                // UA button vertical padding (2px) does not leak through either.
                PaddingTop = Length.Px(0),
                PaddingBottom = Length.Px(0),
                PaddingLeft = Length.Px(t.ItemPaddingStart),
                PaddingRight = Length.Px(0),
                BackgroundColor = Color.Transparent,
                BorderWidth = Length.Px(0),
                Color = t.ItemColor,
                // No cursor here: item.scss gives cursor:pointer only to button/a natives
                // (`button, a { cursor: pointer }`); a plain div native keeps the default arrow.
                // See the tag-qualified rules below.
            },

            // item.scss `button, a { cursor: pointer }` — the pointer hand appears only when the
            // native surface is actually clickable (Button → <button>, Href → <a>).
            [$".ion-item.{mode} button.item-native"] = new()
            {
                Cursor = Cursor.Pointer,
            },
            [$".ion-item.{mode} a.item-native"] = new()
            {
                Cursor = Cursor.Pointer,
            },

            // Hover (item.scss @media (any-hover: hover) :host(.ion-activatable:not(.ion-focused)
            // :hover) .item-native::after): a 4% wash of the item's text color across the row,
            // shown only for clickable items (Button/Href stamp ion-activatable; a plain div item
            // has no hover style). Miko has no ::after opacity layer and no pointer-capability
            // media query (touch devices simply never hover), so the wash becomes a plain :hover
            // rule on the native's background — otherwise transparent, the translucent fill
            // composites over the surface behind the row just like Ionic's overlay. Mirrors the
            // Button/Chip hover ports; the hover state propagates up the hit chain, so hovering
            // any content inside flags the host :hover too.
            [$".ion-item.{mode}.ion-activatable:hover .item-native"] = new()
            {
                BackgroundColor = WithAlpha(t.ItemColor, 10), // --background-hover: currentColor @ .04
            },

            // .item-inner — arranges the label/end/detail row; grows to fill the native surface.
            // Carries the trailing padding (Ionic --inner-padding-end, 16px both modes) so slotted
            // end content stays off the edge while the box itself spans to the item's right edge.
            // Also carries the DEFAULT hairline divider: :host's --inner-border-width defaults to
            // `0 0 1px 0` in both modes (item.md.scss / item.ios.scss), so a default item's
            // divider starts after the leading padding (inset look) and reaches the right edge —
            // matching Ionic, where only lines="full" draws the border across the whole row.
            [$".ion-item.{mode} .item-inner"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                FlexGrow = 1,
                MinHeight = Inherit,
                PaddingRight = Length.Px(t.ItemPaddingEnd),
                BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, t.ItemBorderColor),
            },

            // .input-wrapper — wraps the default slot (the label); grows to take the free space so
            // the end slot / detail icon sit at the trailing edge.
            [$".ion-item.{mode} .input-wrapper"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                FlexGrow = 1,
            },

            // Default-slot label (item.scss ::slotted(ion-label:not([slot="end"]))): grows to
            // take the row's free space, so trailing content in the slot (badges, notes, …) is
            // pushed to the far edge. Scoping to .input-wrapper mirrors :not([slot="end"]) — the
            // port renders the default slot there and slot="end" content in .ion-slot-end.
            // (Ionic also sets width:min-content so the label shrink-wraps; Miko's Length has no
            // min-content and the default flex shrink covers the intent.)
            [$".ion-item.{mode} .input-wrapper .ion-label"] = new()
            {
                FlexGrow = 1,
                MaxWidth = Length.Percent(100),
            },

            // Slotted label margins (item.md.scss / item.ios.scss ::slotted(ion-label)): the
            // label's vertical rhythm inside the row — 10px top/bottom both modes, plus an 8px
            // end margin on iOS only. ::slotted is not slot-scoped, so this applies to labels in
            // the default slot AND the start/end marker spans alike (unlike the flex rule above,
            // which mirrors :not([slot="end"])).
            [$".ion-item.{mode} .ion-label"] = new()
            {
                MarginTop = Length.Px(t.ItemLabelMarginVertical),
                MarginBottom = Length.Px(t.ItemLabelMarginVertical),
                MarginRight = Length.Px(t.ItemLabelMarginEnd),
            },

            // lines="full": move the divider from .item-inner onto the full-width native surface
            // (item.md.scss / item.ios.scss :host(.item-lines-full) sets --border-width and zeroes
            // --inner-border-width).
            [$".ion-item.{mode}.item-lines-full .item-native"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, t.ItemBorderColor),
            },
            [$".ion-item.{mode}.item-lines-full .item-inner"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },

            // lines="inset" needs no rule: the default divider already sits on .item-inner, which
            // starts after the leading padding — the inset look. Setting lines="inset" explicitly
            // only opts the item out of the list-level retargeting below (which matches
            // .item-lines-default only).
            //
            // lines="none": drop the default divider from .item-inner (the native surface has no
            // border by default).
            [$".ion-item.{mode}.item-lines-none .item-inner"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },

            // List-level lines (list.md.scss / list.ios.scss): retarget ONLY .item-lines-default
            // items, so an item's own `lines` always takes priority over the list's.
            // lines="none" — drop the divider (from .item-inner, where the default puts it).
            [$".ion-list.{mode}.list-{mode}-lines-none .ion-item.item-lines-default .item-inner"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },
            // lines="full" — move the divider onto the full-width native surface.
            [$".ion-list.{mode}.list-{mode}-lines-full .ion-item.item-lines-default .item-native"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, t.ItemBorderColor),
            },
            [$".ion-list.{mode}.list-{mode}-lines-full .ion-item.item-lines-default .item-inner"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },
            // lines="inset" needs no rule: the default divider is already inset (on .item-inner).

            // Disabled item: dimmed and non-interactive (item.scss :host(.item-disabled)).
            [$".ion-item.{mode}.item-disabled"] = new()
            {
                Opacity = 0.3f,
                PointerEvents = PointerEvents.None,
            },

            // Slotted icons (item.scss ::slotted(ion-icon) + item.md.scss / item.ios.scss
            // ::slotted(ion-icon[slot…])). Only icons placed DIRECTLY in one of the ITEM's own
            // slots may be styled — ::slotted cannot reach icons inside nested components.
            // Since the ion-slot-* marker class is shared with other components (an ion-button
            // wraps its slots in the same marker spans), the item's own markers are pinned
            // structurally: the start marker is a direct child of .item-native, the end marker
            // and .input-wrapper direct children of .item-inner. Without that, an ion-button's
            // icons would inherit the item's 24px box and slot margins (ion-item.md issue #8).
            //
            // Default-slot icon: the 24px box, NO margin — Ionic styles unslotted icons with
            // font-size only; margins exist solely for icons assigned to the start/end slots.
            [$".ion-item.{mode} .item-inner > .input-wrapper > .ion-icon"] = new()
            {
                Width = Length.Px(24),
                Height = Length.Px(24),
            },

            // start-slotted icon: 12px (md) / 7px (ios) vertical, plus a 32px gap before the
            // label on md ($item-md-icon-start-slot-margin-end; the iOS vars are null).
            [$".ion-item.{mode} .item-native > .ion-slot-start > .ion-icon"] = new()
            {
                Width = Length.Px(24),
                Height = Length.Px(24),
                MarginTop = Length.Px(t.ItemIconSlotMarginVertical),
                MarginBottom = Length.Px(t.ItemIconSlotMarginVertical),
                MarginRight = Length.Px(t.ItemIconStartSlotMarginEnd),
            },

            // end-slotted icon: 12px (md) / 7px (ios) vertical, plus a 16px gap after the
            // label on md ($item-md-icon-end-slot-margin-start; the iOS vars are null).
            [$".ion-item.{mode} .item-inner > .ion-slot-end > .ion-icon"] = new()
            {
                Width = Length.Px(24),
                Height = Length.Px(24),
                MarginTop = Length.Px(t.ItemIconSlotMarginVertical),
                MarginBottom = Length.Px(t.ItemIconSlotMarginVertical),
                MarginLeft = Length.Px(t.ItemIconEndSlotMarginStart),
            },

            // Avatar inside ion-item (item.md.scss / item.ios.scss ::slotted(ion-avatar…)):
            // overrides the default avatar size to a smaller one appropriate for list rows
            // (md 40px, ios 36px — $item-{md|ios}-avatar-width) and carries the media-slot
            // margins: 8px vertical on md; a gap on the label-facing edge for slotted avatars
            // (md: 16px both slots; ios: 8px leading for end-slotted only — iOS's remaining
            // 8px sides are subsumed by the row's vertical centering and .item-inner padding).
            // The slot rules pin the item's own markers structurally, same as the icon rules.
            [$".ion-item.{mode} .ion-avatar"] = new()
            {
                Width = Length.Px(t.ItemAvatarSize),
                Height = Length.Px(t.ItemAvatarSize),
                MarginTop = Length.Px(t.ItemAvatarSlotMarginVertical),
                MarginBottom = Length.Px(t.ItemAvatarSlotMarginVertical),
            },
            [$".ion-item.{mode} .item-native > .ion-slot-start .ion-avatar"] = new()
            {
                MarginRight = Length.Px(t.ItemAvatarStartSlotMarginEnd),
            },
            [$".ion-item.{mode} .item-inner > .ion-slot-end .ion-avatar"] = new()
            {
                MarginLeft = Length.Px(t.ItemAvatarEndSlotMarginStart),
            },

            // Thumbnail inside ion-item (item.md.scss / item.ios.scss ::slotted(ion-thumbnail…)):
            // the item overrides the thumbnail's --size to 56px in BOTH modes
            // ($item-{md|ios}-thumbnail-size) — larger than the standalone 48px, not smaller like
            // the avatar. Ionic groups avatars and thumbnails under the same media-slot margin
            // rules, so the ItemAvatar* margin values apply verbatim here: 8px vertical on md, and
            // a 16px gap on the label-facing edge (md both slots; ios 8px leading for end-slotted).
            [$".ion-item.{mode} .ion-thumbnail"] = new()
            {
                Width = Length.Px(t.ItemThumbnailSize),
                Height = Length.Px(t.ItemThumbnailSize),
                MarginTop = Length.Px(t.ItemAvatarSlotMarginVertical),
                MarginBottom = Length.Px(t.ItemAvatarSlotMarginVertical),
            },
            [$".ion-item.{mode} .item-native > .ion-slot-start .ion-thumbnail"] = new()
            {
                MarginRight = Length.Px(t.ItemAvatarStartSlotMarginEnd),
            },
            [$".ion-item.{mode} .item-inner > .ion-slot-end .ion-thumbnail"] = new()
            {
                MarginLeft = Length.Px(t.ItemAvatarEndSlotMarginStart),
            },

            // Detail chevron at the trailing edge: a muted, slightly smaller icon.
            [$".ion-item.{mode} .item-detail-icon"] = new()
            {
                Width = Length.Px(18),
                Height = Length.Px(18),
                MarginLeft = Length.Px(8),
                Opacity = 0.25f,
            },

            // Inset list: the last item shows no divider at all (list.md.scss / list.ios.scss:
            // .list-{mode}.list-inset ion-item:last-of-type zeroes --border-width AND
            // --inner-border-width). The item-last-in-list marker is stamped by IonList.Build()
            // because Miko's selector set has no :last-of-type. These rules sit last so they win
            // the source-order tie against the equally specific list-lines rules above, and they
            // out-specify the item-level lines rules — matching Ionic, where the inset last-item
            // override beats even an item's own `lines`.
            [$".ion-list.{mode}.list-inset .ion-item.item-last-in-list .item-native"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },
            [$".ion-list.{mode}.list-inset .ion-item.item-last-in-list .item-inner"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
            },
        };

        AddListHeaderColor(css, mode, "primary", t.Primary, Color.White);
        AddListHeaderColor(css, mode, "secondary", t.Secondary, Color.White);
        AddListHeaderColor(css, mode, "tertiary", t.Tertiary, Color.White);
        AddListHeaderColor(css, mode, "success", t.Success, Color.Black);
        AddListHeaderColor(css, mode, "warning", t.Warning, Color.Black);
        AddListHeaderColor(css, mode, "danger", t.Danger, Color.White);
        AddListHeaderColor(css, mode, "light", t.Light, Color.Black);
        AddListHeaderColor(css, mode, "medium", t.Medium, Color.White);
        AddListHeaderColor(css, mode, "dark", t.Dark, Color.White);

        return css;
    }

    private static void AddListHeaderColor(
        CssObject css, string mode, string name, Color background, Color color)
    {
        css[$".ion-list-header.{mode}.ion-color-{name}"] = new()
        {
            BackgroundColor = background,
            Color = color,
        };
    }

    private static Color WithAlpha(Color c, byte alpha) => new(c.R, c.G, c.B, alpha);
}
