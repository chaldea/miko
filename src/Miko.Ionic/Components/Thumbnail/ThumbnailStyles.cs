using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Generates stylesheet rules for the Ionic thumbnail component.
/// Ports <c>thumbnail.scss</c>: a fixed 48px square (<c>--size</c>) block holding a slotted image
/// that fills the host and is cropped with <c>object-fit: cover</c>. There is no mode-specific
/// difference (no <c>thumbnail.md/.ios</c>), but rules are still scoped per mode for registration
/// parity with the rest of the suite.
/// </summary>
internal static class ThumbnailStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        return new CssObject
        {
            // ion-thumbnail — the host: a fixed square block (48px both modes).
            [$".ion-thumbnail.{mode}"] = new()
            {
                Display = Display.Block,
                Width = Length.Px(t.ThumbnailSize),
                Height = Length.Px(t.ThumbnailSize),
                BorderRadius = new BorderRadius(Length.Px(0)),   // --border-radius: 0 default
            },

            // The slotted image fills the host and is cropped (object-fit: cover). object-fit
            // rendering is part of the (not-yet-ported) img component work, so it is omitted here.
            [$".ion-thumbnail.{mode} img"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                Overflow = Overflow.Hidden,
            },
        };
    }
}
