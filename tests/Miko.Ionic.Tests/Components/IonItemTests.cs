using Miko.Testing;
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
    public void IonItem_NativeSurface_HasHairlineBottomBorder_FromTheme()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonItem>(parameters =>
            parameters.Add(nameof(IonItem.ChildContent), Label));

        var native = cut.FindByClass("item-native").Single();
        var style = cut.GetComputedStyle(native);
        style.ShouldNotBeNull();
        // The hairline bottom divider lives on the native surface.
        style.BorderBottomWidth.ShouldBe(Miko.Common.Length.Px(1));
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
}
