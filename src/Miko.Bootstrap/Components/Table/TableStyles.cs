using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class TableToken
{
    public TableToken(Theme theme)
    {
        TableCellPaddingY = Length.Rem(0.5f);
        TableCellPaddingX = Length.Rem(0.5f);
        TableCellPaddingYSm = Length.Rem(0.25f);
        TableCellPaddingXSm = Length.Rem(0.25f);
        TableColor = theme.EmphasisColor;
        TableBg = theme.BodyBg;
        TableAccentBg = Transparent;
        TableBorderColor = theme.BorderColor;
        TableBorderWidth = theme.BorderWidth;
        TableStripedBg = Color.FromRgba(0, 0, 0, 0.05f);
        TableActiveBg = Color.FromRgba(0, 0, 0, 0.1f);
        TableHoverBg = Color.FromRgba(0, 0, 0, 0.075f);

        TablePrimaryBg = Color.FromHex("cfe2ff");
        TableSecondaryBg = Color.FromHex("e2e3e5");
        TableSuccessBg = Color.FromHex("d1e7dd");
        TableInfoBg = Color.FromHex("cff4fc");
        TableWarningBg = Color.FromHex("fff3cd");
        TableDangerBg = Color.FromHex("f8d7da");
        TableLightBg = theme.Light;
        TableDarkBg = theme.Dark;
        TableDarkColor = Color.White;
        TableDarkBorderColor = Color.FromHex("373b3e");
    }

    public Length TableCellPaddingY { get; set; }
    public Length TableCellPaddingX { get; set; }
    public Length TableCellPaddingYSm { get; set; }
    public Length TableCellPaddingXSm { get; set; }
    public Color TableColor { get; set; }
    public Color TableBg { get; set; }
    public Color TableAccentBg { get; set; }
    public Color TableBorderColor { get; set; }
    public float TableBorderWidth { get; set; }
    public Color TableStripedBg { get; set; }
    public Color TableActiveBg { get; set; }
    public Color TableHoverBg { get; set; }

    public Color TablePrimaryBg { get; set; }
    public Color TableSecondaryBg { get; set; }
    public Color TableSuccessBg { get; set; }
    public Color TableInfoBg { get; set; }
    public Color TableWarningBg { get; set; }
    public Color TableDangerBg { get; set; }
    public Color TableLightBg { get; set; }
    public Color TableDarkBg { get; set; }
    public Color TableDarkColor { get; set; }
    public Color TableDarkBorderColor { get; set; }
}

internal static class TableStyles
{
    internal static CssObject GenStyle(TableToken t)
    {
        VariantToken token = new(t.TableCellPaddingY, t.TableCellPaddingX, t.TableColor, t.TableBg, t.TableBorderWidth, t.TableAccentBg);

        return new CssObject
        {
            [".table"] = new()
            {
                Width = Percent(100),
                MarginBottom = Rem(1),
                VerticalAlign = VerticalAlign.Top,
                BorderColor = t.TableBorderColor,
                Color = t.TableColor,

                ["> :not(caption) > * > *"] = GenVariantTable(token),

                ["> tbody"] = new()
                {
                    // VerticalAlign = 
                },

                ["> thead"] = new()
                {
                    VerticalAlign = VerticalAlign.Bottom,
                },
            },

            [".table-sm"] = new()
            {
                ["th"] = new()
                {
                    Padding = new Padding(t.TableCellPaddingYSm, t.TableCellPaddingXSm)
                },

                ["td"] = new()
                {
                    Padding = new Padding(t.TableCellPaddingYSm, t.TableCellPaddingXSm)
                }
            },

            [".table-bordered"] = new()
            {
                BorderWidth = Length.Px(t.TableBorderWidth),
                BorderStyle = BorderStyle.Solid,
                BorderColor = t.TableBorderColor,

                ["th"] = new()
                {
                    BorderWidth = Length.Px(t.TableBorderWidth),
                    BorderStyle = BorderStyle.Solid,
                    BorderColor = t.TableBorderColor
                },

                ["td"] = new()
                {
                    BorderWidth = Length.Px(t.TableBorderWidth),
                    BorderStyle = BorderStyle.Solid,
                    BorderColor = t.TableBorderColor
                }
            },

            [".table-borderless"] = new()
            {
                ["th"] = new()
                {
                    BorderBottom = new BorderSide(0)
                },

                ["td"] = new()
                {
                    BorderBottom = new BorderSide(0)
                }
            },

            // NOTE: .table-striped uses nth-of-type(odd) which is not supported in CssObject
            // Use .table-row-striped class on individual rows instead
            [".table-row-striped"] = new()
            {
                BackgroundColor = t.TableStripedBg
            },

            [".table-active"] = new()
            {
                BackgroundColor = t.TableActiveBg
            },

            // NOTE: .table-hover uses :hover which requires parent context
            // Hover state would need to be applied via .table-hover tr:hover
            [".table-hover tr:hover"] = new()
            {
                BackgroundColor = t.TableHoverBg
            },

            [".table-primary"] = new()
            {
                ["> *"] = GenVariantTable(token with{ TableColor = "#000" }),
            },

            [".table-secondary"] = new()
            {
                BackgroundColor = t.TableSecondaryBg
            },

            [".table-success"] = new()
            {
                BackgroundColor = t.TableSuccessBg
            },

            [".table-info"] = new()
            {
                BackgroundColor = t.TableInfoBg
            },

            [".table-warning"] = new()
            {
                BackgroundColor = t.TableWarningBg
            },

            [".table-danger"] = new()
            {
                BackgroundColor = t.TableDangerBg
            },

            [".table-light"] = new()
            {
                BackgroundColor = t.TableLightBg
            },

            [".table-dark"] = new()
            {
                BackgroundColor = t.TableDarkBg,
                Color = t.TableDarkColor,

                ["th"] = new()
                {
                    BorderColor = t.TableDarkBorderColor
                },

                ["td"] = new()
                {
                    BorderColor = t.TableDarkBorderColor
                }
            },

            [".table-group-divider"] = new()
            {
                BorderTop = new BorderSide(Length.Px(t.TableBorderWidth * 2), BorderStyle.Solid, t.TableBorderColor)
            },

            [".caption-top"] = new()
            {
                // NOTE: caption-side: top not supported in CssObject
            },

            [".table-responsive"] = new()
            {
                OverflowX = Overflow.Auto
            }
        };
    }

    private record struct VariantToken(
        Length TableCellPaddingY,
        Length TableCellPaddingX,
        Color TableColor,
        Color TableBg,
        Length TableBorderWidth,
        Color TableAccentBg);

    private static CssObject GenVariantTable(VariantToken t)
    {
        return new()
        {
            Padding = new Padding(t.TableCellPaddingY, t.TableCellPaddingX),
            Color = t.TableColor,
            BackgroundColor = t.TableBg,
            BorderBottomWidth = t.TableBorderWidth,
            BoxShadow = [new BoxShadow(0, 0, 0, Px(9999), t.TableAccentBg)]
        };
    }
}
