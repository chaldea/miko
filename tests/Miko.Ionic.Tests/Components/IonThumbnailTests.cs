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
}
