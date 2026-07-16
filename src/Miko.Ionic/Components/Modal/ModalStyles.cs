using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-modal</c>. Ported from the Ionic source: <c>modal.scss</c> / <c>.md.scss</c> /
/// <c>.ios.scss</c> (+ their <c>*.vars.scss</c>).
/// <para>
/// A centered card overlay: a fixed full-screen host that centers a wrapper (the modal content
/// surface). The wrapper carries the card background/border-radius/shadow and hosts the modal body
/// (<c>@ChildContent</c> — usually an <c>IonHeader</c> + <c>IonContent</c>). ios also draws a
/// <c>.modal-shadow</c> layer behind the wrapper. In the web the wrapper is fullscreen and only
/// insets past a 768px breakpoint (<c>$modal-inset-*</c>); this native port renders the inset card
/// directly (a capped-width centered surface) since there is no browser viewport media query and the
/// sheet/card gestures are out of scope. Rules are scoped by the active mode class (<c>md</c> /
/// <c>ios</c>); see <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class ModalStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        // $modal-inset-width = 600px, $modal-inset-height-small = 500px (modal.vars.scss). We cap the
        // centered card to these so it reads as a modal rather than a fullscreen sheet.
        var insetWidth = Length.Px(600);   // $modal-inset-width
        var insetHeight = Length.Px(500);  // $modal-inset-height-small

        // --border-radius: modal.scss default 0; md inset = 2px ($modal.md.scss); ios card/sheet =
        // 10px ($modal-ios-border-radius).
        var borderRadius = mode == "ios" ? 10f : 2f;

        // Card elevation. md inset: $modal-inset-box-shadow = 0 28px 48px rgba(0,0,0,.4).
        // ios card (tablet): 0px 0px 30px 10px rgba(0,0,0,0.1).
        var boxShadow = mode == "ios"
            ? new List<BoxShadow>
            {
                new BoxShadow(Length.Px(0), Length.Px(0), Length.Px(30), Length.Px(10), new Color(0, 0, 0, 26)),
            }
            : new List<BoxShadow>
            {
                new BoxShadow(Length.Px(0), Length.Px(28), Length.Px(48), Length.Px(0), new Color(0, 0, 0, 102)),
            };

        var css = new CssObject
        {
            // Host — a fixed full-screen overlay that centers its wrapper (modal.scss :host).
            [$".ion-modal.{mode}"] = new()
            {
                Position = Position.Fixed,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Color = t.TextColor,                        // $modal-text-color = $text-color
                ZIndex = 1000,
            },

            // Closed modal is fully hidden (:host(.overlay-hidden)).
            [$".ion-modal.{mode}.overlay-hidden"] = new()
            {
                Display = Display.None,
            },

            // Backdrop — the tappable dim layer filling the host.
            // --backdrop-opacity: md 0.32, ios 0.4.
            [$".ion-modal.{mode} .modal-backdrop"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                BackgroundColor = t.AlertBackdropColor,     // black; shared overlay backdrop color
                Opacity = mode == "ios" ? 0.4f : 0.32f,     // $modal --backdrop-opacity per mode
                Cursor = Cursor.Pointer,
            },

            // Wrapper — the centered card. Carries the modal surface (background / radius / shadow).
            // modal.scss: width/height var(--width/--height) (100%); capped by the inset max-* so the
            // card stays modal-sized on wide viewports.
            [$".ion-modal.{mode} .modal-wrapper"] = new()
            {
                Position = Position.Relative,
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = Length.Percent(100),                // --width: 100%
                MaxWidth = insetWidth,                       // $modal-inset-width (600px)
                Height = Length.Percent(100),               // --height: 100%
                MaxHeight = insetHeight,                     // $modal-inset-height-small (500px)
                BackgroundColor = t.BackgroundColor,         // --background: $background-color
                BorderRadius = new BorderRadius(Length.Px(borderRadius)),
                BoxShadow = boxShadow,
                OverflowX = Overflow.Hidden,                 // --overflow: hidden
                OverflowY = Overflow.Hidden,
                ZIndex = 10,
            },

            // Shadow layer — sits behind the wrapper (ios draws it; modal.scss .modal-shadow).
            [$".ion-modal.{mode} .modal-shadow"] = new()
            {
                Position = Position.Absolute,
                Width = Length.Percent(100),
                MaxWidth = insetWidth,
                Height = Length.Percent(100),
                MaxHeight = insetHeight,
                BackgroundColor = Color.Transparent,         // .modal-shadow background: transparent
                BorderRadius = new BorderRadius(Length.Px(borderRadius)),
            },
        };

        return css;
    }
}
