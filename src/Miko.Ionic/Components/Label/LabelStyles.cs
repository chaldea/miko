using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-label</c>. Ports <c>label.scss</c> plus the per-mode
/// <c>label.ios.scss</c> / <c>label.md.scss</c> and their vars.
/// <para>
/// A label is the text content of a list row or form control. The base rule is a block box that
/// ellipsises overflowing text; a palette <c>color</c> recolors the text, and the <c>label-*</c>
/// position variants size the label inside an item. Within a tab button the label sits below the
/// icon with the active mode's tab-button typography.
/// </para>
/// </summary>
internal static class LabelStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // label.scss :host-context(.item) — display/box-sizing/text-overflow. Ionic scopes these
            // to an item ancestor; the port applies them to every label because the properties are
            // inert outside a constrained row (ellipsis needs a clipping box to bite) and this keeps
            // a standalone <IonLabel> a plain block, matching the previous behavior.
            [$".ion-label.{mode}"] = new()
            {
                Display = Display.Block,
                TextOverflow = TextOverflow.Ellipsis,
                BoxSizing = BoxSizing.BorderBox,
            },

            // label.scss :host(.ion-text-nowrap) { overflow: hidden } — the clipping box that makes
            // the ellipsis above actually render, since Miko needs overflow on the element that
            // clips, which is the label itself here.
            [$".ion-label.{mode}.ion-text-nowrap"] = new()
            {
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            // label.ios.scss / label.md.scss :host(.ion-text-wrap) — wrapped text gets a looser
            // line-height (both modes 1.5); iOS also drops to a 14px font (md keeps the inherited
            // size, so LabelTextWrapFontSize is null there and the property stays unset).
            [$".ion-label.{mode}.ion-text-wrap"] = new()
            {
                FontSize = t.LabelTextWrapFontSize is float px ? Length.Px(px) : null,
                LineHeight = Length.Em(t.LabelTextWrapLineHeight),
            },

            [$".ion-tab-button.{mode} .ion-label"] = new()
            {
                FontSize = Length.Px(t.TabButtonFontSize),
                MarginTop = Length.Px(2),
                TextAlign = TextAlign.Center,
            },
        };

        AddPositionRules(css, mode, t);

        // ion-color recolors the label text to the named palette base
        // (label.scss `:host(.ion-color) { color: current-color(base) }`). Without these the color
        // attribute stamped an ion-color-* class that no rule matched, making Color a no-op —
        // the same defect class as ISSUE-116 problem 5 in CheckboxStyles.
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
    /// The <c>position</c> variants that do not depend on item focus/value state.
    /// <para>
    /// <c>label-fixed</c> is a fixed-width leading track (label.scss <c>:host(.label-fixed)</c>).
    /// <c>label-stacked</c> / <c>label-floating</c> stretch across the row and zero their bottom
    /// margin; iOS gives a stacked label a 4px bottom margin and a 14px font instead.
    /// </para>
    /// </summary>
    private static void AddPositionRules(CssObject css, string mode, IonicTheme t)
    {
        // label.scss :host(.label-fixed) — flex: 0 0 100px; width/min-width 100px; max-width 200px.
        css[$".ion-label.{mode}.label-fixed"] = new()
        {
            FlexGrow = 0,
            FlexShrink = 0,
            FlexBasis = Length.Px(100),
            Width = Length.Px(100),
            MinWidth = Length.Px(100),
            MaxWidth = Length.Px(200),
        };

        // label.scss :host(.label-stacked), :host(.label-floating) — align-self: stretch;
        // width: auto; max-width: 100%; margin-bottom: 0.
        // label.ios.scss then gives a *stacked* label a 4px bottom margin and a 14px font; md zeroes
        // all four margins, which the shared values already cover (LabelStackedMarginBottom is 0
        // and LabelStackedFontSize null there).
        foreach (var variant in new[] { "label-stacked", "label-floating" })
        {
            bool stacked = variant == "label-stacked";
            css[$".ion-label.{mode}.{variant}"] = new()
            {
                AlignSelf = AlignSelf.Stretch,
                Width = Length.Auto,
                MaxWidth = Length.Percent(100),
                MarginBottom = Length.Px(stacked ? t.LabelStackedMarginBottom : 0f),
                FontSize = stacked && t.LabelStackedFontSize is float px ? Length.Px(px) : null,
            };
        }
    }

    /// <summary>
    /// Emits one named-color variant: the label text takes the palette base color.
    /// <para>
    /// The <c>.ion-color-*</c> compound is one class more specific than the base
    /// <c>.ion-label.{mode}</c> rule, so it wins regardless of the source order in which
    /// <see cref="IonicStyleSheetFactory"/> registers the label rules relative to the item rules.
    /// </para>
    /// </summary>
    private static void AddColor(CssObject css, string mode, string name, Color color)
    {
        css[$".ion-label.{mode}.ion-color-{name}"] = new()
        {
            Color = color,
        };

        // label.ios.scss / label.md.scss :host(.in-item-color)::slotted(p) { color: inherit } —
        // a label inside a colored ion-item follows the item's contrast color instead of the muted
        // paragraph gray. Ionic stamps `in-item-color` via hostContext('ion-item.ion-color'); this
        // port expresses the same ancestor test as a descendant selector, the way CheckboxStyles
        // mirrors hostContext('ion-item'). Miko has no `inherit` keyword, so the item's color is
        // mirrored onto the label rather than inherited.
        //
        // :not(.ion-color) is load-bearing: a descendant selector out-specifies the label's own
        // .ion-color-* rule (5 classes vs 3), so without the guard an explicit color on a label
        // inside a colored item would be silently overridden. Ionic has the same guard — its
        // in-item-color rule only targets slotted <p>, never a label that set its own color.
        css[$".ion-item.{mode}.ion-color-{name} .ion-label.{mode}:not(.ion-color)"] = new()
        {
            Color = color,
        };
    }
}
