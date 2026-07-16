using Miko.Components;
using Miko.Events;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-picker-column</c>. Covers the DOM contract (host, the <c>.picker-opts</c> list,
/// prefix/suffix slots, options rendering as children), the column→option selected-value cascade,
/// selecting an option updating <see cref="IonPickerColumn.Value"/> and raising
/// <see cref="IonPickerColumn.ValueChanged"/>, and disabled gating.
/// </summary>
public class IonPickerColumnTests : IonicComponentTestBase
{
    // The options "javascript" / "typescript" / "csharp" as ChildContent.
    private static RenderFragment Options() => builder =>
    {
        int seq = 0;
        foreach (var v in new[] { "javascript", "typescript", "csharp" })
        {
            var captured = v;
            builder.OpenComponent<IonPickerColumnOption>(seq++);
            builder.AddComponentParameter(seq++, nameof(IonPickerColumnOption.Value), captured);
            builder.AddComponentParameter(seq++, nameof(IonPickerColumnOption.ChildContent), (RenderFragment)(b => b.AddContent(0, captured)));
            builder.CloseComponent();
        }
    };

    private static ComponentUnderTest RenderColumn(TestContext ctx,
        Action<ComponentParameterBuilder<IonPickerColumn>>? configure = null)
        => ctx.Render<IonPickerColumn>(p =>
        {
            p.Add(nameof(IonPickerColumn.ChildContent), Options());
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonPickerColumn_RendersDomContract()
    {
        var cut = RenderColumn(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-picker-column");
        // The scrollable option list wrapper.
        cut.FindByClass("picker-opts").ShouldHaveSingleItem();
        // All three options render.
        cut.FindByClass("ion-picker-column-option").Count.ShouldBe(3);
    }

    [Fact]
    public void IonPickerColumn_RendersPrefixAndSuffixSlots()
    {
        var cut = Context.Render<IonPickerColumn>(p =>
        {
            p.Add(nameof(IonPickerColumn.ChildContent), Options());
            p.Add(nameof(IonPickerColumn.SlotPrefix), (RenderFragment)(b =>
            {
                b.OpenElement(0, "span");
                b.AddAttribute(1, "class", "my-prefix");
                b.CloseElement();
            }));
            p.Add(nameof(IonPickerColumn.SlotSuffix), (RenderFragment)(b =>
            {
                b.OpenElement(0, "span");
                b.AddAttribute(1, "class", "my-suffix");
                b.CloseElement();
            }));
        });

        cut.FindByClass("my-prefix").ShouldHaveSingleItem();
        cut.FindByClass("my-suffix").ShouldHaveSingleItem();
    }

    // ---- Selected-value cascade -------------------------------------------

    [Fact]
    public void IonPickerColumn_Value_MarksMatchingOptionActive()
    {
        var cut = RenderColumn(Context, p => p.Add(nameof(IonPickerColumn.Value), "typescript"));

        var options = cut.FindByClass("ion-picker-column-option");
        var active = options.Where(o => o.HasClass("option-active")).ToList();
        // Exactly the option whose value matches is active.
        active.ShouldHaveSingleItem();
        options[0].ShouldNotHaveClass("option-active");
        options[2].ShouldNotHaveClass("option-active");
    }

    [Fact]
    public void IonPickerColumn_NoValue_NoOptionActive()
    {
        var cut = RenderColumn(Context);

        foreach (var o in cut.FindByClass("ion-picker-column-option"))
        {
            o.ShouldNotHaveClass("option-active");
        }
    }

    // ---- Selection interaction --------------------------------------------

    [Fact]
    public void IonPickerColumn_ClickOption_UpdatesValueAndRaisesValueChanged()
    {
        string? changed = null;
        var cut = RenderColumn(Context, p =>
            p.Add(nameof(IonPickerColumn.ValueChanged),
                EventCallback.Factory.Create<string>(this, v => changed = v)));

        // Click the second option's native button host.
        var option = cut.FindByClass("ion-picker-column-option")[1];
        option.OnClick!.Invoke(new MouseEventArgs { Target = option });

        changed.ShouldBe("typescript");
    }

    [Fact]
    public void IonPickerColumn_ClickActiveOption_IsNoOp()
    {
        var invoked = false;
        var cut = RenderColumn(Context, p =>
        {
            p.Add(nameof(IonPickerColumn.Value), "typescript");
            p.Add(nameof(IonPickerColumn.ValueChanged),
                EventCallback.Factory.Create<string>(this, _ => invoked = true));
        });

        // Re-selecting the already-selected value is a no-op (matches setValue's equality guard).
        var option = cut.FindByClass("ion-picker-column-option")[1];
        option.OnClick!.Invoke(new MouseEventArgs { Target = option });

        invoked.ShouldBeFalse();
    }

    // ---- Disabled ----------------------------------------------------------

    [Fact]
    public void IonPickerColumn_Disabled_StampsClass()
    {
        var cut = RenderColumn(Context, p => p.Add(nameof(IonPickerColumn.Disabled), true));

        cut.Root.ShouldHaveClass("picker-column-disabled");
    }

    [Fact]
    public void IonPickerColumn_Disabled_ClickOption_IsNoOp()
    {
        var invoked = false;
        var cut = RenderColumn(Context, p =>
        {
            p.Add(nameof(IonPickerColumn.Disabled), true);
            p.Add(nameof(IonPickerColumn.ValueChanged),
                EventCallback.Factory.Create<string>(this, _ => invoked = true));
        });

        var option = cut.FindByClass("ion-picker-column-option")[1];
        option.OnClick!.Invoke(new MouseEventArgs { Target = option });

        invoked.ShouldBeFalse();
    }

    // ---- Color / mode ------------------------------------------------------

    [Fact]
    public void IonPickerColumn_Color_StampsIonColorClasses()
    {
        var cut = RenderColumn(Context, p => p.Add(nameof(IonPickerColumn.Color), "danger"));

        cut.Root.ShouldHaveClass("ion-color");
        cut.Root.ShouldHaveClass("ion-color-danger");
    }

    [Fact]
    public void IonPickerColumn_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderColumn(Context);

        cut.Root.Class.ShouldStartWith("ios ion-picker-column");
    }
}
