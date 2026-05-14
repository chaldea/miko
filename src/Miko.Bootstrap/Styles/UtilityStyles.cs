using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class UtilityStyles
{
    private static readonly float[] SpacingScale = [0, 4, 8, 16, 24, 48];

    internal static void Apply(StyleSheet sheet, Theme t)
    {
        ApplyDisplay(sheet);
        ApplyFlex(sheet);
        ApplySpacing(sheet);
        ApplySizing(sheet);
        ApplyText(sheet);
        ApplyColors(sheet, t);
        ApplyBorders(sheet, t);
        ApplyPosition(sheet);
        ApplyOverflow(sheet);
        ApplyOpacity(sheet);
        ApplyVisibility(sheet);
        ApplyMisc(sheet, t);
    }

    private static void ApplyDisplay(StyleSheet sheet)
    {
        sheet.AddRule(Style.Class("d-none").Set(x => x.Display, Display.None));
        sheet.AddRule(Style.Class("d-block").Set(x => x.Display, Display.Block));
        sheet.AddRule(Style.Class("d-inline").Set(x => x.Display, Display.Inline));
        sheet.AddRule(Style.Class("d-inline-block").Set(x => x.Display, Display.InlineBlock));
        sheet.AddRule(Style.Class("d-flex").Set(x => x.Display, Display.Flex));
        sheet.AddRule(Style.Class("d-inline-flex").Set(x => x.Display, Display.Flex));
    }

    private static void ApplyFlex(StyleSheet sheet)
    {
        sheet.AddRule(Style.Class("flex-row").Set(x => x.FlexDirection, FlexDirection.Row));
        sheet.AddRule(Style.Class("flex-column").Set(x => x.FlexDirection, FlexDirection.Column));
        sheet.AddRule(Style.Class("flex-row-reverse").Set(x => x.FlexDirection, FlexDirection.RowReverse));
        sheet.AddRule(Style.Class("flex-column-reverse").Set(x => x.FlexDirection, FlexDirection.ColumnReverse));
        sheet.AddRule(Style.Class("flex-wrap").Set(x => x.FlexWrap, FlexWrap.Wrap));
        sheet.AddRule(Style.Class("flex-nowrap").Set(x => x.FlexWrap, FlexWrap.Nowrap));
        sheet.AddRule(Style.Class("flex-wrap-reverse").Set(x => x.FlexWrap, FlexWrap.WrapReverse));
        sheet.AddRule(Style.Class("flex-fill").Set(x => x.FlexGrow, 1).Set(x => x.FlexShrink, 1).Set(x => x.FlexBasis, Length.Auto));
        sheet.AddRule(Style.Class("flex-grow-0").Set(x => x.FlexGrow, 0));
        sheet.AddRule(Style.Class("flex-grow-1").Set(x => x.FlexGrow, 1));
        sheet.AddRule(Style.Class("flex-shrink-0").Set(x => x.FlexShrink, 0));
        sheet.AddRule(Style.Class("flex-shrink-1").Set(x => x.FlexShrink, 1));

        sheet.AddRule(Style.Class("justify-content-start").Set(x => x.JustifyContent, JustifyContent.FlexStart));
        sheet.AddRule(Style.Class("justify-content-end").Set(x => x.JustifyContent, JustifyContent.FlexEnd));
        sheet.AddRule(Style.Class("justify-content-center").Set(x => x.JustifyContent, JustifyContent.Center));
        sheet.AddRule(Style.Class("justify-content-between").Set(x => x.JustifyContent, JustifyContent.SpaceBetween));
        sheet.AddRule(Style.Class("justify-content-around").Set(x => x.JustifyContent, JustifyContent.SpaceAround));
        sheet.AddRule(Style.Class("justify-content-evenly").Set(x => x.JustifyContent, JustifyContent.SpaceEvenly));

        sheet.AddRule(Style.Class("align-items-start").Set(x => x.AlignItems, AlignItems.FlexStart));
        sheet.AddRule(Style.Class("align-items-end").Set(x => x.AlignItems, AlignItems.FlexEnd));
        sheet.AddRule(Style.Class("align-items-center").Set(x => x.AlignItems, AlignItems.Center));
        sheet.AddRule(Style.Class("align-items-baseline").Set(x => x.AlignItems, AlignItems.Baseline));
        sheet.AddRule(Style.Class("align-items-stretch").Set(x => x.AlignItems, AlignItems.Stretch));

        sheet.AddRule(Style.Class("align-self-auto").Set(x => x.AlignSelf, AlignSelf.Auto));
        sheet.AddRule(Style.Class("align-self-start").Set(x => x.AlignSelf, AlignSelf.FlexStart));
        sheet.AddRule(Style.Class("align-self-end").Set(x => x.AlignSelf, AlignSelf.FlexEnd));
        sheet.AddRule(Style.Class("align-self-center").Set(x => x.AlignSelf, AlignSelf.Center));
        sheet.AddRule(Style.Class("align-self-baseline").Set(x => x.AlignSelf, AlignSelf.Baseline));
        sheet.AddRule(Style.Class("align-self-stretch").Set(x => x.AlignSelf, AlignSelf.Stretch));

        sheet.AddRule(Style.Class("align-content-start").Set(x => x.AlignContent, AlignContent.FlexStart));
        sheet.AddRule(Style.Class("align-content-end").Set(x => x.AlignContent, AlignContent.FlexEnd));
        sheet.AddRule(Style.Class("align-content-center").Set(x => x.AlignContent, AlignContent.Center));
        sheet.AddRule(Style.Class("align-content-between").Set(x => x.AlignContent, AlignContent.SpaceBetween));
        sheet.AddRule(Style.Class("align-content-around").Set(x => x.AlignContent, AlignContent.SpaceAround));
        sheet.AddRule(Style.Class("align-content-stretch").Set(x => x.AlignContent, AlignContent.Stretch));

        for (int i = 0; i < SpacingScale.Length; i++)
        {
            float v = SpacingScale[i];
            sheet.AddRule(Style.Class($"gap-{i}").Set(x => x.Gap, Length.Px(v)));
            sheet.AddRule(Style.Class($"row-gap-{i}").Set(x => x.RowGap, Length.Px(v)));
            sheet.AddRule(Style.Class($"column-gap-{i}").Set(x => x.ColumnGap, Length.Px(v)));
        }
    }

    private static void ApplySpacing(StyleSheet sheet)
    {
        for (int i = 0; i < SpacingScale.Length; i++)
        {
            float v = SpacingScale[i];
            sheet.AddRule(Style.Class($"m-{i}").Set(x => x.Margin, new Margin(v)));
            sheet.AddRule(Style.Class($"mt-{i}").Set(x => x.MarginTop, Length.Px(v)));
            sheet.AddRule(Style.Class($"mb-{i}").Set(x => x.MarginBottom, Length.Px(v)));
            sheet.AddRule(Style.Class($"ms-{i}").Set(x => x.MarginLeft, Length.Px(v)));
            sheet.AddRule(Style.Class($"me-{i}").Set(x => x.MarginRight, Length.Px(v)));
            sheet.AddRule(Style.Class($"mx-{i}").Set(x => x.MarginLeft, Length.Px(v)).Set(x => x.MarginRight, Length.Px(v)));
            sheet.AddRule(Style.Class($"my-{i}").Set(x => x.MarginTop, Length.Px(v)).Set(x => x.MarginBottom, Length.Px(v)));

            sheet.AddRule(Style.Class($"p-{i}").Set(x => x.Padding, new Padding(v)));
            sheet.AddRule(Style.Class($"pt-{i}").Set(x => x.PaddingTop, Length.Px(v)));
            sheet.AddRule(Style.Class($"pb-{i}").Set(x => x.PaddingBottom, Length.Px(v)));
            sheet.AddRule(Style.Class($"ps-{i}").Set(x => x.PaddingLeft, Length.Px(v)));
            sheet.AddRule(Style.Class($"pe-{i}").Set(x => x.PaddingRight, Length.Px(v)));
            sheet.AddRule(Style.Class($"px-{i}").Set(x => x.PaddingLeft, Length.Px(v)).Set(x => x.PaddingRight, Length.Px(v)));
            sheet.AddRule(Style.Class($"py-{i}").Set(x => x.PaddingTop, Length.Px(v)).Set(x => x.PaddingBottom, Length.Px(v)));
        }

        sheet.AddRule(Style.Class("m-auto").Set(x => x.Margin, new Margin(Length.Auto)));
        sheet.AddRule(Style.Class("mt-auto").Set(x => x.MarginTop, Length.Auto));
        sheet.AddRule(Style.Class("mb-auto").Set(x => x.MarginBottom, Length.Auto));
        sheet.AddRule(Style.Class("ms-auto").Set(x => x.MarginLeft, Length.Auto));
        sheet.AddRule(Style.Class("me-auto").Set(x => x.MarginRight, Length.Auto));
        sheet.AddRule(Style.Class("mx-auto").Set(x => x.MarginLeft, Length.Auto).Set(x => x.MarginRight, Length.Auto));
        sheet.AddRule(Style.Class("my-auto").Set(x => x.MarginTop, Length.Auto).Set(x => x.MarginBottom, Length.Auto));
    }

    private static void ApplySizing(StyleSheet sheet)
    {
        foreach (int pct in new[] { 25, 50, 75, 100 })
        {
            sheet.AddRule(Style.Class($"w-{pct}").Set(x => x.Width, Length.Percent(pct)));
            sheet.AddRule(Style.Class($"h-{pct}").Set(x => x.Height, Length.Percent(pct)));
        }
        sheet.AddRule(Style.Class("w-auto").Set(x => x.Width, Length.Auto));
        sheet.AddRule(Style.Class("h-auto").Set(x => x.Height, Length.Auto));
        sheet.AddRule(Style.Class("mw-100").Set(x => x.MaxWidth, Length.Percent(100)));
        sheet.AddRule(Style.Class("mh-100").Set(x => x.MaxHeight, Length.Percent(100)));
        sheet.AddRule(Style.Class("min-vw-100").Set(x => x.MinWidth, Length.Percent(100)));
        sheet.AddRule(Style.Class("min-vh-100").Set(x => x.MinHeight, Length.Percent(100)));
    }

    private static void ApplyText(StyleSheet sheet)
    {
        sheet.AddRule(Style.Class("text-start").Set(x => x.TextAlign, TextAlign.Left));
        sheet.AddRule(Style.Class("text-center").Set(x => x.TextAlign, TextAlign.Center));
        sheet.AddRule(Style.Class("text-end").Set(x => x.TextAlign, TextAlign.Right));
        sheet.AddRule(Style.Class("text-justify").Set(x => x.TextAlign, TextAlign.Justify));

        sheet.AddRule(Style.Class("text-uppercase").Set(x => x.TextTransform, TextTransform.Uppercase));
        sheet.AddRule(Style.Class("text-lowercase").Set(x => x.TextTransform, TextTransform.Lowercase));
        sheet.AddRule(Style.Class("text-capitalize").Set(x => x.TextTransform, TextTransform.Capitalize));

        sheet.AddRule(Style.Class("text-nowrap").Set(x => x.WhiteSpace, WhiteSpace.Nowrap));
        sheet.AddRule(Style.Class("text-wrap").Set(x => x.WhiteSpace, WhiteSpace.Normal));

        sheet.AddRule(Style.Class("text-decoration-none").Set(x => x.TextDecoration, TextDecoration.None));
        sheet.AddRule(Style.Class("text-decoration-underline").Set(x => x.TextDecoration, TextDecoration.Underline));
        sheet.AddRule(Style.Class("text-decoration-line-through").Set(x => x.TextDecoration, TextDecoration.LineThrough));

        sheet.AddRule(Style.Class("fst-italic").Set(x => x.FontStyle, FontStyle.Italic));
        sheet.AddRule(Style.Class("fst-normal").Set(x => x.FontStyle, FontStyle.Normal));

        sheet.AddRule(Style.Class("fw-lighter").Set(x => x.FontWeight, FontWeight.Lighter));
        sheet.AddRule(Style.Class("fw-light").Set(x => x.FontWeight, FontWeight.Light));
        sheet.AddRule(Style.Class("fw-normal").Set(x => x.FontWeight, FontWeight.Normal));
        sheet.AddRule(Style.Class("fw-medium").Set(x => x.FontWeight, FontWeight.Medium));
        sheet.AddRule(Style.Class("fw-semibold").Set(x => x.FontWeight, FontWeight.SemiBold));
        sheet.AddRule(Style.Class("fw-bold").Set(x => x.FontWeight, FontWeight.Bold));
        sheet.AddRule(Style.Class("fw-bolder").Set(x => x.FontWeight, FontWeight.Bolder));

        // Font sizes: fs-1 (40px) through fs-6 (16px)
        float[] fsSizes = [40, 32, 28, 24, 20, 16];
        for (int i = 0; i < fsSizes.Length; i++)
            sheet.AddRule(Style.Class($"fs-{i + 1}").Set(x => x.FontSize, Length.Px(fsSizes[i])));

        // Line heights
        sheet.AddRule(Style.Class("lh-1").Set(x => x.LineHeight, Length.Px(16)));
        sheet.AddRule(Style.Class("lh-sm").Set(x => x.LineHeight, Length.Px(20)));
        sheet.AddRule(Style.Class("lh-base").Set(x => x.LineHeight, Length.Px(24)));
        sheet.AddRule(Style.Class("lh-lg").Set(x => x.LineHeight, Length.Px(32)));
    }

    private static void ApplyColors(StyleSheet sheet, Theme t)
    {
        var textColors = new[]
        {
            ("text-primary",   t.Primary),
            ("text-secondary", t.Secondary),
            ("text-success",   t.Success),
            ("text-danger",    t.Danger),
            ("text-warning",   t.Warning),
            ("text-info",      t.Info),
            ("text-light",     t.Light),
            ("text-dark",      t.Dark),
            ("text-white",     Color.White),
            ("text-black",     Color.Black),
            ("text-body",      t.BodyColor),
            ("text-muted",     t.SecondaryColor),
        };
        foreach (var (cls, color) in textColors)
            sheet.AddRule(Style.Class(cls).Set(x => x.Color, color));

        var bgColors = new[]
        {
            ("bg-primary",     t.Primary),
            ("bg-secondary",   t.Secondary),
            ("bg-success",     t.Success),
            ("bg-danger",      t.Danger),
            ("bg-warning",     t.Warning),
            ("bg-info",        t.Info),
            ("bg-light",       t.Light),
            ("bg-dark",        t.Dark),
            ("bg-white",       Color.White),
            ("bg-black",       Color.Black),
            ("bg-transparent", Color.Transparent),
            ("bg-body",        t.BodyBg),
        };
        foreach (var (cls, color) in bgColors)
            sheet.AddRule(Style.Class(cls).Set(x => x.BackgroundColor, color));
    }

    private static void ApplyBorders(StyleSheet sheet, Theme t)
    {
        sheet.AddRule(Style.Class("border").Set(x => x.Border, new Border(t.BorderWidth, BorderStyle.Solid, t.BorderColor)));
        sheet.AddRule(Style.Class("border-0").Set(x => x.Border, Border.None));
        sheet.AddRule(Style.Class("border-top").Set(x => x.BorderTop, new BorderSide(t.BorderWidth, BorderStyle.Solid, t.BorderColor)));
        sheet.AddRule(Style.Class("border-bottom").Set(x => x.BorderBottom, new BorderSide(t.BorderWidth, BorderStyle.Solid, t.BorderColor)));
        sheet.AddRule(Style.Class("border-start").Set(x => x.BorderLeft, new BorderSide(t.BorderWidth, BorderStyle.Solid, t.BorderColor)));
        sheet.AddRule(Style.Class("border-end").Set(x => x.BorderRight, new BorderSide(t.BorderWidth, BorderStyle.Solid, t.BorderColor)));
        sheet.AddRule(Style.Class("border-top-0").Set(x => x.BorderTop, BorderSide.None));
        sheet.AddRule(Style.Class("border-bottom-0").Set(x => x.BorderBottom, BorderSide.None));
        sheet.AddRule(Style.Class("border-start-0").Set(x => x.BorderLeft, BorderSide.None));
        sheet.AddRule(Style.Class("border-end-0").Set(x => x.BorderRight, BorderSide.None));

        var borderColors = new[]
        {
            ("border-primary",   t.Primary),
            ("border-secondary", t.Secondary),
            ("border-success",   t.Success),
            ("border-danger",    t.Danger),
            ("border-warning",   t.Warning),
            ("border-info",      t.Info),
            ("border-light",     t.Light),
            ("border-dark",      t.Dark),
            ("border-white",     Color.White),
            ("border-black",     Color.Black),
        };
        foreach (var (cls, color) in borderColors)
            sheet.AddRule(Style.Class(cls).Set(x => x.BorderColor, color));

        // Border widths
        for (int i = 1; i <= 5; i++)
            sheet.AddRule(Style.Class($"border-{i}").Set(x => x.BorderWidth, Length.Px(i)));

        // Rounded
        sheet.AddRule(Style.Class("rounded").Set(x => x.BorderRadius, new BorderRadius(t.BorderRadius)));
        sheet.AddRule(Style.Class("rounded-0").Set(x => x.BorderRadius, new BorderRadius(0)));
        sheet.AddRule(Style.Class("rounded-1").Set(x => x.BorderRadius, new BorderRadius(t.BorderRadiusSm)));
        sheet.AddRule(Style.Class("rounded-2").Set(x => x.BorderRadius, new BorderRadius(t.BorderRadius)));
        sheet.AddRule(Style.Class("rounded-3").Set(x => x.BorderRadius, new BorderRadius(t.BorderRadiusLg)));
        sheet.AddRule(Style.Class("rounded-4").Set(x => x.BorderRadius, new BorderRadius(t.BorderRadiusXl)));
        sheet.AddRule(Style.Class("rounded-5").Set(x => x.BorderRadius, new BorderRadius(t.BorderRadiusXxl)));
        sheet.AddRule(Style.Class("rounded-circle").Set(x => x.BorderRadius, new BorderRadius(Length.Percent(50))));
        sheet.AddRule(Style.Class("rounded-pill").Set(x => x.BorderRadius, new BorderRadius(t.BorderRadiusPill)));
        sheet.AddRule(Style.Class("rounded-top").Set(x => x.BorderTopLeftRadius, Length.Px(t.BorderRadius)).Set(x => x.BorderTopRightRadius, Length.Px(t.BorderRadius)));
        sheet.AddRule(Style.Class("rounded-bottom").Set(x => x.BorderBottomLeftRadius, Length.Px(t.BorderRadius)).Set(x => x.BorderBottomRightRadius, Length.Px(t.BorderRadius)));
        sheet.AddRule(Style.Class("rounded-start").Set(x => x.BorderTopLeftRadius, Length.Px(t.BorderRadius)).Set(x => x.BorderBottomLeftRadius, Length.Px(t.BorderRadius)));
        sheet.AddRule(Style.Class("rounded-end").Set(x => x.BorderTopRightRadius, Length.Px(t.BorderRadius)).Set(x => x.BorderBottomRightRadius, Length.Px(t.BorderRadius)));
    }

    private static void ApplyPosition(StyleSheet sheet)
    {
        sheet.AddRule(Style.Class("position-static").Set(x => x.Position, Position.Static));
        sheet.AddRule(Style.Class("position-relative").Set(x => x.Position, Position.Relative));
        sheet.AddRule(Style.Class("position-absolute").Set(x => x.Position, Position.Absolute));
        sheet.AddRule(Style.Class("position-fixed").Set(x => x.Position, Position.Fixed));

        foreach (int pct in new[] { 0, 50, 100 })
        {
            sheet.AddRule(Style.Class($"top-{pct}").Set(x => x.Top, Length.Percent(pct)));
            sheet.AddRule(Style.Class($"bottom-{pct}").Set(x => x.Bottom, Length.Percent(pct)));
            sheet.AddRule(Style.Class($"start-{pct}").Set(x => x.Left, Length.Percent(pct)));
            sheet.AddRule(Style.Class($"end-{pct}").Set(x => x.Right, Length.Percent(pct)));
        }
    }

    private static void ApplyOverflow(StyleSheet sheet)
    {
        sheet.AddRule(Style.Class("overflow-auto").Set(x => x.OverflowX, Overflow.Auto).Set(x => x.OverflowY, Overflow.Auto));
        sheet.AddRule(Style.Class("overflow-hidden").Set(x => x.OverflowX, Overflow.Hidden).Set(x => x.OverflowY, Overflow.Hidden));
        sheet.AddRule(Style.Class("overflow-visible").Set(x => x.OverflowX, Overflow.Visible).Set(x => x.OverflowY, Overflow.Visible));
        sheet.AddRule(Style.Class("overflow-scroll").Set(x => x.OverflowX, Overflow.Scroll).Set(x => x.OverflowY, Overflow.Scroll));
        sheet.AddRule(Style.Class("overflow-x-auto").Set(x => x.OverflowX, Overflow.Auto));
        sheet.AddRule(Style.Class("overflow-x-hidden").Set(x => x.OverflowX, Overflow.Hidden));
        sheet.AddRule(Style.Class("overflow-y-auto").Set(x => x.OverflowY, Overflow.Auto));
        sheet.AddRule(Style.Class("overflow-y-hidden").Set(x => x.OverflowY, Overflow.Hidden));
    }

    private static void ApplyOpacity(StyleSheet sheet)
    {
        sheet.AddRule(Style.Class("opacity-0").Set(x => x.Opacity, 0f));
        sheet.AddRule(Style.Class("opacity-25").Set(x => x.Opacity, 0.25f));
        sheet.AddRule(Style.Class("opacity-50").Set(x => x.Opacity, 0.5f));
        sheet.AddRule(Style.Class("opacity-75").Set(x => x.Opacity, 0.75f));
        sheet.AddRule(Style.Class("opacity-100").Set(x => x.Opacity, 1f));
    }

    private static void ApplyVisibility(StyleSheet sheet)
    {
        sheet.AddRule(Style.Class("visible").Set(x => x.Visibility, Visibility.Visible));
        sheet.AddRule(Style.Class("invisible").Set(x => x.Visibility, Visibility.Hidden));
    }

    private static void ApplyMisc(StyleSheet sheet, Theme t)
    {
        sheet.AddRule(Style.Class("shadow-none").Set(x => x.BoxShadow, null));
        sheet.AddRule(Style.Class("shadow-sm").Set(x => x.BoxShadow, new BoxShadow(0, 2, 4, 0, Color.FromRgba(0, 0, 0, 19))));
        sheet.AddRule(Style.Class("shadow").Set(x => x.BoxShadow, new BoxShadow(0, 8, 16, 0, Color.FromRgba(0, 0, 0, 38))));
        sheet.AddRule(Style.Class("shadow-lg").Set(x => x.BoxShadow, new BoxShadow(0, 16, 48, 0, Color.FromRgba(0, 0, 0, 45))));

        sheet.AddRule(Style.Class("user-select-none").Set(x => x.UserSelect, UserSelect.None));
        sheet.AddRule(Style.Class("user-select-auto").Set(x => x.UserSelect, UserSelect.Auto));
        sheet.AddRule(Style.Class("user-select-all").Set(x => x.UserSelect, UserSelect.All));

        sheet.AddRule(Style.Class("cursor-pointer").Set(x => x.Cursor, Cursor.Pointer));
        sheet.AddRule(Style.Class("cursor-default").Set(x => x.Cursor, Cursor.Default));
        sheet.AddRule(Style.Class("cursor-not-allowed").Set(x => x.Cursor, Cursor.NotAllowed));

        // Z-index
        sheet.AddRule(Style.Class("z-0").Set(x => x.ZIndex, 0));
        sheet.AddRule(Style.Class("z-1").Set(x => x.ZIndex, 1));
        sheet.AddRule(Style.Class("z-2").Set(x => x.ZIndex, 2));
        sheet.AddRule(Style.Class("z-3").Set(x => x.ZIndex, 3));
        sheet.AddRule(Style.Class("z-n1").Set(x => x.ZIndex, -1));
    }
}
