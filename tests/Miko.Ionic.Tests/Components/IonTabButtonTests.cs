using Miko.Common;
using Miko.Components;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Styling;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-tab-button</c>. Covers the DOM contract, the selected-state class, and the
/// badge overlay positioning — a badge dropped into a tab button must float over the top-right of
/// the icon (absolutely positioned), not flow below the label as a normal column item.
/// </summary>
public class IonTabButtonTests : IonicComponentTestBase
{
    private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

    // A tab button holding an icon, label and a badge — the canonical "icon-top" layout from the
    // ion-tab.md example.
    private static RenderFragment IconLabelBadge(string badgeText) => builder =>
    {
        builder.OpenComponent<IonIcon>(0);
        builder.AddComponentParameter(1, nameof(IonIcon.Icon), "heart");
        builder.CloseComponent();

        builder.OpenComponent<IonLabel>(2);
        builder.AddComponentParameter(3, nameof(IonLabel.ChildContent), Text("Favorites"));
        builder.CloseComponent();

        builder.OpenComponent<IonBadge>(4);
        builder.AddComponentParameter(5, nameof(IonBadge.Color), "danger");
        builder.AddComponentParameter(6, nameof(IonBadge.ChildContent), Text(badgeText));
        builder.CloseComponent();
    };

    private static ComponentUnderTest RenderButton(TestContext ctx, RenderFragment child,
        Action<ComponentParameterBuilder<IonTabButton>>? configure = null)
        => ctx.Render<IonTabButton>(p =>
        {
            p.Add(nameof(IonTabButton.ChildContent), child);
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonTabButton_RendersDomContract()
    {
        var cut = RenderButton(Context, IconLabelBadge("47"), p => p.Add(nameof(IonTabButton.Tab), "tab1"));

        cut.Root.TagName.ShouldBe("a");
        cut.Root.ShouldHaveClass("md ion-tab-button");
        cut.FindByClass("ion-badge").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonTabButton_Selected_StampsSelectedClass()
    {
        var cut = RenderButton(Context, IconLabelBadge("47"), p => p.Add(nameof(IonTabButton.Selected), true));

        cut.Root.ShouldHaveClass("tab-selected");
    }

    // ---- Badge overlay positioning ----------------------------------------

    [Fact]
    public void IonTabButton_IsRelative_SoBadgeAnchorsToIt()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderButton(Context, IconLabelBadge("47"));

        // The tab button is the containing block for its absolutely-positioned badge.
        cut.GetComputedStyle(cut.Root)!.Position.ShouldBe(Position.Relative);
    }

    [Fact]
    public void IonTabButton_Badge_IsAbsolutelyPositioned_Md()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderButton(Context, IconLabelBadge("47"));
        var badge = cut.FindByClass("ion-badge").Single();
        var style = cut.GetComputedStyle(badge)!;

        // md: top: 8px; left: calc(50% + 6px) — floated over the icon's top-right.
        style.Position.ShouldBe(Position.Absolute);
        style.Top.Value.ShouldBe(8f);
    }

    [Fact]
    public void IonTabButton_Badge_IsAbsolutelyPositioned_Ios()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderButton(Context, IconLabelBadge("47"));
        var badge = cut.FindByClass("ion-badge").Single();
        var style = cut.GetComputedStyle(badge)!;

        // ios: top: 4px; left: calc(50% + 6px).
        style.Position.ShouldBe(Position.Absolute);
        style.Top.Value.ShouldBe(4f);
    }

    // ---- Tab-button badge overrides (tab-button.md.scss / tab-button.ios.scss) ---------------
    // A badge inside a tab button is restyled for the bar: much smaller than a standalone badge.

    [Fact]
    public void IonTabButton_Badge_UsesMdTabBadgeStyle()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderButton(Context, IconLabelBadge("47"));
        var style = cut.GetComputedStyle(cut.FindByClass("ion-badge").Single())!;

