using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class ModalToken
{
    public ModalToken(Theme theme)
    {
        ModalBg = Color.White;
        ModalBorderColor = theme.BorderColor;
        ModalBorderWidth = theme.BorderWidth;
        ModalBorderRadius = theme.BorderRadius;
        ModalPadding = Length.Rem(1);
        ModalHeaderPaddingY = Length.Rem(1);
        ModalHeaderPaddingX = Length.Rem(1);
        ModalHeaderBorderColor = theme.BorderColor;
        ModalFooterBorderColor = theme.BorderColor;
    }

    public Color ModalBg { get; set; }
    public Color ModalBorderColor { get; set; }
    public float ModalBorderWidth { get; set; }
    public float ModalBorderRadius { get; set; }
    public Length ModalPadding { get; set; }
    public Length ModalHeaderPaddingY { get; set; }
    public Length ModalHeaderPaddingX { get; set; }
    public Color ModalHeaderBorderColor { get; set; }
    public Color ModalFooterBorderColor { get; set; }
}

internal static class ModalStyles
{
    internal static CssObject GenStyle(ModalToken t)
    {
        return new CssObject
        {
            [".modal"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                ZIndex = 1055,
                Display = Display.None,
                Width = Length.Percent(100),
                Height = Length.Percent(100)
            },

            [".modal.show"] = new()
            {
                Display = Display.Block
            },

            [".modal-backdrop"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                BackgroundColor = Color.FromRgba(0, 0, 0, 128),
                ZIndex = 1050
            },

            [".modal-dialog"] = new()
            {
                Position = Position.Relative,
                Width = Length.Auto,
                Margin = new Margin(Length.Rem(0.5f))
            },

            [".modal-content"] = new()
            {
                Position = Position.Relative,
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                BackgroundColor = t.ModalBg,
                Border = new Border(Length.Px(t.ModalBorderWidth), BorderStyle.Solid, t.ModalBorderColor),
                BorderRadius = t.ModalBorderRadius
            },

            [".modal-header"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Padding = new Padding(t.ModalHeaderPaddingY, t.ModalHeaderPaddingX),
                BorderBottom = new BorderSide(Length.Px(t.ModalBorderWidth), BorderStyle.Solid, t.ModalHeaderBorderColor)
            },

            [".modal-body"] = new()
            {
                Position = Position.Relative,
                Padding = new Padding(t.ModalPadding)
            },

            [".modal-footer"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.FlexEnd,
                Padding = new Padding(t.ModalHeaderPaddingY, t.ModalHeaderPaddingX),
                BorderTop = new BorderSide(Length.Px(t.ModalBorderWidth), BorderStyle.Solid, t.ModalFooterBorderColor)
            }
        };
    }
}
