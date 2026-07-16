using Miko.Components;
using Miko.Events;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-picker-column-option</c>. Covers the DOM contract (the host is a native
/// <c>&lt;button&gt;</c>), deriving the active state from the enclosing column's selected value,
/// selecting via click through the column context, disabled no-op, mode class stamping, and the key
/// active/inactive opacity styles.
/// </summary>
public class IonPickerColumnOptionTests : IonicComponentTestBase
{
    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonPickerColumnOption_Standalone_HostIsButton()
    {
        var cut = Context.Render<IonPickerColumnOption>(p =>
        {
            p.Add(nameof(IonPickerColumnOption.Value), "javascript");
            p.Add(nameof(IonPickerColumnOption.ChildContent), (RenderFragment)(b => b.AddContent(0, "Javascript")));
        });

        // The host is the clickable native <button>.
        cut.Root.TagName.ShouldBe("button");
        cut.Root.Class.ShouldBe("md ion-picker-column-option");
    }

    // ---- Active derivation -------------------------------------------------

    [Fact]
    public void IonPickerColumnOption_MatchingColumnValue_IsActive()
    {
        var cut = Context.Render<IonPickerColumn>(p =>
        {
            p.Add(nameof(IonPickerColumn.Value), "javascript");
            p.Add(nameof(IonPickerColumn.ChildContent), (RenderFragment)(b =>
            {
                b.OpenComponent<IonPickerColumnOption>(0);
                b.AddComponentParameter(1, nameof(IonPickerColumnOption.Value), "javascript");
                b.AddComponentParameter(2, nameof(IonPickerColumnOption.ChildContent), (RenderFragment)(bb => bb.AddContent(0, "Javascript")));
                b.CloseComponent();
                b.OpenComponent<IonPickerColumnOption>(3);
                b.AddComponentParameter(4, nameof(IonPickerColumnOption.Value), "typescript");
                b.AddComponentParameter(5, nameof(IonPickerColumnOption.ChildContent), (RenderFragment)(bb => bb.AddContent(0, "Typescript")));
                b.CloseComponent();
            }));
        });

        var options = cut.FindByClass("ion-picker-column-option");
        options[0].ShouldHaveClass("option-active");   // javascript matches the column value
        options[1].ShouldNotHaveClass("option-active"); // typescript does not
    }

    // ---- Click selection ---------------------------------------------------

    [Fact]
    public void IonPickerColumnOption_Click_SelectsViaColumn()
    {
        string? changed = null;
        var cut = Context.Render<IonPickerColumn>(p =>
        {
            p.Add(nameof(IonPickerColumn.ValueChanged),
                EventCallback.Factory.Create<string>(this, v => changed = v));
            p.Add(nameof(IonPickerColumn.ChildContent), (RenderFragment)(b =>
            {
                b.OpenComponent<IonPickerColumnOption>(0);
                b.AddComponentParameter(1, nameof(IonPickerColumnOption.Value), "typescript");
                b.AddComponentParameter(2, nameof(IonPickerColumnOption.ChildContent), (RenderFragment)(bb => bb.AddContent(0, "Typescript")));
                b.CloseComponent();
            }));
        });

        var option = cut.FindByClass("ion-picker-column-option").Single();
        option.OnClick!.Invoke(new MouseEventArgs { Target = option });

        changed.ShouldBe("typescript");
    }

    [Fact]
    public void IonPickerColumnOption_Disabled_StampsClass_AndClickIsNoOp()
    {
        string? changed = null;
        var cut = Context.Render<IonPickerColumn>(p =>
        {
            p.Add(nameof(IonPickerColumn.ValueChanged),
                EventCallback.Factory.Create<string>(this, v => changed = v));
            p.Add(nameof(IonPickerColumn.ChildContent), (RenderFragment)(b =>
            {
                b.OpenComponent<IonPickerColumnOption>(0);
                b.AddComponentParameter(1, nameof(IonPickerColumnOption.Value), "typescript");
                b.AddComponentParameter(2, nameof(IonPickerColumnOption.Disabled), true);
                b.AddComponentParameter(3, nameof(IonPickerColumnOption.ChildContent), (RenderFragment)(bb => bb.AddContent(0, "Typescript")));
                b.CloseComponent();
            }));
        });

        var option = cut.FindByClass("ion-picker-column-option").Single();
        option.ShouldHaveClass("option-disabled");

        option.OnClick!.Invoke(new MouseEventArgs { Target = option });
        changed.ShouldBeNull();
    }

    [Fact]
    public void IonPickerColumnOption_ColumnDisabled_ClickIsNoOp()
    {
        string? changed = null;
        var cut = Context.Render<IonPickerColumn>(p =>
        {
            p.Add(nameof(IonPickerColumn.Disabled), true);
            p.Add(nameof(IonPickerColumn.ValueChanged),
                EventCallback.Factory.Create<string>(this, v => changed = v));
            p.Add(nameof(IonPickerColumn.ChildContent), (RenderFragment)(b =>
            {
                b.OpenComponent<IonPickerColumnOption>(0);
                b.AddComponentParameter(1, nameof(IonPickerColumnOption.Value), "typescript");
                b.AddComponentParameter(2, nameof(IonPickerColumnOption.ChildContent), (RenderFragment)(bb => bb.AddContent(0, "Typescript")));
                b.CloseComponent();
            }));
        });

        var option = cut.FindByClass("ion-picker-column-option").Single();
        option.OnClick!.Invoke(new MouseEventArgs { Target = option });

        changed.ShouldBeNull();
    }

    // ---- Mode --------------------------------------------------------------

    [Fact]
    public void IonPickerColumnOption_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = Context.Render<IonPickerColumnOption>(p =>
        {
            p.Add(nameof(IonPickerColumnOption.Value), "javascript");
            p.Add(nameof(IonPickerColumnOption.ChildContent), (RenderFragment)(b => b.AddContent(0, "Javascript")));
        });

        cut.Root.Class.ShouldStartWith("ios ion-picker-column-option");
    }

    // ---- Key styles --------------------------------------------------------

    [Fact]
    public void IonPickerColumnOption_ActiveOption_IsFullOpacity_InactiveIsFaded()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonPickerColumn>(p =>
        {
            p.Add(nameof(IonPickerColumn.Value), "javascript");
            p.Add(nameof(IonPickerColumn.ChildContent), (RenderFragment)(b =>
            {
                b.OpenComponent<IonPickerColumnOption>(0);
                b.AddComponentParameter(1, nameof(IonPickerColumnOption.Value), "javascript");
                b.AddComponentParameter(2, nameof(IonPickerColumnOption.ChildContent), (RenderFragment)(bb => bb.AddContent(0, "Javascript")));
                b.CloseComponent();
                b.OpenComponent<IonPickerColumnOption>(3);
                b.AddComponentParameter(4, nameof(IonPickerColumnOption.Value), "typescript");
                b.AddComponentParameter(5, nameof(IonPickerColumnOption.ChildContent), (RenderFragment)(bb => bb.AddContent(0, "Typescript")));
                b.CloseComponent();
            }));
        });

        var options = cut.FindByClass("ion-picker-column-option");
        // The active option reads at full opacity; the non-selected one is faded.
        cut.GetComputedStyle(options[0])!.Opacity.ShouldBe(1f);
        cut.GetComputedStyle(options[1])!.Opacity.ShouldBeLessThan(1f);
    }
}
