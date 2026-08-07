using Microsoft.Extensions.DependencyInjection;
using Miko.Testing;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Components;
using Miko.Events;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonItemTests : IonicComponentTestBase
{
    private static readonly RenderFragment Label = builder =>
    {
        builder.OpenComponent<IonLabel>(0);
        builder.AddAttribute(1, nameof(IonLabel.ChildContent), (RenderFragment)(b => b.AddContent(0, "Basic Item")));
        builder.CloseComponent();
    };

    private static readonly RenderFragment Icon = builder =>
    {
        builder.OpenComponent<IonIcon>(0);
        builder.CloseComponent();
    };

    // Renders an IonButton child; pass size to set an explicit Size (null = unset).
    private static RenderFragment ButtonChild(string? size = null, RenderFragment? start = null) => builder =>
    {
        int seq = 0;
        builder.OpenComponent<IonButton>(seq++);
        if (size is not null)
        {
            builder.AddComponentParameter(seq++, nameof(IonButton.Size), size);
        }
        if (start is not null)
        {
            builder.AddComponentParameter(seq++, nameof(IonButton.Start), start);
        }
        builder.AddComponentParameter(seq++, nameof(IonButton.ChildContent),
            (RenderFragment)(b => b.AddContent(0, "OK")));
        builder.CloseComponent();
    };

    // --- DOM contract -------------------------------------------------------

    [Fact]
    public void IonItem_RendersHostWithNativeInnerWrapperStructure()
    {
        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        // Host is a div carrying the ion-item class.
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-item item-lines-default");

        // Nested structure: item-native > item-inner > input-wrapper (item.tsx render()).
        cut.FindByClass("item-native").Count.ShouldBe(1);
        cut.FindByClass("item-inner").Count.ShouldBe(1);
        cut.FindByClass("input-wrapper").Count.ShouldBe(1);
    }

    [Fact]
    public void IonItem_RendersDefaultSlotInsideInputWrapper()
    {
        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        var wrapper = cut.FindByClass("input-wrapper").Single();
        // The label content lives inside the input-wrapper, not directly on the host.
        var text = "";
        Collect(wrapper);
        void Collect(Miko.Core.Element el)
        {
            if (el is Miko.Core.DomElements.TextNode tn) text += tn.Text;
            foreach (var c in el.Children) Collect(c);
        }
        text.ShouldContain("Basic Item");
    }

    [Fact]
    public void IonItem_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        cut.Root.ShouldHaveClass("ios");
        cut.Root.ShouldHaveClass("ion-item");
    }

    // --- lines --------------------------------------------------------------

    [Fact]
    public void IonItem_DefaultLines_StampsLinesDefault()
    {
        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        cut.Root.ShouldHaveClass("item-lines-default");
        cut.Root.ShouldNotHaveClass("item-lines-none");
    }

    [Theory]
    [InlineData("none", "item-lines-none")]
    [InlineData("inset", "item-lines-inset")]
    [InlineData("full", "item-lines-full")]
    public void IonItem_StampsLinesClass_WhenLinesProvided(string lines, string expectedClass)
    {
        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Lines), lines);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        cut.Root.ShouldHaveClass(expectedClass);
        cut.Root.ShouldNotHaveClass("item-lines-default");
    }

    // --- button / href (native tag) ----------------------------------------

    [Fact]
    public void IonItem_RendersButtonNative_AndActivatable_WhenButton()
    {
        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Button), true);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var native = cut.FindByClass("item-native").Single();
        native.TagName.ShouldBe("button");
        cut.Root.ShouldHaveClass("ion-activatable");
    }

    [Fact]
    public void IonItem_RendersAnchorNative_WhenHref()
    {
        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Href), "/details");
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var native = cut.FindByClass("item-native").Single();
        native.TagName.ShouldBe("a");
        cut.Root.ShouldHaveClass("ion-activatable");
    }

    [Fact]
    public void IonItem_RendersDivNative_AndNotActivatable_ByDefault()
    {
        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        var native = cut.FindByClass("item-native").Single();
        native.TagName.ShouldBe("div");
        cut.Root.ShouldNotHaveClass("ion-activatable");
    }

    // --- detail chevron -----------------------------------------------------

    [Fact]
    public void IonItem_ShowsDetailIcon_WhenDetailTrue()
    {
        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Detail), true);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        cut.FindByClass("item-detail-icon").Count.ShouldBe(1);
    }

    [Fact]
    public void IonItem_HidesDetailIcon_ByDefault_OnMd()
    {
        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Button), true);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        // md + clickable does not auto-show the chevron (Ionic: ios only).
        cut.FindByClass("item-detail-icon").Count.ShouldBe(0);
    }

    [Fact]
    public void IonItem_AutoShowsDetailIcon_WhenClickable_OnIos()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Button), true);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        // ios + clickable auto-shows the chevron (Ionic showDetail default).
        cut.FindByClass("item-detail-icon").Count.ShouldBe(1);
    }

    [Fact]
    public void IonItem_ForcesDetailIconOff_WhenDetailFalse_OnIosClickable()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Button), true);
            parameters.Add(nameof(IonItem.Detail), false);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        cut.FindByClass("item-detail-icon").Count.ShouldBe(0);
    }

    // --- color --------------------------------------------------------------

    [Fact]
    public void IonItem_StampsColorClasses_WhenColorProvided()
    {
        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Color), "primary");
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        cut.Root.ShouldHaveClass("ion-color");
        cut.Root.ShouldHaveClass("ion-color-primary");
    }

    // --- disabled -----------------------------------------------------------

    [Fact]
    public void IonItem_StampsDisabledClass_AndState_WhenDisabled()
    {
        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Disabled), true);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        cut.Root.ShouldHaveClass("item-disabled");
        cut.Root.HasState(Miko.Core.ElementState.Disabled).ShouldBeTrue();
    }

    // --- start / end slots --------------------------------------------------

    [Fact]
    public void IonItem_RendersStartAndEndSlots_AsMarkerSpans()
    {
        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Start), (RenderFragment)(b => b.AddContent(0, "S")));
            parameters.Add(nameof(IonItem.End), (RenderFragment)(b => b.AddContent(0, "E")));
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        cut.FindByClass("ion-slot-start").Count.ShouldBe(1);
        cut.FindByClass("ion-slot-end").Count.ShouldBe(1);
    }

    // --- key style / interaction -------------------------------------------

    [Fact]
    public void IonItem_DefaultLines_DrawsHairlineOnItemInner_NotNative()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        // item.md.scss / item.ios.scss :host: the DEFAULT divider is --inner-border-width
        // (0 0 1px 0) on .item-inner — inset from the leading padding. The native surface's
        // --border-width defaults to 0; only lines="full" draws there.
        var nativeStyle = cut.GetComputedStyle(cut.FindByClass("item-native").Single());
        var innerStyle = cut.GetComputedStyle(cut.FindByClass("item-inner").Single());
        nativeStyle.ShouldNotBeNull();
        innerStyle.ShouldNotBeNull();
        nativeStyle.BorderBottomWidth.ShouldBe(Miko.Common.Length.Px(0));
        innerStyle.BorderBottomWidth.ShouldBe(Miko.Common.Length.Px(1));
    }

    [Fact]
    public void IonItem_FullLines_DrawsHairlineAcrossTheNativeSurface()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Lines), "full");
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        // lines="full": --border-width on the native surface, --inner-border-width zeroed
        // (item.md.scss / item.ios.scss :host(.item-lines-full)).
        var nativeStyle = cut.GetComputedStyle(cut.FindByClass("item-native").Single());
        var innerStyle = cut.GetComputedStyle(cut.FindByClass("item-inner").Single());
        nativeStyle.ShouldNotBeNull();
        innerStyle.ShouldNotBeNull();
        nativeStyle.BorderBottomWidth.ShouldBe(Miko.Common.Length.Px(1));
        innerStyle.BorderBottomWidth.ShouldBe(Miko.Common.Length.Px(0));
    }

    [Fact]
    public void IonItem_NoneLines_RemovesTheDivider()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Lines), "none");
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var nativeStyle = cut.GetComputedStyle(cut.FindByClass("item-native").Single());
        var innerStyle = cut.GetComputedStyle(cut.FindByClass("item-inner").Single());
        nativeStyle.ShouldNotBeNull();
        innerStyle.ShouldNotBeNull();
        nativeStyle.BorderBottomWidth.ShouldBe(Miko.Common.Length.Px(0));
        innerStyle.BorderBottomWidth.ShouldBe(Miko.Common.Length.Px(0));
    }

    [Fact]
    public void IonItem_DefaultDivider_IsInsetLike_InLayout()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        // The default divider behaves like lines="inset": it starts after the 16px leading
        // padding and still reaches the item's right edge (the issue-#3 expectation).
        var nativeBox = cut.GetBoxModel(cut.FindByClass("item-native").Single());
        var innerBox = cut.GetBoxModel(cut.FindByClass("item-inner").Single());
        nativeBox.ShouldNotBeNull();
        innerBox.ShouldNotBeNull();
        innerBox.BorderBox.X.ShouldBe(nativeBox.BorderBox.X + 16f, 0.5f);
        innerBox.BorderBox.Right.ShouldBe(nativeBox.BorderBox.Right, 0.5f);
    }

    [Fact]
    public void IonItem_NativeSurface_HasStartPaddingOnly_NoEndPadding()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        // item.md.scss :host: --padding-start 16px, --padding-end stays 0 — the trailing inset
        // lives on .item-inner (--inner-padding-end), not on the native surface.
        var native = cut.FindByClass("item-native").Single();
        var style = cut.GetComputedStyle(native);
        style.ShouldNotBeNull();
        style.PaddingLeft.ShouldBe(Miko.Common.Length.Px(16));
        style.PaddingRight.ShouldBe(Miko.Common.Length.Px(0));
    }

    [Fact]
    public void IonItem_ItemInner_CarriesTheEndPadding()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        var inner = cut.FindByClass("item-inner").Single();
        var style = cut.GetComputedStyle(inner);
        style.ShouldNotBeNull();
        style.PaddingRight.ShouldBe(Miko.Common.Length.Px(16)); // --inner-padding-end
        style.PaddingLeft.ShouldBe(Miko.Common.Length.Px(0));
    }

    [Fact]
    public void IonItem_InsetDivider_ReachesTheItemRightEdge_InLayout()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Lines), "inset");
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        // With padding-end 0 on item-native, the inset divider on .item-inner spans from the
        // 16px start inset all the way to the item's right edge (Ionic's lines="inset" look).
        var nativeBox = cut.GetBoxModel(cut.FindByClass("item-native").Single());
        var innerBox = cut.GetBoxModel(cut.FindByClass("item-inner").Single());
        nativeBox.ShouldNotBeNull();
        innerBox.ShouldNotBeNull();
        innerBox.BorderBox.Right.ShouldBe(nativeBox.BorderBox.Right, 0.5f);
        innerBox.BorderBox.X.ShouldBe(nativeBox.BorderBox.X + 16f, 0.5f);
    }

    [Fact]
    public void IonItem_DefaultSlotLabel_GrowsToFillFreeSpace()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        // item.scss ::slotted(ion-label:not([slot="end"])): flex: 1; max-width: 100%.
        var label = cut.FindByClass("ion-label").Single();
        var style = cut.GetComputedStyle(label);
        style.ShouldNotBeNull();
        style.FlexGrow.ShouldBe(1f);
        style.MaxWidth.ShouldBe(Miko.Common.Length.Percent(100));
    }

    [Fact]
    public void IonItem_EndSlotLabel_KeepsNaturalWidth()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.End), Label);
            parameters.Add(nameof(IonItem.ChildContent), (RenderFragment)(b => b.AddContent(0, "Item")));
        });

        // The flex rule mirrors :not([slot="end"]) — a label in the end slot is excluded.
        var endLabel = cut.FindByClass("ion-slot-end").Single().FindByClass("ion-label").Single();
        var style = cut.GetComputedStyle(endLabel);
        style.ShouldNotBeNull();
        style.FlexGrow.ShouldBe(0f);
    }

    [Fact]
    public void IonItem_DefaultSlotLabel_HasVerticalMargins()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        // item.md.scss ::slotted(ion-label): $item-md-label-margin-* → 10px 0 10px 0.
        var label = cut.FindByClass("ion-label").Single();
        var style = cut.GetComputedStyle(label);
        style.ShouldNotBeNull();
        style.MarginTop.ShouldBe(Miko.Common.Length.Px(10));
        style.MarginBottom.ShouldBe(Miko.Common.Length.Px(10));
        style.MarginRight.ShouldBe(Miko.Common.Length.Px(0));
    }

    [Fact]
    public void IonItem_DefaultSlotLabel_HasEndMargin_OnIos()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        // item.ios.scss ::slotted(ion-label): margin 10px 8px 10px 0.
        var label = cut.FindByClass("ion-label").Single();
        var style = cut.GetComputedStyle(label);
        style.ShouldNotBeNull();
        style.MarginTop.ShouldBe(Miko.Common.Length.Px(10));
        style.MarginBottom.ShouldBe(Miko.Common.Length.Px(10));
        style.MarginRight.ShouldBe(Miko.Common.Length.Px(8));
    }

    [Fact]
    public void IonItem_StartAndEndSlotLabels_AlsoGetSlottedMargins()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Start), Label);
            parameters.Add(nameof(IonItem.End), Label);
            parameters.Add(nameof(IonItem.ChildContent), (RenderFragment)(b => b.AddContent(0, "Item")));
        });

        // ::slotted(ion-label) is not slot-scoped — labels in the named slots get the same
        // vertical margins (only the flex-grow rule excludes slot="end").
        foreach (var slot in new[] { "ion-slot-start", "ion-slot-end" })
        {
            var label = cut.FindByClass(slot).Single().FindByClass("ion-label").Single();
            var style = cut.GetComputedStyle(label);
            style.ShouldNotBeNull();
            style.MarginTop.ShouldBe(Miko.Common.Length.Px(10));
            style.MarginBottom.ShouldBe(Miko.Common.Length.Px(10));
        }
    }

    [Fact]
    public void IonItem_Label_FillsInputWrapper_InLayout()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        // BoxModel assertion: with flex:1 the label box stretches to the wrapper's full content
        // width (it would otherwise shrink-wrap the "Basic Item" text).
        var wrapperBox = cut.GetBoxModel(cut.FindByClass("input-wrapper").Single());
        var labelBox = cut.GetBoxModel(cut.FindByClass("ion-label").Single());
        wrapperBox.ShouldNotBeNull();
        labelBox.ShouldNotBeNull();
        labelBox.Content.Width.ShouldBe(wrapperBox.Content.Width, 0.5f);
    }

    [Fact]
    public void IonItem_InvokesOnClick_WhenClickableTapped()
    {
        var clicked = false;
        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Button), true);
            parameters.Add(nameof(IonItem.OnClick),
                EventCallback.Factory.Create(this, () => clicked = true));
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var native = cut.FindByClass("item-native").Single();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        clicked.ShouldBeTrue();
    }

    [Fact]
    public void IonItem_DoesNotInvokeOnClick_WhenDisabled()
    {
        var clicked = false;
        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Button), true);
            parameters.Add(nameof(IonItem.Disabled), true);
            parameters.Add(nameof(IonItem.OnClick),
                EventCallback.Factory.Create(this, () => clicked = true));
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var native = cut.FindByClass("item-native").Single();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        clicked.ShouldBeFalse();
    }

    // --- native tag resets (item.scss text-inherit) -------------------------

    [Fact]
    public void IonItem_ButtonNative_InheritsTextAlign_NotUaCentered()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Button), true);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        // item.scss .item-native { @include text-inherit(); }: without the reset, the UA
        // `button { text-align: center }` leaks through and centers the item's content.
        var native = cut.FindByClass("item-native").Single();
        native.TagName.ShouldBe("button");
        var style = cut.GetComputedStyle(native);
        style.ShouldNotBeNull();
        style.TextAlign.ShouldBe(Miko.Common.TextAlign.Left);
    }

    [Fact]
    public void IonItem_ButtonNativeContent_InheritsLeftTextAlign()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Button), true);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        // text-align inherits down the row, so the label text is not centered either.
        var label = cut.FindByClass("ion-label").Single();
        var style = cut.GetComputedStyle(label);
        style.ShouldNotBeNull();
        style.TextAlign.ShouldBe(Miko.Common.TextAlign.Left);
    }

    [Fact]
    public void IonItem_ButtonNative_HasNoUaVerticalPadding()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Button), true);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        // The UA `button { padding: 2px 6px }` must not leak: Ionic's --padding-top/bottom
        // default to 0 (item.scss :host), so the native surface has no vertical padding.
        var native = cut.FindByClass("item-native").Single();
        var style = cut.GetComputedStyle(native);
        style.ShouldNotBeNull();
        style.PaddingTop.ShouldBe(Miko.Common.Length.Px(0));
        style.PaddingBottom.ShouldBe(Miko.Common.Length.Px(0));
    }

    [Fact]
    public void IonItem_AnchorNative_InheritsTextDecoration_NotUaUnderlined()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Href), "/details");
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        // The UA `a { text-decoration: underline }` must not leak either (same text-inherit).
        var native = cut.FindByClass("item-native").Single();
        native.TagName.ShouldBe("a");
        var style = cut.GetComputedStyle(native);
        style.ShouldNotBeNull();
        style.TextDecoration.ShouldBe(Miko.Common.TextDecoration.None);
    }

    // --- cursor (item.scss `button, a { cursor: pointer }`) ------------------

    [Fact]
    public void IonItem_DivNative_KeepsDefaultCursor()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        // A plain (non-clickable) item shows the normal arrow, not the pointer hand.
        var native = cut.FindByClass("item-native").Single();
        native.TagName.ShouldBe("div");
        var style = cut.GetComputedStyle(native);
        style.ShouldNotBeNull();
        style.Cursor.ShouldBe(Miko.Common.Cursor.Default);
    }

    [Fact]
    public void IonItem_ButtonNative_HasPointerCursor()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Button), true);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var native = cut.FindByClass("item-native").Single();
        native.TagName.ShouldBe("button");
        var style = cut.GetComputedStyle(native);
        style.ShouldNotBeNull();
        style.Cursor.ShouldBe(Miko.Common.Cursor.Pointer);
    }

    [Fact]
    public void IonItem_AnchorNative_HasPointerCursor()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Href), "/details");
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var native = cut.FindByClass("item-native").Single();
        native.TagName.ShouldBe("a");
        var style = cut.GetComputedStyle(native);
        style.ShouldNotBeNull();
        style.Cursor.ShouldBe(Miko.Common.Cursor.Pointer);
    }

    // --- hover (item.scss :host(.ion-activatable:hover)) ---------------------
    // Ionic paints hover as a 4% currentColor overlay on .item-native::after, only for
    // activatable (clickable) items. Miko exposes it as a plain :hover rule on the native's
    // background; the state propagates up the hit chain, so the host flags :hover too.

    [Fact]
    public void IonItem_ButtonNative_GetsHoverWash_WhenHovered()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Button), true);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var native = cut.FindByClass("item-native").Single();
        cut.GetComputedStyle(native)!.BackgroundColor.ShouldBe(Miko.Common.Color.Transparent);

        var hovered = Hover(cut.Root);
        var after = hovered.GetComputedStyle(hovered.FindByClass("item-native").Single())!.BackgroundColor;
        after.A.ShouldBe((byte)10); // currentColor @ 0.04
    }

    [Fact]
    public void IonItem_AnchorNative_GetsHoverWash_WhenHovered()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Href), "/details");
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var hovered = Hover(cut.Root);
        hovered.GetComputedStyle(hovered.FindByClass("item-native").Single())!.BackgroundColor.A
            .ShouldBe((byte)10);
    }

    [Fact]
    public void IonItem_PlainDivNative_HasNoHoverWash_WhenHovered()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        // A non-clickable item is not ion-activatable — hovering leaves the native transparent.
        var hovered = Hover(cut.Root);
        hovered.GetComputedStyle(hovered.FindByClass("item-native").Single())!.BackgroundColor
            .ShouldBe(Miko.Common.Color.Transparent);
    }

    /// <summary>Re-runs style resolution with <see cref="Miko.Core.ElementState.Hover"/> set.</summary>
    private ComponentUnderTest Hover(Miko.Core.Element root)
    {
        root.SetState(Miko.Core.ElementState.Hover);
        return Context.RenderElement(root);
    }

    // --- slotted buttons (button.tsx: in-item size defaults to small) --------

    [Fact]
    public void IonItem_SlottedButton_DefaultsToSmall_WhenSizeUnset()
    {
        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), ButtonChild()));

        // button.tsx finalSize: size === undefined && inItem → 'small'.
        cut.FindByClass("ion-button").Single().ShouldHaveClass("button-small");
    }

    [Fact]
    public void IonItem_SlottedButton_KeepsExplicitSize()
    {
        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), ButtonChild(size: "large")));

        var button = cut.FindByClass("ion-button").Single();
        button.ShouldHaveClass("button-large");
        button.ShouldNotHaveClass("button-small");
    }

    [Fact]
    public void IonItem_SlottedButton_ExplicitDefaultSize_CountsAsExplicit()
    {
        // Ionic: "Set the size to `default` inside of an item to make it a standard size button."
        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), ButtonChild(size: "default")));

        var button = cut.FindByClass("ion-button").Single();
        button.ShouldHaveClass("button-default");
        button.ShouldNotHaveClass("button-small");
    }

    [Fact]
    public void IonItem_SlottedButton_SmallDefault_PicksUpSmallStyles()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), ButtonChild()));

        // The stamped button-small class picks up the size rules (md host: 13px font-size).
        var button = cut.FindByClass("ion-button").Single();
        var style = cut.GetComputedStyle(button);
        style.ShouldNotBeNull();
        style.FontSize.Value.ShouldBe(13f);
    }

    // --- slotted icons (item.md.scss ::slotted(ion-icon[slot…])) --------------

    [Fact]
    public void IonItem_IconInsideSlottedButton_KeepsButtonIconStyles()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), ButtonChild(start: Icon)));

        // The item's ::slotted(ion-icon) sizing/margins must not reach into the button — the
        // icon keeps the button's own em-based gap, not the item's 16px px margin / 24px box.
        var icon = cut.FindByClass("ion-button").Single().FindByClass("ion-icon").Single();
        var style = cut.GetComputedStyle(icon);
        style.ShouldNotBeNull();
        style.MarginRight.ShouldBe(Miko.Common.Length.Em(0.3f));
        style.Width.ShouldNotBe(Miko.Common.Length.Px(24));
    }

    [Fact]
    public void IonItem_IconInsideButtonInItemStartSlot_KeepsButtonIconStyles()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        // The button itself sits in the item's start slot — the shared ion-slot-start marker
        // class must not make the item's icon rule match the button's icons.
        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Start), ButtonChild(start: Icon));
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var icon = cut.FindByClass("ion-button").Single().FindByClass("ion-icon").Single();
        var style = cut.GetComputedStyle(icon);
        style.ShouldNotBeNull();
        style.MarginRight.ShouldBe(Miko.Common.Length.Em(0.3f));
        style.Width.ShouldNotBe(Miko.Common.Length.Px(24));
    }

    [Fact]
    public void IonItem_StartSlotIcon_HasSlotMargins_OnMd()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Start), Icon);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        // item.md.scss ::slotted(ion-icon[slot]) + [slot="start"]: 12px vertical, 32px end gap.
        var icon = cut.FindByClass("ion-slot-start").Single().FindByClass("ion-icon").Single();
        var style = cut.GetComputedStyle(icon);
        style.ShouldNotBeNull();
        style.Width.ShouldBe(Miko.Common.Length.Px(24));
        style.MarginTop.ShouldBe(Miko.Common.Length.Px(12));
        style.MarginBottom.ShouldBe(Miko.Common.Length.Px(12));
        style.MarginRight.ShouldBe(Miko.Common.Length.Px(32));
    }

    [Fact]
    public void IonItem_StartSlotIcon_HasNoHorizontalMargin_OnIos()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Start), Icon);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        // item.ios.scss ::slotted(ion-icon[slot="start"]): 7px vertical, no horizontal margin.
        var icon = cut.FindByClass("ion-slot-start").Single().FindByClass("ion-icon").Single();
        var style = cut.GetComputedStyle(icon);
        style.ShouldNotBeNull();
        style.MarginTop.ShouldBe(Miko.Common.Length.Px(7));
        style.MarginBottom.ShouldBe(Miko.Common.Length.Px(7));
        style.MarginRight.ShouldBe(Miko.Common.Length.Px(0));
    }

    [Fact]
    public void IonItem_EndSlotIcon_HasLeadingGap_OnMd()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.End), Icon);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        // item.md.scss ::slotted(ion-icon[slot="end"]): 16px leading gap, 12px vertical.
        var icon = cut.FindByClass("ion-slot-end").Single().FindByClass("ion-icon").Single();
        var style = cut.GetComputedStyle(icon);
        style.ShouldNotBeNull();
        style.MarginLeft.ShouldBe(Miko.Common.Length.Px(16));
        style.MarginRight.ShouldBe(Miko.Common.Length.Px(0));
        style.MarginTop.ShouldBe(Miko.Common.Length.Px(12));
    }

    [Fact]
    public void IonItem_DefaultSlotIcon_HasBoxOnly_NoMargin()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Icon));

        // Ionic styles unslotted icons with font-size only — the 24px box, no margins.
        var icon = cut.FindByClass("input-wrapper").Single().FindByClass("ion-icon").Single();
        var style = cut.GetComputedStyle(icon);
        style.ShouldNotBeNull();
        style.Width.ShouldBe(Miko.Common.Length.Px(24));
        style.Height.ShouldBe(Miko.Common.Length.Px(24));
        style.MarginTop.ShouldBe(Miko.Common.Length.Px(0));
        style.MarginRight.ShouldBe(Miko.Common.Length.Px(0));
    }

    [Fact]
    public void IonItem_StartSlotAvatar_KeepsLabelGap()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Start), (RenderFragment)(builder =>
            {
                builder.OpenComponent<IonAvatar>(0);
                builder.CloseComponent();
            }));
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        // item.md.scss ::slotted(ion-avatar[slot="start"]): 16px gap before the label,
        // 8px vertical ($item-md-media-slot-margin-*).
        var avatar = cut.FindByClass("ion-avatar").Single();
        var style = cut.GetComputedStyle(avatar);
        style.ShouldNotBeNull();
        style.MarginRight.ShouldBe(Miko.Common.Length.Px(16));
        style.MarginTop.ShouldBe(Miko.Common.Length.Px(8));
    }

    // --- default href navigation (issues/ion-animation: routerLink) -----------

    // Registers a NavigationManager and wires a capture of its LocationChanged args.
    private (Miko.Routing.NavigationManager Nav, Func<Miko.Routing.NavigationEventArgs?> LastArgs) UseNavigation()
    {
        var nav = new Miko.Routing.NavigationManager();
        Miko.Routing.NavigationEventArgs? last = null;
        nav.LocationChanged += e => last = e;
        Context.Services.AddSingleton(nav);
        return (nav, () => last);
    }

    [Fact]
    public void IonItem_HrefClick_NavigatesForward_WithModeTransition()
    {
        var (nav, lastArgs) = UseNavigation();

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Href), "/details");
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var native = cut.FindByClass("item-native").Single();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        // Forward push: path stacked, direction Forward, md transition attached (default platform).
        nav.CurrentPath.ShouldBe("/details");
        nav.History.ShouldBe(new[] { "/", "/details" });
        var args = lastArgs()!;
        args.Direction.ShouldBe(Miko.Routing.NavigationDirection.Forward);
        args.Transition.ShouldBeSameAs(IonicPageTransitions.MdPageTransition.Push);
    }

    [Fact]
    public void IonItem_HrefClick_UsesIosTransition_OnIos()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        var (_, lastArgs) = UseNavigation();

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Href), "/details");
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var native = cut.FindByClass("item-native").Single();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        lastArgs()!.Transition.ShouldBeSameAs(IonicPageTransitions.IosPageTransition.Push);
    }

    [Fact]
    public void IonItem_HrefClick_RouterDirectionRoot_ClearsStackWithoutTransition()
    {
        var (nav, lastArgs) = UseNavigation();
        nav.NavigateTo("/other");

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Href), "/tab2");
            parameters.Add(nameof(IonItem.RouterDirection), "root");
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var native = cut.FindByClass("item-native").Single();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        nav.CurrentPath.ShouldBe("/tab2");
        nav.History.ShouldBe(new[] { "/tab2" }); // stack cleared
        var args = lastArgs()!;
        args.Direction.ShouldBe(Miko.Routing.NavigationDirection.Root);
        args.Transition.ShouldBeNull();
    }

    [Fact]
    public void IonItem_HrefClick_RouterDirectionBack_UsesPopTransition()
    {
        var (nav, lastArgs) = UseNavigation();
        nav.NavigateTo("/details");

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Href), "/");
            parameters.Add(nameof(IonItem.RouterDirection), "back");
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var native = cut.FindByClass("item-native").Single();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        nav.CurrentPath.ShouldBe("/");
        var args = lastArgs()!;
        args.Direction.ShouldBe(Miko.Routing.NavigationDirection.Back);
        args.Transition.ShouldBeSameAs(IonicPageTransitions.MdPageTransition.Pop);
    }

    [Fact]
    public void IonItem_ButtonClick_WithoutHref_DoesNotNavigate()
    {
        var (nav, _) = UseNavigation();

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Button), true);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var native = cut.FindByClass("item-native").Single();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        nav.CurrentPath.ShouldBe("/");
        nav.History.ShouldBe(new[] { "/" });
    }

    [Fact]
    public void IonItem_HrefClick_WhenDisabled_DoesNotNavigate()
    {
        var (nav, _) = UseNavigation();

        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Href), "/details");
            parameters.Add(nameof(IonItem.Disabled), true);
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var native = cut.FindByClass("item-native").Single();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        nav.CurrentPath.ShouldBe("/");
    }

    [Fact]
    public void IonItem_HrefClick_WithoutNavigationManager_DoesNotThrow()
    {
        // No NavigationManager registered: default navigation is skipped, only OnClick fires.
        var clicked = false;
        var cut = Context.Render<IonItem>(parameters =>
        {
            parameters.Add(nameof(IonItem.Href), "/details");
            parameters.Add(nameof(IonItem.OnClick),
                EventCallback.Factory.Create(this, () => clicked = true));
            parameters.Add(nameof(IonItem.ChildContent), Label);
        });

        var native = cut.FindByClass("item-native").Single();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        clicked.ShouldBeTrue();
    }
}
