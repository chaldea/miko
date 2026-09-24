using Miko.Styling;
using Miko.Styling.Selectors;
using Miko.Core;

namespace Miko.Ionic;

/// <summary>Attaches isolation to the owning host while preserving ancestor/slot conditions.</summary>
internal static class IonicStyleScope
{
    internal static void AddTo(StyleSheet target, CssObject css, string family, string scope)
    {
        var generated = new StyleSheet();
        generated.Add(css);
        foreach (var rule in generated.Rules)
            target.AddRule(Scope(rule.Selector, family, scope), rule.Style);
        foreach (var rule in generated.PseudoElementRules)
            target.AddPseudoElementRule(Scope(rule.Selector, family, scope), rule.Type, rule.Style);
    }

    private static Selector Scope(Selector selector, string family, string scope)
    {
        if (selector is GroupSelector group)
            return new GroupSelector(group.Selectors.Select(branch => Scope(branch, family, scope)));
        var rule = TryScope(selector, family, new ClassSelector(scope))
            ?? throw new InvalidOperationException($"No {family} host found in generated selector.");
        return AddBoundary(rule.Selector, new HostBoundarySelector(rule.Host, scope));
    }

    private sealed record ScopedRule(Selector Selector, string Host);

    private static ScopedRule? TryScope(Selector selector, string family, ClassSelector scope)
    {
        var host = selector switch
        {
            ClassSelector cls when Owns(family, cls.ClassName) => cls,
            CompoundSelector compound => compound.Selectors.OfType<ClassSelector>()
                .FirstOrDefault(part => Owns(family, part.ClassName)),
            _ => null,
        };
        if (host != null)
            return new ScopedRule(new CompoundSelector(selector, scope), host.ClassName);

        // Find the first owning host in the selector tree, leaving context outside that host
        // unscoped (e.g. .ion-slot-start .ion-toggle or .ion-list .ion-item).
        return selector switch
        {
            DescendantSelector d => Combine(d.Ancestor, d.Descendant, family, scope,
                (left, right) => new DescendantSelector(left, right)),
            ChildSelector c => Combine(c.Parent, c.Child, family, scope,
                (left, right) => new ChildSelector(left, right)),
            AdjacentSiblingSelector a => Combine(a.Previous, a.Target, family, scope,
                (left, right) => new AdjacentSiblingSelector(left, right)),
            GeneralSiblingSelector g => Combine(g.Previous, g.Target, family, scope,
                (left, right) => new GeneralSiblingSelector(left, right)),
            _ => null,
        };
    }

    private static ScopedRule? Combine(Selector left, Selector right, string family,
        ClassSelector scope, Func<Selector, Selector, Selector> combine)
    {
        if (TryScope(left, family, scope) is { } scopedLeft)
            return new ScopedRule(combine(scopedLeft.Selector, right), scopedLeft.Host);
        return TryScope(right, family, scope) is { } scopedRight
            ? new ScopedRule(combine(left, scopedRight.Selector), scopedRight.Host) : null;
    }

    private static Selector AddBoundary(Selector selector, Selector boundary) => selector switch
    {
        DescendantSelector d => new DescendantSelector(d.Ancestor, AddBoundary(d.Descendant, boundary)),
        ChildSelector c => new ChildSelector(c.Parent, AddBoundary(c.Child, boundary)),
        AdjacentSiblingSelector a => new AdjacentSiblingSelector(a.Previous, AddBoundary(a.Target, boundary)),
        GeneralSiblingSelector g => new GeneralSiblingSelector(g.Previous, AddBoundary(g.Target, boundary)),
        CompoundSelector c => new CompoundSelector(c.Selectors.Append(boundary)),
        _ => new CompoundSelector(selector, boundary),
    };

    // A descendant rule must not enter another instance of the owning component, even when
    // the outer rule was registered later. Keep this predicate at the target so indexing and
    // hover analysis can still inspect the original combinator tree.
    private sealed class HostBoundarySelector(string host, string scope) : Selector
    {
        public override int Specificity => 0;

        // Reads only class names along the ancestor chain, never text or inline style. Left at the
        // conservative default, the ~450 scoped rules of the Ionic sheet would switch off the
        // engine's content-only and subtree-restyle paths for every Ionic app (ISSUE-146).
        public override bool MayReadContentOrInlineStyle => false;

        public override bool Matches(Element element)
        {
            for (var current = element; current != null; current = current.Parent)
                if (current.HasClass(host)) return current.HasClass(scope);
            return false;
        }
    }

    private static bool Owns(string family, string host) => family switch
    {
        "Accordion" => host is "ion-accordion" or "ion-accordion-group",
        "ActionSheet" => host == "ion-action-sheet",
        "BackButton" => host == "ion-back-button",
        "Breadcrumb" => host is "ion-breadcrumb" or "ion-breadcrumbs",
        "Card" => host is "ion-card" or "ion-card-header" or "ion-card-content" or "ion-card-title" or "ion-card-subtitle",
        "Datetime" => host is "ion-datetime" or "ion-datetime-button",
        "Fab" => host is "ion-fab" or "ion-fab-button" or "ion-fab-list",
        "Grid" => host is "ion-grid" or "ion-row" or "ion-col",
        "InfiniteScroll" => host is "ion-infinite-scroll" or "ion-infinite-scroll-content",
        "InputOtp" => host == "ion-input-otp",
        "Item" => host is "ion-item" or "ion-item-divider" or "ion-item-group" or "ion-item-sliding" or "ion-item-options" or "ion-item-option",
        "List" => host is "ion-list" or "ion-list-header",
        "Menu" => host is "ion-app" or "ion-menu-host" or "ion-menu-button" or "ion-buttons",
        "Overlay" => host == "ion-overlay-host",
        "Picker" => host is "ion-picker" or "ion-picker-column" or "ion-picker-column-option",
        "ProgressBar" => host == "ion-progress-bar",
        "Radio" => host is "ion-radio" or "ion-radio-group",
        "Refresher" => host is "ion-refresher" or "ion-refresher-content",
        "Reorder" => host is "ion-reorder" or "ion-reorder-group",
        "Segment" => host is "ion-segment" or "ion-segment-button" or "ion-segment-content" or "ion-segment-view",
        "Select" => host is "ion-select" or "ion-select-option" or "ion-select-popover" or "ion-select-modal",
        "SkeletonText" => host == "ion-skeleton-text",
        "Slides" => host is "slides-md" or "slides-ios" or "swiper-slide" or "swiper-fade" or "swiper-navigation-disabled",
        "Tab" => host is "ion-tabs" or "ion-tab-bar" or "ion-tab-button",
        _ => host.Equals("ion-" + family, StringComparison.OrdinalIgnoreCase),
    };
}
