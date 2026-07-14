using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-searchbar</c>. Ported from the Ionic source: <c>searchbar.scss</c> (shared base)
/// plus <c>searchbar.md.scss</c> / <c>searchbar.ios.scss</c> (per-mode overrides) and their
/// <c>*.vars.scss</c>.
/// <para>
/// DOM mirrors Ionic's host structure:
/// <code>
/// &lt;div class="ion-searchbar ..."&gt;                  &lt;!-- host: flex row, the state classes --&gt;
///   &lt;div class="searchbar-input-container"&gt;          &lt;!-- relative block wrapping the input --&gt;
///     &lt;input class="searchbar-input" /&gt;               &lt;!-- the rounded text field --&gt;
///     (md) &lt;button class="searchbar-cancel-button"&gt;..&lt;/button&gt;
///     &lt;ion-icon class="searchbar-search-icon" /&gt;
///     &lt;button class="searchbar-clear-button"&gt;
///       &lt;ion-icon class="searchbar-clear-icon" /&gt;
///     &lt;/button&gt;
///   &lt;/div&gt;
///   (ios) &lt;button class="searchbar-cancel-button"&gt;..&lt;/button&gt;
/// &lt;/div&gt;
/// </code>
/// The cancel button is a child of the input-container in md (an icon) and a sibling of it in ios
/// (text), matching Ionic. The clear button stays hidden until the host carries both
/// <c>searchbar-has-value</c> and <c>searchbar-should-show-clear</c>; the cancel button shows per
/// <c>searchbar-should-show-cancel</c> / focus (md) or animated (ios).
/// </para>
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/>
/// for the mode-scoping rationale. Ionic's JS-driven placeholder centering and cancel-slide
/// animation (<c>positionElements</c>) are visual-only and omitted — the port keeps the static,
/// left-aligned layout, like the other ported components drop JS-only animation.
/// </para>
/// </summary>
internal static class SearchbarStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // ion-searchbar — the host. A full-width flex row, centered, that carries the text
            // color/font for the field and icons to inherit (searchbar.scss :host).
            [$".ion-searchbar.{mode}"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                AlignItems = AlignItems.Center,
                Width = Length.Percent(100),
                BoxSizing = BoxSizing.BorderBox,
                Color = t.SearchbarInputTextColor,
                PaddingTop = Length.Px(t.SearchbarPaddingTop),
                PaddingRight = Length.Px(t.SearchbarPaddingEnd),
                PaddingBottom = Length.Px(t.SearchbarPaddingBottom),
                PaddingLeft = Length.Px(t.SearchbarPaddingStart),
            },

            // Disabled host — dimmed and non-interactive (searchbar.scss :host(.searchbar-disabled)).
            [$".ion-searchbar.{mode}.searchbar-disabled"] = new()
            {
                Opacity = 0.4f,
                Cursor = Cursor.Default,
                PointerEvents = PointerEvents.None,
            },

            // .searchbar-input-container — relative block wrapping the input so the absolutely
            // positioned icon / clear / cancel (md) buttons can overlay it (searchbar.scss).
            [$".ion-searchbar.{mode} .searchbar-input-container"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                FlexShrink = 1,
                Width = Length.Percent(100),
            },

            // .searchbar-input — the rounded text field. Block, full-width, no border, inherits
            // font/color from the host; the per-mode rules below add radius / background / size.
            [$".ion-searchbar.{mode} .searchbar-input"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                BorderWidth = Length.Px(0),
                BackgroundColor = t.SearchbarInputBackground,
                FontSize = Length.Px(t.SearchbarInputFontSize),
                FontWeight = FontWeight.Normal,
                BoxSizing = BoxSizing.BorderBox,
            },

            // .searchbar-search-icon — the leading magnifier. Absolute, non-interactive, colored
            // via --icon-color (searchbar.scss + per-mode positioning).
            [$".ion-searchbar.{mode} .searchbar-search-icon"] = new()
            {
                Position = Position.Absolute,
                Color = t.SearchbarSearchIconColor,
                PointerEvents = PointerEvents.None,
            },

            // .searchbar-clear-button — the trailing reset. Hidden by default; revealed by the
            // has-value + should-show-clear rule below (searchbar.scss).
            [$".ion-searchbar.{mode} .searchbar-clear-button"] = new()
            {
                Display = Display.None,
                MarginTop = Length.Px(0),
                MarginRight = Length.Px(0),
                MarginBottom = Length.Px(0),
                MarginLeft = Length.Px(0),
                PaddingTop = Length.Px(0),
                PaddingRight = Length.Px(0),
                PaddingBottom = Length.Px(0),
                PaddingLeft = Length.Px(0),
                MinHeight = Length.Px(0),
                BorderWidth = Length.Px(0),
                BackgroundColor = Color.Transparent,
                Color = t.SearchbarClearIconColor,
            },

            // .searchbar-cancel-button — hidden by default; per-mode rules below reveal it.
            [$".ion-searchbar.{mode} .searchbar-cancel-button"] = new()
            {
                Display = Display.None,
                Height = Length.Percent(100),
                BorderWidth = Length.Px(0),
                Color = t.SearchbarCancelButtonColor,
                BackgroundColor = t.SearchbarCancelButtonBackground,
                Cursor = Cursor.Pointer,
            },

            // .searchbar-cancel-button > div — centers the icon/text inside the cancel button.
            [$".ion-searchbar.{mode} .searchbar-cancel-button > div"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            },

            // Clear button revealed only when there is a value AND clear is allowed
            // (searchbar.scss :host(.searchbar-has-value.searchbar-should-show-clear)).
            [$".ion-searchbar.{mode}.searchbar-has-value.searchbar-should-show-clear .searchbar-clear-button"] = new()
            {
                Display = Display.Block,
            },
        };

        // --- Per-mode input + icon geometry ----------------------------------------------
        if (mode == "md")
        {
            // md input: auto height, 30px line-height, 6px/55px padding (room for the left icon and
            // the trailing clear button), 2px radius, and the elevation shadow.
            css[$".ion-searchbar.{mode} .searchbar-input"] = Merge(css[$".ion-searchbar.{mode} .searchbar-input"], new()
            {
                Height = t.SearchbarInputHeight,
                LineHeight = t.SearchbarInputLineHeight,
                PaddingTop = Length.Px(6),
                PaddingBottom = Length.Px(6),
                PaddingLeft = Length.Px(55),
                PaddingRight = Length.Px(55),
                BorderRadius = Radius(t.SearchbarInputBorderRadius),
                BoxShadow = t.SearchbarInputBoxShadow.Count > 0 ? (StyleProperty<List<BoxShadow>>?)t.SearchbarInputBoxShadow : null,
            });

            // md search icon: 21px, pinned top:11px / left:16px.
            css[$".ion-searchbar.{mode} .searchbar-search-icon"] = Merge(css[$".ion-searchbar.{mode} .searchbar-search-icon"], new()
            {
                Top = Length.Px(11),
                Left = Length.Px(16),
                Width = Length.Px(t.SearchbarSearchIconSize),
                Height = Length.Px(t.SearchbarSearchIconSize),
            });

            // md cancel button: absolute, pinned left:9px; font-size drives the icon.
            css[$".ion-searchbar.{mode} .searchbar-cancel-button"] = Merge(css[$".ion-searchbar.{mode} .searchbar-cancel-button"], new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(9),
                FontSize = Length.Px(t.SearchbarCancelButtonFontSize),
            });

            // md clear button: absolute, pinned right:13px, full height; icon fills it.
            css[$".ion-searchbar.{mode} .searchbar-clear-button"] = Merge(css[$".ion-searchbar.{mode} .searchbar-clear-button"], new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(13),
                Height = Length.Percent(100),
            });
            css[$".ion-searchbar.{mode} .searchbar-clear-icon"] = new()
            {
                Width = Length.Px(t.SearchbarClearIconSize),
                Height = Length.Percent(100),
            };

            // md: the cancel button shows on focus or when should-show-cancel
            // (searchbar.md.scss :host(.searchbar-has-focus/.searchbar-should-show-cancel)).
            css[$".ion-searchbar.{mode}.searchbar-has-focus .searchbar-cancel-button"] = new() { Display = Display.Block };
            css[$".ion-searchbar.{mode}.searchbar-should-show-cancel .searchbar-cancel-button"] = new() { Display = Display.Block };
        }
        else
        {
            // ios: input container gets a 36px min-height; input fills it (100%), 10px radius,
            // 6px/0 padding, translucent fill, 17px text. With a value + clear shown, add right
            // padding so text clears the clear button.
            css[$".ion-searchbar.{mode} .searchbar-input-container"] = Merge(css[$".ion-searchbar.{mode} .searchbar-input-container"], new()
            {
                MinHeight = Length.Px(t.SearchbarInputMinHeight),
            });
            css[$".ion-searchbar.{mode} .searchbar-input"] = Merge(css[$".ion-searchbar.{mode} .searchbar-input"], new()
            {
                Height = Length.Percent(100),
                MinHeight = Length.Px(t.SearchbarInputMinHeight),
                PaddingTop = Length.Px(6),
                PaddingBottom = Length.Px(6),
                PaddingLeft = Length.Px(0),
                PaddingRight = Length.Px(0),
                BorderRadius = Radius(t.SearchbarInputBorderRadius),
            });
            css[$".ion-searchbar.{mode}.searchbar-has-value.searchbar-should-show-clear .searchbar-input"] = new()
            {
                PaddingRight = Length.Px(28),
            };

            // ios search icon: full-height, pinned left:5px (left-aligned by default).
            css[$".ion-searchbar.{mode} .searchbar-search-icon"] = Merge(css[$".ion-searchbar.{mode} .searchbar-search-icon"], new()
            {
                Top = Length.Px(0),
                Left = Length.Px(5),
                Width = Length.Px(t.SearchbarSearchIconSize),
                Height = Length.Percent(100),
            });

            // ios cancel button: a flex-shrink:0 sibling with 12px left padding; font-size 17px.
            css[$".ion-searchbar.{mode} .searchbar-cancel-button"] = Merge(css[$".ion-searchbar.{mode} .searchbar-cancel-button"], new()
            {
                FlexShrink = 0,
                PaddingLeft = Length.Px(12),
                FontSize = Length.Px(t.SearchbarCancelButtonFontSize),
            });

            // ios clear button: absolute, pinned right:0, 30px wide, full height; icon fills it.
            css[$".ion-searchbar.{mode} .searchbar-clear-button"] = Merge(css[$".ion-searchbar.{mode} .searchbar-clear-button"], new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Width = Length.Px(30),
                Height = Length.Percent(100),
            });
            css[$".ion-searchbar.{mode} .searchbar-clear-icon"] = new()
            {
                Width = Length.Px(t.SearchbarClearIconSize),
                Height = Length.Percent(100),
            };

            // ios: the cancel button shows on focus, should-show-cancel, or when animated
            // (searchbar.ios.scss). The port always reveals it under those host classes.
            css[$".ion-searchbar.{mode}.searchbar-has-focus .searchbar-cancel-button"] = new() { Display = Display.Block };
            css[$".ion-searchbar.{mode}.searchbar-should-show-cancel .searchbar-cancel-button"] = new() { Display = Display.Block };
            css[$".ion-searchbar.{mode}.searchbar-animated .searchbar-cancel-button"] = new() { Display = Display.Block };
        }

        // --- Named color fills (Ionic --ion-color-* palette) ---------------------------------
        // A searchbar with a color fills its input with that base; icons inherit the contrast color.
        AddColor(css, mode, "primary", t.Primary);
        AddColor(css, mode, "secondary", t.Secondary);
        AddColor(css, mode, "tertiary", t.Tertiary);
        AddColor(css, mode, "success", t.Success);
        AddColor(css, mode, "warning", t.Warning);
        AddColor(css, mode, "danger", t.Danger);
        AddColor(css, mode, "light", t.Light);
        AddColor(css, mode, "medium", t.Medium);
        AddColor(css, mode, "dark", t.Dark);

        return css;
    }

    private static BorderRadius Radius(float px) => new BorderRadius(Length.Px(px));

    // Merge a base CssObject's style with an overlay (overlay fills only the base's null props).
    // Used to layer per-mode geometry on top of the shared base rule for the same selector.
    private static CssObject Merge(CssObject baseStyle, Style overlay)
    {
        baseStyle.Merge(overlay);
        return baseStyle;
    }

    // Color: input background = base; icons/cancel/clear inherit the host color
    // (searchbar.scss :host(.ion-color) rules).
    private static void AddColor(CssObject css, string mode, string name, Color baseColor)
    {
        css[$".ion-searchbar.{mode}.ion-color-{name} .searchbar-input"] = new()
        {
            BackgroundColor = baseColor,
        };
    }
}
