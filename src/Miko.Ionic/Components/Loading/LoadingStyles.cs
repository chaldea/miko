using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-loading</c>. Ported from the Ionic source: <c>loading.scss</c> /
/// <c>.md.scss</c> / <c>.ios.scss</c> (+ their <c>*.vars.scss</c>).
/// <para>
/// A centered blocking overlay: a fixed full-screen host that centers a small dialog (the wrapper)
/// holding a spinner and an optional message. md draws a flat step-50 (#f2f2f2) card with a 2px
/// radius and an elevation shadow; ios draws an #f9f9f9 card with an 8px radius. The spinner is
/// tinted per mode (md primary, ios a mid gray). Rules are scoped by the active mode class
/// (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// <para>
/// Values that have no dedicated theme token are hardcoded from the Ionic vars files (loading has no
/// tokens on <see cref="IonicTheme"/> yet). The recommended tokens to add are noted inline.
/// </para>
/// </summary>
internal static class LoadingStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var ios = mode == "ios";

        // $loading-*-background: md $background-color-step-50 (#f2f2f2); ios $overlay-ios-background-color (#f9f9f9).
        var background = ios ? Color.FromHex("f9f9f9") : Color.FromHex("f2f2f2");
        // $loading-*-text-color: md $text-color-step-150 (#262626); ios $text-color (#000000).
        var textColor = ios ? Color.FromHex("000000") : Color.FromHex("262626");
        // $loading-*-spinner-color: md ion-color(primary); ios $text-color-step-400 (#666666).
        var spinnerColor = ios ? Color.FromHex("666666") : t.Primary;
        // $loading-*-border-radius: md 2px; ios 8px.
        var borderRadius = ios ? 8f : 2f;
        // $loading-*-max-width: md 280px; ios 270px.
        var maxWidth = ios ? 270f : 280f;
        // $loading-*-padding-top / -bottom: 24px both modes.
        var paddingY = 24f;
        // $loading-*-padding-end / -start: md 24px; ios 34px.
        var paddingX = ios ? 34f : 24f;
        // $loading-*-font-size: 14px both modes.
        var fontSize = 14f;

        var css = new CssObject
        {
            // Host — a fixed full-screen overlay centering its dialog.
            [$".ion-loading.{mode}"] = new()
            {
                Position = Position.Fixed,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Color = textColor,
                FontSize = Length.Px(fontSize),
                ZIndex = 1000,
            },

            // Closed indicator is fully hidden.
            [$".ion-loading.{mode}.overlay-hidden"] = new()
            {
                Display = Display.None,
            },

            // Backdrop — the dim layer filling the host.
            [$".ion-loading.{mode} .loading-backdrop"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                // --backdrop-opacity: md 0.32, ios 0.3. Reuse the shared action-sheet backdrop color.
                BackgroundColor = t.ActionSheetBackdropColor,
                Opacity = ios ? 0.3f : 0.32f,
                Cursor = Cursor.Pointer,
            },

            // Wrapper — the small centered dialog surface.
            [$".ion-loading.{mode} .loading-wrapper"] = new()
            {
                Position = Position.Relative,
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                MaxWidth = Length.Px(maxWidth),
                PaddingTop = Length.Px(paddingY),
                PaddingBottom = Length.Px(paddingY),
                PaddingLeft = Length.Px(paddingX),
                PaddingRight = Length.Px(paddingX),
                BackgroundColor = background,
                BorderRadius = new BorderRadius(Length.Px(borderRadius)),
                ZIndex = 10,
            },

            // Spinner — tinted per mode (ion-spinner { color: var(--spinner-color) }).
            [$".ion-loading.{mode} .loading-spinner"] = new()
            {
                Color = spinnerColor,
            },
            [$".ion-loading.{mode} .loading-spinner .ion-spinner"] = new()
            {
                Color = spinnerColor,
            },

            // Content — the message text. When it sits beside a spinner it gets a 16px start margin.
            [$".ion-loading.{mode} .loading-content"] = new()
            {
                Color = textColor,
                // ios bolds the loading content ($loading-ios-content-font-weight: bold).
                FontWeight = ios ? FontWeight.Bold : FontWeight.Normal,
            },
            // .loading-spinner + .loading-content { margin-inline-start: 16px } (both modes).
            [$".ion-loading.{mode} .loading-spinner + .loading-content"] = new()
            {
                MarginLeft = Length.Px(16),
            },
        };

        // md gives the wrapper a 3-layer-ish elevation shadow ($loading-md-box-shadow: 0 16px 20px rgba(0,0,0,.4)).
        if (!ios)
        {
            css[$".ion-loading.{mode} .loading-wrapper"]!.BoxShadow = new List<BoxShadow>
            {
                new BoxShadow(0, 16, 20, 0, new Color(0, 0, 0, 102)), // rgba(0,0,0,.4)
            };
        }

        return css;
    }
}
