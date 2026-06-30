using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Generates stylesheet rules for the Ionic footer component.
/// Ports the base, iOS, and Material Design footer styles from Ionic Framework.
/// <para>
/// Footer mirrors the toolbar positioning pattern — it's a block-level container at the bottom
/// of the page/modal flow. Toolbars nested inside receive mode-specific borders/shadows (iOS
/// gets a hairline separator at top; MD gets an elevation shadow). Translucent mode (iOS-only)
/// applies a backdrop filter to the background div and dims the toolbar slightly.
/// </para>
/// </summary>
internal static class FooterStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // ion-footer — the host container. Block-level, fills width, sits at bottom of page flow.
            // Positioned relative for the absolute backdrop in translucent iOS mode.
            // z-index 10 matches the header (from Ionic's $z-index-toolbar).
            [$".{mode}.ion-footer"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                Width = Length.Percent(100),
                ZIndex = 10,
            },

            // Footer toolbar padding for safe areas (ion-safe-area-bottom)
            // Applied to the last toolbar when footer-toolbar-padding class is present
            [$".{mode}.ion-footer.footer-toolbar-padding .{mode}.ion-toolbar:last-of-type"] = new()
            {
                // Safe area padding bottom - in real implementation this would use env(safe-area-inset-bottom)
                // For now we set a base value that can be overridden by platform-specific safe area handling
                PaddingBottom = Length.Px(0),
            },
        };

        if (mode == "ios")
        {
            // iOS: hairline border on top of first toolbar
            // Uses HeaderBorderColor from theme (same as header bottom border)
            css[$".{mode}.ion-footer .{mode}.ion-toolbar:first-of-type"] = new()
            {
                BorderTopWidth = Length.Px(0.55f), // Hairline
                BorderTopStyle = BorderStyle.Solid,
                BorderTopColor = t.HeaderBorderColor,
            };

            // Translucent footer background — absolute fill with backdrop filter effect
            css[$".{mode}.ion-footer .footer-background"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                // Note: backdrop-filter (saturate + blur) not yet supported in the rendering engine
                // This serves as a placeholder for future backdrop-filter support
            };

            // Translucent toolbar opacity
            css[$".{mode}.ion-footer.footer-translucent-ios .{mode}.ion-toolbar"] = new()
            {
                Opacity = 0.8f,
            };

            // No border variant
            css[$".{mode}.ion-footer.ion-no-border .{mode}.ion-toolbar:first-of-type"] = new()
            {
                BorderTopWidth = Length.Px(0),
            };

            // Collapse fade - opacity scale inheritance (placeholder for CSS variable support)
            // In Ionic this uses --opacity-scale CSS custom property
            css[$".{mode}.ion-footer.footer-collapse-fade .{mode}.ion-toolbar"] = new()
            {
                // Placeholder for future CSS variable --opacity-scale support
            };
        }
        else if (mode == "md")
        {
            // Material Design: elevation shadow
            css[$".{mode}.ion-footer.footer-md"] = new()
            {
                // MD footer box shadow (0 2px 4px -1px rgba(0,0,0,0.2), 0 4px 5px 0 rgba(0,0,0,0.14), 0 1px 10px 0 rgba(0,0,0,0.12))
                // Using simplified single shadow for now
                BoxShadow = new List<BoxShadow>
                {
                    new BoxShadow
                    {
                        OffsetX = 0,
                        OffsetY = -2,
                        BlurRadius = 4,
                        SpreadRadius = 0,
                        Color = new Color(0, 0, 0, 51), // rgba(0, 0, 0, 0.2)
                    }
                }
            };

            // No border variant (removes shadow)
            css[$".{mode}.ion-footer.footer-md.ion-no-border"] = new()
            {
                BoxShadow = null,
            };
        }

        return css;
    }
}
