using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-button</c>. Ported from the Ionic source: <c>button.scss</c> (shared base) plus
/// <c>button.md.scss</c> / <c>button.ios.scss</c> (per-mode overrides) and their <c>*.vars.scss</c>.
/// <para>
/// DOM mirrors Ionic's host structure:
/// <code>
/// &lt;div class="ion-button ..."&gt;        &lt;!-- host: inline-block, the fill/size/shape/expand classes --&gt;
///   &lt;button class="button-native"&gt;      &lt;!-- the filled clickable surface (radius/border/bg/padding) --&gt;
///     &lt;span class="button-inner"&gt;        &lt;!-- row, centers the slotted content --&gt;
///       (content)
///     &lt;/span&gt;
///   &lt;/button&gt;
/// &lt;/div&gt;
/// </code>
/// The host carries <c>--background</c> / <c>--color</c> semantics through the fill classes
/// (<c>button-solid</c> / <c>button-outline</c> / <c>button-clear</c>); the <c>.button-native</c>
/// paints the actual surface. Size (<c>button-small</c> / <c>-large</c>), shape (<c>button-round</c>),
/// and expand (<c>button-block</c> / <c>-full</c>) tune padding / radius / width.
/// </para>
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/>
/// for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class ButtonStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // ion-button — the host. inline-block so it sizes to its content and flows inline with
            // sibling buttons (Ionic's :host { display: inline-block; width: auto; }). Carries the
            // label color, font, transform, and tracking; the native button inherits them.
            [$".ion-button.{mode}"] = new()
            {
                Display = Display.InlineBlock,
                Color = t.ButtonTextColor,
                FontSize = Length.Px(t.ButtonFontSize),
                FontWeight = t.ButtonFontWeight,
                TextTransform = t.ButtonTextTransform,
                LetterSpacing = t.ButtonLetterSpacing,
                TextAlign = TextAlign.Center,
                TextDecoration = TextDecoration.None,
                MinHeight = t.ButtonMinHeight,
                MarginTop = Length.Px(4),
                MarginBottom = Length.Px(4),
                MarginLeft = Length.Px(2),
                MarginRight = Length.Px(2),
                VerticalAlign = VerticalAlign.Top,
            },

            // disabled host — dimmed and non-interactive (button.scss :host(.button-disabled)).
            [$".ion-button.{mode}.button-disabled"] = new()
            {
                Opacity = 0.5f,
                Cursor = Cursor.Default,
                PointerEvents = PointerEvents.None,
            },

            // .button-native — the painted surface. Flex row filling the host, centered, with the
            // host padding (Ionic's --padding-*), the resolved border radius, and pointer cursor.
            // Background/border/color come from the fill rules below; default here is the solid fill.
            [$".ion-button.{mode} .button-native"] = new()
            {
                Position = Position.Relative,
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                MinHeight = t.ButtonMinHeight,
                PaddingTop = t.ButtonPaddingTop,
                PaddingBottom = t.ButtonPaddingBottom,
                PaddingLeft = t.ButtonPaddingStart,
                PaddingRight = t.ButtonPaddingEnd,
                BorderRadius = Radius(t.ButtonBorderRadius),
                BorderWidth = Length.Px(0),
                LineHeight = Length.Number(1),
                Cursor = Cursor.Pointer,
                BoxSizing = BoxSizing.BorderBox,
                ZIndex = 0,
            },

            // .button-inner — centers the label/icon row (button.scss .button-inner).
            [$".ion-button.{mode} .button-native .button-inner"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                FlexShrink = 0,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                ZIndex = 1,
            },
        };

        // --- Fill variants -------------------------------------------------------------------
        // Solid: primary fill, contrast (white) label, mode elevation shadow.
        css[$".ion-button.{mode}.button-solid .button-native"] = new()
        {
            BackgroundColor = t.ButtonSolidBackground,
            Color = t.ButtonSolidColor,
            BoxShadow = t.ButtonSolidBoxShadow.Count > 0 ? t.ButtonSolidBoxShadow : null,
        };

        // Outline: transparent fill, primary border + label.
        css[$".ion-button.{mode}.button-outline .button-native"] = new()
        {
            BackgroundColor = Color.Transparent,
            Color = t.ButtonTextColor,
            BorderWidth = t.ButtonOutlineBorderWidth,
            BorderStyle = BorderStyle.Solid,
            BorderColor = t.ButtonTextColor,
        };

        // Clear: transparent fill, no border, primary label.
        css[$".ion-button.{mode}.button-clear .button-native"] = new()
        {
            BackgroundColor = Color.Transparent,
            Color = t.ButtonTextColor,
            BorderWidth = Length.Px(0),
        };

        // --- Expand variants -----------------------------------------------------------------
        // block: full-width host, no horizontal margin on the native button.
        css[$".ion-button.{mode}.button-block"] = new() { Display = Display.Block };
        css[$".ion-button.{mode}.button-block .button-native"] = new()
        {
            MarginLeft = Length.Px(0),
            MarginRight = Length.Px(0),
            Width = Length.Percent(100),
        };

        // full: full-width host, square corners, no left/right border (button.scss).
        css[$".ion-button.{mode}.button-full"] = new() { Display = Display.Block };
        css[$".ion-button.{mode}.button-full .button-native"] = new()
        {
            MarginLeft = Length.Px(0),
            MarginRight = Length.Px(0),
            Width = Length.Percent(100),
        };
        css[$".ion-button.{mode}.button-full .button-native"].BorderRadius =
            new BorderRadius(Length.Px(0));

        // --- Size variants -------------------------------------------------------------------
        css[$".ion-button.{mode}.button-small"] = new()
        {
            MinHeight = t.ButtonSmallMinHeight,
            FontSize = Length.Px(t.ButtonSmallFontSize),
        };
        css[$".ion-button.{mode}.button-small .button-native"] = new()
        {
            PaddingTop = t.ButtonSmallPaddingTop,
            PaddingBottom = t.ButtonSmallPaddingBottom,
            PaddingLeft = t.ButtonSmallPaddingX,
            PaddingRight = t.ButtonSmallPaddingX,
            BorderRadius = Radius(t.ButtonSmallBorderRadius),
        };

        css[$".ion-button.{mode}.button-large"] = new()
        {
            MinHeight = t.ButtonLargeMinHeight,
            FontSize = Length.Px(t.ButtonLargeFontSize),
        };
        css[$".ion-button.{mode}.button-large .button-native"] = new()
        {
            PaddingTop = t.ButtonLargePaddingTop,
            PaddingBottom = t.ButtonLargePaddingBottom,
            PaddingLeft = t.ButtonLargePaddingX,
            PaddingRight = t.ButtonLargePaddingX,
            BorderRadius = Radius(t.ButtonLargeBorderRadius),
        };

        // --- Shape: round --------------------------------------------------------------------
        css[$".ion-button.{mode}.button-round .button-native"] = new()
        {
            PaddingTop = Length.Px(0),
            PaddingBottom = Length.Px(0),
            PaddingLeft = Length.Px(26),
            PaddingRight = Length.Px(26),
            BorderRadius = Radius(t.ButtonRoundBorderRadius),
        };

        // --- Icon-only -----------------------------------------------------------------------
        // Square button, symmetric zero padding (button.*.scss :host(.button-has-icon-only)).
        css[$".ion-button.{mode}.button-has-icon-only"] = new()
        {
            MinWidth = Length.Px(t.ButtonIconOnlyMinSize),
            MinHeight = Length.Px(t.ButtonIconOnlyMinSize),
        };
        css[$".ion-button.{mode}.button-has-icon-only .button-native"] = new()
        {
            PaddingTop = Length.Px(0),
            PaddingBottom = Length.Px(0),
            PaddingLeft = Length.Px(0),
            PaddingRight = Length.Px(0),
        };

        // --- Strong --------------------------------------------------------------------------
        css[$".ion-button.{mode}.button-strong"] = new()
        {
            FontWeight = t.ButtonStrongFontWeight,
        };

        // --- Named color fills (Ionic --ion-color-* palette) ---------------------------------
        // A solid button with a color fills with that base and uses its contrast label.
        AddSolidColor(css, mode, "primary", t.Primary, Color.FromHex("ffffff"));
        AddSolidColor(css, mode, "secondary", t.Secondary, Color.FromHex("ffffff"));
        AddSolidColor(css, mode, "tertiary", t.Tertiary, Color.FromHex("ffffff"));
        AddSolidColor(css, mode, "success", t.Success, Color.FromHex("000000"));
        AddSolidColor(css, mode, "warning", t.Warning, Color.FromHex("000000"));
        AddSolidColor(css, mode, "danger", t.Danger, Color.FromHex("ffffff"));
        AddSolidColor(css, mode, "light", t.Light, Color.FromHex("000000"));
        AddSolidColor(css, mode, "medium", t.Medium, Color.FromHex("ffffff"));
        AddSolidColor(css, mode, "dark", t.Dark, Color.FromHex("ffffff"));

        // Outline/clear with a color use that base for the border + label, transparent fill.
        AddTextColor(css, mode, "primary", t.Primary);
        AddTextColor(css, mode, "secondary", t.Secondary);
        AddTextColor(css, mode, "tertiary", t.Tertiary);
        AddTextColor(css, mode, "success", t.Success);
        AddTextColor(css, mode, "warning", t.Warning);
        AddTextColor(css, mode, "danger", t.Danger);
        AddTextColor(css, mode, "light", t.Light);
        AddTextColor(css, mode, "medium", t.Medium);
        AddTextColor(css, mode, "dark", t.Dark);

        return css;
    }

    private static BorderRadius Radius(float px) => new BorderRadius(Length.Px(px));

    // Solid color: base fill + contrast label on the native surface.
    private static void AddSolidColor(CssObject css, string mode, string name, Color baseColor, Color contrast)
    {
        css[$".ion-button.{mode}.button-solid.ion-color-{name} .button-native"] = new()
        {
            BackgroundColor = baseColor,
            Color = contrast,
        };
    }

    // Outline + clear color: base for border/label, transparent fill.
    private static void AddTextColor(CssObject css, string mode, string name, Color baseColor)
    {
        css[$".ion-button.{mode}.button-outline.ion-color-{name} .button-native"] = new()
        {
            BackgroundColor = Color.Transparent,
            Color = baseColor,
            BorderColor = baseColor,
        };
        css[$".ion-button.{mode}.button-clear.ion-color-{name} .button-native"] = new()
        {
            BackgroundColor = Color.Transparent,
            Color = baseColor,
        };
    }
}
