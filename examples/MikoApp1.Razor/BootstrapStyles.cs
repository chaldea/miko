using Miko.Common;
using Miko.Core.DomElements;
using Miko.Styling;

namespace MikoApp1.Razor;

public static class BootstrapStyles
{
    public static class Colors
    {
        public static readonly Color Primary = Color.FromRgb(0, 123, 255);
        public static readonly Color Secondary = Color.FromRgb(108, 117, 125);
        public static readonly Color Success = Color.FromRgb(40, 167, 69);
        public static readonly Color Danger = Color.FromRgb(220, 53, 69);
        public static readonly Color Warning = Color.FromRgb(255, 193, 7);
        public static readonly Color Info = Color.FromRgb(23, 162, 184);
        public static readonly Color Light = Color.FromRgb(248, 249, 250);
        public static readonly Color Dark = Color.FromRgb(52, 58, 64);

        public static readonly Color TextWhite = Color.White;
        public static readonly Color TextDark = Color.FromRgb(33, 37, 41);
    }

    public static StyleSheet CreateBootstrapStyleSheet()
    {
        var styleSheet = new StyleSheet();

        // Container styles
        styleSheet.AddRule(Style.Class("container")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Column)
            .Set(x => x.Padding, new Padding(40))
            .Set(x => x.BackgroundColor, Color.White));

