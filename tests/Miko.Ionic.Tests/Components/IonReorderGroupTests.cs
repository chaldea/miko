using Miko.Components;
using Miko.Common;
using Miko.Core;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Styling;
using Miko.Animation;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-reorder-group</c> — the container that owns whether its nested reorder handles
/// are enabled. Covers the DOM contract, the default-disabled behavior (Ionic defaults
/// <c>disabled</c> to true), the disabled class stamping, and the disabled flag cascaded down to the
/// nested <see cref="IonReorder"/> children via <see cref="IonReorderGroupContext"/>.
/// </summary>
public class IonReorderGroupTests : IonicComponentTestBase
{
    private static RenderFragment Reorder() => builder =>
    {
        builder.OpenComponent<IonReorder>(0);
        builder.CloseComponent();
    };

    private static ComponentUnderTest RenderGroup(TestContext ctx,
        Action<ComponentParameterBuilder<IonReorderGroup>>? configure = null)
        => ctx.Render<IonReorderGroup>(p =>
        {
            p.Add(nameof(IonReorderGroup.ChildContent), Reorder());
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonReorderGroup_RendersDomContract()
    {
        var cut = RenderGroup(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-reorder-group");
        cut.FindByClass("ion-reorder").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonReorderGroup_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderGroup(Context);

        cut.Root.Class.ShouldStartWith("ios ion-reorder-group");
    }

    // ---- Disabled default / class -----------------------------------------

    [Fact]
    public void IonReorderGroup_IsDisabledByDefault_StampsClass()
    {
        // Ionic defaults disabled to true; the group must be explicitly enabled.
        var cut = RenderGroup(Context);

        cut.Root.ShouldHaveClass("reorder-group-disabled");
    }

    [Fact]
    public void IonReorderGroup_Enabled_DoesNotStampDisabledClass()
    {
        var cut = RenderGroup(Context, p => p.Add(nameof(IonReorderGroup.Disabled), false));

        cut.Root.ShouldNotHaveClass("reorder-group-disabled");
    }

    // ---- Disabled cascade --------------------------------------------------

    [Fact]
    public void IonReorderGroup_CascadesDisabled_ToReorderChildren()
    {
        // Default (disabled) group: nested reorder handles are hidden.
        var cut = RenderGroup(Context);

        cut.FindByClass("ion-reorder").ShouldHaveSingleItem()
            .ShouldHaveClass("reorder-hidden");
    }

    [Fact]
    public void IonReorderGroup_CascadesEnabled_ToReorderChildren()
    {
        var cut = RenderGroup(Context, p => p.Add(nameof(IonReorderGroup.Disabled), false));

        cut.FindByClass("ion-reorder").ShouldHaveSingleItem()
            .ShouldHaveClass("reorder-enabled");
    }

    [Fact]
    public void IonReorderGroup_CascadedContext_ReflectsDisabledFlag()
    {
        var group = new IonReorderGroup { Disabled = false };
        group.ChildContent = Reorder();
        group.Build();   // runs OnParametersSet → rebuilds the cascaded context

        CascadedContext(group).Disabled.ShouldBeFalse();
    }

    [Fact]
    public void IonReorderGroup_DraggingHandle_RaisesFromAndToIndexes()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        (int From, int To)? result = null;
        var cut = Context.Render<IonReorderGroup>(p =>
        {
            p.Add(nameof(IonReorderGroup.Disabled), false);
            p.Add(nameof(IonReorderGroup.OnItemReorder),
                EventCallback.Factory.Create<(int From, int To)>(this, value => result = value));
            p.Add(nameof(IonReorderGroup.ChildContent), Items(3));
        });

        var handles = cut.FindByClass("ion-reorder");
        handles.Count.ShouldBe(3);
        cut.Root.OnMouseDown.ShouldNotBeNull();
        var dispatcher = new EventDispatcher();

        dispatcher.Dispatch(handles[0], EventTypes.MouseDown, MouseArgs(handles[0], 10, true));
        dispatcher.Dispatch(handles[0], EventTypes.MouseMove, MouseArgs(handles[0], 80, true));

        var selected = cut.FindByClass("reorder-selected").ShouldHaveSingleItem();
        TranslateY(selected).ShouldBe(70f);
        selected.Style!.Position!.Value.Value.ShouldBe(Position.Relative);
        selected.Style.ZIndex!.Value.Value.ShouldBe(101);

        var shifted = cut.FindByClass("reorder-item-shift-up");
        shifted.Count.ShouldBe(2);
        shifted.ShouldAllBe(item => TranslateY(item) == -20f);
        shifted.ShouldAllBe(item => item.Style!.Transitions!.Value.Value.Count == 0);

        var live = Context.RenderElement(cut.Root);
        var selectedStyle = live.GetComputedStyle(selected)!;
        selectedStyle.Opacity.ShouldBe(0.8f);
        selectedStyle.BoxShadow!.Value.Value.ShouldHaveSingleItem();

        dispatcher.Dispatch(handles[0], EventTypes.MouseUp, MouseArgs(handles[0], 80, false));

        result.ShouldBe((0, 2));
        cut.Root.ShouldNotHaveClass("reorder-list-active");
        cut.FindByClass("reorder-selected").ShouldBeEmpty();
        cut.FindByClass("reorder-item-shift-up").ShouldBeEmpty();
        selected.Style!.Transform.ShouldBeNull();
    }

    [Fact]
    public void IonReorderGroup_DraggingWithinTargetRange_UpdatesSiblingOffsetContinuously()
    {
        var cut = Context.Render<IonReorderGroup>(p =>
        {
            p.Add(nameof(IonReorderGroup.Disabled), false);
            p.Add(nameof(IonReorderGroup.ChildContent), Items(3));
        });

        var handle = cut.FindByClass("ion-reorder")[0];
        var dispatcher = new EventDispatcher();
        dispatcher.Dispatch(handle, EventTypes.MouseDown, MouseArgs(handle, 10, true));

        dispatcher.Dispatch(handle, EventTypes.MouseMove, MouseArgs(handle, 20, true));
        var firstSibling = cut.FindByClass("reorder-item-shift-up").ShouldHaveSingleItem();
        TranslateY(firstSibling).ShouldBe(-10f);
        firstSibling.Style!.Transitions!.Value.Value.ShouldBeEmpty();

        dispatcher.Dispatch(handle, EventTypes.MouseMove, MouseArgs(handle, 31, true));
        var shifted = cut.FindByClass("reorder-item-shift-up");
        shifted.Count.ShouldBe(2);
        TranslateY(shifted[0]).ShouldBe(-20f);
        TranslateY(shifted[1]).ShouldBe(-1f);

        dispatcher.Dispatch(handle, EventTypes.MouseMove, MouseArgs(handle, 35, true));
        shifted = cut.FindByClass("reorder-item-shift-up");
        TranslateY(shifted[0]).ShouldBe(-20f);
        TranslateY(shifted[1]).ShouldBe(-5f);
        shifted.ShouldAllBe(item => item.Style!.Transitions!.Value.Value.Count == 0);
    }

    [Fact]
    public void IonReorderGroup_Release_KeepsFinalVisualsUntilCallbackCompletes()
    {
        Element? selected = null;
        var selectedWasActiveInCallback = false;
        var selectedOffsetInCallback = 0f;
        (int From, int To)? result = null;
        var cut = Context.Render<IonReorderGroup>(p =>
        {
            p.Add(nameof(IonReorderGroup.Disabled), false);
            p.Add(nameof(IonReorderGroup.OnItemReorder),
                EventCallback.Factory.Create<(int From, int To)>(this, value =>
                {
                    result = value;
                    selectedWasActiveInCallback = selected!.HasClass("reorder-selected");
                    selectedOffsetInCallback = TranslateY(selected);
                }));
            p.Add(nameof(IonReorderGroup.ChildContent), Items(3));
        });

        var handle = cut.FindByClass("ion-reorder")[0];
        var dispatcher = new EventDispatcher();
        dispatcher.Dispatch(handle, EventTypes.MouseDown, MouseArgs(handle, 10, true));
        dispatcher.Dispatch(handle, EventTypes.MouseMove, MouseArgs(handle, 50, true));
        selected = cut.FindByClass("reorder-selected").ShouldHaveSingleItem();

        dispatcher.Dispatch(handle, EventTypes.MouseUp, MouseArgs(handle, 50, false));

        result.ShouldBe((0, 2));
        selectedWasActiveInCallback.ShouldBeTrue();
        selectedOffsetInCallback.ShouldBe(40f);
        selected.HasClass("reorder-selected").ShouldBeFalse();
        selected.Style!.Transform.ShouldBeNull();
        cut.FindByClass("reorder-item-shift-up").ShouldBeEmpty();
    }

    [Fact]
    public void IonReorderGroup_DraggingUp_MovesPreviousItemDown()
    {
        (int From, int To)? result = null;
        var cut = Context.Render<IonReorderGroup>(p =>
        {
            p.Add(nameof(IonReorderGroup.Disabled), false);
            p.Add(nameof(IonReorderGroup.OnItemReorder),
                EventCallback.Factory.Create<(int From, int To)>(this, value => result = value));
            p.Add(nameof(IonReorderGroup.ChildContent), Items(3));
        });

        var handle = cut.FindByClass("ion-reorder")[1];
        var dispatcher = new EventDispatcher();
        dispatcher.Dispatch(handle, EventTypes.MouseDown, MouseArgs(handle, 50, true));
        dispatcher.Dispatch(handle, EventTypes.MouseMove, MouseArgs(handle, 20, true));

        TranslateY(cut.FindByClass("reorder-selected").ShouldHaveSingleItem()).ShouldBe(-30f);
        TranslateY(cut.FindByClass("reorder-item-shift-down").ShouldHaveSingleItem()).ShouldBe(20f);

        dispatcher.Dispatch(handle, EventTypes.MouseUp, MouseArgs(handle, 20, false));
        result.ShouldBe((1, 0));
    }

    [Fact]
    public void IonReorderGroup_DraggingUp_UpdatesSiblingOffsetContinuously()
    {
        var cut = Context.Render<IonReorderGroup>(p =>
        {
            p.Add(nameof(IonReorderGroup.Disabled), false);
            p.Add(nameof(IonReorderGroup.ChildContent), Items(3));
        });

        var handle = cut.FindByClass("ion-reorder")[2];
        var dispatcher = new EventDispatcher();
        dispatcher.Dispatch(handle, EventTypes.MouseDown, MouseArgs(handle, 50, true));
        dispatcher.Dispatch(handle, EventTypes.MouseMove, MouseArgs(handle, 40, true));

        var shiftedItem = cut.FindByClass("reorder-item-shift-down").ShouldHaveSingleItem();
        TranslateY(shiftedItem).ShouldBe(10f);
        shiftedItem.Style!.Transitions!.Value.Value.ShouldBeEmpty();

        dispatcher.Dispatch(handle, EventTypes.MouseMove, MouseArgs(handle, 25, true));
        var shifted = cut.FindByClass("reorder-item-shift-down");
        shifted.Count.ShouldBe(2);
        TranslateY(shifted[0]).ShouldBe(5f);
        TranslateY(shifted[1]).ShouldBe(20f);
    }

    [Fact]
    public void IonReorderGroup_DisabledHandle_DoesNotStartDrag()
    {
        (int From, int To)? result = null;
        var cut = Context.Render<IonReorderGroup>(p =>
        {
            p.Add(nameof(IonReorderGroup.OnItemReorder),
                EventCallback.Factory.Create<(int From, int To)>(this, value => result = value));
            p.Add(nameof(IonReorderGroup.ChildContent), Items(2));
        });

        var handle = cut.FindByClass("ion-reorder")[0];
        var dispatcher = new EventDispatcher();
        dispatcher.Dispatch(handle, EventTypes.MouseDown, MouseArgs(handle, 10, true));
        dispatcher.Dispatch(handle, EventTypes.MouseUp, MouseArgs(handle, 50, false));

        result.ShouldBeNull();
        cut.FindByClass("reorder-selected").ShouldBeEmpty();
    }

    private static RenderFragment Items(int count) => builder =>
    {
        for (var index = 0; index < count; index++)
        {
            builder.OpenElement(index * 3, "div");
            builder.AddAttribute(index * 3 + 1, "style", new Style { Height = Length.Px(20) });
            builder.OpenComponent<IonReorder>(index * 3 + 2);
            builder.CloseComponent();
            builder.CloseElement();
        }
    };

    private static MouseEventArgs MouseArgs(Element target, float y, bool pressed)
        => new()
        {
            Target = target,
            Y = y,
            IsButtonPressed = pressed,
            Button = MouseButton.Left,
            TargetHeight = target.OffsetHeight,
        };

    private static float TranslateY(Element element)
        => element.Style!.Transform!.Value.Value.Functions
            .OfType<TransformFunction.TranslateY>()
            .Last()
            .Y.Value;

    // Reads back the IonReorderGroupContext the group cascades to its reorder handles — the same
    // value a child reads via [CascadingParameter].
    private static IonReorderGroupContext CascadedContext(IonReorderGroup group)
    {
        var field = typeof(IonReorderGroup).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        return (IonReorderGroupContext)field.GetValue(group)!;
    }
}
