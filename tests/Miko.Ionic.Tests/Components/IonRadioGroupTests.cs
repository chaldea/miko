using Miko.Components;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-radio-group</c>. Covers the DOM contract (the group div + cascaded radios), the
/// value cascade to radios, selecting a radio updating the group value and raising
/// <c>ValueChanged</c>, and the <c>AllowEmptySelection</c> toggle-off behavior. The single-select
/// selection rules are driven through the cascaded <see cref="IonRadioGroupContext"/>'s
/// <c>RequestSelect</c> callback — the same callback a radio click invokes.
/// </summary>
public class IonRadioGroupTests : IonicComponentTestBase
{
    private static RenderFragment Radios() => builder =>
    {
        int seq = 0;
        foreach (var v in new[] { "a", "b", "c" })
        {
            var captured = v;
            builder.OpenComponent<IonRadio>(seq++);
            builder.AddComponentParameter(seq++, nameof(IonRadio.Value), captured);
            builder.AddComponentParameter(seq++, nameof(IonRadio.ChildContent),
                (RenderFragment)(b => b.AddContent(0, captured.ToUpperInvariant())));
            builder.CloseComponent();
        }
    };

    private static ComponentUnderTest RenderGroup(TestContext ctx,
        Action<ComponentParameterBuilder<IonRadioGroup>>? configure = null)
        => ctx.Render<IonRadioGroup>(p =>
        {
            p.Add(nameof(IonRadioGroup.ChildContent), Radios());
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonRadioGroup_RendersDomContract()
    {
        var cut = RenderGroup(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-radio-group");
        cut.FindByClass("ion-radio").Count.ShouldBe(3);
    }

    [Fact]
    public void IonRadioGroup_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderGroup(Context);

        cut.Root.Class.ShouldStartWith("ios ion-radio-group");
    }

    // ---- Value cascade -----------------------------------------------------

    [Fact]
    public void IonRadioGroup_Value_ChecksMatchingRadio()
    {
        var cut = RenderGroup(Context, p => p.Add(nameof(IonRadioGroup.Value), "b"));

        var radios = cut.FindByClass("ion-radio");
        radios[0].ShouldNotHaveClass("radio-checked");
        radios[1].ShouldHaveClass("radio-checked");
        radios[2].ShouldNotHaveClass("radio-checked");
    }

    [Fact]
    public void IonRadioGroup_NoValue_ChecksNothing()
    {
        var cut = RenderGroup(Context);

        foreach (var r in cut.FindByClass("ion-radio"))
        {
            r.ShouldNotHaveClass("radio-checked");
        }
    }

    // ---- Selection via the cascaded context --------------------------------

    [Fact]
    public async Task IonRadioGroup_RequestSelect_SetsValueAndRaisesChanged()
    {
        string? changed = null;
        var group = new IonRadioGroup
        {
            ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v),
        };
        group.ChildContent = Radios();
        group.Build();

        await CascadedContext(group).RequestSelect.InvokeAsync("b");

        changed.ShouldBe("b");
    }

    [Fact]
    public async Task IonRadioGroup_RequestSelect_ReplacesPreviousValue()
    {
        string? changed = null;
        var group = new IonRadioGroup
        {
            Value = "a",
            ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v),
        };
        group.ChildContent = Radios();
        group.Build();

        await CascadedContext(group).RequestSelect.InvokeAsync("c");

        changed.ShouldBe("c");
    }

    [Fact]
    public async Task IonRadioGroup_RequestSelect_SameValue_WithoutAllowEmpty_IsNoOp()
    {
        var invoked = false;
        var group = new IonRadioGroup
        {
            Value = "a",
            ValueChanged = EventCallback.Factory.Create<string?>(this, _ => invoked = true),
        };
        group.ChildContent = Radios();
        group.Build();

        // Re-selecting the current value cannot toggle a radio off (empty selection disabled).
        await CascadedContext(group).RequestSelect.InvokeAsync("a");

        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task IonRadioGroup_RequestSelect_SameValue_WithAllowEmpty_ClearsValue()
    {
        string? changed = "unset";
        var group = new IonRadioGroup
        {
            Value = "a",
            AllowEmptySelection = true,
            ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v),
        };
        group.ChildContent = Radios();
        group.Build();

        // Re-selecting the current value clears it when empty selection is allowed.
        await CascadedContext(group).RequestSelect.InvokeAsync("a");

        changed.ShouldBeNull();
    }

    [Fact]
    public async Task IonRadioGroup_RequestSelect_Disabled_IsNoOp()
    {
        var invoked = false;
        var group = new IonRadioGroup
        {
            Disabled = true,
            ValueChanged = EventCallback.Factory.Create<string?>(this, _ => invoked = true),
        };
        group.ChildContent = Radios();
        group.Build();

        await CascadedContext(group).RequestSelect.InvokeAsync("b");

        invoked.ShouldBeFalse();
    }

    // Reads back the IonRadioGroupContext the group cascades to its radios — the same value a child
    // reads via [CascadingParameter]. Its RequestSelect is what a radio click invokes.
    private static IonRadioGroupContext CascadedContext(IonRadioGroup group)
    {
        var field = typeof(IonRadioGroup).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        return (IonRadioGroupContext)field.GetValue(group)!;
    }
}
