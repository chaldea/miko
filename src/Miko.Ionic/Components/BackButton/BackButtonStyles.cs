using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-back-button</c>. Ported from the Ionic source: <c>back-button.scss</c> (shared
/// base) plus <c>back-button.md.scss</c> / <c>back-button.ios.scss</c> and their <c>*.vars.scss</c>.
/// <para>
/// DOM mirrors Ionic's host structure:
/// <code>
/// &lt;div class="ion-back-button ..."&gt;   &lt;!-- host: display:none unless show-back-button --&gt;
///   &lt;button class="button-native"&gt;      &lt;!-- clickable surface with flex layout --&gt;
///     &lt;span class="button-inner"&gt;        &lt;!-- row, centers the icon + text --&gt;
///       &lt;ion-icon&gt;&lt;/ion-icon&gt;            &lt;!-- the back arrow icon --&gt;
///       &lt;span class="button-text"&gt;&lt;/span&gt; &lt;!-- the "Back" text (iOS only by default) --&gt;
///     &lt;/span&gt;
///   &lt;/button&gt;
/// &lt;/div&gt;
/// </code>
/// The host is hidden by default (<c>display: none</c>) and shown when <c>show-back-button</c> is
/// present. The icon defaults to chevron-back (iOS) or arrow-back-sharp (MD). The text defaults
/// to "Back" on iOS, null on MD.
/// </para>
/// </summary>
internal static class BackButtonStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject();

        if (mode == "ios")
        {
            AddIosStyles(css, t);
        }
        else
        {
            AddMdStyles(css, t);
        }

        return css;
    }

    private static void AddIosStyles(CssObject css, IonicTheme t)
    {
        // ion-back-button — the host (back-button.ios.scss :host).
        css[".ion-back-button.ios"] = new()
        {
            Display = Display.None,
            MinHeight = Length.Px(32),
            FontSize = Length.Px(17),
            Color = t.Primary,
            TextAlign = TextAlign.Center,
            TextDecoration = TextDecoration.None,
        };

        // show-back-button — the host is shown (back-button.scss :host(.show-back-button)).
        css[".ion-back-button.ios.show-back-button"] = new()
        {
            Display = Display.Block,
        };

        // disabled — dimmed and non-interactive (back-button.scss :host(.back-button-disabled)).
        css[".ion-back-button.ios.back-button-disabled"] = new()
        {
            Cursor = Cursor.Default,
            Opacity = 0.5f,
            PointerEvents = PointerEvents.None,
        };

        // .button-native — the clickable surface (back-button.scss .button-native).
        css[".ion-back-button.ios .button-native"] = new()
        {
            Display = Display.Block,
            Position = Position.Relative,
            Width = Length.Percent(100),
            Height = Length.Percent(100),
            MinHeight = Length.Px(32),
            BorderRadius = new BorderRadius(Length.Px(4)),
            BorderWidth = Length.Px(0),
            BackgroundColor = Color.Transparent,
            LineHeight = Length.Number(1),
            Cursor = Cursor.Pointer,
            Overflow = Overflow.Visible,
            ZIndex = 99,  // $z-index-toolbar-buttons in Ionic
        };

        // .button-inner — centers the icon + text row (back-button.scss .button-inner).
        css[".ion-back-button.ios .button-native .button-inner"] = new()
        {
            Display = Display.Flex,
            Position = Position.Relative,
            FlexDirection = FlexDirection.Row,
            FlexShrink = 0,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.Center,
            Width = Length.Percent(100),
            Height = Length.Percent(100),
            ZIndex = 1,
        };

        // icon — the back arrow (back-button.scss ion-icon, back-button.ios.scss :host).
        css[".ion-back-button.ios .back-button-icon"] = new()
        {
            FontSize = Length.Em(1.6f),  // relative to the host font size (back-button.ios.scss --icon-font-size: 1.6em)
            MarginLeft = Length.Px(-4),
            MarginRight = Length.Px(1),
            PointerEvents = PointerEvents.None,
        };

        // text — the "Back" label (back-button.md.scss .button-text, though iOS doesn't have extra padding).
        css[".ion-back-button.ios .button-text"] = new()
        {
        };
    }

    private static void AddMdStyles(CssObject css, IonicTheme t)
    {
        // ion-back-button — the host (back-button.md.scss :host).
        css[".ion-back-button.md"] = new()
        {
            Display = Display.None,
            MinHeight = Length.Px(32),
            MinWidth = Length.Px(44),
            FontSize = Length.Px(14),
            FontWeight = FontWeight.Medium,
            Color = t.TextColor,
            TextAlign = TextAlign.Center,
            TextDecoration = TextDecoration.None,
            TextTransform = TextTransform.Uppercase,
        };

        // show-back-button — the host is shown (back-button.scss :host(.show-back-button)).
        css[".ion-back-button.md.show-back-button"] = new()
        {
            Display = Display.Block,
        };

        // disabled — dimmed and non-interactive (back-button.scss :host(.back-button-disabled)).
        css[".ion-back-button.md.back-button-disabled"] = new()
        {
            Cursor = Cursor.Default,
            Opacity = 0.5f,
            PointerEvents = PointerEvents.None,
        };

        // .button-native — the clickable surface (back-button.scss .button-native).
        css[".ion-back-button.md .button-native"] = new()
        {
            Display = Display.Block,
            Position = Position.Relative,
            Width = Length.Percent(100),
            Height = Length.Percent(100),
            MinHeight = Length.Px(32),
            PaddingLeft = Length.Px(12),
            PaddingRight = Length.Px(12),
            BorderRadius = new BorderRadius(Length.Px(4)),
            BorderWidth = Length.Px(0),
            BackgroundColor = Color.Transparent,
            LineHeight = Length.Number(1),
            Cursor = Cursor.Pointer,
            Overflow = Overflow.Visible,
            ZIndex = 0,
        };

        // .button-inner — centers the icon + text row (back-button.scss .button-inner).
        css[".ion-back-button.md .button-native .button-inner"] = new()
        {
            Display = Display.Flex,
            Position = Position.Relative,
            FlexDirection = FlexDirection.Row,
            FlexShrink = 0,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.Center,
            Width = Length.Percent(100),
            Height = Length.Percent(100),
            ZIndex = 1,
        };

        // icon — the back arrow (back-button.md.scss ion-icon).
        css[".ion-back-button.md .back-button-icon"] = new()
        {
            FontSize = Length.Px(24),
            TextAlign = TextAlign.Left,
            PointerEvents = PointerEvents.None,
        };

        // text — the button label (back-button.md.scss .button-text).
        css[".ion-back-button.md .button-text"] = new()
        {
            PaddingLeft = Length.Px(4),
            PaddingRight = Length.Px(4),
        };

        // has-icon-only — circular ripple effect (back-button.md.scss :host(.back-button-has-icon-only)).
        css[".ion-back-button.md.back-button-has-icon-only"] = new()
        {
            MinWidth = Length.Px(48),
            MinHeight = Length.Px(48),
            BorderRadius = new BorderRadius(Length.Percent(50)),
        };
    }
}
