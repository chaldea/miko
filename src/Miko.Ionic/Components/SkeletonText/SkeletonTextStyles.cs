using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Generates stylesheet rules for the Ionic skeleton-text component.
/// Ports <c>skeleton-text.scss</c> + <c>skeleton-text.vars.scss</c>: a full-width placeholder bar
/// painted with a translucent gray fill (<c>rgba(text-color, .065)</c>) and rounded to inherit its
/// container radius, with a 4px top/bottom margin and no pointer/selection interaction.
/// <para>
/// Ionic's <c>skeleton-text-animated</c> variant uses a keyframe shimmer gradient; Miko has no
/// direct analog for that CSS animation here, so the animated state is modeled statically as the
/// lighter pulse fill (<c>rgba(text-color, .135)</c>). The marker class is still stamped so callers
/// can target the animated state. There is no mode-specific difference (a single shared SCSS), but
/// rules are scoped per mode for registration parity with the rest of the suite.
/// </para>
/// </summary>
internal static class SkeletonTextStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        return new CssObject
        {
            // ion-skeleton-text — the host bar: a full-width gray block with a small vertical margin.
            [$".ion-skeleton-text.{mode}"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                MarginTop = Length.Px(4),
                MarginBottom = Length.Px(4),
                BackgroundColor = t.SkeletonTextBackground,
                LineHeight = Length.Px(10),
                UserSelect = UserSelect.None,
                PointerEvents = PointerEvents.None,
            },

            // The inner spacer span is inline-block (gives the bar its line-height height).
            [$".ion-skeleton-text.{mode} span"] = new()
            {
                Display = Display.InlineBlock,
            },

            // Animated variant — Ionic shimmers via a keyframe gradient; modeled here as the
            // lighter static pulse fill plus the marker class.
            [$".ion-skeleton-text.{mode}.skeleton-text-animated"] = new()
            {
                BackgroundColor = t.SkeletonTextBackgroundAnimated,
            },
        };
    }
}
