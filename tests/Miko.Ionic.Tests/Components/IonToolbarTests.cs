using Miko.Components;
using Miko.Core;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ISSUE: <c>ion-toolbar</c> must expose Ionic's slots (<c>start</c>, <c>secondary</c>, default,
/// <c>primary</c>, <c>end</c>) as <c>RenderFragment</c> parameters so callers can place
/// <c>IonButtons</c>/<c>IonTitle</c> content at the leading/trailing edges of the toolbar.
/// </summary>
public class IonToolbarTests : IonicComponentTestBase
{
    // A render fragment that emits a single <IonButtons slot="..."> wrapper, so each slot
    // produces an identifiable .ion-buttons element in the rendered DOM.
    private static RenderFragment Buttons(string slot) => builder =>
    {
        builder.OpenComponent<IonButtons>(0);
        builder.AddComponentParameter(1, nameof(IonButtons.Slot), slot);
        builder.CloseComponent();
    };

    private static RenderFragment Title => builder =>
    {
        builder.OpenComponent<IonTitle>(0);
        builder.CloseComponent();
    };

    [Fact]
    public void IonToolbar_RendersWithCorrectClassAndStructure()
    {
        var cut = Context.Render<IonToolbar>();

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-toolbar");
        // Faithful port of toolbar.tsx: background layer + container wrapping a content region.
        cut.FindByClass("toolbar-background").Count.ShouldBe(1);
        cut.FindByClass("toolbar-container").Count.ShouldBe(1);
        cut.FindByClass("toolbar-content").Count.ShouldBe(1);
    }

    [Fact]
    public void IonToolbar_RendersAllSlots()
    {
        var cut = Context.Render<IonToolbar>(p => p
            .Add(nameof(IonToolbar.Start), Buttons("start"))
            .Add(nameof(IonToolbar.Secondary), Buttons("secondary"))
            .AddChildContent(Title)
            .Add(nameof(IonToolbar.Primary), Buttons("primary"))
            .Add(nameof(IonToolbar.End), Buttons("end")));

        // Every slot rendered into the toolbar container.
        cut.FindByClass("ion-buttons").Count.ShouldBe(4);
        cut.FindByClass("ion-title").Count.ShouldBe(1);
    }

    [Fact]
    public void IonToolbar_RendersSlotsInDocumentOrder()
    {
        var cut = Context.Render<IonToolbar>(p => p
            .Add(nameof(IonToolbar.Start), Buttons("start"))
            .Add(nameof(IonToolbar.Secondary), Buttons("secondary"))
            .AddChildContent(Title)
            .Add(nameof(IonToolbar.Primary), Buttons("primary"))
            .Add(nameof(IonToolbar.End), Buttons("end")));

        var container = cut.FindByClass("toolbar-container").ShouldHaveSingleItem();

        // Container holds, in source order: start, secondary, toolbar-content (default slot),
        // primary, end. CSS order would reorder these visually per mode, but the DOM is fixed.
        var slots = container.Children;
        slots.Count.ShouldBe(5);
        slots[0].ShouldHaveClass("ion-buttons");      // Start slot
        slots[1].ShouldHaveClass("ion-buttons");      // Secondary slot
        slots[2].ShouldHaveClass("toolbar-content");  // default ChildContent wrapper
        slots[3].ShouldHaveClass("ion-buttons");      // Primary slot
        slots[4].ShouldHaveClass("ion-buttons");      // End slot

        // The default slot content (the title) sits inside toolbar-content.
        var content = cut.FindByClass("toolbar-content").ShouldHaveSingleItem();
        content.Children.ShouldHaveSingleItem().ShouldHaveClass("ion-title");
    }

    [Fact]
    public void IonToolbar_WithNoSlots_HasEmptyContent()
    {
        var cut = Context.Render<IonToolbar>();

        // The container always carries the toolbar-content wrapper; with no slots set it is the
        // only child and is itself empty.
        var container = cut.FindByClass("toolbar-container").ShouldHaveSingleItem();
        container.Children.ShouldHaveSingleItem().ShouldHaveClass("toolbar-content");
        cut.FindByClass("toolbar-content").ShouldHaveSingleItem().Children.Count.ShouldBe(0);
    }

    [Fact]
    public void IonToolbar_MultipleNamedSlots_CanBeUsedSimultaneously()
    {
        // ISSUE-072: Verify that multiple named RenderFragment parameters (Start, End, etc.) can
        // be set at the same time without compilation errors. This tests the fix to IsIgnorableWhitespace
        // in ComponentLoweringPass.cs that allows whitespace between named slot tags.
        var cut = Context.Render<IonToolbar>(p => p
            .Add(nameof(IonToolbar.Start), Buttons("start"))
            .Add(nameof(IonToolbar.End), Buttons("end")));

        // Both slots should be rendered
        var buttons = cut.FindByClass("ion-buttons");
        buttons.Count.ShouldBe(2);

        // Start should be first, End should be last
        var container = cut.FindByClass("toolbar-container").ShouldHaveSingleItem();
        container.Children[0].ShouldHaveClass("ion-buttons"); // Start
        container.Children[^1].ShouldHaveClass("ion-buttons"); // End
    }
}
