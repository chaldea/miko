using Miko.Components;
using Miko.Ionic.Components;
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

    // Reads back the IonReorderGroupContext the group cascades to its reorder handles — the same
    // value a child reads via [CascadingParameter].
    private static IonReorderGroupContext CascadedContext(IonReorderGroup group)
    {
        var field = typeof(IonReorderGroup).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        return (IonReorderGroupContext)field.GetValue(group)!;
    }
}