        // tab-button.md.vars.scss: 8px font, 3/2/2/2 padding, min-width 12, radius 8, normal weight.
        style.FontSize.Value.ShouldBe(8f);
        style.PaddingTop.Value.ShouldBe(3f);
        style.PaddingRight.Value.ShouldBe(2f);
        style.PaddingBottom.Value.ShouldBe(2f);
        style.PaddingLeft.Value.ShouldBe(2f);
        style.MinWidth.Value.ShouldBe(12f);
        style.BorderTopLeftRadius.Value.ShouldBe(8f);
        style.FontWeight.ShouldBe(FontWeight.Normal);
        style.BoxSizing.ShouldBe(BoxSizing.BorderBox);
    }

    [Fact]
    public void IonTabButton_Badge_UsesIosTabBadgeStyle()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderButton(Context, IconLabelBadge("47"));
        var style = cut.GetComputedStyle(cut.FindByClass("ion-badge").Single())!;

        // tab-button.ios.scss: 12px font, 1/6 padding, 16px line-height; base bold weight and
        // 10px radius / min-width carry over.
        style.FontSize.Value.ShouldBe(12f);
        style.PaddingTop.Value.ShouldBe(1f);
        style.PaddingRight.Value.ShouldBe(6f);
        style.PaddingBottom.Value.ShouldBe(1f);
        style.PaddingLeft.Value.ShouldBe(6f);
        style.LineHeight.Value.ShouldBe(16f);
        style.FontWeight.ShouldBe(FontWeight.Bold);
        style.MinWidth.Value.ShouldBe(10f);
        style.BorderTopLeftRadius.Value.ShouldBe(10f);
    }

    [Fact]
    public void IonTabButton_Badge_Empty_RendersAsEightPixelDot_Md()
    {
        var cut = RenderTabBar(emptyBadge: true);

        var badge = cut.FindByClass("ion-badge").Single();
        var style = cut.GetComputedStyle(badge)!;

        // tab-button.md.scss ::slotted(ion-badge:empty): the base `display: none` is overridden and
        // the badge collapses to an 8x8 dot.
        style.Display.ShouldBe(Display.Block);
        style.MinWidth.Value.ShouldBe(8f);
        style.Height.Value.ShouldBe(8f);

        var badgeBox = cut.GetBoxModel(badge)!;
        badgeBox.MarginBox.Width.ShouldBe(8f);
        badgeBox.MarginBox.Height.ShouldBe(8f);
    }

    [Fact]
    public void IonTabButton_Badge_Empty_StaysHidden_Ios()
    {
        // tab-button.ios.scss has no :empty override, so the base badge rule hides it. display:none
        // elements are pruned from the layout tree, so assert on the matched stylesheet rule.
        var sheet = IonicStyleSheetFactory.CreateAllModes();

        var badge = new Miko.Core.DomElements.DivElement { Class = "ios ion-badge ion-color ion-color-danger" };
        var tabButton = new Miko.Core.DomElements.DivElement { Class = "ios ion-tab-button" };
        tabButton.AddChild(badge);

        var rule = sheet.Rules
            .Where(r => r.Selector.Matches(badge))
            .OrderByDescending(r => r.Selector.Specificity)
            .FirstOrDefault(r => r.Style.Display is not null);

        rule.ShouldNotBeNull();
        rule.Style.Display!.Value.Value.ShouldBe(Display.None);
    }

    // Regression for the reported bug: the badge floated below the label instead of over the icon.
    // The overlay's painted box must sit in the top band of the button (past its left half), not in
    // the lower flow region where the label lives.
    [Fact]
    public void IonTabButton_Badge_RendersInTopRegion_NotBelowLabel()
    {
        // Render inside a bar so the button gets a real (non-viewport) height to anchor against.
        var cut = RenderTabBar();

        var button = cut.FindByClass("ion-tab-button").First();
        var badge = cut.FindByClass("ion-badge").Single();

        var buttonBox = cut.GetBoxModel(button)!;
        var badgeBox = cut.GetBoxModel(badge)!;

        // Badge pinned near the top edge of the button (top: 8px in md), well above vertical center.
        badgeBox.MarginBox.Top.ShouldBeLessThan(buttonBox.Content.Top + buttonBox.Content.Height / 2f);
        // ...and offset to the right of the button's horizontal center (left: calc(50% + 6px)).
        var buttonCenterX = buttonBox.Content.Left + buttonBox.Content.Width / 2f;
        badgeBox.MarginBox.Left.ShouldBeGreaterThan(buttonCenterX);
    }

    // A tab bar with one badge-bearing button, laid out so its buttons get a concrete height.
    private ComponentUnderTest RenderTabBar(bool emptyBadge = false) => Context.Render<IonTabBar>(p =>
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        p.Add(nameof(IonTabBar.ChildContent), (RenderFragment)(builder =>
        {
            builder.OpenComponent<IonTabButton>(0);
            builder.AddComponentParameter(1, nameof(IonTabButton.Tab), "tab1");
            builder.AddComponentParameter(2, nameof(IonTabButton.ChildContent),
                emptyBadge ? EmptyBadge() : IconLabelBadge("47"));
            builder.CloseComponent();
        }));
    });

    // Icon + label + an empty badge (the notification-dot shape).
    private static RenderFragment EmptyBadge() => builder =>
    {
        builder.OpenComponent<IonIcon>(0);
        builder.AddComponentParameter(1, nameof(IonIcon.Icon), "heart");
        builder.CloseComponent();

        builder.OpenComponent<IonLabel>(2);
        builder.AddComponentParameter(3, nameof(IonLabel.ChildContent), Text("Favorites"));
        builder.CloseComponent();

        builder.OpenComponent<IonBadge>(4);
        builder.AddComponentParameter(5, nameof(IonBadge.Color), "danger");
        builder.CloseComponent();
    };
}
