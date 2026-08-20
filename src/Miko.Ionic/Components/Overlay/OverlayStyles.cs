using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

internal static class OverlayStyles
{
    internal static CssObject GenStyle(string mode) => new()
    {
        [$".ion-overlay-host.{mode}"] = new()
        {
            Position = Position.Absolute,
            Top = Length.Px(0),
            Right = Length.Px(0),
            Bottom = Length.Px(0),
            Left = Length.Px(0),
            PointerEvents = PointerEvents.None,
            ZIndex = 1000,
        },
        [$".ion-overlay-host.{mode} .ion-alert"] = new()
        {
            PointerEvents = PointerEvents.Auto,
        },
        [$".ion-overlay-host.{mode} .ion-action-sheet"] = new() { PointerEvents = PointerEvents.Auto },
        [$".ion-overlay-host.{mode} .ion-loading"] = new() { PointerEvents = PointerEvents.Auto },
        [$".ion-overlay-host.{mode} .ion-modal"] = new() { PointerEvents = PointerEvents.Auto },
        [$".ion-overlay-host.{mode} .ion-popover"] = new() { PointerEvents = PointerEvents.Auto },
    };
}
