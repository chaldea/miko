using Miko.Common;
using Miko.Components;
using Miko.Core.DomElements;
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
/// Gesture behavior belongs to <see cref="IonReorderGroupTests"/>; these tests cover the handle's
/// rendering and enabled/disabled state.
/// </para>
/// </summary>
public class IonReorderTests : IonicComponentTestBase
{
    // A reorder handle (with no custom child, so the default icon is used).
    private static RenderFragment Reorder() => builder =>
    {
        builder.OpenComponent<IonReorder>();
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
                var __e1 = builder.OpenElement<SpanElement>();
                __e1.Class = "custom-handle";
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

    // Real nesting: IonReorderGroup > IonList-less items, each the shape of Ionic's two usages —
    // a handle in the item's End slot, or an IonReorder wrapping the whole item.
    private ComponentUnderTest RenderStyledGroup(bool disabled, bool wrapper)
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        return Context.Render<IonReorderGroup>(p =>
        {
            p.Add(nameof(IonReorderGroup.Disabled), disabled);
            p.Add(nameof(IonReorderGroup.ChildContent), (RenderFragment)(b =>
            {
                if (wrapper)
                {
                    var reorder = b.OpenComponent<IonReorder>();
                    reorder.ChildContent = r =>
                    {
                        var item = r.OpenComponent<IonItem>();
                        item.ChildContent = text => text.AddContent("Wrapped item");
                        r.CloseComponent();
                    };
                    b.CloseComponent();
                }
                else
                {
                    var item = b.OpenComponent<IonItem>();
                    item.ChildContent = text => text.AddContent("Item");
                    item.End = end =>
                    {
                        end.OpenComponent<IonReorder>();
                        end.CloseComponent();
                    };
                    b.CloseComponent();
                }
            }));
        });
    }

    [Fact]
    public void IonReorder_Style_SlottedHandle_HiddenInDisabledGroup()
    {
        var cut = RenderStyledGroup(disabled: true, wrapper: false);

        // display:none prunes the handle from the layout tree, so it has no computed style.
        cut.GetComputedStyle(cut.FindByClass("ion-reorder").Single()).ShouldBeNull();
    }

    [Fact]
    public void IonReorder_Style_SlottedHandle_ShownInEnabledGroup()
    {
        var cut = RenderStyledGroup(disabled: false, wrapper: false);

        cut.GetComputedStyle(cut.FindByClass("ion-reorder").Single())!.Display.ShouldBe(Display.Block);
    }

    /// <summary>
    /// ISSUE-153: a disabled group hid every IonReorder, including one wrapping a whole item, so
    /// the items vanished. Ionic hides only slotted handles (reorder.scss <c>:host([slot])</c>).
    /// </summary>
    [Fact]
    public void IonReorder_Style_WrapperKeepsItsItemVisibleInDisabledGroup()
    {
        var cut = RenderStyledGroup(disabled: true, wrapper: true);

        var item = cut.FindByClass("ion-item").Single();
        cut.GetComputedStyle(cut.FindByClass("ion-reorder").Single()).ShouldNotBeNull();
        cut.GetBoxModel(item)!.BorderBox.Height.ShouldBeGreaterThan(0f);
    }
}
