using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-accordion</c> / <c>ion-accordion-group</c>. Covers the DOM contract (header +
/// content wrappers, the injected toggle icon), the group→accordion expanded cascade, the
/// header-click toggle request with single/multiple rules, disabled/readonly gating, and the
/// expand-inset / mode class stamping.
/// </summary>
public class IonAccordionTests : IonicComponentTestBase
{
    // A single accordion with a header label and content, bound to the given value.
    private static RenderFragment Accordion(string value, string label) => builder =>
    {
        builder.OpenComponent<IonAccordion>(0);
        builder.AddComponentParameter(1, nameof(IonAccordion.Value), value);
        builder.AddComponentParameter(2, nameof(IonAccordion.Header), (RenderFragment)(b => b.AddContent(0, label)));
        builder.AddComponentParameter(3, nameof(IonAccordion.Content), (RenderFragment)(b => b.AddContent(0, label + " body")));
        builder.CloseComponent();
    };

    // Three accordions: first / second / third.
    private static RenderFragment ThreeAccordions() => builder =>
    {
        int seq = 0;
        foreach (var v in new[] { "first", "second", "third" })
        {
            var captured = v;
            builder.OpenComponent<IonAccordion>(seq++);
            builder.AddComponentParameter(seq++, nameof(IonAccordion.Value), captured);
            builder.AddComponentParameter(seq++, nameof(IonAccordion.Header), (RenderFragment)(b => b.AddContent(0, captured)));
            builder.AddComponentParameter(seq++, nameof(IonAccordion.Content), (RenderFragment)(b => b.AddContent(0, captured + " body")));
            builder.CloseComponent();
        }
    };

    private static ComponentUnderTest RenderGroup(TestContext ctx, RenderFragment child,
        Action<ComponentParameterBuilder<IonAccordionGroup>>? configure = null)
        => ctx.Render<IonAccordionGroup>(p =>
        {
            p.Add(nameof(IonAccordionGroup.ChildContent), child);
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonAccordionGroup_RendersDomContract()
    {
        var cut = RenderGroup(Context, ThreeAccordions());

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-accordion-group");
        // compact is the default expansion behavior.
        cut.Root.ShouldHaveClass("accordion-group-expand-compact");
        cut.FindByClass("ion-accordion").Count.ShouldBe(3);
    }

    [Fact]
    public void IonAccordion_RendersHeaderContentAndToggleIcon()
    {
        var cut = RenderGroup(Context, Accordion("first", "First"));

        var accordion = cut.FindByClass("ion-accordion").ShouldHaveSingleItem();
        accordion.FindByClass("accordion-header").ShouldHaveSingleItem();
        accordion.FindByClass("accordion-content").ShouldHaveSingleItem();
        accordion.FindByClass("accordion-content-wrapper").ShouldHaveSingleItem();
        // The chevron toggle icon is injected into the header.
        accordion.FindByClass("ion-accordion-toggle-icon").ShouldHaveSingleItem();
    }

    // ---- Expanded cascade --------------------------------------------------

    [Fact]
    public void IonAccordion_Collapsed_ByDefault()
    {
        var cut = RenderGroup(Context, ThreeAccordions());

        foreach (var a in cut.FindByClass("ion-accordion"))
        {
            a.ShouldHaveClass("accordion-collapsed");
            a.ShouldNotHaveClass("accordion-expanded");
        }
    }

    [Fact]
    public void IonAccordionGroup_Value_ExpandsMatchingAccordion()
    {
        var cut = RenderGroup(Context, ThreeAccordions(),
            p => p.Add(nameof(IonAccordionGroup.Value), "second"));

        var accordions = cut.FindByClass("ion-accordion");
        // Only the accordion whose value matches is expanded.
        var expanded = accordions.Where(a => a.HasClass("accordion-expanded")).ToList();
        expanded.ShouldHaveSingleItem();
        accordions[0].ShouldHaveClass("accordion-collapsed");
        accordions[2].ShouldHaveClass("accordion-collapsed");
    }

    [Fact]
    public void IonAccordionGroup_Multiple_ExpandsSeveralAccordions()
    {
        var cut = RenderGroup(Context, ThreeAccordions(), p =>
        {
            p.Add(nameof(IonAccordionGroup.Multiple), true);
            p.Add(nameof(IonAccordionGroup.Values), (IReadOnlyList<string>)new[] { "first", "third" });
        });

        var accordions = cut.FindByClass("ion-accordion");
        accordions[0].ShouldHaveClass("accordion-expanded");
        accordions[1].ShouldHaveClass("accordion-collapsed");
        accordions[2].ShouldHaveClass("accordion-expanded");
    }

    // ---- Toggle interaction ------------------------------------------------

    [Fact]
    public async Task IonAccordionGroup_RequestToggle_ExpandsCollapsedAccordion_Single()
    {
        string? changed = null;
        var group = new IonAccordionGroup
        {
            ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v),
        };
        group.ChildContent = Accordion("first", "First");

        // Build so OnParametersSet rebuilds the cascaded context bound to this instance.
        BuildInScope(group);

        // Drive the same callback the accordion header invokes on click.
        await CascadedContext(group).RequestToggle.InvokeAsync(("first", true));

        changed.ShouldBe("first");
    }

    [Fact]
    public async Task IonAccordionGroup_RequestToggle_CollapsesExpandedAccordion_Single()
    {
        string? changed = "unset";
        var group = new IonAccordionGroup
        {
            Value = "first",
            ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v),
        };
        group.ChildContent = Accordion("first", "First");
        BuildInScope(group);

        // Collapsing the currently expanded value clears it (single-select).
        await CascadedContext(group).RequestToggle.InvokeAsync(("first", false));

        changed.ShouldBeNull();
    }

    [Fact]
    public async Task IonAccordionGroup_RequestToggle_Single_ReplacesPreviousValue()
    {
        string? changed = null;
        var group = new IonAccordionGroup
        {
            Value = "first",
            ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v),
        };
        group.ChildContent = ThreeAccordions();
        BuildInScope(group);

        // Expanding another value replaces the single selection.
        await CascadedContext(group).RequestToggle.InvokeAsync(("second", true));

        changed.ShouldBe("second");
    }

    [Fact]
    public async Task IonAccordionGroup_RequestToggle_Multiple_AccumulatesValues()
    {
        IReadOnlyList<string>? changed = null;
        var group = new IonAccordionGroup
        {
            Multiple = true,
            Values = new[] { "first" },
            ValuesChanged = EventCallback.Factory.Create<IReadOnlyList<string>>(this, v => changed = v),
        };
        group.ChildContent = ThreeAccordions();
        BuildInScope(group);

        await CascadedContext(group).RequestToggle.InvokeAsync(("third", true));

        changed.ShouldNotBeNull();
        changed!.ShouldContain("first");
        changed!.ShouldContain("third");
    }

    [Fact]
    public async Task IonAccordionGroup_RequestToggle_Disabled_IsNoOp()
    {
        var invoked = false;
        var group = new IonAccordionGroup
        {
            Disabled = true,
            ValueChanged = EventCallback.Factory.Create<string?>(this, _ => invoked = true),
        };
        group.ChildContent = Accordion("first", "First");
        BuildInScope(group);

        await CascadedContext(group).RequestToggle.InvokeAsync(("first", true));

        invoked.ShouldBeFalse();
    }

    [Fact]
    public void IonAccordionGroup_Disabled_StampsGroupClass_AndGatesAccordions()
    {
        var cut = RenderGroup(Context, ThreeAccordions(),
            p => p.Add(nameof(IonAccordionGroup.Disabled), true));

        cut.Root.ShouldHaveClass("accordion-group-disabled");
        // Every accordion inherits the disabled state from the group.
        foreach (var a in cut.FindByClass("ion-accordion"))
        {
            a.ShouldHaveClass("accordion-disabled");
        }
    }

    [Fact]
    public void IonAccordionGroup_Readonly_StampsGroupClass()
    {
        var cut = RenderGroup(Context, ThreeAccordions(),
            p => p.Add(nameof(IonAccordionGroup.Readonly), true));

        cut.Root.ShouldHaveClass("accordion-group-readonly");
        foreach (var a in cut.FindByClass("ion-accordion"))
        {
            a.ShouldHaveClass("accordion-readonly");
        }
    }

    // ---- Expand / mode -----------------------------------------------------

    [Fact]
    public void IonAccordionGroup_Inset_StampsExpandInsetClass()
    {
        var cut = RenderGroup(Context, ThreeAccordions(),
            p => p.Add(nameof(IonAccordionGroup.Expand), "inset"));

        cut.Root.ShouldHaveClass("accordion-group-expand-inset");
    }

    [Fact]
    public void IonAccordionGroup_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderGroup(Context, ThreeAccordions());

        cut.Root.Class.ShouldStartWith("ios ion-accordion-group");
    }

    // ---- Key style ---------------------------------------------------------

    [Fact]
    public void IonAccordion_CollapsedContent_IsHidden()
    {
        // A collapsed accordion's content has display:none, so it is pruned from the layout tree
        // (no computed style is collected). Assert on the matched stylesheet rule instead: the
        // collapsed-scoped rule sets the content region to display:none.
        var sheet = IonicStyleSheetFactory.CreateAllModes();

        var content = new Miko.Core.DomElements.DivElement { Class = "accordion-content" };
        var collapsedAccordion = new Miko.Core.DomElements.DivElement { Class = "md ion-accordion accordion-collapsed" };
        collapsedAccordion.AddChild(content);

        var rule = sheet.Rules
            .Where(r => r.Selector.Matches(content))
            .OrderByDescending(r => r.Selector.Specificity)
            .FirstOrDefault(r => r.Style.Display is not null);

        rule.ShouldNotBeNull();
        rule.Style.Display!.Value.Value.ShouldBe(Display.None);
    }

    [Fact]
    public void IonAccordion_ExpandedContent_IsShown()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderGroup(Context, Accordion("first", "First"),
            p => p.Add(nameof(IonAccordionGroup.Value), "first"));

        var content = cut.FindByClass("accordion-content").ShouldHaveSingleItem();
        // An expanded accordion's content is laid out and displays as a block.
        cut.GetComputedStyle(content)!.Display.ShouldBe(Display.Block);
    }

    // ---- Header divider (issue #2) -----------------------------------------

    [Fact]
    public void IonAccordion_Collapsed_HeaderDivider_SpansTheFullRow()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderGroup(Context, ItemAccordion("first", "First"));

        // Collapsed: the divider moves off .item-inner (inset, indented by the leading padding)
        // onto the full-width .item-native — Ionic's slotted-item --border-width.
        var nativeStyle = cut.GetComputedStyle(cut.FindByClass("item-native").Single());
        var innerStyle = cut.GetComputedStyle(cut.FindByClass("item-inner").Single());
        nativeStyle.ShouldNotBeNull();
        innerStyle.ShouldNotBeNull();
        nativeStyle.BorderBottomWidth.ShouldBe(Length.Px(1));
        innerStyle.BorderBottomWidth.ShouldBe(Length.Px(0));
    }

    [Fact]
    public void IonAccordion_Collapsed_HeaderDivider_ReachesBothEdges_InLayout()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderGroup(Context, ItemAccordion("first", "First"));

        // The bordered box must span the whole item host — no left/right padding inset.
        var host = cut.FindByClass("ion-item").Single();
        var hostBox = cut.GetBoxModel(host);
        var nativeBox = cut.GetBoxModel(cut.FindByClass("item-native").Single());
        hostBox.ShouldNotBeNull();
        nativeBox.ShouldNotBeNull();
        nativeBox.BorderBox.X.ShouldBe(hostBox.BorderBox.X, 0.5f);
        nativeBox.BorderBox.Right.ShouldBe(hostBox.BorderBox.Right, 0.5f);
    }

    [Fact]
    public void IonAccordion_Expanded_HeaderDivider_IsRemoved()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderGroup(Context, ItemAccordion("first", "First"),
            p => p.Add(nameof(IonAccordionGroup.Value), "first"));

        // Expanded: accordion.scss zeroes both --border-width and --inner-border-width on the
        // slotted header item, so neither box draws a divider.
        var nativeStyle = cut.GetComputedStyle(cut.FindByClass("item-native").Single());
        var innerStyle = cut.GetComputedStyle(cut.FindByClass("item-inner").Single());
        nativeStyle.ShouldNotBeNull();
        innerStyle.ShouldNotBeNull();
        nativeStyle.BorderBottomWidth.ShouldBe(Length.Px(0));
        innerStyle.BorderBottomWidth.ShouldBe(Length.Px(0));
    }

    [Fact]
    public void IonAccordion_Collapsed_HeaderItem_KeepsExplicitLines()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderGroup(Context, ItemAccordion("first", "First", lines: "none"));

        // accordion.tsx only defaults the header item's lines when unset
        // (`if (ionItem.lines === undefined)`), so an explicit lines="none" still removes the
        // divider entirely — the full-row retarget targets .item-lines-default only.
        var nativeStyle = cut.GetComputedStyle(cut.FindByClass("item-native").Single());
        var innerStyle = cut.GetComputedStyle(cut.FindByClass("item-inner").Single());
        nativeStyle.ShouldNotBeNull();
        innerStyle.ShouldNotBeNull();
        nativeStyle.BorderBottomWidth.ShouldBe(Length.Px(0));
        innerStyle.BorderBottomWidth.ShouldBe(Length.Px(0));
    }

    // ---- Toggle icon (issue #3) --------------------------------------------

    [Fact]
    public void IonAccordion_ToggleIcon_KeepsTheEndPadding()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderGroup(Context, ItemAccordion("first", "First"));

        // Ionic slots the chevron into the header item's end slot, so .item-inner's
        // --inner-padding-end (16px) keeps it off the right edge. The port renders the icon
        // outside .item-inner, so the wrapper carries the same 16px gap itself.
        var icon = cut.FindByClass("ion-accordion-toggle-icon").Single();
        var style = cut.GetComputedStyle(icon);
        style.ShouldNotBeNull();
        style.MarginRight.ShouldBe(Length.Px(16));
    }

    [Fact]
    public void IonAccordion_ToggleIcon_StaysOffTheRightEdge_InLayout()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderGroup(Context, ItemAccordion("first", "First"));

        var header = cut.FindByClass("accordion-header").Single();
        var icon = cut.FindByClass("ion-accordion-toggle-icon").Single();
        var headerBox = cut.GetBoxModel(header);
        var iconBox = cut.GetBoxModel(icon);
        headerBox.ShouldNotBeNull();
        iconBox.ShouldNotBeNull();
        // 16px of clearance between the icon and the header's trailing edge.
        (headerBox.BorderBox.Right - iconBox.BorderBox.Right).ShouldBe(16f, 0.5f);
    }

    [Fact]
    public void IonAccordion_Header_HasHoverWash_WhenHovered()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderGroup(Context, ItemAccordion("first", "First"));

        // Un-hovered the header is transparent; hovering paints the 4% wash. At runtime the hover
        // state propagates up the hit chain, so the header itself carries it whenever the pointer
        // is anywhere over the row.
        cut.GetComputedStyle(cut.FindByClass("accordion-header").Single())!
            .BackgroundColor.ShouldBe(Miko.Common.Color.Transparent);

        cut.FindByClass("accordion-header").Single().SetState(Miko.Core.ElementState.Hover);
        var hovered = Context.RenderElement(cut.Root);
        hovered.GetComputedStyle(hovered.FindByClass("accordion-header").Single())!
            .BackgroundColor.A.ShouldBe((byte)10);
    }

    // An accordion whose header is a real IonItem — the shape Ionic documents (and the one the
    // divider / toggle-icon rules target).
    private static RenderFragment ItemAccordion(string value, string label, string? lines = null) => builder =>
    {
        builder.OpenComponent<IonAccordion>(0);
        builder.AddComponentParameter(1, nameof(IonAccordion.Value), value);
        builder.AddComponentParameter(2, nameof(IonAccordion.Header), (RenderFragment)(hb =>
        {
            hb.OpenComponent<IonItem>(0);
            if (lines is not null)
            {
                hb.AddComponentParameter(4, nameof(IonItem.Lines), lines);
            }
            hb.AddComponentParameter(1, nameof(IonItem.ChildContent), (RenderFragment)(lb =>
            {
                lb.OpenComponent<IonLabel>(0);
                lb.AddComponentParameter(1, nameof(IonLabel.ChildContent), (RenderFragment)(tb => tb.AddContent(0, label)));
                lb.CloseComponent();
            }));
            hb.CloseComponent();
        }));
        builder.AddComponentParameter(3, nameof(IonAccordion.Content), (RenderFragment)(b => b.AddContent(0, label + " body")));
        builder.CloseComponent();
    };

    // Builds the group so OnParametersSet runs and rebuilds the cascaded context bound to this
    // instance. The toggle logic under test is mode-independent, so a bare build (PlatformInfo null
    // → md fallback) suffices — no service scope needed.
    private static void BuildInScope(IonAccordionGroup group)
    {
        group.Build();
    }

    // Reads back the IonAccordionGroupContext the group cascades to its accordions — the same value
    // a child reads via [CascadingParameter]. Its RequestToggle is what a header click invokes.
    private static IonAccordionGroupContext CascadedContext(IonAccordionGroup group)
    {
        var field = typeof(IonAccordionGroup).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        return (IonAccordionGroupContext)field.GetValue(group)!;
    }
}
