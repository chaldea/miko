using System.Transactions;
using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class NavbarToken
{
    public NavbarToken(Theme t)
    {
        NavbarPaddingX = Length.Px(0);
        NavbarPaddingY = Length.Rem(0.5f);
        NavbarColor = Color.From(t.EmphasisColor, 0.65f);
        NavbarHoverColor = Color.From(t.EmphasisColor, 0.8f);
        NavbarDisabledColor = Color.From(t.EmphasisColor, 0.3f);
        NavbarActiveColor = Color.From(t.EmphasisColor, 1f);
        NavbarBrandPaddingY = Length.Rem(0.3125f);
        NavbarBrandMarginEnd = Length.Rem(1);
        NavbarBrandFontSize = Length.Rem(1.25f);
        NavbarBrandColor = Color.From(t.EmphasisColor, 1f);
        NavbarBrandHoverColor = Color.From(t.EmphasisColor, 1f);
        NavbarNavLinkPaddingX = Length.Rem(0.5f);
        NavbarTogglerPaddingY = Length.Rem(0.25f);
        NavbarTogglerPaddingX = Length.Rem(0.75f);
        NavbarTogglerFontSize = Length.Rem(1.25f);
        // NavbarTogglerIconBg = 
        NavbarTogglerBorderColor = Color.From(t.EmphasisColor, 0.15f);
        NavbarTogglerBorderRadius = t.BorderRadius;
        NavbarTogglerFocusWidth = Length.Rem(0.25f);
        NavbarTogglerTransition = Transition.For(x => x.BoxShadow).Duration(0.15f).EaseInOut();
    }

    public Length NavbarPaddingX { get; set; }
    public Length NavbarPaddingY { get; set; }
    public Color NavbarColor { get; set; }
    public Color NavbarHoverColor { get; set; }
    public Color NavbarDisabledColor { get; set; }
    public Color NavbarActiveColor { get; set; }

    public Length NavbarBrandPaddingY { get; set; }
    public Length NavbarBrandMarginEnd { get; set; }
    public Length NavbarBrandFontSize { get; set; }
    public Color NavbarBrandColor { get; set; }
    public Color NavbarBrandHoverColor { get; set; }

    public Length NavbarNavLinkPaddingX { get; set; }

    public Length NavbarTogglerPaddingY { get; set; }
    public Length NavbarTogglerPaddingX { get; set; }
    public Length NavbarTogglerFontSize { get; set; }
    public BackgroundImage? NavbarTogglerIconBg { get; set; }
    public Color NavbarTogglerBorderColor { get; set; }
    public float NavbarTogglerBorderRadius { get; set; }
    public Length NavbarTogglerFocusWidth { get; set; }
    public Transition NavbarTogglerTransition { get; set; }
}

internal static class NavbarStyles
{
    internal static CssObject GenStyle(NavbarToken t)
    {
        var containerFlexProperties = new CssObject()
        {
            Display = Display.Flex,
            FlexWrap = FlexWrap.Nowrap,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.SpaceBetween
        };
        return new CssObject
        {
            [".navbar"] = new()
            {
                Position = Position.Relative,
                Display = Display.Flex,
                FlexWrap = FlexWrap.Wrap,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceBetween,
                Padding = new Padding(t.NavbarPaddingY, t.NavbarPaddingX),

                ["> .container, > .container-fluid"] = containerFlexProperties,
            },

            [".navbar-brand"] = new()
            {
                PaddingTop = Length.Rem(0.3125f),
                PaddingBottom = Length.Rem(0.3125f),
                MarginRight = t.NavbarBrandMarginEnd,
                FontSize = t.NavbarBrandFontSize,
                Color = t.NavbarBrandColor,
                TextDecoration = TextDecoration.None,
                // white-space: nowrap;
                ["&:hover, &:focus"] = new()
                {
                    Color = t.NavbarBrandHoverColor,
                    TextDecoration = TextDecoration.None
                }
            },

            [".navbar-nav"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                PaddingLeft = Length.Px(0),
                MarginBottom = Length.Px(0),
                // list-style: none;

                [".nav-link"] = new()
                {
                    ["&.active,&.show"] = new()
                    {
                        Color = t.NavbarActiveColor,
                    }
                },

                [".dropdown-menu"] = new()
                {
                    Position = Position.Static,
                }
            }
        };
    }
}
