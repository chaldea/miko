using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the <c>ion-select</c> family: the select host itself, the (hidden)
/// <c>ion-select-option</c> data carriers, and the two overlay bodies
/// <c>ion-select-popover</c> / <c>ion-select-modal</c>.
/// <para>
/// Ports select.scss / select.md.scss / select.ios.scss / select.md.outline.scss plus
/// select-popover.scss / select-popover.md.scss and select-modal.scss.
/// </para>
/// </summary>
internal static class SelectStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            [$".ion-select.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.SelectMinHeight),
                Color = t.SelectTextColor,
                FontSize = Length.Px(t.SelectFontSize),
            },

            // select.scss :host(.in-item) { flex: 1 1 0 }. Without this the full-width select
            // cannot shrink with the other content in an ion-item and wraps onto another line.
            [$".ion-select.{mode}.in-item"] = new()
            {
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Px(0),
            },

            // Floating and stacked labels need extra vertical room above the selected value.
            [$".ion-select.{mode}.select-label-placement-floating"] = new()
            {
                MinHeight = Length.Px(56),
            },
            [$".ion-select.{mode}.select-label-placement-stacked"] = new()
            {
                MinHeight = Length.Px(56),
            },

            // select.scss :host(.select-disabled) { pointer-events: none } — the opacity dim is the
            // ported equivalent of Ionic's per-mode disabled opacity.
            [$".ion-select.{mode}.select-disabled"] = new()
            {
                Opacity = 0.4f,
                PointerEvents = PointerEvents.None,
            },

            [$".ion-select.{mode} .select-wrapper"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                FlexGrow = 1,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceBetween,
                Position = Position.Relative,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.SelectMinHeight),
                Cursor = Cursor.Pointer,
            },

            [$".ion-select.{mode} .select-wrapper-inner"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Auto,
                Width = Length.Percent(100),
                MinWidth = Length.Px(0),
                MinHeight = Length.Px(t.SelectMinHeight),
                PaddingTop = Length.Px(t.SelectPaddingTop),
                PaddingRight = Length.Px(t.SelectPaddingEnd),
                PaddingBottom = Length.Px(t.SelectPaddingBottom),
                PaddingLeft = Length.Px(t.SelectPaddingStart),
                BoxSizing = BoxSizing.BorderBox,
            },

            [$".ion-select.{mode} .label-text-wrapper"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                FlexShrink = 0,
                MaxWidth = Length.Px(200),
                Color = t.SelectLabelColor,
            },

            // select.scss: an empty label slot is hidden so it adds no margins.
            [$".ion-select.{mode} .label-text-wrapper-hidden"] = new()
            {
                Display = Display.None,
            },

            // select.scss .label-text: ellipsize rather than wrap (matters for label-placement=fixed).
            [$".ion-select.{mode} .label-text"] = new()
            {
                WhiteSpace = WhiteSpace.Nowrap,
                Overflow = Overflow.Hidden,
                TextOverflow = TextOverflow.Ellipsis,
            },

            [$".ion-select.{mode} .native-wrapper"] = new()
            {
                Display = Display.Flex,
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Percent(0),
                Position = Position.Relative,
                AlignItems = AlignItems.Center,
                MinWidth = Length.Px(0),
                // select.scss: keeps .select-text truncating with ellipses instead of overflowing.
                Overflow = Overflow.Hidden,
            },

            // select.scss `button { @include visually-hidden() }` — the focus target fills the
            // native wrapper but is fully transparent. It is the element that takes focus; the
            // visible text sits beneath it.
            [$".ion-select.{mode} .select-focus-el"] = new()
            {
                Position = Position.Absolute,
                Left = Length.Px(0),
                Right = Length.Px(0),
                Top = Length.Px(0),
                Bottom = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                Margin = new Margin(Length.Px(0)),
                Padding = new Padding(Length.Px(0)),
                BorderWidth = Length.Px(0),
                BackgroundColor = Color.Transparent,
                Opacity = 0f,
                Overflow = Overflow.Hidden,
                Cursor = Cursor.Pointer,
            },

            [$".ion-select.{mode} .select-text"] = new()
            {
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Percent(0),
                MinWidth = Length.Px(0),
                Color = t.SelectTextColor,
                WhiteSpace = WhiteSpace.Nowrap,
                Overflow = Overflow.Hidden,
                TextOverflow = TextOverflow.Ellipsis,
            },

            [$".ion-select.{mode} .select-placeholder"] = new()
            {
                Color = t.SelectPlaceholderColor,
            },

            [$".ion-select.{mode} .select-icon"] = new()
            {
                Width = Length.Px(20),
                Height = Length.Px(20),
                MarginLeft = Length.Px(8),
                FlexShrink = 0,
            },

            [$".ion-select.{mode} .select-highlight"] = new()
            {
                Height = Length.Px(2),
                BackgroundColor = t.SelectHighlightColor,
                Width = Length.Percent(100),
            },

            // Label placement - start (default): label before the value and margin on its end.
            [$".ion-select.{mode}.select-label-placement-start .select-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Row,
            },
            [$".ion-select.{mode}.select-label-placement-start .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(16),
                MarginLeft = Length.Px(0),
            },

            // Label placement - end: reverse the row and put the gap on the label's start edge.
            [$".ion-select.{mode}.select-label-placement-end .select-wrapper"] = new()
            {
                FlexDirection = FlexDirection.RowReverse,
            },
            [$".ion-select.{mode}.select-label-placement-end .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(0),
                MarginLeft = Length.Px(16),
            },

            // Label placement - fixed: the start label owns a stable 100px column.
            [$".ion-select.{mode}.select-label-placement-fixed .select-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Row,
            },
            [$".ion-select.{mode}.select-label-placement-fixed .label-text-wrapper"] = new()
            {
                MarginRight = Length.Px(16),
                MarginLeft = Length.Px(0),
                FlexGrow = 0,
                FlexShrink = 0,
                FlexBasis = Length.Px(100),
                Width = Length.Px(100),
                MinWidth = Length.Px(100),
                MaxWidth = Length.Px(200),
            },

            // Label placement - stacked/floating: label above the value, aligned to the start.
            [$".ion-select.{mode}.select-label-placement-stacked .select-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.FlexStart,
            },
            [$".ion-select.{mode}.select-label-placement-floating .select-wrapper"] = new()
            {
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.FlexStart,
            },
            [$".ion-select.{mode}.select-label-placement-stacked .label-text-wrapper"] = new()
            {
                MaxWidth = Length.Percent(100),
                FontSize = Length.Px(12),
                MarginBottom = Length.Px(2),
                TransformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(0)),
            },
            [$".ion-select.{mode}.select-label-placement-floating .label-text-wrapper"] = new()
            {
                MaxWidth = Length.Percent(100),
                FontSize = Length.Px(12),
                MarginBottom = Length.Px(2),
                TransformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(0)),
                Transform = new Transform(
                    new TransformFunction.TranslateY(Length.Percent(100)),
                    new TransformFunction.Scale(1f, 1f)),
            },
            [$".ion-select.{mode}.label-floating .label-text-wrapper"] = new()
            {
                Transform = new Transform(
                    new TransformFunction.TranslateY(Length.Percent(50)),
                    new TransformFunction.Scale(0.75f, 0.75f)),
            },
            [$".ion-select.{mode}.select-label-placement-stacked .select-wrapper-inner"] = new()
            {
                Width = Length.Percent(100),
            },
            [$".ion-select.{mode}.select-label-placement-floating .select-wrapper-inner"] = new()
            {
                Width = Length.Percent(100),
            },
            [$".ion-select.{mode}.select-label-placement-stacked .native-wrapper"] = new()
            {
                MarginTop = Length.Px(1),
                Width = Length.Percent(100),
            },
            [$".ion-select.{mode}.select-label-placement-floating .native-wrapper"] = new()
            {
                MarginTop = Length.Px(1),
                Width = Length.Percent(100),
            },
            [$".ion-select.{mode}.select-label-placement-stacked .select-icon"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Height = Length.Percent(100),
            },
            [$".ion-select.{mode}.select-label-placement-floating .select-icon"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Height = Length.Percent(100),
            },

            [$".ion-select.{mode}.select-fill-solid .select-wrapper-inner"] = new()
            {
                BackgroundColor = mode == "md" ? new Color(0, 0, 0, 10) : t.SelectBackground,
                BorderRadius = new BorderRadius(Length.Px(t.SelectBorderRadius)),
                PaddingLeft = Length.Px(12),
                PaddingRight = Length.Px(12),
            },

            [$".ion-select.{mode}.select-fill-outline .select-wrapper-inner"] = new()
            {
                BorderWidth = Length.Px(1),
                BorderStyle = BorderStyle.Solid,
                BorderColor = t.SelectBorderColor,
                BorderRadius = new BorderRadius(Length.Px(t.SelectBorderRadius)),
                PaddingLeft = Length.Px(12),
                PaddingRight = Length.Px(12),
            },

            [$".ion-select.{mode}.select-shape-round .select-wrapper-inner"] = new()
            {
                BorderRadius = new BorderRadius(Length.Px(t.SelectRoundBorderRadius)),
            },

            [$".ion-select.{mode} .select-bottom"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                PaddingTop = Length.Px(4),
                FontSize = Length.Px(12),
            },

            [$".ion-select.{mode} .helper-text"] = new()
            {
                Color = t.SelectHelperColor,
            },

            [$".ion-select.{mode} .error-text"] = new()
            {
                Color = t.SelectErrorColor,
            },

            // select-option.scss `:host { display: none }` — the option is a data carrier only; the
            // real, selectable UI lives in the overlay the select opens.
            [$".ion-select-option.{mode}"] = new()
            {
                Display = Display.None,
            },
        };

        AddJustify(css, mode);
        AddOutlineContainer(css, mode, t);
        AddOverlayBodies(css, mode, t);

        AddColor(css, mode, "primary", t.Primary);
        AddColor(css, mode, "secondary", t.Secondary);
        AddColor(css, mode, "tertiary", t.Tertiary);
        AddColor(css, mode, "success", t.Success);
        AddColor(css, mode, "warning", t.Warning);
        AddColor(css, mode, "danger", t.Danger);
        AddColor(css, mode, "light", t.Light);
        AddColor(css, mode, "medium", t.Medium);
        AddColor(css, mode, "dark", t.Dark);

        return css;
    }

    /// <summary>
    /// select.scss packs the label and select within the line via <c>justify-content</c>. Ionic
    /// only applies this when the label is not floating/stacked; the component already withholds
    /// the class in that case, so the rules can be unconditional here.
    /// </summary>
    private static void AddJustify(CssObject css, string mode)
    {
        css[$".ion-select.{mode}.select-justify-start .select-wrapper"] = new()
        {
            JustifyContent = JustifyContent.FlexStart,
        };
        css[$".ion-select.{mode}.select-justify-end .select-wrapper"] = new()
        {
            JustifyContent = JustifyContent.FlexEnd,
        };
        css[$".ion-select.{mode}.select-justify-space-between .select-wrapper"] = new()
        {
            JustifyContent = JustifyContent.SpaceBetween,
        };
    }

    /// <summary>
    /// Ports select.md.outline.scss: with <c>fill="outline"</c> the border is drawn by three
    /// absolutely positioned fragments (start / notch / end) laid over the select, so a floating
    /// label can "cut out" of the top border by dropping the notch fragment's top border.
    /// </summary>
    private static void AddOutlineContainer(CssObject css, string mode, IonicTheme t)
    {
        css[$".ion-select.{mode}.select-fill-outline .select-outline-container"] = new()
        {
            Display = Display.Flex,
            Position = Position.Absolute,
            Left = Length.Px(0),
            Right = Length.Px(0),
            Top = Length.Px(0),
            Bottom = Length.Px(0),
            Width = Length.Percent(100),
            Height = Length.Percent(100),
        };

        // Every fragment carries the top+bottom border; start/end add their own outer edge and
        // rounded corner. Each selector is declared ONCE — a CssObject key assigned twice replaces
        // the earlier style rather than merging into it.
        css[$".ion-select.{mode}.select-fill-outline .select-outline-notch"] = new()
        {
            BorderTopWidth = Length.Px(1),
            BorderTopStyle = BorderStyle.Solid,
            BorderTopColor = t.SelectBorderColor,
            BorderBottomWidth = Length.Px(1),
            BorderBottomStyle = BorderStyle.Solid,
            BorderBottomColor = t.SelectBorderColor,
            BoxSizing = BoxSizing.BorderBox,
        };

        css[$".ion-select.{mode}.select-fill-outline .select-outline-start"] = new()
        {
            BorderTopWidth = Length.Px(1),
            BorderTopStyle = BorderStyle.Solid,
            BorderTopColor = t.SelectBorderColor,
            BorderBottomWidth = Length.Px(1),
            BorderBottomStyle = BorderStyle.Solid,
            BorderBottomColor = t.SelectBorderColor,
            BorderLeftWidth = Length.Px(1),
            BorderLeftStyle = BorderStyle.Solid,
            BorderLeftColor = t.SelectBorderColor,
            BorderTopLeftRadius = Length.Px(t.SelectBorderRadius),
            BorderBottomLeftRadius = Length.Px(t.SelectBorderRadius),
            BoxSizing = BoxSizing.BorderBox,
            Width = Length.Px(8),
            PointerEvents = PointerEvents.None,
        };

        css[$".ion-select.{mode}.select-fill-outline .select-outline-end"] = new()
        {
            BorderTopWidth = Length.Px(1),
            BorderTopStyle = BorderStyle.Solid,
            BorderTopColor = t.SelectBorderColor,
            BorderBottomWidth = Length.Px(1),
            BorderBottomStyle = BorderStyle.Solid,
            BorderBottomColor = t.SelectBorderColor,
            BorderRightWidth = Length.Px(1),
            BorderRightStyle = BorderStyle.Solid,
            BorderRightColor = t.SelectBorderColor,
            BorderTopRightRadius = Length.Px(t.SelectBorderRadius),
            BorderBottomRightRadius = Length.Px(t.SelectBorderRadius),
            BoxSizing = BoxSizing.BorderBox,
            FlexGrow = 1,
            PointerEvents = PointerEvents.None,
        };

        // The spacer sizes the notch to the scaled label text; it is never visible or interactive.
        css[$".ion-select.{mode}.select-fill-outline .notch-spacer"] = new()
        {
            PaddingRight = Length.Px(8),
            FontSize = Length.Px(12),
            Opacity = 0f,
            PointerEvents = PointerEvents.None,
        };

        // A floating label sits in the gap, so the notch loses its top border to make the cut-out.
        css[$".ion-select.{mode}.select-fill-outline.label-floating .select-outline-notch"] = new()
        {
            BorderTopWidth = Length.Px(0),
        };

        css[$".ion-select.{mode} .select-outline-notch-hidden"] = new()
        {
            Display = Display.None,
        };

        // With the outline fill the border lives on the container fragments, not the inner wrapper.
        css[$".ion-select.{mode}.select-fill-outline .select-wrapper-inner"] = new()
        {
            BorderWidth = Length.Px(0),
        };
    }

    /// <summary>
    /// Ports select-popover.scss / select-popover.md.scss / select-modal.scss — the bodies rendered
    /// inside <c>ion-popover</c> and <c>ion-modal</c> for those two interfaces.
    /// </summary>
    private static void AddOverlayBodies(CssObject css, string mode, IonicTheme t)
    {
        // select-popover.scss: the popover body is its own scroll container (it does not use
        // ion-content), and the list/header margins are reset.
        css[$".ion-select-popover.{mode}"] = new()
        {
            Display = Display.Block,
            OverflowY = Overflow.Auto,
        };

        css[$".ion-select-popover.{mode} .ion-list"] = new()
        {
            Margin = new Margin(Length.Px(0)),
        };

        css[$".ion-select-popover.{mode} .ion-list-header"] = new()
        {
            Margin = new Margin(Length.Px(0)),
        };

        css[$".ion-select-popover.{mode} .ion-label"] = new()
        {
            Margin = new Margin(Length.Px(0)),
        };

        // select-modal.scss `:host { height: 100% }` — the modal body fills the presented card so
        // its ion-content can scroll.
        css[$".ion-select-modal.{mode}"] = new()
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Column,
            Height = Length.Percent(100),
        };

        // select-popover.md.scss / select-modal.md.scss: the checked row is tinted with the primary
        // color at 8% (md only — ios leaves the row untinted).
        if (mode == "md")
        {
            var checkedBackground = new Color(t.Primary.R, t.Primary.G, t.Primary.B, 20); // ~8%

            css[$".ion-select-popover.{mode} .item-radio-checked"] = new()
            {
                BackgroundColor = checkedBackground,
            };
            css[$".ion-select-modal.{mode} .item-radio-checked"] = new()
            {
                BackgroundColor = checkedBackground,
            };
            css[$".ion-select-popover.{mode} .item-checkbox-checked"] = new()
            {
                Color = t.Primary,
            };
            css[$".ion-select-modal.{mode} .item-checkbox-checked"] = new()
            {
                Color = t.Primary,
            };
        }
    }

    private static void AddColor(CssObject css, string mode, string name, Color color)
    {
        css[$".ion-select.{mode}.ion-color-{name}"] = new()
        {
            Color = color,
        };
        css[$".ion-select.{mode}.ion-color-{name} .select-highlight"] = new()
        {
            BackgroundColor = color,
        };
        css[$".ion-select.{mode}.ion-color-{name} .select-icon"] = new()
        {
            Color = color,
        };
    }
}
