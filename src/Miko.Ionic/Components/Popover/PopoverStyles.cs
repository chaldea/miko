using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-popover</c>. Ported from the Ionic source: <c>popover.scss</c> / <c>.md.scss</c>
/// / <c>.ios.scss</c> (+ their <c>*.vars.scss</c>).
/// <para>
/// A small floating content box: a fixed full-screen host that (in the web) positions a wrapper next
/// to the trigger. This native port does no runtime anchor math, so the host centers the wrapper and
/// the <c>.popover-content</c> box carries the surface (background / radius / shadow / capped width).
/// An optional <c>.popover-arrow</c> points at the trigger (rotated by the <c>popover-side-*</c>
/// class). md is a 250px-wide 4px-radius box with a 3-layer Material shadow; ios is a narrower 200px
/// 10px-radius box with no shadow (a desktop shadow variant is elided). Rules are scoped by the
/// active mode class (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/> for the mode-scoping
/// rationale.
/// </para>
/// </summary>
internal static class PopoverStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        // Content width: $popover-md-width = 250px, $popover-ios-width = 200px.
        var width = Length.Px(mode == "ios" ? 200 : 250);
        // Content corner radius: $popover-md-border-radius = 4px, $popover-ios-border-radius = 10px.
        var borderRadius = mode == "ios" ? 10f : 4f;

        // Content elevation. md: $popover-md-box-shadow (3-layer Material). ios: none by default
        // (a $popover-ios-desktop-box-shadow variant exists but is desktop-only, elided here).
        var boxShadow = mode == "ios"
            ? new List<BoxShadow>()
            : new List<BoxShadow>
            {
                // $popover-md-box-shadow: 0 5px 5px -3px rgba(0,0,0,.2),
                //                         0 8px 10px 1px rgba(0,0,0,.14),
                //                         0 3px 14px 2px rgba(0,0,0,.12)
                new BoxShadow(Length.Px(0), Length.Px(5), Length.Px(5), Length.Px(-3), new Color(0, 0, 0, 51)),
                new BoxShadow(Length.Px(0), Length.Px(8), Length.Px(10), Length.Px(1), new Color(0, 0, 0, 36)),
                new BoxShadow(Length.Px(0), Length.Px(3), Length.Px(14), Length.Px(2), new Color(0, 0, 0, 31)),
            };

        var css = new CssObject
        {
            // Host — a fixed full-screen overlay that centers its wrapper (popover.scss :host).
            [$".ion-popover.{mode}"] = new()
            {
                Position = Position.Fixed,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Color = t.TextColor,                        // $popover-text-color = $text-color
                ZIndex = 1000,                               // $z-index-overlay
            },

            // Closed popover is fully hidden (:host(.overlay-hidden)).
            [$".ion-popover.{mode}.overlay-hidden"] = new()
            {
                Display = Display.None,
            },

            // Backdrop — the tappable dim layer filling the host.
            // --backdrop-opacity: md 0.32, ios 0.08.
            [$".ion-popover.{mode} .popover-backdrop"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                BackgroundColor = t.AlertBackdropColor,     // black; shared overlay backdrop color
                Opacity = mode == "ios" ? 0.08f : 0.32f,    // $popover --backdrop-opacity per mode
                Cursor = Cursor.Pointer,
            },

            // Wrapper — positioning container for the content box + arrow.
            [$".ion-popover.{mode} .popover-wrapper"] = new()
            {
                Position = Position.Relative,
                ZIndex = 10,                                 // $z-index-overlay-wrapper
            },

            // Content — the floating surface holding the popover body.
            [$".ion-popover.{mode} .popover-content"] = new()
            {
                Position = Position.Relative,
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = width,                               // --width per mode
                MaxHeight = Length.Percent(90),              // --max-height: 90%
                BackgroundColor = t.BackgroundColor,         // --background: $popover-background-color
                BorderRadius = new BorderRadius(Length.Px(borderRadius)),
                BoxShadow = boxShadow,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Auto,                   // overflow: auto
                ZIndex = 10,                                 // $z-index-overlay-wrapper
            },

            // Arrow — the little pointer at the trigger. A rotated square peeking out of the content;
            // rotation per side is a marker here (the web uses popover-side-* transforms). The visible
            // square matches the content background.
            [$".ion-popover.{mode} .popover-arrow"] = new()
            {
                Position = Position.Absolute,
                Display = Display.Block,
                Width = Length.Px(20),
                Height = Length.Px(10),
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
                ZIndex = 11,
                BackgroundColor = t.BackgroundColor,         // matches --background (arrow ::after fill)
            },
        };

        return css;
    }
}
