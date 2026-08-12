using Miko.Animation;
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

        // The resting offset between the toast and its anchor edge. Ionic does NOT express this as a
        // CSS offset — `.toast-top`/`.toast-bottom` really are at 0 — it is where the enter animation
        // settles: translateY(calc(+offset + safe-area-top)) for top, and
        // translateY(calc(-offset - safe-area-bottom)) for bottom (animations/utils.ts
        // getAnimationPosition; md 8px, ios 10px). Modelled here as the animation's end transform,
        // which is why a toast at `bottom: 0` still visibly clears the screen edge.
        var restTop = Length.Px(t.ToastEdgeOffset) + Length.SafeAreaInsetTop;
        var restBottom = -(Length.Px(t.ToastEdgeOffset) + Length.SafeAreaInsetBottom);

        var css = new CssObject
        {
            // Host — a full-screen, pointer-transparent layer above the page. Always mounted (NOT
            // display:none when closed) so the enter/leave animations can run: Miko detects a
            // transition by diffing against the PREVIOUS frame's computed style, and a display:none
            // element has no layout box to diff against — so a toast that only appears on open would
            // jump straight to its end state. Same always-mounted approach as IonActionSheet.
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

            // Closed & settled: transparent to input, so the page below stays interactive. Ionic's
            // :host(.overlay-hidden) is display:none; here the host keeps its box (see above) and the
            // wrapper is instead parked off-screen / at zero opacity by the rules below.
            [$".ion-toast.{mode}.overlay-hidden"] = new()
            {
                PointerEvents = PointerEvents.None,
            },

            // Wrapper — the rounded card. Centered horizontally with margin:auto and side insets.
            // Pointer-events stay off until the host is mounted, so a closed (but still mounted)
            // toast can't swallow taps meant for the page.
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
                PointerEvents = PointerEvents.None,
                ZIndex = 10,
            },

            // Open, or animating out: the card catches taps on its buttons.
            [$".ion-toast.{mode}.toast-mounted .toast-wrapper"] = new()
            {
                PointerEvents = PointerEvents.Auto,
            },

            // Position anchors. Both edges really are at 0 — the visible gap is the resting transform
            // applied by the enter animation below (see restTop/restBottom).
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

            // Button — a border-less action; colored per mode (--button-color).
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

        AddAnimation(css, mode, ios, t, restTop, restBottom);

        // ion-color tinting: the wrapper fills with the named color's base and the text uses its
        // contrast (createColorClasses / :host(.ion-color) .toast-wrapper).
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

    /// <summary>
    /// The enter/leave animations (<c>animations/{md|ios}.{enter|leave}.ts</c>).
    /// <para>
    /// Both modes animate only the wrapper. ios slides it in from off-screen —
    /// <c>translateY(-100%) → translateY(restTop)</c> for <c>top</c>, <c>translateY(100%) →
    /// translateY(restBottom)</c> for <c>bottom</c> — while md parks it at the resting transform and
    /// only cross-fades opacity (0.01 → 1). <c>middle</c> is a pure fade in both modes.
    /// </para>
    /// <para>
    /// NOTE on which duration goes where: Miko picks the transition list from the PREVIOUS frame's
    /// computed style (MikoEngine captures it before re-layout), unlike CSS which reads the
    /// after-change style. So the list on a rule governs the transition OUT of that state: the
    /// closed (base) rule drives the ENTER animation, and the <c>.toast-open</c> rule drives the
    /// LEAVE. Enter is 400ms, leave 300ms, in both modes.
    /// </para>
    /// </summary>
    private static void AddAnimation(CssObject css, string mode, bool ios, IonicTheme t,
        Length restTop, Length restBottom)
    {
        // ios easing differs between enter (a springy overshoot) and leave; md shares one curve.
        static TransitionBuilder Enter(TransitionBuilder b, bool ios, float duration) => ios
            ? b.Duration(duration).CubicBezier(0.155f, 1.105f, 0.295f, 1.12f)  // ios.enter.ts
            : b.Duration(duration).CubicBezier(0.36f, 0.66f, 0.04f, 1f);       // md.enter.ts
        static TransitionBuilder Leave(TransitionBuilder b, float duration) =>
            b.Duration(duration).CubicBezier(0.36f, 0.66f, 0.04f, 1f);         // both leave.ts

        var enterMs = t.ToastEnterDuration;
        var leaveMs = t.ToastLeaveDuration;

        // Closed wrapper: transparent, and (ios) parked off-screen. Its transition list drives ENTER.
        css[$".ion-toast.{mode} .toast-wrapper"]!.Opacity = 0.01f;
        css[$".ion-toast.{mode} .toast-wrapper"]!.Transitions = new List<Transition>
        {
            Enter(Transition.For(x => x.Opacity), ios, enterMs).Build(),
            Enter(Transition.For(x => x.Transform), ios, enterMs).Build(),
        };

        // Open wrapper: fully opaque. Its list drives LEAVE.
        css[$".ion-toast.{mode}.toast-open .toast-wrapper"] = new()
        {
            Opacity = 1f,
            Transitions = new List<Transition>
            {
                Leave(Transition.For(x => x.Opacity), leaveMs).Build(),
                Leave(Transition.For(x => x.Transform), leaveMs).Build(),
            },
        };

        // Per-position parked (closed) transform. md never travels — it sits at its resting offset
        // and only fades — so its closed and open transforms are identical.
        var parkedTop = ios ? new Transform(new TransformFunction.TranslateY(Length.Percent(-100))) : new Transform(new TransformFunction.TranslateY(restTop));
        var parkedBottom = ios ? new Transform(new TransformFunction.TranslateY(Length.Percent(100))) : new Transform(new TransformFunction.TranslateY(restBottom));

        css[$".ion-toast.{mode} .toast-wrapper.toast-top"]!.Transform = parkedTop;
        css[$".ion-toast.{mode} .toast-wrapper.toast-bottom"]!.Transform = parkedBottom;

        // Resting (open) transform — the offset that lifts the toast off the screen edge.
        css[$".ion-toast.{mode}.toast-open .toast-wrapper.toast-top"] = new()
        {
            Transform = new Transform(new TransformFunction.TranslateY(restTop)),
        };
        css[$".ion-toast.{mode}.toast-open .toast-wrapper.toast-bottom"] = new()
        {
            Transform = new Transform(new TransformFunction.TranslateY(restBottom)),
        };

        // middle — a pure fade with no travel in either mode; the wrapper is vertically centered
        // (ios.enter.ts / md.enter.ts compute `top` from the host and wrapper heights).
        css[$".ion-toast.{mode} .toast-wrapper.toast-middle"] = new()
        {
            Top = Length.Percent(50),
            Transform = new Transform(new TransformFunction.TranslateY(Length.Percent(-50))),
        };
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
        // :host(.ion-color) { --button-color: inherit } — a tinted toast drops the default primary
        // button color (which is near-invisible against a dark/primary surface) and reads the host's
        // contrast color instead, so buttons match the message text. Miko has no CSS `inherit`, so
        // the contrast value is mirrored onto the buttons directly.
        css[$".ion-toast.{mode}.ion-color-{name} .toast-button"] = new()
        {
            Color = contrast,
        };
        // :host(.ion-color) .toast-button-cancel { color: inherit } — cancel also follows the host.
        css[$".ion-toast.{mode}.ion-color-{name} .toast-button-cancel"] = new()
        {
            Color = contrast,
        };
    }
}
