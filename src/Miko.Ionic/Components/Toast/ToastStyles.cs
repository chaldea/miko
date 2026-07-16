using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-toast</c>. Ported from the Ionic source: <c>toast.scss</c> / <c>.md.scss</c> /
/// <c>.ios.scss</c> (+ their <c>*.vars.scss</c>).
/// <para>
/// A non-blocking notification: a full-screen, pointer-transparent host holding a wrapper anchored
/// to the top/bottom/middle. The wrapper is a rounded, capped-width card (md a dark #333 surface
/// with light text and an elevation shadow; ios a light #f9f9f9 surface with dark text) whose
/// container flows an optional icon, the header/message content, and start/end button groups.
/// A named palette color tints the wrapper (<c>ion-color-*</c>). Rules are scoped by the active mode
/// class (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// <para>
/// Values that have no dedicated theme token are hardcoded from the Ionic vars files (toast has no
/// tokens on <see cref="IonicTheme"/> yet). Recommended tokens to add are noted inline.
/// </para>
/// </summary>
internal static class ToastStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var ios = mode == "ios";

        // $toast-md-background: $background-color-step-800 (#333333); $toast-ios-background-color: step-50 (#f9f9f9).
        var background = ios ? Color.FromHex("f9f9f9") : Color.FromHex("333333");
        // $toast-md-color: $text-color-step-950 (#f2f2f2 light); $toast-ios-title-color: step-150 (#262626).
        var color = ios ? Color.FromHex("262626") : Color.FromHex("f2f2f2");
        // $toast-*-border-radius: md 4px; ios 14px.
        var borderRadius = ios ? 14f : 4f;
        // --button-color: md ion-color(primary); ios $toast-ios-button-color = ion-color(primary).
        var buttonColor = t.Primary;
        // Cancel button text color: md $text-color-step-900 (near-black on the dark toast → light).
        var cancelButtonColor = ios ? t.Primary : Color.FromHex("1a1a1a");
        // --start / --end horizontal inset: md 8px; ios 10px.
        var edgeInset = ios ? 10f : 8f;
        // $toast-max-width: 700px (both modes).
        var maxWidth = 700f;
        // $toast-*-content-padding: md 14/16; ios 15/15.
        var contentPaddingY = ios ? 15f : 14f;
        var contentPaddingX = ios ? 15f : 16f;
        // $toast-*-header-font-weight: 500 both modes.
        var headerWeight = FontWeight.Medium;
        // $toast-*-header-margin-bottom: 2px both modes.
        var headerMarginBottom = 2f;
        // $toast-*-button padding: md 10/15; ios 10/15.
        var buttonPaddingY = 10f;
        var buttonPaddingX = 15f;
        // $toast-*-button-font-size: md 14px; ios 17px.
        var buttonFontSize = ios ? 17f : 14f;
        // $toast-*-button-font-weight: 500 both modes.
        var buttonWeight = FontWeight.Medium;
        // $toast-*-font-size: md 14px; ios 14px.
        var fontSize = 14f;

        var css = new CssObject
        {
            // Host — a full-screen, pointer-transparent layer above the page.
            [$".ion-toast.{mode}"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                Color = color,
                FontSize = Length.Px(fontSize),
                PointerEvents = PointerEvents.None,
                ZIndex = 1000,
            },

            // Closed toast is fully hidden.
            [$".ion-toast.{mode}.overlay-hidden"] = new()
            {
                Display = Display.None,
            },

            // Wrapper — the rounded card. Centered horizontally with margin:auto and side insets;
            // pointer-events re-enabled so its buttons stay tappable on the transparent host.
            [$".ion-toast.{mode} .toast-wrapper"] = new()
            {
                Position = Position.Absolute,
                Display = Display.Block,
                MarginTop = Length.Auto,
                MarginBottom = Length.Auto,
                MarginLeft = Length.Auto,
                MarginRight = Length.Auto,
                Left = Length.Px(edgeInset),
                Right = Length.Px(edgeInset),
                MaxWidth = Length.Px(maxWidth),
                BackgroundColor = background,
                BorderRadius = new BorderRadius(Length.Px(borderRadius)),
                PointerEvents = PointerEvents.Auto,
                ZIndex = 10,
            },

            // Position anchors.
            [$".ion-toast.{mode} .toast-wrapper.toast-top"] = new()
            {
                Top = Length.Px(0),
            },
            [$".ion-toast.{mode} .toast-wrapper.toast-bottom"] = new()
            {
                Bottom = Length.Px(0),
            },

            // Container — a centered flex row of icon + content + button groups.
            [$".ion-toast.{mode} .toast-container"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
            },

            // Leading icon.
            [$".ion-toast.{mode} .toast-icon"] = new()
            {
                MarginLeft = Length.Px(16),
                FontSize = Length.Em(1.4f),
            },

            // Content — the header/message column; takes the remaining width.
            [$".ion-toast.{mode} .toast-content"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center,
                FlexGrow = 1,
                MinWidth = Length.Px(0),
                PaddingTop = Length.Px(contentPaddingY),
                PaddingBottom = Length.Px(contentPaddingY),
                PaddingLeft = Length.Px(contentPaddingX),
                PaddingRight = Length.Px(contentPaddingX),
            },

            // Header — bold line above the message.
            [$".ion-toast.{mode} .toast-header"] = new()
            {
                MarginBottom = Length.Px(headerMarginBottom),
                FontWeight = headerWeight,
            },

            // Message — the body text.
            [$".ion-toast.{mode} .toast-message"] = new()
            {
                FlexGrow = 1,
                WhiteSpace = WhiteSpace.Normal,
            },

            // Button group — a flex row of buttons.
            [$".ion-toast.{mode} .toast-button-group"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
            },

            // Button — a bordered-less action; colored per mode.
            [$".ion-toast.{mode} .toast-button"] = new()
            {
                PaddingTop = Length.Px(buttonPaddingY),
                PaddingBottom = Length.Px(buttonPaddingY),
                PaddingLeft = Length.Px(buttonPaddingX),
                PaddingRight = Length.Px(buttonPaddingX),
                BorderWidth = Length.Px(0),
                BackgroundColor = Color.Transparent,
                Color = buttonColor,
                FontSize = Length.Px(buttonFontSize),
                FontWeight = buttonWeight,
                Cursor = Cursor.Pointer,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            // Cancel button — its own text color.
            [$".ion-toast.{mode} .toast-button-cancel"] = new()
            {
                Color = cancelButtonColor,
            },

            // Button inner — centers icon + label.
            [$".ion-toast.{mode} .toast-button-inner"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
            },

            // Button icon.
            [$".ion-toast.{mode} .toast-button-icon"] = new()
            {
                FontSize = Length.Em(1.4f),
            },
        };

        // md gives the wrapper a Material elevation shadow ($toast-md-box-shadow, a 3-layer stack).
        if (!ios)
        {
            css[$".ion-toast.{mode} .toast-wrapper"]!.BoxShadow = new List<BoxShadow>
            {
                new BoxShadow(0, 3, 5, -1, new Color(0, 0, 0, 51)),  // rgba(0,0,0,.2)
                new BoxShadow(0, 6, 10, 0, new Color(0, 0, 0, 36)),  // rgba(0,0,0,.14)
                new BoxShadow(0, 1, 18, 0, new Color(0, 0, 0, 31)),  // rgba(0,0,0,.12)
            };
            // md uppercases button labels ($toast-md-button-text-transform: uppercase).
            css[$".ion-toast.{mode} .toast-button"]!.TextTransform = TextTransform.Uppercase;
            // md cancel button text is the near-black step-900 (on the dark surface it reads light).
            css[$".ion-toast.{mode} .toast-button-cancel"]!.Color = Color.FromHex("1a1a1a");
        }

        // ion-color tinting: the wrapper fills with the named color's base and the text uses its
        // contrast (createColorClasses / :host(.ion-color) .toast-wrapper). Cancel keeps inherited.
        AddColor(css, mode, "primary", t.Primary, Color.White);
        AddColor(css, mode, "secondary", t.Secondary, Color.White);
        AddColor(css, mode, "tertiary", t.Tertiary, Color.White);
        AddColor(css, mode, "success", t.Success, Color.Black);
        AddColor(css, mode, "warning", t.Warning, Color.Black);
        AddColor(css, mode, "danger", t.Danger, Color.White);
        AddColor(css, mode, "light", t.Light, Color.Black);
        AddColor(css, mode, "medium", t.Medium, Color.White);
        AddColor(css, mode, "dark", t.Dark, Color.White);

        return css;
    }

    // :host(.ion-color) { color: contrast } and .toast-wrapper { background: base }.
    private static void AddColor(CssObject css, string mode, string name, Color background, Color contrast)
    {
        css[$".ion-toast.{mode}.ion-color-{name}"] = new()
        {
            Color = contrast,
        };
        css[$".ion-toast.{mode}.ion-color-{name} .toast-wrapper"] = new()
        {
            BackgroundColor = background,
        };
        css[$".ion-toast.{mode}.ion-color-{name} .toast-content"] = new()
        {
            Color = contrast,
        };
    }
}
