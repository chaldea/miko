using Miko.Common;
using Miko.Components;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Styling;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-content</c>. Covers the DOM contract (background layer + scroll container +
/// the named <c>fixed</c> slot, and the ios transition layers), the scrollX / scrollY axis gating,
/// fixedSlotPlacement ordering, the color / fullscreen / overscroll class stamping, and the key
/// styles that now live in <see cref="ContentStyles"/>.
/// </summary>
public class IonContentTests : IonicComponentTestBase
{
    private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

    private ComponentUnderTest RenderContent(
        Action<ComponentParameterBuilder<IonContent>>? configure = null, bool withStyles = false)
    {
        if (withStyles) Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        return Context.Render<IonContent>(p =>
        {
            p.Add(nameof(IonContent.ChildContent), Text("body"));
            configure?.Invoke(p);
        });
    }

    // Element children, skipping the whitespace text nodes the Razor compiler emits between tags.
    private static List<Miko.Core.Element> ElementChildren(Miko.Core.Element parent)
        => parent.Children.Where(c => c is not Miko.Core.DomElements.TextNode).ToList();

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonContent_RendersDomContract()
    {
        var cut = RenderContent();

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-content");
        // content.tsx renders the background layer plus the scroll container.
        cut.FindByClass("background-content").ShouldHaveSingleItem();
        cut.FindByClass("inner-scroll").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonContent_BackgroundLayer_PrecedesScrollContainer()
    {
        var cut = RenderContent();

        // #background-content is rendered first so it paints beneath the scrollable content.
        var children = ElementChildren(cut.Root);
        children[0].ShouldHaveClass("background-content");
        children[1].ShouldHaveClass("inner-scroll");
    }

    [Fact]
    public void IonContent_ChildContent_RendersInsideScrollContainer()
    {
        var cut = RenderContent();

        var inner = cut.FindByClass("inner-scroll").ShouldHaveSingleItem();
        inner.TextContent.ShouldBe("body");
    }

    [Fact]
    public void IonContent_NoFixedContent_OmitsFixedSlot()
    {
        var cut = RenderContent();

        cut.FindByClass("ion-slot-fixed").ShouldBeEmpty();
    }

    [Fact]
    public void IonContent_MdMode_OmitsTransitionEffect()
    {
        var cut = RenderContent();

        // content.tsx: transitionShadow = mode === 'ios'.
        cut.FindByClass("transition-effect").ShouldBeEmpty();
    }

    [Fact]
    public void IonContent_IosMode_RendersTransitionEffectLayers()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderContent();

        var effect = cut.FindByClass("transition-effect").ShouldHaveSingleItem();
        effect.FindByClass("transition-cover").ShouldHaveSingleItem();
        effect.FindByClass("transition-shadow").ShouldHaveSingleItem();
    }

    // ---- Fixed slot --------------------------------------------------------

    [Fact]
    public void IonContent_FixedContent_RendersDirectlyWithoutAWrapper()
    {
        var cut = RenderContent(p => p.Add(nameof(IonContent.Fixed), FixedBox()));

        // Ionic's <slot name="fixed"> projects the slotted elements as children of the host, and
        // ::slotted() styles those elements themselves. So the marker must land ON the fixed
        // element — no wrapper element may be introduced, or the wrapper would become the
        // positioned box and demote the real content to a child of an inline box.
        var slot = cut.FindByClass("fixed-box").ShouldHaveSingleItem();
        slot.TagName.ShouldBe("div");
        slot.ShouldHaveClass("ion-slot-fixed");
        cut.FindByTagName("span").ShouldBeEmpty();
    }

    [Fact]
    public void IonContent_FixedContent_KeepsItsOwnBlockLayout()
    {
        var cut = RenderContent(p => p.Add(nameof(IonContent.Fixed), FixedBox()), withStyles: true);

        // The regression the wrapper caused: nested block children must stay block-level boxes,
        // not be squeezed into the wrapper's inline formatting context.
        var box = cut.FindByClass("fixed-box").ShouldHaveSingleItem();
        cut.GetComputedStyle(box)!.Display.ShouldBe(Display.Block);

        var heading = box.FindByTagName("h1").ShouldHaveSingleItem();
        var para = box.FindByTagName("p").ShouldHaveSingleItem();
        cut.GetComputedStyle(heading)!.Display.ShouldBe(Display.Block);
        cut.GetComputedStyle(para)!.Display.ShouldBe(Display.Block);
        // Stacked vertically, as block siblings.
        cut.GetBoxModel(para)!.BorderBox.Y.ShouldBeGreaterThan(cut.GetBoxModel(heading)!.BorderBox.Y);
    }

    [Fact]
    public void IonContent_MultipleFixedElements_AreEachStamped()
    {
        var cut = RenderContent(p => p.Add(nameof(IonContent.Fixed), (RenderFragment)(b =>
        {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "class", "first");
            b.CloseElement();
            b.OpenElement(2, "div");
            b.AddAttribute(3, "class", "second");
            b.CloseElement();
        })));

        // A multi-root fragment arrives wrapped in a transparent FragmentElement; the marker has to
        // reach the real elements inside it.
        cut.FindByClass("first").ShouldHaveSingleItem().ShouldHaveClass("ion-slot-fixed");
        cut.FindByClass("second").ShouldHaveSingleItem().ShouldHaveClass("ion-slot-fixed");
    }

    [Fact]
    public void IonContent_FixedContent_SitsOutsideTheScrollContainer()
    {
        var cut = RenderContent(p => p.Add(nameof(IonContent.Fixed), FixedBox()));

        // The whole point of the fixed slot: it is a SIBLING of .inner-scroll, so it does not
        // scroll away with the content.
        var slot = cut.FindByClass("ion-slot-fixed").ShouldHaveSingleItem();
        slot.Parent.ShouldNotBeNull();
        slot.Parent!.HasClass("ion-content").ShouldBeTrue();
        cut.FindByClass("inner-scroll").ShouldHaveSingleItem()
            .FindByClass("ion-slot-fixed").ShouldBeEmpty();
    }

    [Fact]
    public void IonContent_OwnParts_AreNotStampedAsFixed()
    {
        var cut = RenderContent(p => p.Add(nameof(IonContent.Fixed), FixedBox()));

        // The stamping pass identifies fixed content as "the host's children that IonContent did
        // not author"; its own parts must be left alone.
        cut.FindByClass("background-content").ShouldHaveSingleItem()
            .ShouldNotHaveClass("ion-slot-fixed");
        cut.FindByClass("inner-scroll").ShouldHaveSingleItem()
            .ShouldNotHaveClass("ion-slot-fixed");
    }

    [Fact]
    public void IonContent_FixedSlotPlacementAfter_RendersFixedLast()
    {
        var cut = RenderContent(p => p.Add(nameof(IonContent.Fixed), FixedBox()));

        // Default placement is "after" — the fixed slot follows the scroll container.
        var children = ElementChildren(cut.Root);
        var innerIndex = children.FindIndex(c => c.HasClass("inner-scroll"));
        var fixedIndex = children.FindIndex(c => c.HasClass("ion-slot-fixed"));
        fixedIndex.ShouldBeGreaterThan(innerIndex);
    }

    [Fact]
    public void IonContent_FixedSlotPlacementBefore_RendersFixedFirst()
    {
        var cut = RenderContent(p =>
        {
            p.Add(nameof(IonContent.Fixed), FixedBox());
            p.Add(nameof(IonContent.FixedSlotPlacement), "before");
        });

        var children = ElementChildren(cut.Root);
        var innerIndex = children.FindIndex(c => c.HasClass("inner-scroll"));
        var fixedIndex = children.FindIndex(c => c.HasClass("ion-slot-fixed"));
        fixedIndex.ShouldBeLessThan(innerIndex);
    }

    [Fact]
    public void IonContent_Style_FixedSlot_IsPulledOutOfFlow()
    {
        var cut = RenderContent(p => p.Add(nameof(IonContent.Fixed), FixedBox()), withStyles: true);

        // content.scss ::slotted([slot="fixed"]) — absolutely positioned so it stays pinned.
        var slot = cut.FindByClass("ion-slot-fixed").ShouldHaveSingleItem();
        cut.GetComputedStyle(slot)!.Position.ShouldBe(Position.Absolute);
    }

    [Fact]
    public void IonContent_Style_FixedSlot_TakesNoInsetsOfItsOwn()
    {
        Context.ViewportWidth = 400;
        Context.ViewportHeight = 300;

        // A self-positioning fixed element (IonFab pins bottom/end) must shrink-wrap and sit in its
        // own corner. If the fixed-slot rule also set top/left, the element would get all four
        // insets and stretch across the whole content instead.
        //
        // Rendered inside an IonPage, as in a real app: ion-content's height comes from `flex: 1`
        // against the page column. On its own it is a height:auto block and collapses to its
        // content (the scroll container is absolute and contributes nothing), leaving no bottom
        // edge for the fab to pin against.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = Context.Render<IonPage>(page => page.Add(nameof(IonPage.ChildContent), (RenderFragment)(pb =>
        {
            pb.OpenComponent<IonContent>(0);
            pb.AddComponentParameter(1, nameof(IonContent.ChildContent), Text("body"));
            pb.AddComponentParameter(2, nameof(IonContent.Fixed), (RenderFragment)(b =>
            {
                b.OpenComponent<IonFab>(0);
                b.AddComponentParameter(1, nameof(IonFab.Vertical), "bottom");
                b.AddComponentParameter(2, nameof(IonFab.Horizontal), "end");
                b.CloseComponent();
            }));
            pb.CloseComponent();
        })));

        var hostBox = cut.GetBoxModel(cut.FindByClass("ion-content").ShouldHaveSingleItem()).ShouldNotBeNull();
        var fabBox = cut.GetBoxModel(cut.FindByClass("ion-fab").ShouldHaveSingleItem()).ShouldNotBeNull();
        fabBox.BorderBox.Width.ShouldBeLessThan(hostBox.Content.Width / 2);
        fabBox.BorderBox.X.ShouldBeGreaterThan(hostBox.Content.Width / 2);
        fabBox.BorderBox.Y.ShouldBeGreaterThan(hostBox.Content.Height / 2);
    }

    [Fact]
    public void IonContent_Style_InsetlessFixedContent_StaysAtTheContentOrigin()
    {
        Context.ViewportWidth = 400;
        Context.ViewportHeight = 300;

        // Ionic's own test page slots a bare <div slot="fixed"> with no insets. In the browser it
        // keeps its static position — the top of the content area — because .inner-scroll is
        // absolute and so never advances the flow cursor.
        var cut = RenderContent(p => p.Add(nameof(IonContent.Fixed), FixedBox()), withStyles: true);

        var hostBox = cut.GetBoxModel(cut.Root).ShouldNotBeNull();
        var fixedBox = cut.GetBoxModel(cut.FindByClass("fixed-box").ShouldHaveSingleItem())
            .ShouldNotBeNull();
        fixedBox.BorderBox.X.ShouldBe(hostBox.Content.X, 0.5f);
        fixedBox.BorderBox.Y.ShouldBe(hostBox.Content.Y, 0.5f);
    }

    // Mirrors Ionic's content/test/fixed/index.html: a bare div holding nested block content.
    private static RenderFragment FixedBox() => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "fixed-box");
        builder.OpenElement(2, "h1");
        builder.AddContent(3, "Fixed content");
        builder.CloseElement();
        builder.OpenElement(4, "p");
        builder.AddContent(5, "Fixed paragraph");
        builder.CloseElement();
        builder.CloseElement();
    };

    // ---- Scroll axes (scrollX / scrollY) -----------------------------------

    [Fact]
    public void IonContent_DefaultScrollAxes_StampScrollYOnly()
    {
        var cut = RenderContent();

        // Ionic defaults: scrollY = true, scrollX = false.
        var inner = cut.FindByClass("inner-scroll").ShouldHaveSingleItem();
        inner.ShouldHaveClass("scroll-y");
        inner.ShouldNotHaveClass("scroll-x");
    }

    [Theory]
    [InlineData(false, true, Overflow.Hidden, Overflow.Auto)]
    [InlineData(true, false, Overflow.Auto, Overflow.Hidden)]
    [InlineData(true, true, Overflow.Auto, Overflow.Auto)]
    [InlineData(false, false, Overflow.Hidden, Overflow.Hidden)]
    public void IonContent_Style_ScrollAxes_GateEachOverflowIndependently(
        bool scrollX, bool scrollY, Overflow expectedX, Overflow expectedY)
    {
        var cut = RenderContent(p =>
        {
            p.Add(nameof(IonContent.ScrollX), scrollX);
            p.Add(nameof(IonContent.ScrollY), scrollY);
        }, withStyles: true);

        // content.scss: .inner-scroll is overflow:hidden; .scroll-y / .scroll-x opt each axis in.
        var style = cut.GetComputedStyle(cut.FindByClass("inner-scroll").ShouldHaveSingleItem())!;
        style.OverflowX.ShouldBe(expectedX);
        style.OverflowY.ShouldBe(expectedY);
    }

    [Fact]
    public void IonContent_ScrollYFalse_DropsScrollYClass()
    {
        var cut = RenderContent(p => p.Add(nameof(IonContent.ScrollY), false));

        cut.FindByClass("inner-scroll").ShouldHaveSingleItem().ShouldNotHaveClass("scroll-y");
    }

    // ---- Overscroll --------------------------------------------------------

    [Fact]
    public void IonContent_MdMode_DoesNotForceOverscroll()
    {
        var cut = RenderContent();

        // shouldForceOverscroll(): undefined → mode === 'ios'.
        cut.Root.ShouldNotHaveClass("overscroll");
    }

    [Fact]
    public void IonContent_IosMode_ForcesOverscrollByDefault()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderContent();

        cut.Root.ShouldHaveClass("overscroll");
        cut.FindByClass("inner-scroll").ShouldHaveSingleItem().ShouldHaveClass("overscroll");
    }

    [Fact]
    public void IonContent_ExplicitForceOverscrollFalse_OverridesIosDefault()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderContent(p => p.Add(nameof(IonContent.ForceOverscroll), false));

        cut.Root.ShouldNotHaveClass("overscroll");
    }

    [Fact]
    public void IonContent_ExplicitForceOverscrollTrue_AppliesOnMd()
    {
        var cut = RenderContent(p => p.Add(nameof(IonContent.ForceOverscroll), true));

        cut.Root.ShouldHaveClass("overscroll");
    }

    // ---- Class stamping ----------------------------------------------------

    [Fact]
    public void IonContent_Fullscreen_StampsMarkerClass()
    {
        var cut = RenderContent(p => p.Add(nameof(IonContent.Fullscreen), true));

        cut.Root.ShouldHaveClass("content-fullscreen");
    }

    [Fact]
    public void IonContent_NotFullscreen_OmitsMarkerClass()
    {
        var cut = RenderContent(p => p.Add(nameof(IonContent.Fullscreen), false));

        cut.Root.ShouldNotHaveClass("content-fullscreen");
    }

    [Fact]
    public void IonContent_Color_StampsColorClasses()
    {
        var cut = RenderContent(p => p.Add(nameof(IonContent.Color), "danger"));

        cut.Root.ShouldHaveClass("ion-color");
        cut.Root.ShouldHaveClass("ion-color-danger");
    }

    [Fact]
    public void IonContent_AlwaysStampsDirectionClass()
    {
        var cut = RenderContent();

        // content.tsx stamps content-ltr / content-rtl; this port is LTR-only for now.
        cut.Root.ShouldHaveClass("content-ltr");
    }

    [Fact]
    public void IonContent_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderContent();

        cut.Root.Class.ShouldStartWith("ios ion-content");
    }

    [Fact]
    public void IonContent_CustomClass_IsPreserved()
    {
        var cut = RenderContent(p => p.Add(nameof(IonContent.Class), "ion-padding"));

        cut.Root.ShouldHaveClass("ion-padding");
        cut.Root.ShouldHaveClass("ion-content");
    }

    // ---- Key styles (relocated to ContentStyles) ---------------------------

    [Fact]
    public void IonContent_Style_HostFillsRemainingHeight()
    {
        var cut = RenderContent(withStyles: true);

        // content.scss :host — display:block, position:relative, flex:1, width:100%.
        var style = cut.GetComputedStyle(cut.Root)!;
        style.Display.ShouldBe(Display.Block);
        style.Position.ShouldBe(Position.Relative);
        style.FlexGrow.ShouldBe(1);
        style.Width.ShouldBe(Length.Percent(100));
    }

    [Fact]
    public void IonContent_Style_BackgroundLayer_CoversTheHost()
    {
        var cut = RenderContent(withStyles: true);

        var style = cut.GetComputedStyle(cut.FindByClass("background-content").ShouldHaveSingleItem())!;
        style.Position.ShouldBe(Position.Absolute);
        style.BackgroundColor.ShouldBe(IonicTheme.CreateMd().ContentBackground);
    }

    [Fact]
    public void IonContent_Style_Color_TintsScrollContainerAndBackground()
    {
        var cut = RenderContent(p => p.Add(nameof(IonContent.Color), "danger"), withStyles: true);

        // content.scss :host(.ion-color) .inner-scroll { background: base; color: contrast }.
        var expected = IonicTheme.CreateMd().Danger;
        var inner = cut.GetComputedStyle(cut.FindByClass("inner-scroll").ShouldHaveSingleItem())!;
        inner.BackgroundColor.ShouldBe(expected);
        inner.Color.ShouldBe(Color.FromHex("ffffff"));
        cut.GetComputedStyle(cut.FindByClass("background-content").ShouldHaveSingleItem())!
            .BackgroundColor.ShouldBe(expected);
    }

    [Fact]
    public void IonContent_Style_ScrollContainerFillsHost_InLayout()
    {
        Context.ViewportWidth = 400;
        Context.ViewportHeight = 300;

        var cut = RenderContent(withStyles: true);

        var hostBox = cut.GetBoxModel(cut.Root).ShouldNotBeNull();
        var innerBox = cut.GetBoxModel(cut.FindByClass("inner-scroll").ShouldHaveSingleItem())
            .ShouldNotBeNull();
        innerBox.BorderBox.Width.ShouldBe(hostBox.Content.Width, 0.5f);
        innerBox.BorderBox.Height.ShouldBe(hostBox.Content.Height, 0.5f);
    }

    // ---- Style location (issue #1) -----------------------------------------

    [Fact]
    public void IonicStyleSheet_CarriesContentRules_AfterTheRelocation()
    {
        // Problem 1 moved the ion-content rules out of PageStyles into ContentStyles (next to the
        // component). Both types are internal, so the observable contract is the shipped sheet:
        // the relocated rules must still be registered by the factory — otherwise the move would
        // silently drop ion-content styling.
        var host = new Miko.Core.DomElements.DivElement { Class = "md ion-content" };
        var inner = new Miko.Core.DomElements.DivElement { Class = "inner-scroll scroll-y" };
        host.AddChild(inner);

        var sheet = IonicStyleSheetFactory.CreateAllModes();

        sheet.Rules.ShouldContain(r => r.Selector.Matches(host));
        // And the descendant rules that only ContentStyles emits.
        sheet.Rules.Any(r => r.Selector.Matches(inner) && r.Style.OverflowY != null).ShouldBeTrue();
    }

    [Fact]
    public void IonContent_StylesApply_WithoutRenderingAnIonPage()
    {
        // A standalone ion-content (no ion-page ancestor) must still be styled. This is the
        // regression the relocation could plausibly cause — a rule accidentally scoped under
        // .ion-page would only apply inside a page.
        var cut = RenderContent(withStyles: true);

        var style = cut.GetComputedStyle(cut.Root)!;
        style.Position.ShouldBe(Position.Relative);
        style.BackgroundColor.ShouldBe(IonicTheme.CreateMd().ContentBackground);
        cut.GetComputedStyle(cut.FindByClass("inner-scroll").ShouldHaveSingleItem())!
            .OverflowY.ShouldBe(Overflow.Auto);
    }
}
