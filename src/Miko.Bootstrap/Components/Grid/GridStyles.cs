using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class GridToken
{
    public GridToken(Theme theme)
    {
        GridGutterWidth = Length.Rem(1.5f);
        ContainerPaddingX = Length.Rem(0.75f);
    }

    public Length GridGutterWidth { get; set; }
    public Length ContainerPaddingX { get; set; }
}

internal static class GridStyles
{
    internal static CssObject GenStyle(GridToken t)
    {
        var styles = new CssObject
        {
            [".container"] = new()
            {
                Width = Length.Percent(100),
                PaddingRight = t.ContainerPaddingX,
                PaddingLeft = t.ContainerPaddingX,
                MarginRight = Length.Auto,
                MarginLeft = Length.Auto
            },

            [".container-fluid"] = new()
            {
                Width = Length.Percent(100),
                PaddingRight = t.ContainerPaddingX,
                PaddingLeft = t.ContainerPaddingX,
                MarginRight = Length.Auto,
                MarginLeft = Length.Auto
            },

            [".row"] = new()
            {
                Display = Display.Flex,
                FlexWrap = FlexWrap.Wrap,
                MarginRight = Length.Rem(-0.75f),
                MarginLeft = Length.Rem(-0.75f)
            },

            [".col"] = new()
            {
                FlexGrow = 1,
                FlexBasis = Length.Px(0),
                MaxWidth = Length.Percent(100),
                PaddingRight = t.ContainerPaddingX,
                PaddingLeft = t.ContainerPaddingX
            },

            [".col-auto"] = new()
            {
                FlexGrow = 0,
                FlexShrink = 0,
                Width = Length.Auto,
                PaddingRight = t.ContainerPaddingX,
                PaddingLeft = t.ContainerPaddingX
            }
        };

        for (int i = 1; i <= 12; i++)
        {
            styles[$".col-{i}"] = new CssObject
            {
                FlexGrow = 0,
                FlexShrink = 0,
                Width = Length.Percent(i * 100f / 12f),
                PaddingRight = t.ContainerPaddingX,
                PaddingLeft = t.ContainerPaddingX
            };
        }

        for (int i = 1; i <= 11; i++)
        {
            styles[$".offset-{i}"] = new CssObject
            {
                MarginLeft = Length.Percent(i * 100f / 12f)
            };
        }

        // NOTE: Responsive breakpoint classes (.col-sm-*, .col-md-*, etc.) are not supported
        // because CssObject does not support @media queries

        var gutterSizes = new[] { 0f, 0.25f, 0.5f, 1f, 1.5f, 3f };
        for (int i = 0; i < gutterSizes.Length; i++)
        {
            var size = gutterSizes[i];
            styles[$".g-{i}"] = new CssObject
            {
                MarginRight = Length.Rem(-size / 2),
                MarginLeft = Length.Rem(-size / 2),
                MarginTop = Length.Rem(-size),

                ["> *"] = new CssObject
                {
                    PaddingRight = Length.Rem(size / 2),
                    PaddingLeft = Length.Rem(size / 2),
                    MarginTop = Length.Rem(size)
                }
            };

            styles[$".gx-{i}"] = new CssObject
            {
                MarginRight = Length.Rem(-size / 2),
                MarginLeft = Length.Rem(-size / 2),

                ["> *"] = new CssObject
                {
                    PaddingRight = Length.Rem(size / 2),
                    PaddingLeft = Length.Rem(size / 2)
                }
            };

            styles[$".gy-{i}"] = new CssObject
            {
                MarginTop = Length.Rem(-size),

                ["> *"] = new CssObject
                {
                    MarginTop = Length.Rem(size)
                }
            };
        }

        return styles;
    }
}
