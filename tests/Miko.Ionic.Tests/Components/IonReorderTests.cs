using Miko.Common;
using Miko.Components;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-reorder</c> — the drag handle placed inside a reorderable list item. Covers the
/// DOM contract, the default handle icon when no child content is provided, the enabled/hidden state
/// derived from the enclosing <see cref="IonReorderGroup"/>'s cascaded disabled flag, and mode
/// stamping.
/// <para>
/// This is a structural / visual port: the live drag-to-reorder gesture is not implemented, so there
/// is no interaction to drive here — only the class + cascade contract.
/// </para>
/// </summary>
public class IonReorderTests : IonicComponentTestBase
{
    // A reorder handle (with no custom child, so the default icon is used).
    private static RenderFragment Reorder() => builder =>
    {
        builder.OpenComponent<IonReorder>(0);
        builder.CloseComponent();
    };

    // Renders a reorder handle inside a group so the group's disabled flag cascades to it.
    private static ComponentUnderTest RenderInGroup(TestContext ctx, bool disabled)
        => ctx.Render<IonReorderGroup>(p =>
        {
            p.Add(nameof(IonReorderGroup.Disabled), disabled);
            p.Add(nameof(IonReorderGroup.ChildContent), Reorder());
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonReorder_RendersDomContract()
    {
        var cut = Context.Render<IonReorder>();

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-reorder");
    }

    [Fact]
    public void IonReorder_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = Context.Render<IonReorder>();

        cut.Root.Class.ShouldStartWith("ios ion-reorder");
    }

    [Fact]
    public void IonReorder_RendersDefaultIcon_WhenNoChildContent()
    {
        var cut = Context.Render<IonReorder>();

        // The default drag glyph is rendered as an IonIcon carrying the reorder-icon class.
        cut.FindByClass("reorder-icon").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonReorder_RendersCustomChild_InsteadOfDefaultIcon()
    {
        var cut = Context.Render<IonReorder>(p =>
            p.Add(nameof(IonReorder.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-handle");
                builder.CloseElement();
            })));

        cut.FindByClass("custom-handle").ShouldHaveSingleItem();
        // The default icon should not be rendered when a custom child is provided.
        cut.FindByClass("reorder-icon").ShouldBeEmpty();
    }

    // ---- Enabled / hidden state from the group ----------------------------

    [Fact]
    public void IonReorder_NoGroup_IsHidden()
    {
        // Rendered standalone (no enclosing group), the handle is hidden and not enabled.
        var cut = Context.Render<IonReorder>();

        cut.Root.ShouldHaveClass("reorder-hidden");
        cut.Root.ShouldNotHaveClass("reorder-enabled");
    }

    [Fact]
    public void IonReorder_InDisabledGroup_IsHidden()
    {
        var cut = RenderInGroup(Context, disabled: true);

        var reorder = cut.FindByClass("ion-reorder").ShouldHaveSingleItem();
        reorder.ShouldHaveClass("reorder-hidden");
        reorder.ShouldNotHaveClass("reorder-enabled");
    }

    [Fact]
    public void IonReorder_InEnabledGroup_IsEnabled()
    {
        var cut = RenderInGroup(Context, disabled: false);

        var reorder = cut.FindByClass("ion-reorder").ShouldHaveSingleItem();
        reorder.ShouldHaveClass("reorder-enabled");
        reorder.ShouldNotHaveClass("reorder-hidden");
    }

    // ---- Key style ---------------------------------------------------------

    [Fact]
    public void IonReorder_Style_HiddenByDefault()
    {
        // The host is display:none until an enabling group reveals it. A collapsed/hidden host is
        // pruned from the layout tree, so assert on the matched stylesheet rule instead.
        var sheet = IonicStyleSheetFactory.CreateAllModes();

        var handle = new Miko.Core.DomElements.DivElement { Class = "md ion-reorder reorder-hidden" };

        var rule = sheet.Rules
            .Where(r => r.Selector.Matches(handle))
            .OrderByDescending(r => r.Selector.Specificity)
            .FirstOrDefault(r => r.Style.Display is not null);

        rule.ShouldNotBeNull();
        rule.Style.Display!.Value.Value.ShouldBe(Display.None);
    }

    [Fact]
    public void IonReorder_Style_EnabledShowsBlock()
    {
        var sheet = IonicStyleSheetFactory.CreateAllModes();

        var handle = new Miko.Core.DomElements.DivElement { Class = "md ion-reorder reorder-enabled" };

        var rule = sheet.Rules
            .Where(r => r.Selector.Matches(handle))
            .OrderByDescending(r => r.Selector.Specificity)
            .FirstOrDefault(r => r.Style.Display is not null);

        rule.ShouldNotBeNull();
        rule.Style.Display!.Value.Value.ShouldBe(Display.Block);
    }
}
