using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-progress-bar</c>. Ported from Ionic's source: <c>progress-bar.scss</c> /
/// <c>progress-bar.md.scss</c> / <c>progress-bar.ios.scss</c> and their <c>*.vars.scss</c>.
/// <para>
/// The host is a thin full-width bar (4px both modes; md square corners, ios a full pill). The
/// <c>.progress-buffer-bar</c> is the track (background = primary @ .3) and the <c>.progress</c> is
/// the value fill (background = solid primary), both absolutely positioned and filling the host
/// height. Their horizontal extent scales with buffer / value — Ionic uses <c>transform:
/// scaleX()</c>, but here the width is bound inline as a percentage (see the razor), so these rules
/// intentionally do not pin a width. Indeterminate mode renders two stripe bars
/// (<c>.indeterminate-bar-primary/secondary</c>); their sliding animation is a marker only.
/// </para>
/// </summary>
internal static class ProgressBarStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var radius = t.ProgressBarBorderRadius > 0
            ? new BorderRadius(Length.Px(t.ProgressBarBorderRadius))
            : new BorderRadius(Length.Px(0));

        var css = new CssObject
        {
            // ion-progress-bar — the host. Ionic's :host: block, relative, full width, fixed height,
            // clipped. No background of its own (the buffer bar / track supplies it).
            [$".ion-progress-bar.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                Width = Length.Percent(100),
                Height = Length.Px(t.ProgressBarHeight),
                BorderRadius = radius,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            // .progress — the value fill. Absolutely positioned, full-height, solid primary; sits
            // above the buffer track. Width comes from the inline percentage bound in the razor.
            [$".ion-progress-bar.{mode} .progress"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                Bottom = Length.Px(0),
                Height = Length.Percent(100),
                BackgroundColor = t.ProgressBarProgressBackground,
            },

            // .progress-buffer-bar — the track. Full-height, primary @ .3; below the fill. Width comes
            // from the inline percentage bound in the razor (determinate) or fills the host
            // (indeterminate, where it hosts the two stripe bars).
            [$".ion-progress-bar.{mode} .progress-buffer-bar"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                Bottom = Length.Px(0),
                Height = Length.Percent(100),
                BackgroundColor = t.ProgressBarBackground,
            },

            // Indeterminate stripe bars — absolutely positioned, full size, solid primary. The
            // left-to-right slide animation is not modeled; the classes are markers.
            [$".ion-progress-bar.{mode} .indeterminate-bar-primary"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            },
            [$".ion-progress-bar.{mode} .indeterminate-bar-secondary"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            },
            [$".ion-progress-bar.{mode} .progress-indeterminate"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                BackgroundColor = t.ProgressBarProgressBackground,
            },
        };

        // Color palette overrides — the fill / stripe become the named base color and the track its
        // .3-alpha tint. Mirrors Ionic's :host(.ion-color) rules.
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

    private static void AddColor(CssObject css, string mode, string name, Color baseColor)
    {
        var track = new Color(baseColor.R, baseColor.G, baseColor.B, 77); // base @ .3 alpha

        css[$".ion-progress-bar.{mode}.ion-color-{name} .progress"] = new()
        {
            BackgroundColor = baseColor,
        };
        css[$".ion-progress-bar.{mode}.ion-color-{name} .progress-indeterminate"] = new()
        {
            BackgroundColor = baseColor,
        };
        css[$".ion-progress-bar.{mode}.ion-color-{name} .progress-buffer-bar"] = new()
        {
            BackgroundColor = track,
        };
    }
}
