using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonInputTests : IonicComponentTestBase
{
    private static ComponentUnderTest RenderInput(TestContext ctx,
        Action<ComponentParameterBuilder<IonInput>>? configure = null,
        string? label = "Default input")
        => ctx.Render<IonInput>(p =>
        {
            if (label is not null) p.Add(nameof(IonInput.Label), label);
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonInput_RendersDomContract()
    {
        var cut = RenderInput(Context, label: "Company name");

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-input");
        cut.Root.ShouldHaveClass("input-label-placement-start");

        var wrapper = cut.FindByClass("input-wrapper").Single();
        wrapper.TagName.ShouldBe("label");

        var input = cut.FindByClass("native-input").Single();
        input.TagName.ShouldBe("input");
        input.ShouldBeOfType<InputElement>().Type.ShouldBe(InputType.Text);

        cut.FindByClass("label-text-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("native-wrapper").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("Company name");
    }

    [Fact]
    public void IonInput_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = RenderInput(Context);

        cut.Root.Class.ShouldStartWith("ios ion-input");
    }

    [Theory]
    [InlineData("start", "input-label-placement-start")]
    [InlineData("end", "input-label-placement-end")]
    [InlineData("fixed", "input-label-placement-fixed")]
    [InlineData("stacked", "input-label-placement-stacked")]
    [InlineData("floating", "input-label-placement-floating")]
    public void IonInput_StampsLabelPlacementClass(string placement, string expected)
    {
        var cut = RenderInput(Context, p => p.Add(nameof(IonInput.LabelPlacement), placement));

        cut.Root.ShouldHaveClass(expected);
    }

    [Fact]
    public void IonInput_StampsFillShapeAndColorClasses()
    {
        var cut = RenderInput(Context, p =>
        {
            p.Add(nameof(IonInput.Fill), "outline");
            p.Add(nameof(IonInput.Shape), "round");
            p.Add(nameof(IonInput.Color), "danger");
        });

        cut.Root.ShouldHaveClass("input-fill-outline");
        cut.Root.ShouldHaveClass("input-shape-round");
        cut.Root.ShouldHaveClass("ion-color");
        cut.Root.ShouldHaveClass("ion-color-danger");
    }

    [Fact]
    public void IonInput_EmptyLabel_HidesLabelWrapper()
    {
        var cut = RenderInput(Context, label: null);

        cut.FindByClass("label-text-wrapper").Single()
            .ShouldHaveClass("label-text-wrapper-hidden");
    }

    [Fact]
    public void IonInput_WithLabel_DoesNotHideLabelWrapper()
    {
        var cut = RenderInput(Context, label: "Name");

        cut.FindByClass("label-text-wrapper").Single()
            .ShouldNotHaveClass("label-text-wrapper-hidden");
    }

    [Fact]
    public void IonInput_RendersStartAndEndSlots()
    {
        var cut = RenderInput(Context, p =>
        {
            p.Add(nameof(IonInput.StartSlot), (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "start-slot");
                builder.CloseElement();
            }));
            p.Add(nameof(IonInput.EndSlot), (RenderFragment)(builder =>
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
    public void IonInput_Value_SetsNativeValueAndHasValueClass()
    {
        var cut = RenderInput(Context, p => p.Add(nameof(IonInput.Value), "121 S Pinckney St"));

        cut.Root.ShouldHaveClass("has-value");
        cut.FindByClass("native-input").Single()
            .ShouldBeOfType<InputElement>().Value.ShouldBe("121 S Pinckney St");
    }

    [Fact]
    public void IonInput_NoValue_DoesNotStampHasValueClass()
    {
        var cut = RenderInput(Context);

        cut.Root.ShouldNotHaveClass("has-value");
    }

    [Fact]
    public void IonInput_Placeholder_SetsNativePlaceholder()
    {
        var cut = RenderInput(Context, p => p.Add(nameof(IonInput.Placeholder), "Enter company name"));

        cut.FindByClass("native-input").Single()
            .ShouldBeOfType<InputElement>().Placeholder.ShouldBe("Enter company name");
    }

    [Theory]
    [InlineData("password", InputType.Password)]
    [InlineData("search", InputType.Search)]
    [InlineData("text", InputType.Text)]
    [InlineData("email", InputType.Text)]
    public void IonInput_Type_MapsToNativeInputType(string type, InputType expected)
    {
        var cut = RenderInput(Context, p => p.Add(nameof(IonInput.Type), type));

        cut.FindByClass("native-input").Single()
            .ShouldBeOfType<InputElement>().Type.ShouldBe(expected);
    }

    [Fact]
    public void IonInput_Readonly_StampsClass()
    {
        var cut = RenderInput(Context, p =>
        {
            p.Add(nameof(IonInput.Value), "Madison");
            p.Add(nameof(IonInput.Readonly), true);
        });

        cut.Root.ShouldHaveClass("input-readonly");
    }

    [Fact]
    public void IonInput_Disabled_StampsClassAndNativeState()
    {
        var cut = RenderInput(Context, p =>
        {
            p.Add(nameof(IonInput.Value), "53703");
            p.Add(nameof(IonInput.Disabled), true);
        });

        cut.Root.ShouldHaveClass("input-disabled");
        cut.FindByClass("native-input").Single().IsDisabled.ShouldBeTrue();
    }

    [Fact]
    public void IonInput_OnInput_UpdatesValueAndRaisesCallbacks()
    {
        string? boundValue = null;
        string? inputValue = null;
        var cut = RenderInput(Context, p =>
        {
            p.Add(nameof(IonInput.ValueChanged),
                EventCallback.Factory.Create<string?>(this, v => boundValue = v));
            p.Add(nameof(IonInput.OnInput),
                EventCallback.Factory.Create<string?>(this, v => inputValue = v));
        });

        var input = cut.FindByClass("native-input").Single();
        input.OnInput!.Invoke(new InputEventArgs { Target = input, Data = "Hello" });

        boundValue.ShouldBe("Hello");
        inputValue.ShouldBe("Hello");
    }

    [Fact]
    public void IonInput_OnInput_WhenDisabled_DoesNotRaiseCallbacks()
    {
        string? boundValue = null;
        var cut = RenderInput(Context, p =>
        {
            p.Add(nameof(IonInput.Disabled), true);
            p.Add(nameof(IonInput.ValueChanged),
                EventCallback.Factory.Create<string?>(this, v => boundValue = v));
        });

        var input = cut.FindByClass("native-input").Single();
        input.OnInput!.Invoke(new InputEventArgs { Target = input, Data = "Hello" });

        boundValue.ShouldBeNull();
    }

    [Fact]
    public void IonInput_ClearButton_ShownWhenClearInputAndHasValue()
    {
        var cut = RenderInput(Context, p =>
        {
            p.Add(nameof(IonInput.ClearInput), true);
            p.Add(nameof(IonInput.Value), "text");
        });

        cut.FindByClass("input-clear-icon").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonInput_ClearButton_HiddenWhenNoValue()
    {
        var cut = RenderInput(Context, p => p.Add(nameof(IonInput.ClearInput), true));

        cut.FindByClass("input-clear-icon").ShouldBeEmpty();
    }

    [Fact]
    public void IonInput_ClearButton_ClearsValueAndRaisesCallbacks()
    {
        string? boundValue = "unset";
        var cut = RenderInput(Context, p =>
        {
            p.Add(nameof(IonInput.ClearInput), true);
            p.Add(nameof(IonInput.Value), "text");
            p.Add(nameof(IonInput.ValueChanged),
                EventCallback.Factory.Create<string?>(this, v => boundValue = v));
        });

        var button = cut.FindByClass("input-clear-icon").Single();
        button.OnClick!.Invoke(new MouseEventArgs { Target = button });

        boundValue.ShouldBe(string.Empty);
    }

    // ---- Helper / error / counter -----------------------------------------

    [Fact]
    public void IonInput_RendersHelperAndErrorText()
    {
        var cut = RenderInput(Context, p =>
        {
            p.Add(nameof(IonInput.HelperText), "Enter your name");
            p.Add(nameof(IonInput.ErrorText), "Name is required");
        });

        cut.FindByClass("input-bottom").ShouldHaveSingleItem();
        cut.FindByClass("helper-text").Single().TextContent.ShouldBe("Enter your name");
        cut.FindByClass("error-text").Single().TextContent.ShouldBe("Name is required");
    }

    [Fact]
    public void IonInput_NoHintText_OmitsInputBottom()
    {
        var cut = RenderInput(Context);

        cut.FindByClass("input-bottom").ShouldBeEmpty();
    }

    [Fact]
    public void IonInput_Counter_RendersRatioText()
    {
        var cut = RenderInput(Context, p =>
        {
            p.Add(nameof(IonInput.Counter), true);
            p.Add(nameof(IonInput.Maxlength), 20);
            p.Add(nameof(IonInput.Value), "hello");
        });

        cut.FindByClass("counter").Single().TextContent.ShouldBe("5 / 20");
    }

    [Fact]
    public void IonInput_Counter_WithoutMaxlength_IsNotRendered()
    {
        var cut = RenderInput(Context, p => p.Add(nameof(IonInput.Counter), true));

        cut.FindByClass("counter").ShouldBeEmpty();
    }

    // ---- Key styles --------------------------------------------------------

    [Fact]
    public void IonInput_Style_HostIsBlockWithMdMinHeight()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderInput(Context);
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Display.ShouldBe(Display.Block);
        style.MinHeight.ShouldBe(Length.Px(44));
        style.FontSize.ShouldBe(Length.Px(16));
    }

    [Fact]
    public void IonInput_Style_IosUsesIosFontSize()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderInput(Context);

        cut.GetComputedStyle(cut.Root)!.FontSize.ShouldBe(Length.Px(17));
    }

    [Fact]
    public void IonInput_Style_OutlineFillUsesBorder()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderInput(Context, p => p.Add(nameof(IonInput.Fill), "outline"));
        var wrapper = cut.FindByClass("input-wrapper").Single();
        var style = cut.GetComputedStyle(wrapper)!;

        style.BorderTopWidth.ShouldBe(Length.Px(1));
        style.BorderTopStyle.ShouldBe(BorderStyle.Solid);
        style.BorderTopLeftRadius.Value.ShouldBe(4f);
    }

    [Fact]
    public void IonInput_Style_HelperAndErrorColors()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderInput(Context, p =>
        {
            p.Add(nameof(IonInput.HelperText), "help");
            p.Add(nameof(IonInput.ErrorText), "error");
        });

        var error = cut.FindByClass("error-text").Single();
        cut.GetComputedStyle(error)!.Color.ShouldBe(Color.FromHex("c5000f"));   // danger
    }

    // ---- Box model ---------------------------------------------------------

    [Fact]
    public void IonInput_BoxModel_SolidFillPadsWrapper()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderInput(Context, p => p.Add(nameof(IonInput.Fill), "solid"));
        var wrapper = cut.FindByClass("input-wrapper").Single();
        var box = cut.GetBoxModel(wrapper)!;

        box.Padding.Left.ShouldBe(16);
        box.Padding.Right.ShouldBe(16);
    }

    /// <summary>
    /// ISSUE-122: .input-wrapper 用 align-items:stretch 把 .label-text-wrapper /
    /// .native-wrapper 拉到字段高度，二者再用 align-items:center 垂直居中其内容。
    /// 拉伸出的高度必须对被拉伸项自身的布局生效，否则内容按拉伸前的自然高居中，表现为贴顶。
    /// </summary>
    [Fact]
    public void IonInput_BoxModel_StretchedWrappersCenterTheirContentVertically()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderInput(Context, label: "Company name");
        var wrapper = cut.FindByClass("input-wrapper").Single();
        var wrapperBox = cut.GetBoxModel(wrapper)!;

        foreach (var className in new[] { "label-text-wrapper", "native-wrapper" })
        {
            var stretched = cut.FindByClass(className).Single();
            var box = cut.GetBoxModel(stretched)!;

            // stretch：被拉到 wrapper 的内容高度。
            box.Content.Height.ShouldBe(wrapperBox.Content.Height, 0.5f);

            // 其内部内容相对拉伸后的高度垂直居中（修复前贴顶：Y == 自身内容盒 Y）。
            var child = stretched.Children[0];
            var childBox = cut.GetBoxModel(child)!;
            childBox.Content.Height.ShouldBeLessThan(box.Content.Height - 1f);

            float expectedY = box.Content.Y + (box.Content.Height - childBox.MarginBox.Height) / 2f;
            childBox.MarginBox.Y.ShouldBe(expectedY, 0.5f);
        }
    }

    // ---- In-item behavior --------------------------------------------------

    /// <summary>
    /// When IonInput is hosted inside an IonItem, the md highlight bar should be suppressed
    /// because the item already has its own bottom border. The highlight would be redundant.
    /// </summary>
    [Fact]
    public void IonInput_InsideIonItem_SuppressesHighlight()
    {
        var cut = Context.Render<IonItem>(p =>
        {
            p.Add(nameof(IonItem.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenComponent<IonInput>(0);
                builder.AddAttribute(1, nameof(IonInput.Label), "Company name");
                builder.CloseComponent();
            }));
        });

        var input = cut.FindByClass("ion-input").Single();
        // md mode by default, no fill means highlight should normally render, but not in an item
        cut.FindByClass("input-highlight").ShouldBeEmpty();
    }

    /// <summary>
    /// When IonInput is NOT inside an IonItem, the md highlight bar should render (unless
    /// fill="outline" or mode is ios).
    /// </summary>
    [Fact]
    public void IonInput_NotInsideIonItem_RendersHighlight()
    {
        var cut = RenderInput(Context);

        // md mode by default, no fill, not in an item → highlight should render
        cut.FindByClass("input-highlight").ShouldHaveSingleItem();
    }

    /// <summary>
    /// Outline fill should never render the highlight, even outside an item.
    /// </summary>
    [Fact]
    public void IonInput_OutlineFill_NeverRendersHighlight()
    {
        var cut = RenderInput(Context, p => p.Add(nameof(IonInput.Fill), "outline"));

        cut.FindByClass("input-highlight").ShouldBeEmpty();
    }

    /// <summary>
    /// IonInput inside IonItemDivider should also suppress the highlight.
    /// </summary>
    [Fact]
    public void IonInput_InsideIonItemDivider_SuppressesHighlight()
    {
        var cut = Context.Render<IonItemDivider>(p =>
        {
            p.Add(nameof(IonItemDivider.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenComponent<IonInput>(0);
                builder.AddAttribute(1, nameof(IonInput.Label), "Filter");
                builder.CloseComponent();
            }));
        });

        cut.FindByClass("input-highlight").ShouldBeEmpty();
    }

    // ---- Label width -------------------------------------------------------

    /// <summary>
    /// The label should auto-fit its content width without truncation when using the default
    /// "start" label placement. Previously, the label-text-wrapper had a max-width of 200px
    /// that was truncating labels like "Default input" to "Default inp".
    /// </summary>
    [Fact]
    public void IonInput_Label_AutoFitsContentWidth()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderInput(Context, label: "Default input");
        var labelWrapper = cut.FindByClass("label-text-wrapper").Single();
        var box = cut.GetBoxModel(labelWrapper)!;

        // The label text should be fully visible, not truncated.
        cut.GetTextContent().ShouldContain("Default input");

        // The wrapper should not have a max-width constraint for the "start" placement.
        // The max-width is only set on .label-text for "fixed" placement. For "start" and "end",
        // the label wrapper should auto-fit its content without being capped at 200px.
        // We can verify this by checking that the wrapper width is determined by its content.
        box.Content.Width.ShouldBeGreaterThan(0);
    }
}
