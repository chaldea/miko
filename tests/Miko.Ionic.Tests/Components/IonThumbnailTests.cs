using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonThumbnailTests : IonicComponentTestBase
{
    private static readonly RenderFragment ImgChild = builder =>
    {
        builder.OpenElement(0, "img");
        builder.AddAttribute(1, "src", "https://example.com/photo.jpg");
        builder.CloseElement();
    };

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonThumbnail_RendersDefaultDom()
    {
        var cut = Context.Render<IonThumbnail>(p => p.Add(nameof(IonThumbnail.ChildContent), ImgChild));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-thumbnail");
    }

    [Fact]
    public void IonThumbnail_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonThumbnail>(p => p.Add(nameof(IonThumbnail.ChildContent), ImgChild));

        cut.Root.Class.ShouldBe("ios ion-thumbnail");
    }

    [Fact]
    public void IonThumbnail_WrapsSlottedImageVerbatim()
    {
        var cut = Context.Render<IonThumbnail>(p => p.Add(nameof(IonThumbnail.ChildContent), ImgChild));

        cut.Root.Children.Count.ShouldBe(1);
        var img = cut.Root.Children[0];
        img.TagName.ShouldBe("img");
        img.ShouldBeOfType<Miko.Core.DomElements.ImageElement>()
            .Source.Value.ShouldBe("https://example.com/photo.jpg");
    }

    // ---- Key styles --------------------------------------------------------

    [Fact]
    public void IonThumbnail_HasIntrinsicSquareSize()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonThumbnail>(p => p.Add(nameof(IonThumbnail.ChildContent), ImgChild));
        var style = cut.GetComputedStyle(cut.Root)!;

        style.Display.ShouldBe(Display.Block);
        style.Width.ShouldBe(Length.Px(48));
        style.Height.ShouldBe(Length.Px(48));
    }

    [Fact]
    public void IonThumbnail_SharedSizeAcrossModes()
    {
        // Thumbnail has no per-mode difference — 48px square in both md and ios.
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonThumbnail>(p => p.Add(nameof(IonThumbnail.ChildContent), ImgChild));

        cut.GetComputedStyle(cut.Root)!.Width.ShouldBe(Length.Px(48));
    }

    [Fact]
    public void IonThumbnail_ImgChild_FillsHost()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonThumbnail>(p => p.Add(nameof(IonThumbnail.ChildContent), ImgChild));
        var img = cut.Root.Children[0];
        var style = cut.GetComputedStyle(img)!;

        style.Width.ShouldBe(Length.Percent(100));
        style.Height.ShouldBe(Length.Percent(100));
        style.OverflowX.ShouldBe(Overflow.Hidden);
        style.OverflowY.ShouldBe(Overflow.Hidden);
    }

    [Fact]
    public void IonThumbnail_LaysOutAtIntrinsicSize()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonThumbnail>(p => p.Add(nameof(IonThumbnail.ChildContent), ImgChild));
        var box = cut.GetBoxModel(cut.Root)!;

        box.Content.Width.ShouldBe(48f);
        box.Content.Height.ShouldBe(48f);
    }

    // ---- Slotted inside ion-item -------------------------------------------
    // item.md.scss / item.ios.scss override --size to 56px and add the media-slot margins:
    // 8px vertical (md) plus a 16px gap on the label-facing edge.

    /// <summary>Renders an ion-item with a thumbnail in the given slot and a label as the body.</summary>
    private Element RenderThumbnailInItem(bool inStartSlot)
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        RenderFragment thumbnail = builder =>
        {
            builder.OpenComponent<IonThumbnail>(0);
            builder.AddAttribute(1, nameof(IonThumbnail.ChildContent), ImgChild);
            builder.CloseComponent();
        };

        var cut = Context.Render<IonItem>(p => p
            .Add(inStartSlot ? nameof(IonItem.Start) : nameof(IonItem.End), thumbnail)
            .Add(nameof(IonItem.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenComponent<IonLabel>(0);
                builder.AddAttribute(1, nameof(IonLabel.ChildContent), (RenderFragment)(b => b.AddContent(0, "Item")));
                builder.CloseComponent();
            })));

        _lastCut = cut;
        return cut.FindByClass("ion-thumbnail").Single();
    }

    private Miko.Testing.ComponentUnderTest _lastCut = null!;

    [Fact]
    public void IonThumbnail_InsideItem_IsLarger_Md()
    {
        // The item overrides --size upward: 56px, not the standalone 48px
        // (item.md.vars.scss $item-md-thumbnail-size).
        var thumbnail = RenderThumbnailInItem(inStartSlot: true);
        var style = _lastCut.GetComputedStyle(thumbnail)!;

        style.Width.ShouldBe(Length.Px(56));
        style.Height.ShouldBe(Length.Px(56));
    }

    [Fact]
    public void IonThumbnail_InsideItem_IsLarger_Ios()
    {
        // ios uses the same 56px ($item-ios-thumbnail-size), unlike the avatar which differs per mode.
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var thumbnail = RenderThumbnailInItem(inStartSlot: true);
        var style = _lastCut.GetComputedStyle(thumbnail)!;

        style.Width.ShouldBe(Length.Px(56));
        style.Height.ShouldBe(Length.Px(56));
    }

    [Fact]
    public void IonThumbnail_InStartSlot_HasVerticalAndTrailingMargins_Md()
    {
        // ::slotted(ion-thumbnail) → 8px vertical; [slot="start"] → margin-inline-end: 16px.
        var thumbnail = RenderThumbnailInItem(inStartSlot: true);
        var style = _lastCut.GetComputedStyle(thumbnail)!;

        style.MarginTop.ShouldBe(Length.Px(8));
        style.MarginBottom.ShouldBe(Length.Px(8));
        style.MarginRight.ShouldBe(Length.Px(16));
    }

    [Fact]
    public void IonThumbnail_InEndSlot_HasVerticalAndLeadingMargins_Md()
    {
        // ::slotted(ion-thumbnail) → 8px vertical; [slot="end"] → margin-inline-start: 16px.
        var thumbnail = RenderThumbnailInItem(inStartSlot: false);
        var style = _lastCut.GetComputedStyle(thumbnail)!;

        style.MarginTop.ShouldBe(Length.Px(8));
        style.MarginBottom.ShouldBe(Length.Px(8));
        style.MarginLeft.ShouldBe(Length.Px(16));
    }

    [Fact]
    public void IonThumbnail_InsideItem_LaysOutAtLargerSizeWithMargins()
    {
        // BoxModel assertion: the slotted thumbnail actually occupies a 56px square and reserves
        // the 16px gap toward the label, rather than staying at its standalone 48px/no-margin box.
        var thumbnail = RenderThumbnailInItem(inStartSlot: true);
        var box = _lastCut.GetBoxModel(thumbnail)!;

        box.Content.Width.ShouldBe(56f);
        box.Content.Height.ShouldBe(56f);
        box.Margin.Right.ShouldBe(16f);
        box.Margin.Top.ShouldBe(8f);
        box.Margin.Bottom.ShouldBe(8f);
    }
}
