using Miko.Common;
using Miko.Components;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-picker</c>. Covers the DOM contract (host class, the highlight band and fade
/// markers, columns rendering as children), the multi-column marker computed in Build(), mode class
/// stamping, and a key layout style (the fixed 200px height).
/// </summary>
public class IonPickerTests : IonicComponentTestBase
{
    // A single picker column with a couple of options.
    private static RenderFragment Column(string value) => builder =>
    {
        builder.OpenComponent<IonPickerColumn>(0);
        builder.AddComponentParameter(1, nameof(IonPickerColumn.Value), value);
        builder.AddComponentParameter(2, nameof(IonPickerColumn.ChildContent), (RenderFragment)(b =>
        {
            b.OpenComponent<IonPickerColumnOption>(0);
            b.AddComponentParameter(1, nameof(IonPickerColumnOption.Value), value);
            b.AddComponentParameter(2, nameof(IonPickerColumnOption.ChildContent), (RenderFragment)(bb => bb.AddContent(0, value)));
            b.CloseComponent();
        }));
        builder.CloseComponent();
    };

    // Several columns, each with one option.
    private static RenderFragment Columns(params string[] values) => builder =>
    {
        int seq = 0;
        foreach (var v in values)
        {
            var captured = v;
            builder.OpenComponent<IonPickerColumn>(seq++);
            builder.AddComponentParameter(seq++, nameof(IonPickerColumn.Value), captured);
            builder.AddComponentParameter(seq++, nameof(IonPickerColumn.ChildContent), (RenderFragment)(b =>
            {
                b.OpenComponent<IonPickerColumnOption>(0);
                b.AddComponentParameter(1, nameof(IonPickerColumnOption.Value), captured);
                b.AddComponentParameter(2, nameof(IonPickerColumnOption.ChildContent), (RenderFragment)(bb => bb.AddContent(0, captured)));
                b.CloseComponent();
            }));
            builder.CloseComponent();
        }
    };

    private static ComponentUnderTest RenderPicker(TestContext ctx, RenderFragment child)
        => ctx.Render<IonPicker>(p => p.Add(nameof(IonPicker.ChildContent), child));

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonPicker_RendersDomContract()
    {
        var cut = RenderPicker(Context, Column("a"));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-picker");
        // The fade markers and the center highlight band are part of the contract.
        cut.FindByClass("picker-before").ShouldHaveSingleItem();
        cut.FindByClass("picker-after").ShouldHaveSingleItem();
        cut.FindByClass("picker-highlight").ShouldHaveSingleItem();
        // The column renders inside the picker.
        cut.FindByClass("ion-picker-column").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonPicker_SingleColumn_HasNoMultipleColumnsClass()
    {
        var cut = RenderPicker(Context, Column("a"));

        cut.Root.ShouldNotHaveClass("picker-has-multiple-columns");
    }

    [Fact]
    public void IonPicker_MultipleColumns_StampsMultipleColumnsClass()
    {
        var cut = RenderPicker(Context, Columns("hour", "minute"));

        cut.FindByClass("ion-picker-column").Count.ShouldBe(2);
        cut.Root.ShouldHaveClass("picker-has-multiple-columns");
    }

    [Fact]
    public void IonPicker_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderPicker(Context, Column("a"));

        cut.Root.Class.ShouldStartWith("ios ion-picker");
    }

    // ---- Key style ---------------------------------------------------------

    [Fact]
    public void IonPicker_Style_HostIsFixedHeightFlexRow()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderPicker(Context, Column("a"));

        var style = cut.GetComputedStyle(cut.Root)!;
        style.Display.ShouldBe(Display.Flex);
        // picker.scss :host height: 200px.
        style.Height.ShouldBe(Length.Px(200));
    }
}