        // Row
        styleSheet.AddRule(Style.Class("row")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Row)
            .Set(x => x.MarginBottom, Length.Px(20)));

        // Base button
        styleSheet.AddRule(Style.For<ButtonElement>()
            .Set(x => x.Display, Display.InlineBlock)
            .Set(x => x.Padding, new Padding(6, 12))
            .Set(x => x.Margin, new Margin(0, 10, 10, 0))
            .Set(x => x.FontSize, 14)
            .Set(x => x.FontWeight, FontWeight.Normal)
            .Set(x => x.Border, new Border(1, BorderStyle.Solid, Color.Transparent))
            .Set(x => x.BorderRadius, new BorderRadius(4)));

        // Button color variants
        AddButtonVariant(styleSheet, "btn-primary", Colors.Primary, Colors.TextWhite);
        AddButtonVariant(styleSheet, "btn-secondary", Colors.Secondary, Colors.TextWhite);
        AddButtonVariant(styleSheet, "btn-success", Colors.Success, Colors.TextWhite);
        AddButtonVariant(styleSheet, "btn-danger", Colors.Danger, Colors.TextWhite);
        AddButtonVariant(styleSheet, "btn-warning", Colors.Warning, Colors.TextDark);
        AddButtonVariant(styleSheet, "btn-info", Colors.Info, Colors.TextWhite);
        AddButtonVariant(styleSheet, "btn-light", Colors.Light, Colors.TextDark);
        AddButtonVariant(styleSheet, "btn-dark", Colors.Dark, Colors.TextWhite);

        // Button hover
        AddButtonHover(styleSheet, "btn-primary", Color.FromHex("0069d9"), Color.FromHex("0062cc"));
        AddButtonHover(styleSheet, "btn-secondary", Color.FromHex("5a6268"), Color.FromHex("545b62"));
        AddButtonHover(styleSheet, "btn-success", Color.FromHex("218838"), Color.FromHex("1e7e34"));
        AddButtonHover(styleSheet, "btn-danger", Color.FromHex("c82333"), Color.FromHex("bd2130"));
        AddButtonHover(styleSheet, "btn-warning", Color.FromHex("e0a800"), Color.FromHex("d39e00"));
        AddButtonHover(styleSheet, "btn-info", Color.FromHex("138496"), Color.FromHex("117a8b"));
        AddButtonHover(styleSheet, "btn-light", Color.FromHex("e2e6ea"), Color.FromHex("dae0e5"));
        AddButtonHover(styleSheet, "btn-dark", Color.FromHex("23272b"), Color.FromHex("1d2124"));

        // Outline button variants
        AddOutlineButtonVariant(styleSheet, "btn-outline-primary", Colors.Primary);
        AddOutlineButtonVariant(styleSheet, "btn-outline-secondary", Colors.Secondary);
        AddOutlineButtonVariant(styleSheet, "btn-outline-success", Colors.Success);
        AddOutlineButtonVariant(styleSheet, "btn-outline-danger", Colors.Danger);
        AddOutlineButtonVariant(styleSheet, "btn-outline-warning", Colors.Warning);
        AddOutlineButtonVariant(styleSheet, "btn-outline-info", Colors.Info);
        AddOutlineButtonVariant(styleSheet, "btn-outline-dark", Colors.Dark);

        // Button sizes
        styleSheet.AddRule(Style.Class("btn-sm")
            .Set(x => x.Padding, new Padding(4, 8))
            .Set(x => x.FontSize, Length.Px(12)));

        styleSheet.AddRule(Style.Class("btn-lg")
            .Set(x => x.Padding, new Padding(10, 16))
            .Set(x => x.FontSize, Length.Px(16)));

        // Headings
        styleSheet.AddRule(Style.For<H1Element>()
            .Set(x => x.FontSize, Length.Px(32))
            .Set(x => x.FontWeight, FontWeight.Bold)
            .Set(x => x.MarginBottom, Length.Px(20))
            .Set(x => x.Color, Colors.Dark));

        styleSheet.AddRule(Style.For<H2Element>()
            .Set(x => x.FontSize, Length.Px(24))
            .Set(x => x.FontWeight, FontWeight.Bold)
            .Set(x => x.MarginTop, Length.Px(20))
            .Set(x => x.MarginBottom, Length.Px(15))
            .Set(x => x.Color, Colors.Dark));

        AddFormControlStyles(styleSheet);
        AddListStyles(styleSheet);
        AddTableStyles(styleSheet);
        AddIconStyles(styleSheet);
        return styleSheet;
    }

    private static void AddFormControlStyles(StyleSheet styleSheet)
    {
        styleSheet.AddRule(Style.Class("form-control")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.Width, Length.Percent(100))
            .Set(x => x.Padding, new Padding(6, 12))
            .Set(x => x.FontSize, Length.Px(14))
            .Set(x => x.FontWeight, FontWeight.Normal)
            .Set(x => x.Color, Colors.TextDark)
            .Set(x => x.BackgroundColor, Color.White)
            .Set(x => x.Border, new Border(1, BorderStyle.Solid, Color.FromHex("ced4da")))
            .Set(x => x.BorderRadius, new BorderRadius(6)));

        styleSheet.AddRule(Style.Class("form-control").Focus()
            .Set(x => x.Color, Color.FromHex("212529"))
            .Set(x => x.BackgroundColor, Color.White)
            .Set(x => x.BorderColor, Color.FromHex("86b7fe")));

        styleSheet.AddRule(Style.Class("form-control-lg")
            .Set(x => x.Padding, new Padding(8, 16))
            .Set(x => x.FontSize, Length.Px(18))
            .Set(x => x.BorderRadius, new BorderRadius(6)));

        styleSheet.AddRule(Style.Class("form-control-sm")
            .Set(x => x.Padding, new Padding(4, 8))
            .Set(x => x.FontSize, Length.Px(12))
            .Set(x => x.BorderRadius, new BorderRadius(3)));

        styleSheet.AddRule(Style.Class("form-select")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.Width, Length.Percent(90))
            .Set(x => x.PaddingTop, Length.Px(6))
            .Set(x => x.PaddingRight, Length.Px(36))
            .Set(x => x.PaddingBottom, Length.Px(6))
            .Set(x => x.PaddingLeft, Length.Px(12))
            .Set(x => x.FontSize, Length.Px(14))
            .Set(x => x.Color, Colors.TextDark)
            .Set(x => x.BackgroundColor, Color.White)
            .Set(x => x.Border, new Border(1, BorderStyle.Solid, Color.FromHex("ced4da")))
            .Set(x => x.BorderRadius, new BorderRadius(4)));

        styleSheet.AddRule(Style.Class("form-select-lg")
            .Set(x => x.PaddingTop, Length.Px(8))
            .Set(x => x.PaddingRight, Length.Px(40))
            .Set(x => x.PaddingBottom, Length.Px(8))
            .Set(x => x.PaddingLeft, Length.Px(16))
            .Set(x => x.FontSize, Length.Px(18))
            .Set(x => x.BorderRadius, new BorderRadius(6)));

        styleSheet.AddRule(Style.Class("form-select-sm")
            .Set(x => x.PaddingTop, Length.Px(4))
            .Set(x => x.PaddingRight, Length.Px(32))
            .Set(x => x.PaddingBottom, Length.Px(4))
            .Set(x => x.PaddingLeft, Length.Px(8))
            .Set(x => x.FontSize, Length.Px(12))
            .Set(x => x.BorderRadius, new BorderRadius(3)));

        styleSheet.AddRule(Style.Class("form-label")
            .Set(x => x.Display, Display.InlineBlock)
            .Set(x => x.MarginBottom, Length.Px(8))
            .Set(x => x.FontSize, Length.Px(14))
            .Set(x => x.FontWeight, FontWeight.Normal)
            .Set(x => x.Color, Colors.TextDark));

        styleSheet.AddRule(Style.Class("form-text")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.MarginTop, Length.Px(4))
            .Set(x => x.FontSize, Length.Px(12))
            .Set(x => x.Color, Colors.Secondary));

        styleSheet.AddRule(Style.Class("form-check")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Row)
            .Set(x => x.AlignItems, AlignItems.Center)
            .Set(x => x.MarginBottom, Length.Px(8))
            .Set(x => x.PaddingLeft, Length.Px(0)));

        styleSheet.AddRule(Style.Class("form-check-label")
            .Set(x => x.Display, Display.InlineBlock)
            .Set(x => x.MarginLeft, Length.Px(8))
            .Set(x => x.FontSize, Length.Px(14))
            .Set(x => x.Color, Colors.TextDark));

        styleSheet.AddRule(Style.Class("mb-3")
            .Set(x => x.MarginBottom, Length.Px(16)));

        styleSheet.AddRule(Style.Class("mb-4")
            .Set(x => x.MarginBottom, Length.Px(24)));

        styleSheet.AddRule(Style.Class("is-valid")
            .Set(x => x.BorderColor, Colors.Success));

        styleSheet.AddRule(Style.Class("is-invalid")
            .Set(x => x.BorderColor, Colors.Danger));

        styleSheet.AddRule(Style.Class("valid-feedback")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.MarginTop, Length.Px(4))
            .Set(x => x.FontSize, Length.Px(12))
            .Set(x => x.Color, Colors.Success));

        styleSheet.AddRule(Style.Class("invalid-feedback")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.MarginTop, Length.Px(4))
            .Set(x => x.FontSize, Length.Px(12))
            .Set(x => x.Color, Colors.Danger));

        styleSheet.AddRule(Style.Class("input-group")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Row)
            .Set(x => x.AlignItems, AlignItems.Stretch)
            .Set(x => x.Width, Length.Percent(100)));

        styleSheet.AddRule(Style.Class("input-group-control")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexGrow, 1)
            .Set(x => x.FlexShrink, 1)
            .Set(x => x.FlexBasis, 0));

        styleSheet.AddRule(Style.Class("input-group-text")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.AlignItems, AlignItems.Center)
            .Set(x => x.Padding, new Padding(6, 12))
            .Set(x => x.FontSize, Length.Px(14))
            .Set(x => x.Color, Colors.TextDark)
            .Set(x => x.TextAlign, TextAlign.Center)
            .Set(x => x.BackgroundColor, Color.FromHex("e9ecef"))
            .Set(x => x.Border, new Border(1, BorderStyle.Solid, Color.FromHex("ced4da")))
            .Set(x => x.BorderRadius, new BorderRadius(6)));

        styleSheet.AddRule(Style.Class("form-range")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.Width, Length.Percent(100))
            .Set(x => x.Height, Length.Px(24)));

        styleSheet.AddRule(Style.Class("disabled")
            .Set(x => x.BackgroundColor, Color.FromHex("e9ecef"))
            .Set(x => x.Color, Colors.Secondary));
    }

    private static void AddListStyles(StyleSheet styleSheet)
    {
        styleSheet.AddRule(Style.Class("col")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.FlexGrow, 1)
            .Set(x => x.FlexShrink, 1)
            .Set(x => x.FlexBasis, 0)
            .Set(x => x.PaddingRight, Length.Px(15))
            .Set(x => x.PaddingLeft, Length.Px(15)));

        styleSheet.AddRule(Style.Class("list-group")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Column)
            .Set(x => x.PaddingLeft, Length.Px(0))
            .Set(x => x.MarginTop, Length.Px(0))
            .Set(x => x.MarginBottom, Length.Px(20))
            .Set(x => x.BorderRadius, new BorderRadius(6)));

        styleSheet.AddRule(Style.Class("list-group-item")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.PaddingTop, Length.Px(12))
            .Set(x => x.PaddingRight, Length.Px(16))
            .Set(x => x.PaddingBottom, Length.Px(12))
            .Set(x => x.PaddingLeft, Length.Px(16))
            .Set(x => x.Color, Colors.TextDark)
            .Set(x => x.BackgroundColor, Color.White)
            .Set(x => x.Border, new Border(1, BorderStyle.Solid, Color.FromHex("dee2e6")))
            .Set(x => x.MarginTop, Length.Px(-1)));

        styleSheet.AddRule(Style.Class("list-group-item-active")
            .Set(x => x.Color, Color.White)
            .Set(x => x.BackgroundColor, Colors.Primary)
            .Set(x => x.BorderColor, Colors.Primary));

        styleSheet.AddRule(Style.Class("list-group-item-disabled")
            .Set(x => x.Color, Colors.Secondary)
            .Set(x => x.BackgroundColor, Color.White));

        styleSheet.AddRule(Style.Class("list-group-flush")
            .Set(x => x.BorderRadius, new BorderRadius(0)));

        styleSheet.AddRule(Style.Class("list-group-numbered")
            .Set(x => x.PaddingLeft, Length.Px(40)));

        // Contextual list group item variants
        AddListGroupItemVariant(styleSheet, "list-group-item-primary",
            Color.FromHex("cfe2ff"), Color.FromHex("084298"), Color.FromHex("b6d4fe"));
        AddListGroupItemVariant(styleSheet, "list-group-item-secondary",
            Color.FromHex("e2e3e5"), Color.FromHex("41464b"), Color.FromHex("c4c8cb"));
        AddListGroupItemVariant(styleSheet, "list-group-item-success",
            Color.FromHex("d1e7dd"), Color.FromHex("0f5132"), Color.FromHex("badbcc"));
        AddListGroupItemVariant(styleSheet, "list-group-item-danger",
            Color.FromHex("f8d7da"), Color.FromHex("842029"), Color.FromHex("f5c2c7"));
        AddListGroupItemVariant(styleSheet, "list-group-item-warning",
            Color.FromHex("fff3cd"), Color.FromHex("664d03"), Color.FromHex("ffecb5"));
        AddListGroupItemVariant(styleSheet, "list-group-item-info",
            Color.FromHex("cff4fc"), Color.FromHex("055160"), Color.FromHex("b6effb"));
        AddListGroupItemVariant(styleSheet, "list-group-item-light",
            Color.FromHex("fefefe"), Color.FromHex("636464"), Color.FromHex("fdfdfe"));
        AddListGroupItemVariant(styleSheet, "list-group-item-dark",
            Color.FromHex("d3d3d4"), Color.FromHex("141619"), Color.FromHex("bababc"));
    }

    private static void AddListGroupItemVariant(StyleSheet styleSheet, string className, Color bgColor, Color textColor, Color borderColor)
    {
        styleSheet.AddRule(Style.Class(className)
            .Set(x => x.BackgroundColor, bgColor)
            .Set(x => x.Color, textColor)
            .Set(x => x.BorderColor, borderColor));
    }

    private static void AddButtonVariant(StyleSheet styleSheet, string className, Color bgColor, Color textColor)
    {
        styleSheet.AddRule(Style.Class(className)
            .Set(x => x.BackgroundColor, bgColor)
            .Set(x => x.Color, textColor)
            .Set(x => x.BorderColor, bgColor));
    }

    private static void AddButtonHover(StyleSheet styleSheet, string className, Color bgColor, Color borderColor)
    {
        styleSheet.AddRule(Style.Class(className).Hover()
            .Set(x => x.BackgroundColor, bgColor)
            .Set(x => x.BorderColor, borderColor));
    }

    private static void AddOutlineButtonVariant(StyleSheet styleSheet, string className, Color color)
    {
        styleSheet.AddRule(Style.Class(className)
            .Set(x => x.BackgroundColor, Color.Transparent)
            .Set(x => x.Color, color)
            .Set(x => x.BorderColor, color));
    }

    private static void AddTableStyles(StyleSheet styleSheet)
    {
        styleSheet.AddRule(Style.Class("table")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.Width, Length.Percent(100))
            .Set(x => x.MarginBottom, Length.Px(16))
            .Set(x => x.Color, Colors.TextDark)
            .Set(x => x.BorderBottom, new BorderSide(1, BorderStyle.Solid, Color.FromHex("dee2e6"))));

        styleSheet.AddRule(Style.For<TheadElement>()
            .Set(x => x.Display, Display.Block)
            .Set(x => x.BorderBottom, new BorderSide(2, BorderStyle.Solid, Color.FromHex("dee2e6"))));

        styleSheet.AddRule(Style.For<TbodyElement>()
            .Set(x => x.Display, Display.Block));

        styleSheet.AddRule(Style.For<TrElement>()
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Row)
            .Set(x => x.BorderBottom, new BorderSide(1, BorderStyle.Solid, Color.FromHex("dee2e6"))));

        styleSheet.AddRule(Style.For<ThElement>()
            .Set(x => x.Display, Display.Block)
            .Set(x => x.FlexGrow, 1)
            .Set(x => x.FlexShrink, 1)
            .Set(x => x.FlexBasis, 0)
            .Set(x => x.Padding, new Padding(12))
            .Set(x => x.FontWeight, FontWeight.Bold)
            .Set(x => x.TextAlign, TextAlign.Left));

        styleSheet.AddRule(Style.For<TdElement>()
            .Set(x => x.Display, Display.Block)
            .Set(x => x.FlexGrow, 1)
            .Set(x => x.FlexShrink, 1)
            .Set(x => x.FlexBasis, 0)
            .Set(x => x.Padding, new Padding(12)));

        styleSheet.AddRule(Style.Class("table-row-striped")
            .Set(x => x.BackgroundColor, Color.FromHex("f2f2f2")));

        styleSheet.AddRule(Style.Class("table-bordered")
            .Set(x => x.Border, new Border(1, BorderStyle.Solid, Color.FromHex("dee2e6"))));

        styleSheet.AddRule(Style.Class("table-sm")
            .Set(x => x.FontSize, Length.Px(12)));

        styleSheet.AddRule(Style.Class("table-dark")
            .Set(x => x.Color, Color.White)
            .Set(x => x.BackgroundColor, Color.FromHex("212529"))
            .Set(x => x.BorderColor, Color.FromHex("373b3e")));

        // Contextual table row variants
        AddTableRowVariant(styleSheet, "table-primary", Color.FromHex("cfe2ff"), Color.FromHex("084298"));
        AddTableRowVariant(styleSheet, "table-secondary", Color.FromHex("e2e3e5"), Color.FromHex("41464b"));
        AddTableRowVariant(styleSheet, "table-success", Color.FromHex("d1e7dd"), Color.FromHex("0f5132"));
        AddTableRowVariant(styleSheet, "table-danger", Color.FromHex("f8d7da"), Color.FromHex("842029"));
        AddTableRowVariant(styleSheet, "table-warning", Color.FromHex("fff3cd"), Color.FromHex("664d03"));
        AddTableRowVariant(styleSheet, "table-info", Color.FromHex("cff4fc"), Color.FromHex("055160"));
        AddTableRowVariant(styleSheet, "table-light", Color.FromHex("fcfcfd"), Color.FromHex("636464"));
        AddTableRowVariant(styleSheet, "table-dark", Color.FromHex("d3d3d4"), Color.FromHex("141619"));
    }

    private static void AddTableRowVariant(StyleSheet styleSheet, string className, Color bgColor, Color textColor)
    {
        styleSheet.AddRule(Style.Class(className)
            .Set(x => x.BackgroundColor, bgColor)
            .Set(x => x.Color, textColor));
    }

    private static void AddIconStyles(StyleSheet styleSheet)
    {
        styleSheet.AddRule(Style.Class("icon-row")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Row)
            .Set(x => x.MarginBottom, Length.Px(20))
            .Set(x => x.AlignItems, AlignItems.FlexEnd));

        styleSheet.AddRule(Style.Class("icon-item")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Column)
            .Set(x => x.AlignItems, AlignItems.Center)
            .Set(x => x.Margin, new Margin(0, 20, 10, 0))
            .Set(x => x.Width, Length.Px(80)));

        styleSheet.AddRule(Style.Class("bi")
            .Set(x => x.FontFamily, "bootstrap-icons, Arial")
            .Set(x => x.FontSize, Length.Px(24))
            .Set(x => x.Color, Colors.TextDark)
            .Set(x => x.MarginBottom, Length.Px(8)));

        styleSheet.AddRule(Style.Class("icon-label")
            .Set(x => x.FontSize, Length.Px(12))
            .Set(x => x.Color, Colors.Secondary));

        styleSheet.AddRule(Style.Class("icon-sm")
            .Set(x => x.FontSize, Length.Px(16)));

        styleSheet.AddRule(Style.Class("icon-md")
            .Set(x => x.FontSize, Length.Px(24)));

        styleSheet.AddRule(Style.Class("icon-lg")
            .Set(x => x.FontSize, Length.Px(32)));

        styleSheet.AddRule(Style.Class("icon-xl")
            .Set(x => x.FontSize, Length.Px(48)));

        styleSheet.AddRule(Style.Class("icon-primary")
            .Set(x => x.Color, Colors.Primary));

        styleSheet.AddRule(Style.Class("icon-success")
            .Set(x => x.Color, Colors.Success));

        styleSheet.AddRule(Style.Class("icon-danger")
            .Set(x => x.Color, Colors.Danger));

        styleSheet.AddRule(Style.Class("icon-warning")
            .Set(x => x.Color, Colors.Warning));

        styleSheet.AddRule(Style.Class("icon-info")
            .Set(x => x.Color, Colors.Info));

        styleSheet.AddRule(Style.Class("icon-btn")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Row)
            .Set(x => x.AlignItems, AlignItems.Center)
            .Set(x => x.Width, Length.Px(100)));

        styleSheet.AddRule(Style.Class("btn-icon")
            .Set(x => x.FontSize, Length.Px(16))
            .Set(x => x.MarginRight, Length.Px(6))
            .Set(x => x.MarginBottom, Length.Px(0))
            .Set(x => x.Color, Colors.TextWhite));
    }
}
