using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-modal</c>. Ported from Ionic's <c>modal.scss</c>, <c>modal.md.scss</c>, and
/// <c>modal.ios.scss</c>.
/// <para>
/// The default modal surface fills the overlay host. Sheet modals keep that full-height surface,
/// anchor it to the bottom, and translate it according to the active breakpoint.
/// </para>
/// </summary>
internal static class ModalStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var iosCardShadow = new List<BoxShadow>
        {
            new BoxShadow(Length.Px(0), Length.Px(0), Length.Px(30), Length.Px(10),
                new Color(0, 0, 0, 26)),
        };

        var css = new CssObject
        {
            // The overlay host always covers the viewport. Flex centering remains useful when a
            // caller gives the modal surface a custom size.
            [$".ion-modal.{mode}"] = new()
            {
                Position = Position.Fixed,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Color = t.TextColor,
                ZIndex = 1000,
            },

            [$".ion-modal.{mode}.overlay-hidden"] = new()
            {
                Display = Display.None,
            },

            [$".ion-modal.{mode} .modal-backdrop"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                BackgroundColor = t.AlertBackdropColor,
                Opacity = mode == "ios" ? 0.4f : 0.32f,
                Cursor = Cursor.Pointer,
            },

            // modal.scss base variables: width/height 100%, min/max auto, radius 0, shadow none.
            [$".ion-modal.{mode} .modal-wrapper"] = new()
            {
                Position = Position.Relative,
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = Length.Percent(100),
                MinWidth = Length.Auto,
                MaxWidth = Length.Auto,
                Height = Length.Percent(100),
                MinHeight = Length.Auto,
                MaxHeight = Length.Auto,
                BackgroundColor = t.BackgroundColor,
                BorderRadius = BorderRadius.None,
                BoxShadow = new List<BoxShadow>(),
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
                ZIndex = 10,
            },

            // iOS renders this transparent sibling behind the wrapper for card/sheet effects.
            [$".ion-modal.{mode} .modal-shadow"] = new()
            {
                Position = Position.Absolute,
                Width = Length.Percent(100),
                MinWidth = Length.Auto,
                MaxWidth = Length.Auto,
                Height = Length.Percent(100),
                MinHeight = Length.Auto,
                MaxHeight = Length.Auto,
                BackgroundColor = Color.Transparent,
                BorderRadius = BorderRadius.None,
                BoxShadow = new List<BoxShadow>(),
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
                ZIndex = 10,
            },

            // A sheet leaves the top safe area plus Ionic's 10px visual gap uncovered.
            [$".ion-modal.{mode}.modal-sheet .modal-wrapper"] = new()
            {
                Position = Position.Absolute,
                Height = Length.Percent(100) - Length.SafeAreaInsetTop - Length.Px(10),
            },

            [$".ion-modal.{mode}.modal-sheet .modal-shadow"] = new()
            {
                Position = Position.Absolute,
                Height = Length.Percent(100) - Length.SafeAreaInsetTop - Length.Px(10),
            },

            // Ionic only rounds the exposed top corners of an iOS sheet.
            [".ion-modal.ios.modal-sheet .modal-wrapper"] = new()
            {
                BorderRadius = new BorderRadius(
                    Length.Px(10), Length.Px(10), Length.Px(0), Length.Px(0)),
            },

            [".ion-modal.ios.modal-sheet .modal-shadow"] = new()
            {
                BorderRadius = new BorderRadius(Length.Px(10)),
            },

            // Keep the existing iOS presenting/card affordance without changing the default
            // fullscreen modal surface.
            [".ion-modal.ios.modal-card .modal-wrapper"] = new()
            {
                BorderRadius = new BorderRadius(Length.Px(10)),
                BoxShadow = iosCardShadow,
            },

            [$".ion-modal.{mode} .modal-handle"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(5),
                Left = Length.Px(0),
                Right = Length.Px(0),
                Width = Length.Px(36),
                Height = Length.Px(5),
                MarginLeft = Length.Auto,
                MarginRight = Length.Auto,
                BorderWidth = Length.Px(0),
                BorderStyle = BorderStyle.None,
                BorderRadius = new BorderRadius(Length.Px(8)),
                BackgroundColor = Color.FromHex("c0c0be"),
                Cursor = Cursor.Pointer,
                ZIndex = 11,
            },
        };

        return css;
    }
}
