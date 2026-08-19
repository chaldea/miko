using Miko.Animation;
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
/// scaleX()</c>, but here determinate widths are bound inline as percentages (see the razor).
/// The track defaults to full width for indeterminate mode. Indeterminate mode renders two stripe bars
/// (<c>.indeterminate-bar-primary/secondary</c>) with keyframe animations bound by the component.
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
            // clipped. The always-present buffer bar supplies the visible background track.
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

            // Reversing the host mirrors every fill and animation around its center, matching
            // Ionic's :host(.progress-bar-reversed) { transform: scaleX(-1) }.
            [$".ion-progress-bar.{mode}.progress-bar-reversed"] = new()
            {
                Transform = Transform.FromScale(-1f, 1f),
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
                TransformOrigin = TransformOrigin.TopLeft,
                ZIndex = 2,
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
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                BackgroundColor = t.ProgressBarBackground,
                ZIndex = 1,
            },

            // Buffer stream: two translated clipping containers expose only the unbuffered range.
            [$".ion-progress-bar.{mode} .buffer-circles-container"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },
            [$".ion-progress-bar.{mode} .buffer-circles"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(-10),
                Bottom = Length.Px(0),
                Left = Length.Px(-10),
                Height = Length.Percent(100),
                BoxSizing = BoxSizing.BorderBox,
                BorderTopWidth = Length.Px(t.ProgressBarHeight),
                BorderTopStyle = BorderStyle.Dotted,
                BorderTopColor = t.ProgressBarBackground,
                ZIndex = 0,
            },

            // Ionic offsets the two stripe containers before their translate animations run.
            [$".ion-progress-bar.{mode} .indeterminate-bar-primary"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Percent(-145.166611f),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            },
            [$".ion-progress-bar.{mode} .indeterminate-bar-secondary"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Percent(-54.888891f),
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
                TransformOrigin = TransformOrigin.TopLeft,
                ZIndex = 2,
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
        css[$".ion-progress-bar.{mode}.ion-color-{name} .buffer-circles"] = new()
        {
            BorderTopColor = track,
        };
    }
}
