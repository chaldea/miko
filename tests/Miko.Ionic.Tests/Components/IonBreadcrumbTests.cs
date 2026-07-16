using Miko.Common;
using Miko.Components;
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
}
