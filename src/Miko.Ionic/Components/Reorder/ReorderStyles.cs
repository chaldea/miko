using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-reorder</c> and <c>ion-reorder-group</c>. Ported from Ionic's source:
/// <c>reorder.scss</c> / <c>reorder.md.scss</c> / <c>reorder.ios.scss</c> and
/// <c>reorder-group.scss</c> (plus the <c>*.vars.scss</c>).
/// <para>
/// The reorder handle (<c>.ion-reorder</c>) is <c>display:none</c> by default (Ionic's
/// <c>:host([slot]) { display:none }</c>) and only revealed when its group is enabled — the
/// component stamps <c>reorder-enabled</c> (shown) or <c>reorder-hidden</c> (hidden) based on the
/// cascaded group context. The <c>.reorder-icon</c> is the drag glyph, sized and dimmed per mode
/// (md: 31px / 0.3 opacity, ios: 34px / 0.4 opacity — from the reorder mode vars). The group
/// (<c>.ion-reorder-group</c>) is a plain block container.
/// </para>
/// <para>
/// No theme tokens are used: the icon size / opacity are small mode-specific constants hardcoded here
/// (see the <c>$reorder-*</c> comments) rather than added to the shared theme. Rules are scoped by
/// the active mode class (<c>md</c> / <c>ios</c>) like the other ported components.
/// </para>
/// </summary>
internal static class ReorderStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        // $reorder-md-icon-* / $reorder-ios-icon-* — the drag glyph size and dimming per mode.
        var iconFontSize = mode == "ios" ? 34f : 31f;   // $reorder-{ios|md}-icon-font-size
        var iconOpacity = mode == "ios" ? 0.4f : 0.3f;  // $reorder-{ios|md}-icon-opacity

        var css = new CssObject
        {
            // ion-reorder — the drag handle host. Ionic keeps it hidden (display:none) until the
            // enclosing group is enabled; line-height:0 and z-index:100 come from reorder.scss.
            [$".ion-reorder.{mode}"] = new()
            {
                Display = Display.None,
                LineHeight = Length.Number(0),
                ZIndex = 100,
            },

            // .reorder-hidden — explicit hidden marker stamped when the group is missing/disabled.
            [$".ion-reorder.{mode}.reorder-hidden"] = new()
            {
                Display = Display.None,
            },

            // .reorder-enabled — the group is enabled, so the handle is shown and grabbable
            // (Ionic's `.reorder-enabled ion-reorder { display:block; cursor:grab }`). Miko's Cursor
            // enum has no `grab`; Move is the closest available drag affordance.
            [$".ion-reorder.{mode}.reorder-enabled"] = new()
            {
                Display = Display.Block,
                Cursor = Cursor.Move,
            },

            // .reorder-icon — the default drag glyph, block-level so it sizes cleanly.
            [$".ion-reorder.{mode} .reorder-icon"] = new()
            {
                Display = Display.Block,
                FontSize = Length.Px(iconFontSize),
                Opacity = iconOpacity,
            },

            // ion-reorder-group — the list container. Plain block; when a reorder is in progress
            // Ionic adds reorder-list-active (transition on children) — modeled as a marker class.
            [$".ion-reorder-group.{mode}"] = new()
            {
                Display = Display.Block,
            },
        };

        return css;
    }
}
