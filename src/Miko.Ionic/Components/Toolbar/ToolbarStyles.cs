using Miko.Common;
using Miko.Styling;
using Miko.Ionic.Styles;

namespace Miko.Ionic.Components;

/// <summary>
/// Stylesheet for <see cref="IonToolbar"/>. Ported from <c>toolbar.scss</c>,
/// <c>toolbar.md.scss</c>, and <c>toolbar.ios.scss</c>.
/// </summary>
public static class ToolbarStyles
{
    public static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // :host — the toolbar container with color/background/border/padding CSS variables
            [$".ion-toolbar.{mode}"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                BackgroundColor = t.ToolbarBackground,
                Color = t.ToolbarColor,
                TextAlign = TextAlign.Left,
                Position = Position.Relative,
                ZIndex = 10,
                BoxSizing = BoxSizing.BorderBox,
                MinHeight = Length.Px(t.ToolbarMinHeight),
                // Every toolbar clears a side notch. HeaderStyles adds the top inset only to the
                // first toolbar in a header.
                PaddingLeft = Length.SafeAreaInsetLeft,
                PaddingRight = Length.SafeAreaInsetRight,
            },

            // .toolbar-background — the solid backdrop
            [$".ion-toolbar.{mode} .toolbar-background"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                BackgroundColor = t.ToolbarBackground,
                BorderColor = t.ToolbarBorderColor,
                BorderStyle = BorderStyle.Solid,
                ZIndex = -1,
                PointerEvents = PointerEvents.None,
            },

            // .toolbar-container — flexbox that arranges the slots + content horizontally
            [$".ion-toolbar.{mode} .toolbar-container"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                FlexDirection = FlexDirection.Row,
                FlexWrap = FlexWrap.Nowrap,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceBetween,
                Width = Length.Percent(100),
                // Ionic constrains the toolbar's minimum height, but leaves the block size
                // automatic so wrapping/default-slot content (for example breadcrumbs) can grow
                // the toolbar beyond the minimum.
                MinHeight = Length.Px(t.ToolbarMinHeight),
                // Keep the content above the absolutely-positioned toolbar background.
                ZIndex = 1,
            },

            // .toolbar-content — the flex-item for the default slot. It gets flex:1 so it takes
            // remaining space; normal children stay in block flow while a direct progress bar is
            // taken out of flow by the ::slotted equivalent below.
            [$".ion-toolbar.{mode} .toolbar-content"] = new()
            {
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Auto,
                MinWidth = Length.Px(0),
                MaxWidth = Length.Percent(100),
                AlignItems = AlignItems.Center,
                // Keep ordinary default-slot children in block flow rather than a horizontal row.
                Display = Display.Block,
            },

            // toolbar.scss ::slotted(ion-progress-bar): pin a progress bar from the default slot
            // across the toolbar container's bottom edge. Width is auto so left+right determine
            // the size from the positioned toolbar-container rather than the slot wrapper width.
            [$".ion-toolbar.{mode} .toolbar-content > .ion-progress-bar"] = new()
            {
                Position = Position.Absolute,
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                Width = Length.Auto,
            },
        };

        if (mode == "md")
        {
            GenMd(css, t);
        }
        else
        {
            GenIos(css, t);
        }

        return css;
    }

    private static void GenMd(CssObject css, IonicTheme t)
    {
        // Material Design toolbar.md.scss
        css[".ion-toolbar.md .toolbar-background"].BorderBottomWidth = Length.Px(0);
    }

    private static void GenIos(CssObject css, IonicTheme t)
    {
        // iOS toolbar.ios.scss
        css[".ion-toolbar.ios .toolbar-background"].BorderBottomWidth = Length.Px(0f);

        // toolbar.ios.scss: :host(.toolbar-segment) .toolbar-content { display: inline-flex; }
        // IonSegment's width:auto then participates as a flex item and shrinks to its content.
        css[".ion-toolbar.ios.toolbar-segment .toolbar-content"] = new()
        {
            Display = Display.InlineFlex,
        };
    }
}
