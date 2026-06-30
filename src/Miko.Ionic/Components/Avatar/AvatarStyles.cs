using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Generates stylesheet rules for the Ionic avatar component.
/// Ports the avatar styles from Ionic Framework (<c>avatar.scss</c> + the per-mode
/// <c>avatar.md.scss</c> / <c>avatar.ios.scss</c>).
/// <para>
/// The avatar is a block-level square container clipped into a circle (50% border-radius). It has
/// an intrinsic size from the theme — 64px in md, 48px in ios (<c>$avatar-md-width</c> /
/// <c>$avatar-ios-width</c>). The slotted image fills the host and is itself rounded + clipped
/// (<c>overflow: hidden</c>) so it matches the circular host.
/// </para>
/// </summary>
internal static class AvatarStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        return new CssObject
        {
            // ion-avatar — the host container. A block-level square (width == height from the theme)
            // rounded into a full circle (50% radius). md is 64px, ios is 48px.
            [$".{mode}.ion-avatar"] = new()
            {
                Display = Display.Block,
                Width = Length.Px(t.AvatarSize),
                Height = Length.Px(t.AvatarSize),
                BorderRadius = new BorderRadius(Length.Percent(50)),
            },

            // The slotted image fills the host, is rounded to match, and clips its own overflow —
            // matching Ionic's `::slotted(img) { border-radius; width: 100%; height: 100%;
            // object-fit: cover; overflow: hidden; }`. object-fit rendering is part of the
            // (not-yet-ported) img component work, so it is omitted here for now.
            [$".{mode}.ion-avatar img"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                BorderRadius = new BorderRadius(Length.Percent(50)),
                Overflow = Overflow.Hidden,
            },
        };
    }
}
