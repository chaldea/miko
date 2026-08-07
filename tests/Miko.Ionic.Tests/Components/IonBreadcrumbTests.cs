using Miko.Common;
using Miko.Components;
using Miko.Events;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-breadcrumb</c> / <c>ion-breadcrumbs</c>. Covers the DOM contract (the wrapping
/// row, each crumb's native anchor/span + separator), the last-crumb resolution (active + separator
/// stripped) the container performs in <c>Build()</c>, href → anchor, the mode-specific separator
/// glyph, disabled/color class stamping, and a key layout style.
/// </summary>
public class IonBreadcrumbTests : IonicComponentTestBase
{
    // A single crumb with the given href/label.
    private static RenderFragment Crumb(string label, string? href = null) => builder =>
    {
        builder.OpenComponent<IonBreadcrumb>(0);
        if (href is not null) builder.AddComponentParameter(1, nameof(IonBreadcrumb.Href), href);
        builder.AddComponentParameter(2, nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b =>
            b.AddContent(0, label)));
        builder.CloseComponent();
    };

    // A breadcrumbs bar with Home / Electronics / Cameras crumbs.
    private static RenderFragment ThreeCrumbs() => builder =>
    {
        int seq = 0;
        foreach (var (label, href) in new[] { ("Home", "#home"), ("Electronics", "#el"), ("Cameras", (string?)null) })
        {
            builder.OpenComponent<IonBreadcrumb>(seq++);
            if (href is not null) builder.AddComponentParameter(seq++, nameof(IonBreadcrumb.Href), href);
            var captured = label;
            builder.AddComponentParameter(seq++, nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b =>
                b.AddContent(0, captured)));
            builder.CloseComponent();
        }
    };

    private static ComponentUnderTest RenderBreadcrumbs(TestContext ctx, RenderFragment child,
        Action<ComponentParameterBuilder<IonBreadcrumbs>>? configure = null)
        => ctx.Render<IonBreadcrumbs>(p =>
        {
            p.Add(nameof(IonBreadcrumbs.ChildContent), child);
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonBreadcrumbs_RendersDomContract()
    {
        var cut = RenderBreadcrumbs(Context, ThreeCrumbs());

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-breadcrumbs");
        cut.FindByClass("ion-breadcrumb").Count.ShouldBe(3);
    }

    [Fact]
    public void IonBreadcrumb_RendersNativeAndSeparator()
    {
        var cut = Context.Render<IonBreadcrumb>(p =>
            p.Add(nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b => b.AddContent(0, "Home"))));

        cut.Root.ShouldHaveClass("md ion-breadcrumb");
        // A standalone crumb (no container) keeps its native span and separator.
        cut.FindByClass("breadcrumb-native").ShouldHaveSingleItem();
        cut.FindByClass("breadcrumb-separator").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("Home");
    }

    [Fact]
    public void IonBreadcrumb_WithHref_RendersAnchor()
    {
        var cut = Context.Render<IonBreadcrumb>(p =>
        {
            p.Add(nameof(IonBreadcrumb.Href), "#home");
            p.Add(nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b => b.AddContent(0, "Home")));
        });

        var native = cut.FindByClass("breadcrumb-native").ShouldHaveSingleItem();
        native.TagName.ShouldBe("a");
        // href makes it activatable/focusable (Ionic clickable markers).
        cut.Root.ShouldHaveClass("ion-activatable");
    }

    [Fact]
    public void IonBreadcrumb_WithoutHref_RendersSpan()
    {
        var cut = Context.Render<IonBreadcrumb>(p =>
            p.Add(nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b => b.AddContent(0, "Home"))));

        cut.FindByClass("breadcrumb-native").ShouldHaveSingleItem().TagName.ShouldBe("span");
        cut.Root.ShouldNotHaveClass("ion-activatable");
    }

    // ---- Last-crumb resolution (container Build post-pass) -----------------

    [Fact]
    public void IonBreadcrumbs_MarksLastCrumbActive_AndStripsItsSeparator()
    {
        var cut = RenderBreadcrumbs(Context, ThreeCrumbs());

        var crumbs = cut.FindByClass("ion-breadcrumb");
        var last = crumbs[^1];
        last.ShouldHaveClass("breadcrumb-active");
        // The last crumb has no trailing separator; the earlier ones do.
        last.FindByClass("breadcrumb-separator").ShouldBeEmpty();
        crumbs[0].FindByClass("breadcrumb-separator").ShouldHaveSingleItem();
        crumbs[1].FindByClass("breadcrumb-separator").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonBreadcrumbs_DoesNotOverrideExplicitActive()
    {
        // When a crumb is explicitly active, the last one is NOT auto-activated.
        var cut = RenderBreadcrumbs(Context, builder =>
        {
            builder.OpenComponent<IonBreadcrumb>(0);
            builder.AddComponentParameter(1, nameof(IonBreadcrumb.Active), true);
            builder.AddComponentParameter(2, nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b => b.AddContent(0, "Home")));
            builder.CloseComponent();

            builder.OpenComponent<IonBreadcrumb>(3);
            builder.AddComponentParameter(4, nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b => b.AddContent(0, "Away")));
            builder.CloseComponent();
        });

        var crumbs = cut.FindByClass("ion-breadcrumb");
        crumbs[0].ShouldHaveClass("breadcrumb-active");
        crumbs[1].ShouldNotHaveClass("breadcrumb-active");
    }

    // ---- Mode-specific separator ------------------------------------------

    [Fact]
    public void IonBreadcrumb_Md_UsesSlashSeparator()
    {
        var cut = Context.Render<IonBreadcrumb>(p =>
            p.Add(nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b => b.AddContent(0, "Home"))));

        var separator = cut.FindByClass("breadcrumb-separator").ShouldHaveSingleItem();
        // md separator is the "/" glyph, not an icon.
        separator.FindByClass("ion-icon").ShouldBeEmpty();
        cut.GetTextContent().ShouldContain("/");
    }

    [Fact]
    public void IonBreadcrumb_Ios_UsesChevronSeparator()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = Context.Render<IonBreadcrumb>(p =>
            p.Add(nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b => b.AddContent(0, "Home"))));

        var separator = cut.FindByClass("breadcrumb-separator").ShouldHaveSingleItem();
        // ios separator is a forward-chevron icon.
        separator.FindByClass("ion-icon").ShouldHaveSingleItem();
    }

    // ---- State / color classes --------------------------------------------

    [Fact]
    public void IonBreadcrumb_Disabled_StampsClass()
    {
        var cut = Context.Render<IonBreadcrumb>(p =>
        {
            p.Add(nameof(IonBreadcrumb.Disabled), true);
            p.Add(nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b => b.AddContent(0, "Home")));
        });

        cut.Root.ShouldHaveClass("breadcrumb-disabled");
    }

    [Fact]
    public void IonBreadcrumb_Color_StampsColorClasses()
    {
        var cut = Context.Render<IonBreadcrumb>(p =>
        {
            p.Add(nameof(IonBreadcrumb.Color), "primary");
            p.Add(nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b => b.AddContent(0, "Home")));
        });

        cut.Root.ShouldHaveClass("ion-color");
        cut.Root.ShouldHaveClass("ion-color-primary");
    }

    // ---- Key style ---------------------------------------------------------

    [Fact]
    public void IonBreadcrumbs_Style_IsFlexWrapRow()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderBreadcrumbs(Context, ThreeCrumbs());

        var style = cut.GetComputedStyle(cut.Root)!;
        style.Display.ShouldBe(Display.Flex);
        style.FlexWrap.ShouldBe(FlexWrap.Wrap);
    }

    // ---- Separator slot (slot="separator") ---------------------------------

    [Fact]
    public void IonBreadcrumb_CustomSeparator_ReplacesDefaultGlyph()
    {
        var cut = Context.Render<IonBreadcrumb>(p =>
        {
            p.Add(nameof(IonBreadcrumb.Separator), (RenderFragment)(b => b.AddContent(0, "›")));
            p.Add(nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b => b.AddContent(0, "Home")));
        });

        cut.FindByClass("breadcrumb-separator").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("›");
        // The default md "/" glyph is no longer rendered.
        cut.GetTextContent().ShouldNotContain("/");
    }

    [Fact]
    public void IonBreadcrumbs_StillStripsCustomSeparator_FromLastCrumb()
    {
        var cut = RenderBreadcrumbs(Context, builder =>
        {
            for (int i = 0; i < 2; i++)
            {
                var captured = i;
                builder.OpenComponent<IonBreadcrumb>(i * 10);
                builder.AddComponentParameter(i * 10 + 1, nameof(IonBreadcrumb.Separator),
                    (RenderFragment)(b => b.AddContent(0, "›")));
                builder.AddComponentParameter(i * 10 + 2, nameof(IonBreadcrumb.ChildContent),
                    (RenderFragment)(b => b.AddContent(0, $"Crumb{captured}")));
                builder.CloseComponent();
            }
        });

        var crumbs = cut.FindByClass("ion-breadcrumb");
        crumbs[0].FindByClass("breadcrumb-separator").ShouldHaveSingleItem();
        crumbs[1].FindByClass("breadcrumb-separator").ShouldBeEmpty();
    }

    // ---- Slotted icons: size + gap -----------------------------------------

    [Fact]
    public void IonBreadcrumb_SlottedIcon_IsSized18Px()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonBreadcrumb>(p =>
        {
            p.Add(nameof(IonBreadcrumb.Start), (RenderFragment)(b =>
            {
                b.OpenComponent<IonIcon>(0);
                b.AddComponentParameter(1, nameof(IonIcon.Icon), "home");
                b.CloseComponent();
            }));
            p.Add(nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b => b.AddContent(0, "Home")));
        });

        var icon = cut.FindByClass("ion-icon").ShouldHaveSingleItem();
        var style = cut.GetComputedStyle(icon)!;
        style.Width.ShouldBe(Length.Px(18));
        style.Height.ShouldBe(Length.Px(18));
    }

    [Fact]
    public void IonBreadcrumb_SlottedStartAndEndIcons_Have8PxGapToLabel()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        RenderFragment Icon(int seq, string name) => b =>
        {
            b.OpenComponent<IonIcon>(seq);
            b.AddComponentParameter(seq + 1, nameof(IonIcon.Icon), name);
            b.CloseComponent();
        };

        var cut = Context.Render<IonBreadcrumb>(p =>
        {
            p.Add(nameof(IonBreadcrumb.Start), Icon(0, "home"));
            p.Add(nameof(IonBreadcrumb.End), Icon(2, "star"));
            p.Add(nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b => b.AddContent(0, "Home")));
        });

        // The slots render inside ion-slot-* marker spans (Ionic ::slotted([slot=…])).
        var startIcon = cut.FindByClass("ion-slot-start").ShouldHaveSingleItem()
            .FindByClass("ion-icon").ShouldHaveSingleItem();
        cut.GetComputedStyle(startIcon)!.MarginRight.ShouldBe(Length.Px(8));

        var endIcon = cut.FindByClass("ion-slot-end").ShouldHaveSingleItem()
            .FindByClass("ion-icon").ShouldHaveSingleItem();
        cut.GetComputedStyle(endIcon)!.MarginLeft.ShouldBe(Length.Px(8));
    }

    // ---- MaxItems / collapsed indicator ------------------------------------

    // A breadcrumbs bar with five plain crumbs.
    private static RenderFragment FiveCrumbs() => builder =>
    {
        int seq = 0;
        foreach (var label in new[] { "Home", "Electronics", "Cameras", "Film", "Digital" })
        {
            var captured = label;
            builder.OpenComponent<IonBreadcrumb>(seq++);
            builder.AddComponentParameter(seq++, nameof(IonBreadcrumb.ChildContent),
                (RenderFragment)(b => b.AddContent(0, captured)));
            builder.CloseComponent();
        }
    };

    [Fact]
    public void IonBreadcrumbs_MaxItems_CollapsesMiddleCrumbs()
    {
        var cut = RenderBreadcrumbs(Context, FiveCrumbs(), p =>
            p.Add(nameof(IonBreadcrumbs.MaxItems), 4));

        var crumbs = cut.FindByClass("ion-breadcrumb");
        crumbs.Count.ShouldBe(5);

        // The first crumb stays fully visible.
        crumbs[0].ShouldNotHaveClass("breadcrumb-collapsed");
        crumbs[0].FindByClass("breadcrumb-native").ShouldHaveSingleItem();
        crumbs[0].FindByClass("breadcrumb-separator").ShouldHaveSingleItem();

        // The first collapsed crumb hosts the indicator and keeps its separator.
        crumbs[1].ShouldHaveClass("breadcrumb-collapsed");
        var indicator = crumbs[1].FindByClass("breadcrumbs-collapsed-indicator").ShouldHaveSingleItem();
        indicator.FindByClass("ion-icon").ShouldHaveSingleItem();
        crumbs[1].FindByClass("breadcrumb-separator").ShouldHaveSingleItem();

        // The remaining collapsed crumbs render nothing but their (hidden) native content.
        crumbs[2].ShouldHaveClass("breadcrumb-collapsed");
        crumbs[2].FindByClass("breadcrumb-separator").ShouldBeEmpty();
        crumbs[2].FindByClass("breadcrumbs-collapsed-indicator").ShouldBeEmpty();
        crumbs[3].ShouldHaveClass("breadcrumb-collapsed");
        crumbs[3].FindByClass("breadcrumb-separator").ShouldBeEmpty();

        // Exactly one indicator overall.
        cut.FindByClass("breadcrumbs-collapsed-indicator").Count.ShouldBe(1);

        // The last crumb is unaffected: active, no separator.
        crumbs[4].ShouldNotHaveClass("breadcrumb-collapsed");
        crumbs[4].ShouldHaveClass("breadcrumb-active");
        crumbs[4].FindByClass("breadcrumb-separator").ShouldBeEmpty();
    }

    [Fact]
    public void IonBreadcrumbs_MaxItems_CollapsedCrumbHidesNativeContent()
    {
        // A collapsed crumb's native content has display:none, so it is pruned from the layout
        // tree (no computed style is collected). Assert on the matched stylesheet rule instead:
        // the breadcrumb-collapsed rule hides .breadcrumb-native (Ionic
        // :host(.breadcrumb-collapsed) .breadcrumb-native { display: none }).
        var sheet = IonicStyleSheetFactory.CreateAllModes();

        var native = new Miko.Core.DomElements.DivElement { Class = "breadcrumb-native" };
        var collapsedCrumb = new Miko.Core.DomElements.DivElement { Class = "md ion-breadcrumb breadcrumb-collapsed" };
        collapsedCrumb.AddChild(native);

        var rule = sheet.Rules
            .Where(r => r.Selector.Matches(native))
            .OrderByDescending(r => r.Selector.Specificity)
            .FirstOrDefault(r => r.Style.Display is not null);

        rule.ShouldNotBeNull();
        rule.Style.Display!.Value.Value.ShouldBe(Display.None);
    }

    [Fact]
    public void IonBreadcrumbs_MaxItems_DoesNotCollapse_WhenCountWithinMax()
    {
        var cut = RenderBreadcrumbs(Context, ThreeCrumbs(), p =>
            p.Add(nameof(IonBreadcrumbs.MaxItems), 5));

        cut.FindByClass("breadcrumbs-collapsed-indicator").ShouldBeEmpty();
        cut.FindByClass("breadcrumb-collapsed").ShouldBeEmpty();
    }

    [Fact]
    public void IonBreadcrumbs_MaxItems_DoesNotCollapse_WhenBeforePlusAfterExceedsMax()
    {
        var cut = RenderBreadcrumbs(Context, FiveCrumbs(), p =>
        {
            p.Add(nameof(IonBreadcrumbs.MaxItems), 3);
            p.Add(nameof(IonBreadcrumbs.ItemsBeforeCollapse), 2);
            p.Add(nameof(IonBreadcrumbs.ItemsAfterCollapse), 2);
        });

        cut.FindByClass("breadcrumbs-collapsed-indicator").ShouldBeEmpty();
        cut.FindByClass("breadcrumb-collapsed").ShouldBeEmpty();
    }

    [Fact]
    public void IonBreadcrumbs_ItemsAfterCollapseZero_IndicatorCrumbActsAsLast()
    {
        var cut = RenderBreadcrumbs(Context, FiveCrumbs(), p =>
        {
            p.Add(nameof(IonBreadcrumbs.MaxItems), 3);
            p.Add(nameof(IonBreadcrumbs.ItemsAfterCollapse), 0);
        });

        var crumbs = cut.FindByClass("ion-breadcrumb");

        // The indicator crumb becomes the "last" one: active, no trailing separator.
        crumbs[1].ShouldHaveClass("breadcrumb-collapsed");
        crumbs[1].FindByClass("breadcrumbs-collapsed-indicator").ShouldHaveSingleItem();
        crumbs[1].FindByClass("breadcrumb-separator").ShouldBeEmpty();
        crumbs[1].ShouldHaveClass("breadcrumb-active");

        // The actual last crumb collapses silently (Ionic behavior when itemsAfterCollapse = 0).
        crumbs[4].ShouldHaveClass("breadcrumb-collapsed");
        crumbs[4].ShouldNotHaveClass("breadcrumb-active");
        crumbs[4].FindByClass("breadcrumb-separator").ShouldBeEmpty();
    }

    [Fact]
    public void IonBreadcrumbs_CollapsedIndicatorClick_RaisesOnCollapsedClick()
    {
        IonBreadcrumbCollapsedClickEventArgs? received = null;
        var cut = RenderBreadcrumbs(Context, FiveCrumbs(), p =>
        {
            p.Add(nameof(IonBreadcrumbs.MaxItems), 4);
            p.Add(nameof(IonBreadcrumbs.OnCollapsedClick),
                EventCallback.Factory.Create<IonBreadcrumbCollapsedClickEventArgs>(this, args => received = args));
        });

        var indicator = cut.FindByClass("breadcrumbs-collapsed-indicator").ShouldHaveSingleItem();
        indicator.OnClick.ShouldNotBeNull();
        indicator.OnClick!.Invoke(new MouseEventArgs { Target = indicator });

        received.ShouldNotBeNull();
        // The collapsed set is the middle three crumbs, in document order.
        var crumbs = cut.FindByClass("ion-breadcrumb");
        received!.CollapsedBreadcrumbs.Count.ShouldBe(3);
        received.CollapsedBreadcrumbs.ShouldBe(new[] { crumbs[1], crumbs[2], crumbs[3] });
    }

    // ---- Color ---------------------------------------------------------

    [Fact]
    public void IonBreadcrumbs_Color_RecolorsEveryCrumbToPaletteBase()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderBreadcrumbs(Context, ThreeCrumbs(), p =>
            p.Add(nameof(IonBreadcrumbs.Color), "primary"));

        var primary = IonicTheme.CreateMd().Primary;
        var crumbs = cut.FindByClass("ion-breadcrumb");

        foreach (var crumb in crumbs)
        {
            crumb.ShouldHaveClass("in-breadcrumbs-color");
            var native = crumb.FindByClass("breadcrumb-native").ShouldHaveSingleItem();
            cut.GetComputedStyle(native)!.Color.ShouldBe(primary);
        }

        // A separator also takes the palette base (Ionic
        // :host(.in-breadcrumbs-color) .breadcrumb-separator).
        var separator = crumbs[0].FindByClass("breadcrumb-separator").ShouldHaveSingleItem();
        cut.GetComputedStyle(separator)!.Color.ShouldBe(primary);
    }

    [Fact]
    public void IonBreadcrumbs_Color_WinsOverActiveCrumbColor()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderBreadcrumbs(Context, ThreeCrumbs(), p =>
            p.Add(nameof(IonBreadcrumbs.Color), "primary"));

        // The active (last) crumb takes the palette base, not the active text color (Ionic
        // :host(.in-breadcrumbs-color.breadcrumb-active)).
        var activeNative = cut.FindByClass("ion-breadcrumb")[^1]
            .FindByClass("breadcrumb-native").ShouldHaveSingleItem();
        cut.GetComputedStyle(activeNative)!.Color.ShouldBe(IonicTheme.CreateMd().Primary);
    }

    [Fact]
    public void IonBreadcrumb_Color_RecolorsNativeContent()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonBreadcrumb>(p =>
        {
            p.Add(nameof(IonBreadcrumb.Color), "danger");
            p.Add(nameof(IonBreadcrumb.ChildContent), (RenderFragment)(b => b.AddContent(0, "Home")));
        });

        var native = cut.FindByClass("breadcrumb-native").ShouldHaveSingleItem();
        cut.GetComputedStyle(native)!.Color.ShouldBe(IonicTheme.CreateMd().Danger);
    }
}
