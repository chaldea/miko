using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>Styles for <c>ion-grid</c>, <c>ion-row</c>, and <c>ion-col</c>.</summary>
internal static class GridStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        return new CssObject
        {
            [$".ion-grid.{mode}"] = new()
            {
                Display = Display.Block,
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Percent(0),
                Width = Length.Percent(100),
                PaddingTop = Length.Px(t.GridPadding),
                PaddingRight = Length.Px(t.GridPadding),
                PaddingBottom = Length.Px(t.GridPadding),
                PaddingLeft = Length.Px(t.GridPadding),
                MarginLeft = Length.Auto,
                MarginRight = Length.Auto,
                BoxSizing = BoxSizing.BorderBox,
            },

            [$".ion-grid.{mode}.grid-fixed"] = new()
            {
                MaxWidth = Length.Px(t.GridFixedWidth),
            },

            [$".ion-row.{mode}"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                FlexWrap = FlexWrap.Wrap,
                Width = Length.Percent(100),
            },

            [$".ion-col.{mode}"] = new()
            {
                Position = Position.Relative,
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Percent(0),
                Width = Length.Percent(100),
                MaxWidth = Length.Percent(100),
                MinHeight = Length.Px(1),
                PaddingTop = Length.Px(t.GridColumnPadding),
                PaddingRight = Length.Px(t.GridColumnPadding),
                PaddingBottom = Length.Px(t.GridColumnPadding),
                PaddingLeft = Length.Px(t.GridColumnPadding),
                BoxSizing = BoxSizing.BorderBox,
            },
        };
    }
}
