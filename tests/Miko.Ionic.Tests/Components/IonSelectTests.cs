using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonSelectTests : IonicComponentTestBase
{
    private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

    private static RenderFragment Options => builder =>
    {
        builder.OpenComponent<IonSelectOption>(0);
        builder.AddComponentParameter(1, nameof(IonSelectOption.Value), "a");
        builder.AddComponentParameter(2, nameof(IonSelectOption.ChildContent), Text("Alpha"));
        builder.CloseComponent();

        builder.OpenComponent<IonSelectOption>(3);
        builder.AddComponentParameter(4, nameof(IonSelectOption.Value), "b");
        builder.AddComponentParameter(5, nameof(IonSelectOption.ChildContent), Text("Beta"));
        builder.CloseComponent();
    };

    private static ComponentUnderTest RenderSelect(TestContext ctx,
        Action<ComponentParameterBuilder<IonSelect>>? configure = null)
        => ctx.Render<IonSelect>(p =>
        {
            p.Add(nameof(IonSelect.ChildContent), Options);
            configure?.Invoke(p);
        });

    [Fact]
    public void IonSelect_RendersDomContract()
    {
        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Placeholder), "Pick one"));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-select");

        var wrapper = cut.FindByClass("select-wrapper").Single();
        wrapper.TagName.ShouldBe("label");

        var select = cut.FindByClass("select-native").Single();
        select.TagName.ShouldBe("select");
        select.ShouldBeOfType<SelectElement>();

        cut.FindByClass("select-icon").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("Pick one");
    }

    [Fact]
    public void IonSelect_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = RenderSelect(Context);

        cut.Root.Class.ShouldStartWith("ios ion-select");
    }

    [Fact]
    public void IonSelect_StampsLabelFillShapeJustifyAndColorClasses()
    {
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Label), "Status");
            p.Add(nameof(IonSelect.Fill), "outline");
            p.Add(nameof(IonSelect.Shape), "round");
            p.Add(nameof(IonSelect.Justify), "space-between");
            p.Add(nameof(IonSelect.Color), "danger");
        });

        cut.Root.ShouldHaveClass("select-label-placement-start");
        cut.Root.ShouldHaveClass("select-fill-outline");
        cut.Root.ShouldHaveClass("select-shape-round");
        cut.Root.ShouldHaveClass("select-justify-space-between");
        cut.Root.ShouldHaveClass("ion-color-danger");
        cut.GetTextContent().ShouldContain("Status");
    }

    [Fact]
    public void IonSelect_ValueSelectsNativeOptionAndDisplaysText()
    {
        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Value), "b"));

        var select = cut.FindByClass("select-native").Single().ShouldBeOfType<SelectElement>();
        select.Value.ShouldBe("b");
        select.SelectedIndex.ShouldBe(1);
        cut.Root.ShouldHaveClass("has-value");
        cut.FindByClass("select-text").Single().TextContent.ShouldBe("Beta");
    }

    [Fact]
    public void IonSelect_SelectedTextOverridesOptionText()
    {
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Value), "b");
            p.Add(nameof(IonSelect.SelectedText), "Custom Beta");
        });

        cut.FindByClass("select-text").Single().TextContent.ShouldBe("Custom Beta");
    }

    [Fact]
    public void IonSelect_DisabledStampsClassAndNativeState()
    {
        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Disabled), true));

        cut.Root.ShouldHaveClass("select-disabled");
        cut.FindByClass("select-native").Single().IsDisabled.ShouldBeTrue();
    }

    [Fact]
    public void IonSelect_RendersStartAndEndSlots()
    {
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.StartSlot), (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "start-slot");
                builder.CloseElement();
            }));
            p.Add(nameof(IonSelect.EndSlot), (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "end-slot");
                builder.CloseElement();
            }));
        });

        cut.FindByClass("start-slot").ShouldHaveSingleItem();
        cut.FindByClass("end-slot").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonSelectOption_RendersNativeOptionAndSyncsState()
    {
        var cut = Context.Render<IonSelectOption>(p =>
        {
            p.Add(nameof(IonSelectOption.Value), "x");
            p.Add(nameof(IonSelectOption.Selected), true);
            p.Add(nameof(IonSelectOption.Disabled), true);
            p.AddChildContent(Text("Option X"));
        });

        var option = cut.Root.ShouldBeOfType<OptionElement>();
        option.TagName.ShouldBe("option");
        option.ShouldHaveClass("ion-select-option");
        option.Value.ShouldBe("x");
        option.Selected.ShouldBeTrue();
        option.IsDisabled.ShouldBeTrue();
        option.TextContent.ShouldBe("Option X");
    }

    [Fact]
    public void IonSelect_ValueChangedAndOnChange_AreInvokedFromNativeChange()
    {
        string? changedValue = null;
        ChangeEventArgs? receivedArgs = null;
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.ValueChanged),
                EventCallback.Factory.Create<string?>(this, v => changedValue = v));
            p.Add(nameof(IonSelect.OnChange),
                EventCallback.Factory.Create<ChangeEventArgs>(this, e => receivedArgs = e));
        });

        var select = cut.FindByClass("select-native").Single().ShouldBeOfType<SelectElement>();
        select.SelectedIndex = 1;
        select.OnChange!.Invoke(new ChangeEventArgs { Target = select, NewValue = "b" });

        changedValue.ShouldBe("b");
        receivedArgs.ShouldNotBeNull();
        receivedArgs.Target.ShouldBe(select);
    }

    [Fact]
    public void IonSelect_Style_DefaultUsesBlockAndMinHeight()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderSelect(Context);
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Display.ShouldBe(Display.Block);
        style.MinHeight.ShouldBe(Length.Px(48));
    }

    [Fact]
    public void IonSelect_OutlineStyle_UsesBorder()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Fill), "outline"));
        var inner = cut.FindByClass("select-wrapper-inner").Single();
        var style = cut.GetComputedStyle(inner)!;

        style.BorderTopWidth.ShouldBe(Length.Px(1));
        style.BorderTopStyle.ShouldBe(BorderStyle.Solid);
        style.BorderTopLeftRadius.Value.ShouldBe(4f);
    }

    [Fact]
    public void IonSelect_IosStyle_UsesIosMinHeight()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderSelect(Context);

        cut.GetComputedStyle(cut.Root)!.MinHeight.ShouldBe(Length.Px(44));
    }
}
