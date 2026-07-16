using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-textarea</c>. Mirrors <see cref="IonInputTests"/> — the textarea is the
/// multi-line sibling of ion-input and shares its label / native-control / bottom-hint / highlight
/// layout. Covers the DOM contract, value binding, disabled/readonly classes + native sync, label
/// placement, the helper/error/counter bottom row, ios vs md, and a couple of key styles.
/// </summary>
public class IonTextareaTests : IonicComponentTestBase
{
    private static ComponentUnderTest RenderTextarea(TestContext ctx,
        Action<ComponentParameterBuilder<IonTextarea>>? configure = null,
        string? label = "Default textarea")
        => ctx.Render<IonTextarea>(p =>
        {
            if (label is not null) p.Add(nameof(IonTextarea.Label), label);
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonTextarea_RendersDomContract()
    {
        var cut = RenderTextarea(Context, label: "Comments");

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-textarea");
        cut.Root.ShouldHaveClass("textarea-label-placement-start");

        var wrapper = cut.FindByClass("textarea-wrapper").Single();
        wrapper.TagName.ShouldBe("label");

        var textarea = cut.FindByClass("native-textarea").Single();
        textarea.TagName.ShouldBe("textarea");
        textarea.ShouldBeOfType<TextAreaElement>();

        cut.FindByClass("label-text-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("textarea-wrapper-inner").ShouldHaveSingleItem();
        cut.FindByClass("native-wrapper").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("Comments");
    }

    [Fact]
    public void IonTextarea_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = RenderTextarea(Context);

        cut.Root.Class.ShouldStartWith("ios ion-textarea");
    }

    [Theory]
    [InlineData("start", "textarea-label-placement-start")]
    [InlineData("end", "textarea-label-placement-end")]
    [InlineData("fixed", "textarea-label-placement-fixed")]
    [InlineData("stacked", "textarea-label-placement-stacked")]
    [InlineData("floating", "textarea-label-placement-floating")]
    public void IonTextarea_StampsLabelPlacementClass(string placement, string expected)
    {
        var cut = RenderTextarea(Context, p => p.Add(nameof(IonTextarea.LabelPlacement), placement));

        cut.Root.ShouldHaveClass(expected);
    }

    [Fact]
    public void IonTextarea_StampsFillShapeAndColorClasses()
    {
        var cut = RenderTextarea(Context, p =>
        {
            p.Add(nameof(IonTextarea.Fill), "outline");
            p.Add(nameof(IonTextarea.Shape), "round");
            p.Add(nameof(IonTextarea.Color), "danger");
        });

        cut.Root.ShouldHaveClass("textarea-fill-outline");
        cut.Root.ShouldHaveClass("textarea-shape-round");
        cut.Root.ShouldHaveClass("ion-color");
        cut.Root.ShouldHaveClass("ion-color-danger");
    }

    [Fact]
    public void IonTextarea_EmptyLabel_HidesLabelWrapper()
    {
        var cut = RenderTextarea(Context, label: null);

        cut.FindByClass("label-text-wrapper").Single()
            .ShouldHaveClass("label-text-wrapper-hidden");
    }

    [Fact]
    public void IonTextarea_WithLabel_DoesNotHideLabelWrapper()
    {
        var cut = RenderTextarea(Context, label: "Name");

        cut.FindByClass("label-text-wrapper").Single()
            .ShouldNotHaveClass("label-text-wrapper-hidden");
    }

    [Fact]
    public void IonTextarea_RendersStartAndEndSlots()
    {
        var cut = RenderTextarea(Context, p =>
        {
            p.Add(nameof(IonTextarea.StartSlot), (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "start-slot");
                builder.CloseElement();
            }));
            p.Add(nameof(IonTextarea.EndSlot), (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "end-slot");
                builder.CloseElement();
            }));
        });

        cut.FindByClass("start-slot").ShouldHaveSingleItem();
        cut.FindByClass("end-slot").ShouldHaveSingleItem();
    }

    // ---- State / interaction ----------------------------------------------

    [Fact]
    public void IonTextarea_Value_SetsNativeValueAndHasValueClass()
    {
        var cut = RenderTextarea(Context, p => p.Add(nameof(IonTextarea.Value), "Some feedback"));

        cut.Root.ShouldHaveClass("has-value");
        cut.FindByClass("native-textarea").Single()
            .ShouldBeOfType<TextAreaElement>().Value.ShouldBe("Some feedback");
    }

    [Fact]
    public void IonTextarea_NoValue_DoesNotStampHasValueClass()
    {
        var cut = RenderTextarea(Context);

        cut.Root.ShouldNotHaveClass("has-value");
    }

    [Fact]
    public void IonTextarea_Placeholder_SetsNativePlaceholder()
    {
        var cut = RenderTextarea(Context, p => p.Add(nameof(IonTextarea.Placeholder), "Enter your comments"));

        cut.FindByClass("native-textarea").Single()
            .ShouldBeOfType<TextAreaElement>().Placeholder.ShouldBe("Enter your comments");
    }

    [Fact]
    public void IonTextarea_Rows_SetsNativeRows()
    {
        var cut = RenderTextarea(Context, p => p.Add(nameof(IonTextarea.Rows), 6));

        cut.FindByClass("native-textarea").Single()
            .ShouldBeOfType<TextAreaElement>().Rows.ShouldBe(6);
    }

    [Fact]
    public void IonTextarea_AutoGrow_StampsClass()
    {
        var cut = RenderTextarea(Context, p => p.Add(nameof(IonTextarea.AutoGrow), true));

        cut.Root.ShouldHaveClass("textarea-auto-grow");
    }

    [Fact]
    public void IonTextarea_Readonly_StampsClass()
    {
        var cut = RenderTextarea(Context, p =>
        {
            p.Add(nameof(IonTextarea.Value), "read only text");
            p.Add(nameof(IonTextarea.Readonly), true);
        });

        cut.Root.ShouldHaveClass("textarea-readonly");
    }

    [Fact]
    public void IonTextarea_Disabled_StampsClassAndNativeState()
    {
        var cut = RenderTextarea(Context, p =>
        {
            p.Add(nameof(IonTextarea.Value), "disabled text");
            p.Add(nameof(IonTextarea.Disabled), true);
        });

        cut.Root.ShouldHaveClass("textarea-disabled");
        cut.FindByClass("native-textarea").Single().IsDisabled.ShouldBeTrue();
    }

    [Fact]
    public void IonTextarea_OnInput_UpdatesValueAndRaisesCallbacks()
    {
        string? boundValue = null;
        string? inputValue = null;
        var cut = RenderTextarea(Context, p =>
        {
            p.Add(nameof(IonTextarea.ValueChanged),
                EventCallback.Factory.Create<string?>(this, v => boundValue = v));
            p.Add(nameof(IonTextarea.OnInput),
                EventCallback.Factory.Create<string?>(this, v => inputValue = v));
        });

        var textarea = cut.FindByClass("native-textarea").Single();
        textarea.OnInput!.Invoke(new InputEventArgs { Target = textarea, Data = "Hello world" });

        boundValue.ShouldBe("Hello world");
        inputValue.ShouldBe("Hello world");
    }

    [Fact]
    public void IonTextarea_OnInput_WhenDisabled_DoesNotRaiseCallbacks()
    {
        string? boundValue = null;
        var cut = RenderTextarea(Context, p =>
        {
            p.Add(nameof(IonTextarea.Disabled), true);
            p.Add(nameof(IonTextarea.ValueChanged),
                EventCallback.Factory.Create<string?>(this, v => boundValue = v));
        });

        var textarea = cut.FindByClass("native-textarea").Single();
        textarea.OnInput!.Invoke(new InputEventArgs { Target = textarea, Data = "Hello" });

        boundValue.ShouldBeNull();
    }

    // ---- Helper / error / counter -----------------------------------------

    [Fact]
    public void IonTextarea_RendersHelperAndErrorText()
    {
        var cut = RenderTextarea(Context, p =>
        {
            p.Add(nameof(IonTextarea.HelperText), "Enter your feedback");
            p.Add(nameof(IonTextarea.ErrorText), "Feedback is required");
        });

        cut.FindByClass("textarea-bottom").ShouldHaveSingleItem();
        cut.FindByClass("helper-text").Single().TextContent.ShouldBe("Enter your feedback");
        cut.FindByClass("error-text").Single().TextContent.ShouldBe("Feedback is required");
    }

    [Fact]
    public void IonTextarea_NoHintText_OmitsTextareaBottom()
    {
        var cut = RenderTextarea(Context);

        cut.FindByClass("textarea-bottom").ShouldBeEmpty();
    }

    [Fact]
    public void IonTextarea_Counter_RendersRatioText()
    {
        var cut = RenderTextarea(Context, p =>
        {
            p.Add(nameof(IonTextarea.Counter), true);
            p.Add(nameof(IonTextarea.Maxlength), 100);
            p.Add(nameof(IonTextarea.Value), "hello");
        });

        cut.FindByClass("counter").Single().TextContent.ShouldBe("5 / 100");
    }

    [Fact]
    public void IonTextarea_Counter_WithoutMaxlength_IsNotRendered()
    {
        var cut = RenderTextarea(Context, p => p.Add(nameof(IonTextarea.Counter), true));

        cut.FindByClass("counter").ShouldBeEmpty();
    }

    // ---- Key styles --------------------------------------------------------

    [Fact]
    public void IonTextarea_Style_HostIsBlockWithMdMinHeight()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderTextarea(Context);
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Display.ShouldBe(Display.Block);
        style.MinHeight.ShouldBe(Length.Px(44));
        style.FontSize.ShouldBe(Length.Px(16));
    }

    [Fact]
    public void IonTextarea_Style_IosUsesIosFontSize()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderTextarea(Context);

        cut.GetComputedStyle(cut.Root)!.FontSize.ShouldBe(Length.Px(17));
    }

    [Fact]
    public void IonTextarea_Style_OutlineFillUsesBorder()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderTextarea(Context, p => p.Add(nameof(IonTextarea.Fill), "outline"));
        var wrapper = cut.FindByClass("textarea-wrapper").Single();
        var style = cut.GetComputedStyle(wrapper)!;

        style.BorderTopWidth.ShouldBe(Length.Px(1));
        style.BorderTopStyle.ShouldBe(BorderStyle.Solid);
        style.BorderTopLeftRadius.Value.ShouldBe(4f);
    }

    [Fact]
    public void IonTextarea_Style_HelperAndErrorColors()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderTextarea(Context, p =>
        {
            p.Add(nameof(IonTextarea.HelperText), "help");
            p.Add(nameof(IonTextarea.ErrorText), "error");
        });

        var error = cut.FindByClass("error-text").Single();
        cut.GetComputedStyle(error)!.Color.ShouldBe(Color.FromHex("c5000f"));   // danger
    }

    // ---- Box model ---------------------------------------------------------

    [Fact]
    public void IonTextarea_BoxModel_SolidFillPadsWrapper()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderTextarea(Context, p => p.Add(nameof(IonTextarea.Fill), "solid"));
        var wrapper = cut.FindByClass("textarea-wrapper").Single();
        var box = cut.GetBoxModel(wrapper)!;

        box.Padding.Left.ShouldBe(16);
        box.Padding.Right.ShouldBe(16);
    }
}
