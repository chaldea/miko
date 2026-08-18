using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Styling;
using Miko.Testing;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-select</c> and its option data carriers.
/// <para>
/// The DOM contract mirrors <c>select.tsx</c>: there is NO native <c>&lt;select&gt;</c>. The field
/// renders its own text plus a visually-hidden focus button, <c>ion-select-option</c> renders
/// nothing (it only carries data), and clicking the host opens one of four overlays —
/// alert (default), action-sheet, popover, or modal.
/// </para>
/// </summary>
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

    private static ComponentUnderTest RenderSelectInItem(TestContext ctx,
        string labelPlacement = "start", string? placeholder = "Pick one")
        => ctx.Render<IonItem>(p => p.Add(nameof(IonItem.ChildContent), (RenderFragment)(builder =>
        {
            builder.OpenComponent<IonSelect>(0);
            builder.AddComponentParameter(1, nameof(IonSelect.Label), "Status");
            builder.AddComponentParameter(2, nameof(IonSelect.LabelPlacement), labelPlacement);
            builder.AddComponentParameter(3, nameof(IonSelect.Placeholder), placeholder);
            builder.AddComponentParameter(4, nameof(IonSelect.ChildContent), Options);
            builder.CloseComponent();
        })));

    /// <summary>Clicks the select host, which is what opens the overlay (select.tsx onClick).</summary>
    private static void ClickHost(ComponentUnderTest cut)
        => cut.Root.OnClick!.Invoke(new MouseEventArgs { Target = cut.Root });

    /// <summary>
    /// Concatenated text of a subtree. Re-rendering swaps the root's children in place (see
    /// ComponentBase.StateHasChanged), so post-click assertions re-read from the same root rather
    /// than from the original ComponentUnderTest snapshot.
    /// </summary>
    private static string TextOf(Element element)
    {
        if (element is Miko.Core.DomElements.TextNode text) return text.Text;
        var sb = new System.Text.StringBuilder();
        foreach (var child in element.Children) sb.Append(TextOf(child));
        return sb.ToString();
    }

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonSelect_RendersDomContract()
    {
        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Placeholder), "Pick one"));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-select");
        cut.Root.ShouldHaveClass("ion-focusable");
        cut.Root.ShouldHaveClass("select-ltr");

        var wrapper = cut.FindByClass("select-wrapper").Single();
        wrapper.TagName.ShouldBe("label");

        cut.FindByClass("select-wrapper-inner").ShouldHaveSingleItem();
        cut.FindByClass("native-wrapper").ShouldHaveSingleItem();
        cut.FindByClass("select-text").ShouldHaveSingleItem();
        cut.FindByClass("select-icon").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("Pick one");
    }

    [Fact]
    public void IonSelect_NativeWrapper_HoldsTextAndFocusButton_WithoutNativeSelect()
    {
        // select.tsx renders <div class="select-text"> + a visually-hidden <button> (the focus
        // target). A native <select> would delegate the option UI to the browser, which Miko has
        // no equivalent for — the overlay is what shows the options.
        var cut = RenderSelect(Context);

        var native = cut.FindByClass("native-wrapper").Single();
        native.Children.Select(c => c.TagName).ShouldBe(new[] { "div", "button" });

        var focusEl = cut.FindByClass("select-focus-el").Single();
        focusEl.TagName.ShouldBe("button");

        cut.FindByTagName("select").ShouldBeEmpty();
        cut.FindByTagName("option").ShouldBeEmpty();
    }

    [Fact]
    public void IonSelectOption_IsAHiddenDataCarrier()
    {
        // select-option.scss is `:host { display: none }` — the option contributes data, not UI.
        var cut = Context.Render<IonSelectOption>(p =>
        {
            p.Add(nameof(IonSelectOption.Value), "x");
            p.Add(nameof(IonSelectOption.Disabled), true);
            p.AddChildContent(Text("Option X"));
        });

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("ion-select-option");
        cut.Root.ShouldHaveClass("select-option-disabled");
    }

    [Fact]
    public void IonSelectOption_HiddenByStylesheet()
    {
        // select-option.scss `:host { display: none }`. A display:none root has no layout box at
        // all (rendering one throws), so assert the matched stylesheet rule instead of a computed
        // style — the same approach IonicStyleSheetTests uses for hidden elements.
        var option = new IonSelectOption { Value = "x" };
        var root = option.Build();

        var display = IonicStyleSheetFactory.CreateAllModes().Rules
            .Where(r => r.Selector.Matches(root))
            .Select(r => r.Style.Display)
            .LastOrDefault(d => d is not null);

        display.ShouldBe(Display.None);
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
    public void IonSelect_JustifyIsDroppedForFloatingLabel()
    {
        // select.tsx: `justifyEnabled = !hasFloatingOrStackedLabel && justify !== undefined`.
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Label), "Status");
            p.Add(nameof(IonSelect.LabelPlacement), "floating");
            p.Add(nameof(IonSelect.Justify), "space-between");
        });

        cut.Root.ShouldNotHaveClass("select-justify-space-between");
    }

    [Fact]
    public void IonSelect_InItem_StampsContextClassAndSuppressesOwnHighlight()
    {
        var cut = RenderSelectInItem(Context);
        var select = cut.FindByClass("ion-select").Single();

        select.ShouldHaveClass("in-item");
        select.FindByClass("select-highlight").ShouldBeEmpty();
    }

    [Fact]
    public void IonSelect_Standalone_DoesNotStampInItemClass()
    {
        var cut = RenderSelect(Context);

        cut.Root.ShouldNotHaveClass("in-item");
    }

    [Fact]
    public void IonSelect_OutlineFill_RendersNotchStructure()
    {
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Label), "Status");
            p.Add(nameof(IonSelect.Fill), "outline");
        });

        cut.FindByClass("select-outline-container").ShouldHaveSingleItem();
        cut.FindByClass("select-outline-start").ShouldHaveSingleItem();
        cut.FindByClass("select-outline-end").ShouldHaveSingleItem();
        cut.FindByClass("notch-spacer").ShouldHaveSingleItem();
        cut.FindByClass("select-outline-notch").Single().ShouldNotHaveClass("select-outline-notch-hidden");
    }

    [Fact]
    public void IonSelect_OutlineFill_HidesNotchWithoutLabel()
    {
        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Fill), "outline"));

        cut.FindByClass("select-outline-notch").Single().ShouldHaveClass("select-outline-notch-hidden");
        cut.FindByClass("label-text-wrapper").Single().ShouldHaveClass("label-text-wrapper-hidden");
    }

    [Fact]
    public void IonSelect_FloatingOrStackedLabel_RendersIconOutsideInnerWrapper()
    {
        // select.tsx renders the icon outside .select-wrapper-inner for floating/stacked labels so
        // it centers against the whole select rather than the vertically offset inner row.
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Label), "Status");
            p.Add(nameof(IonSelect.LabelPlacement), "stacked");
        });

        var icon = cut.FindByClass("select-icon").Single();
        var inner = cut.FindByClass("select-wrapper-inner").Single();

        inner.FindByClass("select-icon").ShouldBeEmpty();
        icon.Parent.ShouldBe(cut.FindByClass("select-wrapper").Single());
    }

    [Fact]
    public void IonSelect_DefaultLabelPlacement_RendersIconInsideInnerWrapper()
    {
        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Label), "Status"));

        var inner = cut.FindByClass("select-wrapper-inner").Single();
        inner.FindByClass("select-icon").ShouldHaveSingleItem();
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
    public void IonSelect_HelperAndErrorText_RenderInBottomContent()
    {
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.HelperText), "Choose wisely");
            p.Add(nameof(IonSelect.ErrorText), "Required");
        });

        cut.FindByClass("select-bottom").ShouldHaveSingleItem();
        cut.FindByClass("helper-text").Single().TextContent.ShouldBe("Choose wisely");
        cut.FindByClass("error-text").Single().TextContent.ShouldBe("Required");
    }

    // ---- Value / displayed text -------------------------------------------

    [Fact]
    public void IonSelect_Value_DisplaysMatchingOptionText()
    {
        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Value), "b"));

        cut.Root.ShouldHaveClass("has-value");
        var text = cut.FindByClass("select-text").Single();
        text.TextContent.ShouldBe("Beta");
        text.ShouldNotHaveClass("select-placeholder");
    }

    [Fact]
    public void IonSelect_WithoutValue_ShowsPlaceholder()
    {
        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Placeholder), "Pick one"));

        cut.Root.ShouldNotHaveClass("has-value");
        cut.Root.ShouldHaveClass("has-placeholder");
        var text = cut.FindByClass("select-text").Single();
        text.TextContent.ShouldBe("Pick one");
        text.ShouldHaveClass("select-placeholder");
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
    public void IonSelect_Multiple_JoinsSelectedTextsWithComma()
    {
        // select.tsx generateText(): array values map to their option text and join with ", ".
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Multiple), true);
            p.Add(nameof(IonSelect.Values), (IReadOnlyList<string>)new[] { "a", "b" });
        });

        cut.FindByClass("select-text").Single().TextContent.ShouldBe("Alpha, Beta");
        cut.Root.ShouldHaveClass("has-value");
    }

    [Fact]
    public void IonSelect_OptionWithoutValue_FallsBackToItsText()
    {
        // getOptionValue(): `value === undefined ? el.textContent : value`.
        var cut = Context.Render<IonSelect>(p =>
        {
            p.Add(nameof(IonSelect.Value), "Gamma");
            p.Add(nameof(IonSelect.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenComponent<IonSelectOption>(0);
                builder.AddComponentParameter(1, nameof(IonSelectOption.ChildContent), Text("Gamma"));
                builder.CloseComponent();
            }));
        });

        cut.FindByClass("select-text").Single().TextContent.ShouldBe("Gamma");
    }

    // ---- Label floating ----------------------------------------------------

    [Fact]
    public void IonSelect_StackedLabel_AlwaysFloats()
    {
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Label), "Status");
            p.Add(nameof(IonSelect.LabelPlacement), "stacked");
        });

        cut.Root.ShouldHaveClass("label-floating");
    }

    [Fact]
    public void IonSelect_FloatingLabel_FloatsOnlyWithValue()
    {
        var empty = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Label), "Status");
            p.Add(nameof(IonSelect.LabelPlacement), "floating");
        });
        empty.Root.ShouldNotHaveClass("label-floating");

        var filled = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Label), "Status");
            p.Add(nameof(IonSelect.LabelPlacement), "floating");
            p.Add(nameof(IonSelect.Value), "b");
        });
        filled.Root.ShouldHaveClass("label-floating");
    }

    [Fact]
    public void IonSelect_FloatingLabel_FloatsWithStartSlotContent()
    {
        // TODO(FW-5592) in select.tsx: start/end slot content forces the label to float.
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Label), "Status");
            p.Add(nameof(IonSelect.LabelPlacement), "floating");
            p.Add(nameof(IonSelect.StartSlot), (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.CloseElement();
            }));
        });

        cut.Root.ShouldHaveClass("label-floating");
    }

    // ---- Overlay opening ---------------------------------------------------

    [Fact]
    public void IonSelect_DefaultsToAlertInterface_AndOpensOnClick()
    {
        var cut = RenderSelect(Context);

        cut.FindByClass("ion-alert").Single().ShouldHaveClass("overlay-hidden");
        cut.Root.ShouldNotHaveClass("select-expanded");

        ClickHost(cut);


        cut.Root.ShouldHaveClass("select-expanded");
        var alert = cut.Root.FindByClass("ion-alert").Single();
        alert.ShouldNotHaveClass("overlay-hidden");
        alert.ShouldHaveClass("select-alert");
        alert.ShouldHaveClass("single-select-alert");
    }

    [Fact]
    public void IonSelect_AlertInterface_RendersRadioInputsAndOkCancelButtons()
    {
        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Value), "b"));

        ClickHost(cut);


        // Single-value → radio group; the selected option is checked.
        var radios = cut.Root.FindByClass("alert-radio-button");
        radios.Count.ShouldBe(2);
        radios[1].ShouldHaveClass("alert-radio-button-checked");
        TextOf(cut.Root).ShouldContain("Alpha");
        TextOf(cut.Root).ShouldContain("Beta");

        var buttons = cut.Root.FindByClass("alert-button");
        buttons.Count.ShouldBe(2);
        TextOf(cut.Root).ShouldContain("Cancel");
        TextOf(cut.Root).ShouldContain("OK");
    }

    [Fact]
    public void IonSelect_AlertInterface_UsesCheckboxInputsWhenMultiple()
    {
        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Multiple), true));

        ClickHost(cut);


        cut.Root.FindByClass("alert-checkbox-button").Count.ShouldBe(2);
        cut.Root.FindByClass("ion-alert").Single().ShouldHaveClass("multiple-select-alert");
    }

    [Fact]
    public void IonSelect_AlertHeaderFallsBackToLabel()
    {
        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Label), "Airport"));

        ClickHost(cut);

        cut.Root.FindByClass("alert-title").Single().TextContent.ShouldBe("Airport");
    }

    [Fact]
    public void IonSelect_ActionSheetInterface_RendersOptionButtonsPlusCancel()
    {
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Interface), "action-sheet");
            p.Add(nameof(IonSelect.Value), "a");
        });

        ClickHost(cut);


        var sheet = cut.Root.FindByClass("ion-action-sheet").Single();
        sheet.ShouldNotHaveClass("overlay-hidden");

        // Two options + a cancel button; the selected one carries role="selected".
        cut.Root.FindByClass("action-sheet-button").Count.ShouldBe(3);
        cut.Root.FindByClass("action-sheet-selected").ShouldHaveSingleItem();
        cut.Root.FindByClass("action-sheet-cancel").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonSelect_ActionSheetWithMultiple_FallsBackToAlert()
    {
        // select.tsx warns and swaps to the alert interface for multi-value action sheets.
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Interface), "action-sheet");
            p.Add(nameof(IonSelect.Multiple), true);
        });

        cut.FindByClass("ion-action-sheet").ShouldBeEmpty();
        cut.FindByClass("ion-alert").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonSelect_PopoverInterface_RendersSelectPopoverBody()
    {
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Interface), "popover");
            p.Add(nameof(IonSelect.Value), "b");
        });

        ClickHost(cut);


        var popover = cut.Root.FindByClass("ion-popover").Single();
        popover.ShouldNotHaveClass("overlay-hidden");
        popover.ShouldHaveClass("select-popover");

        cut.Root.FindByClass("ion-select-popover").ShouldHaveSingleItem();
        cut.Root.FindByClass("ion-radio").Count.ShouldBe(2);
        cut.Root.FindByClass("item-radio-checked").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonSelect_ModalInterface_RendersSelectModalBody()
    {
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Interface), "modal");
            p.Add(nameof(IonSelect.InterfaceHeader), "Airports");
        });

        ClickHost(cut);


        var modal = cut.Root.FindByClass("ion-modal").Single();
        modal.ShouldNotHaveClass("overlay-hidden");
        modal.ShouldHaveClass("select-modal");

        cut.Root.FindByClass("ion-select-modal").ShouldHaveSingleItem();
        cut.Root.FindByClass("ion-toolbar").ShouldHaveSingleItem();
        TextOf(cut.Root).ShouldContain("Airports");
        // select.tsx openModal() forwards the select's own cancelText to the modal, so the close
        // button reads "Cancel" here rather than ion-select-modal's standalone "Close" default.
        TextOf(cut.Root).ShouldContain("Cancel");
    }

    [Fact]
    public void IonSelectModal_StandaloneCancelTextDefaultsToClose()
    {
        var cut = Context.Render<IonSelectModal>(p =>
            p.Add(nameof(IonSelectModal.Options),
                (IReadOnlyList<IonSelectOptionData>)new[]
                {
                    new IonSelectOptionData { Value = "a", Text = "Alpha" },
                }));

        TextOf(cut.Root).ShouldContain("Close");
    }

    [Fact]
    public void IonSelect_DisabledDoesNotOpenOverlay()
    {
        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Disabled), true));

        cut.Root.ShouldHaveClass("select-disabled");

        ClickHost(cut);

        cut.Root.FindByClass("ion-alert").Single().ShouldHaveClass("overlay-hidden");
        cut.Root.ShouldNotHaveClass("select-expanded");
    }

    [Fact]
    public void IonSelect_ExpandedIcon_StampsMarkerClass()
    {
        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.ExpandedIcon), Ionicons.ChevronUp));

        cut.Root.ShouldHaveClass("has-expanded-icon");
    }

    [Fact]
    public void IonSelect_OpenedAlert_IsAViewportOverlayRatherThanInlineContent()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = RenderSelect(Context);

        ClickHost(cut);
        var updated = Context.RenderElement(cut.Root);
        var alert = updated.Root.FindByClass("ion-alert").Single();
        var style = updated.GetComputedStyle(alert)!;
        var box = updated.GetBoxModel(alert)!;

        style.Position.ShouldBe(Position.Fixed);
        box.BorderBox.X.ShouldBe(0f, 0.01f);
        box.BorderBox.Y.ShouldBe(0f, 0.01f);
        box.BorderBox.Width.ShouldBe(Context.ViewportWidth, 0.01f);
        box.BorderBox.Height.ShouldBe(Context.ViewportHeight, 0.01f);
        alert.FindByClass("alert-radio-button").Count.ShouldBe(2);
    }

    /// <summary>
    /// The real nesting is <c>ion-list &gt; ion-item &gt; ion-select</c>, and BOTH wrappers clip:
    /// <c>.ion-item</c> is <c>overflow: hidden</c> with a 48px min-height, and an inset
    /// <c>.ion-list</c> clips too. The overlay is mounted inside the select host, so it has two
    /// clipping ancestors between it and the viewport.
    /// <para>
    /// issues/ion-select.md 问题 4: the overlay was painted in place and got sliced down to the
    /// item's 48px — the options were simply not on screen. Layout was never the problem (the box
    /// below has always been the full viewport); the render pass was, so this asserts the pixel and
    /// hit-test consequences through the real component nesting rather than a bare select.
    /// </para>
    /// </summary>
    [Fact]
    public void IonSelect_OverlayInsideAClippingItem_IsPaintedAndHittableAcrossTheViewport()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var list = Context.Render<IonList>(p => p.Add(nameof(IonList.ChildContent), (RenderFragment)(builder =>
        {
            builder.OpenComponent<IonItem>(0);
            builder.AddComponentParameter(1, nameof(IonItem.ChildContent), (RenderFragment)(inner =>
            {
                inner.OpenComponent<IonSelect>(0);
                inner.AddComponentParameter(1, nameof(IonSelect.Label), "Status");
                inner.AddComponentParameter(2, nameof(IonSelect.ChildContent), Options);
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        })));

        var select = list.Root.FindByClass("ion-select").Single();
        select.OnClick!.Invoke(new MouseEventArgs { Target = select });

        var updated = Context.RenderElement(list.Root);
        var alert = updated.Root.FindByClass("ion-alert").Single();

        // The clipping ancestor really is short — otherwise this fixture proves nothing.
        var item = updated.Root.FindByClass("ion-item").Single();
        var itemBox = updated.GetBoxModel(item)!;
        itemBox.BorderBox.Height.ShouldBeLessThan(Context.ViewportHeight / 2f,
            "the ion-item must be far shorter than the overlay for the clip to matter");
        updated.GetComputedStyle(item)!.OverflowY.ShouldBe(Overflow.Hidden,
            "this test is only meaningful while ion-item clips its overflow");

        updated.GetBoxModel(alert)!.BorderBox.Height.ShouldBe(Context.ViewportHeight, 0.01f);

        // The paint + hit-test halves of the fix: a point far below the 48px item — inside the
        // overlay's backdrop — must resolve into the overlay subtree, not to whatever sits behind.
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(
            new SKImageInfo((int)Context.ViewportWidth, (int)Context.ViewportHeight));
        engine.Initialize(list.Root, Context.StyleSheets, surface.Canvas,
            Context.ViewportWidth, Context.ViewportHeight);
        engine.Render(surface.Canvas);

        float probeY = Context.ViewportHeight - 20f;
        var hit = engine.HitTest(Context.ViewportWidth / 2f, probeY);

        hit.ShouldNotBeNull();
        IsInside(hit, alert).ShouldBeTrue(
            $"the point (…, {probeY}) is far outside the 48px ion-item, but the fixed overlay covers it");
    }

    /// <summary>
    /// Picking an option by actually clicking it must select it — the whole point of the overlay.
    /// <para>
    /// Two defects made this a no-op even once the overlay was visible and hit-testable, and both
    /// come from the same root cause: this port mounts the overlay INSIDE the select host, whereas
    /// Ionic's overlay controller creates it as a detached sibling.
    /// </para>
    /// <list type="number">
    /// <item>Every tap inside the overlay bubbled back up to the host's own <c>@onclick</c>, which
    /// re-asserted the expanded state.</item>
    /// <item><c>IonAlert</c> ticks a radio by mutating the <c>IonAlertInput</c> objects it was
    /// handed and only reports the result on dismiss, but <c>Build()</c> minted fresh inputs on
    /// every render — so the tick was discarded and OK confirmed nothing.</item>
    /// </list>
    /// This drives the real sequence a user performs: open → tap an option → tap OK.
    /// </summary>
    [Fact]
    public void IonSelect_ClickingAnOptionThenOk_SelectsIt()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        string? changed = null;
        var cut = Context.Render<IonItem>(p => p.Add(nameof(IonItem.ChildContent), (RenderFragment)(builder =>
        {
            builder.OpenComponent<IonSelect>(0);
            builder.AddComponentParameter(1, nameof(IonSelect.Label), "Status");
            builder.AddComponentParameter(2, nameof(IonSelect.Placeholder), "Pick one");
            builder.AddComponentParameter(3, nameof(IonSelect.ValueChanged),
                EventCallback.Factory.Create<string?>(this, v => changed = v));
            builder.AddComponentParameter(4, nameof(IonSelect.ChildContent), Options);
            builder.CloseComponent();
        })));

        var select = cut.Root.FindByClass("ion-select").Single();
        select.OnClick!.Invoke(new MouseEventArgs { Target = select });

        // Tap the second option ("Beta"). The click target is the label div inside the button, so
        // this also exercises the bubbling path that used to re-trigger the host handler.
        var radio = cut.Root.FindByClass("alert-radio-button")[1];
        var label = radio.FindByClass("alert-radio-label").Single();
        Dispatch(label);

        Context.RenderElement(cut.Root)
            .Root.FindByClass("alert-radio-button-checked")
            .Count.ShouldBe(1, "tapping an option must tick it and survive the re-render");

        // Confirm with OK (the button without the cancel role).
        var ok = cut.Root.FindByClass("alert-button")
            .First(b => !(b.Class ?? string.Empty).Contains("role-cancel"));
        Dispatch(ok);

        changed.ShouldBe("b");

        var final = Context.RenderElement(cut.Root);
        TextOf(final.Root.FindByClass("select-text").Single()).ShouldBe("Beta");
        final.Root.FindByClass("ion-alert").Single().ShouldHaveClass("overlay-hidden");
    }

    [Fact]
    public void IonSelect_CancellingTheOverlay_LeavesTheValueAlone()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Placeholder), "Pick one"));
        ClickHost(cut);

        Dispatch(cut.Root.FindByClass("alert-radio-button")[0]);
        var cancel = cut.Root.FindByClass("alert-button")
            .First(b => (b.Class ?? string.Empty).Contains("role-cancel"));
        Dispatch(cancel);

        var final = Context.RenderElement(cut.Root);
        TextOf(final.Root.FindByClass("select-text").Single()).ShouldBe("Pick one",
            "cancelling must not commit the ticked option");
        final.Root.FindByClass("ion-alert").Single().ShouldHaveClass("overlay-hidden");
    }

    /// <summary>
    /// Dispatches a bubbling click the way <c>MikoInteractionController.HandleClick</c> does, so the
    /// event travels the real ancestor chain rather than being poked straight into one handler.
    /// </summary>
    private static void Dispatch(Element target)
        => new EventDispatcher().Dispatch(target, EventTypes.Click,
            new MouseEventArgs { Target = target, Button = MouseButton.Left, Bubbles = true });

    /// <summary>Whether <paramref name="element"/> is <paramref name="ancestor"/> or sits under it.</summary>
    private static bool IsInside(Element element, Element ancestor)
    {
        for (var node = element; node != null; node = node.Parent)
        {
            if (ReferenceEquals(node, ancestor)) return true;
        }
        return false;
    }

    // ---- Selection round-trip ---------------------------------------------

    [Fact]
    public void IonSelect_PopoverSelection_UpdatesValueAndClosesOverlay()
    {
        string? changed = null;
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Interface), "popover");
            p.Add(nameof(IonSelect.ValueChanged),
                EventCallback.Factory.Create<string?>(this, v => changed = v));
        });

        ClickHost(cut);

        // Tapping a radio inside the popover reports the selection up through the group.
        var radioWrapper = cut.Root.FindByClass("radio-wrapper")[1];
        radioWrapper.OnClick!.Invoke(new MouseEventArgs { Target = radioWrapper });

        changed.ShouldBe("b");


        cut.Root.FindByClass("select-text").Single().TextContent.ShouldBe("Beta");
        cut.Root.ShouldNotHaveClass("select-expanded");
        cut.Root.FindByClass("ion-popover").Single().ShouldHaveClass("overlay-hidden");
    }

    [Fact]
    public void IonSelect_AlertConfirm_AppliesCheckedInputValue()
    {
        string? changed = null;
        var cut = RenderSelect(Context, p =>
            p.Add(nameof(IonSelect.ValueChanged),
                EventCallback.Factory.Create<string?>(this, v => changed = v)));

        ClickHost(cut);

        // Check the second radio, then confirm with OK — the alert reports the checked value as
        // its dismiss data.
        var radio = cut.Root.FindByClass("alert-radio-button")[1];
        radio.OnClick!.Invoke(new MouseEventArgs { Target = radio });

        var ok = cut.Root.FindByClass("alert-button")[1];
        ok.OnClick!.Invoke(new MouseEventArgs { Target = ok });

        changed.ShouldBe("b");
        cut.Root.ShouldNotHaveClass("select-expanded");
    }

    [Fact]
    public void IonSelect_AlertCancel_RaisesOnCancelAndKeepsValue()
    {
        var cancelled = false;
        var dismissed = false;
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Value), "a");
            p.Add(nameof(IonSelect.OnCancel), EventCallback.Factory.Create(this, () => cancelled = true));
            p.Add(nameof(IonSelect.OnDismiss), EventCallback.Factory.Create(this, () => dismissed = true));
        });

        ClickHost(cut);

        var cancel = cut.Root.FindByClass("alert-button")[0];
        cancel.OnClick!.Invoke(new MouseEventArgs { Target = cancel });

        cancelled.ShouldBeTrue();
        dismissed.ShouldBeTrue();


        cut.Root.ShouldNotHaveClass("select-expanded");
        cut.Root.FindByClass("select-text").Single().TextContent.ShouldBe("Alpha");
    }

    [Fact]
    public void IonSelect_ActionSheetSelection_AppliesTappedOptionValue()
    {
        string? changed = null;
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Interface), "action-sheet");
            p.Add(nameof(IonSelect.ValueChanged),
                EventCallback.Factory.Create<string?>(this, v => changed = v));
        });

        ClickHost(cut);

        var option = cut.Root.FindByClass("action-sheet-button")[1];
        option.OnClick!.Invoke(new MouseEventArgs { Target = option });

        changed.ShouldBe("b");
        cut.Root.FindByClass("select-text").Single().TextContent.ShouldBe("Beta");
    }

    [Fact]
    public void IonSelect_MultipleSelection_KeepsOverlayOpenAndAccumulatesValues()
    {
        IReadOnlyList<string>? changed = null;
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Interface), "popover");
            p.Add(nameof(IonSelect.Multiple), true);
            p.Add(nameof(IonSelect.Values), (IReadOnlyList<string>)new[] { "a" });
            p.Add(nameof(IonSelect.ValuesChanged),
                EventCallback.Factory.Create<IReadOnlyList<string>>(this, v => changed = v));
        });

        ClickHost(cut);

        var checkboxWrapper = cut.Root.FindByClass("checkbox-wrapper")[1];
        checkboxWrapper.OnClick!.Invoke(new MouseEventArgs { Target = checkboxWrapper });

        changed.ShouldBe(new[] { "a", "b" });

        // Multi-value overlays stay open so more options can be ticked.
        cut.Root.ShouldHaveClass("select-expanded");
    }

    [Fact]
    public void IonSelect_ModalCancel_ClosesWithoutSelecting()
    {
        var cancelled = false;
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Interface), "modal");
            p.Add(nameof(IonSelect.OnCancel), EventCallback.Factory.Create(this, () => cancelled = true));
        });

        ClickHost(cut);

        var closeButton = cut.Root.FindByClass("button-native").Single();
        closeButton.OnClick!.Invoke(new MouseEventArgs { Target = closeButton });

        cancelled.ShouldBeTrue();
        cut.Root.ShouldNotHaveClass("select-expanded");
    }

    // ---- Focus -------------------------------------------------------------

    [Fact]
    public void IonSelect_FocusButton_TogglesHasFocusClassAndRaisesCallbacks()
    {
        var focused = false;
        var blurred = false;
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.OnFocus), EventCallback.Factory.Create(this, () => focused = true));
            p.Add(nameof(IonSelect.OnBlur), EventCallback.Factory.Create(this, () => blurred = true));
        });

        var focusEl = cut.FindByClass("select-focus-el").Single();
        focusEl.OnFocus!.Invoke(new FocusEventArgs { Target = focusEl });

        focused.ShouldBeTrue();
        cut.Root.ShouldHaveClass("has-focus");

        var refocusEl = cut.Root.FindByClass("select-focus-el").Single();
        refocusEl.OnBlur!.Invoke(new FocusEventArgs { Target = refocusEl });

        blurred.ShouldBeTrue();
        cut.Root.ShouldNotHaveClass("has-focus");
    }

    // ---- Styles ------------------------------------------------------------

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
        var start = cut.FindByClass("select-outline-start").Single();
        var style = cut.GetComputedStyle(start)!;

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

    [Theory]
    [InlineData("start", FlexDirection.Row)]
    [InlineData("fixed", FlexDirection.Row)]
    [InlineData("end", FlexDirection.RowReverse)]
    [InlineData("stacked", FlexDirection.Column)]
    [InlineData("floating", FlexDirection.Column)]
    public void IonSelect_LabelPlacement_ChangesWrapperDirection(string placement, FlexDirection expected)
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Label), "Status");
            p.Add(nameof(IonSelect.LabelPlacement), placement);
            p.Add(nameof(IonSelect.Placeholder), "Pick one");
        });

        var wrapper = cut.FindByClass("select-wrapper").Single();
        cut.GetComputedStyle(wrapper)!.FlexDirection.ShouldBe(expected);
    }

    [Fact]
    public void IonSelect_FixedLabelPlacement_UsesFixedLabelWidth()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = RenderSelect(Context, p =>
        {
            p.Add(nameof(IonSelect.Label), "Status");
            p.Add(nameof(IonSelect.LabelPlacement), "fixed");
        });

        var label = cut.FindByClass("label-text-wrapper").Single();
        var style = cut.GetComputedStyle(label)!;

        style.Width.ShouldBe(Length.Px(100));
        style.MinWidth.ShouldBe(Length.Px(100));
        style.FlexGrow.ShouldBe(0f);
        style.FlexShrink.ShouldBe(0f);
        style.FlexBasis.ShouldBe(Length.Px(100));
    }

    [Fact]
    public void IonSelect_Style_InItem_HostCanShrinkWithinItem()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = RenderSelectInItem(Context);
        var select = cut.FindByClass("ion-select").Single();
        var style = cut.GetComputedStyle(select)!;

        style.FlexGrow.ShouldBe(1f);
        style.FlexShrink.ShouldBe(1f);
        style.FlexBasis.ShouldBe(Length.Px(0));
    }

    // ---- Layout ------------------------------------------------------------

    [Fact]
    public void IonSelect_InnerWrapper_LaysTextAndIconOnOneRow()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderSelect(Context, p => p.Add(nameof(IonSelect.Value), "b"));

        var text = cut.GetBoxModel(cut.FindByClass("select-text").Single())!;
        var icon = cut.GetBoxModel(cut.FindByClass("select-icon").Single())!;

        // Same row: the icon sits after the text horizontally, not below it.
        icon.BorderBox.X.ShouldBeGreaterThan(text.BorderBox.X);
        icon.BorderBox.Y.ShouldBeLessThan(text.BorderBox.Y + text.BorderBox.Height);
    }

    [Fact]
    public void IonSelect_WithoutPlaceholder_KeepsIconAtEndEdge()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = RenderSelect(Context);

        var inner = cut.GetBoxModel(cut.FindByClass("select-wrapper-inner").Single())!;
        var icon = cut.GetBoxModel(cut.FindByClass("select-icon").Single())!;

        icon.BorderBox.X.ShouldBeGreaterThan(inner.Content.X + inner.Content.Width / 2f);
        icon.BorderBox.Right.ShouldBe(inner.Content.Right, 0.01f);
    }
}
