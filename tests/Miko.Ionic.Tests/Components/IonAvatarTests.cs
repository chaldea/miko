using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Components;
using Miko.Core;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonAvatarTests : IonicComponentTestBase
{
    private static readonly RenderFragment MinimalChild = builder =>
    {
        builder.OpenElement(0, "span");
        builder.CloseElement();
    };

    [Fact]
    public void IonAvatar_RendersWithCorrectDefaultClass()
    {
        // Act
        var cut = Context.Render<IonAvatar>(parameters =>
            parameters.Add(nameof(IonAvatar.ChildContent), MinimalChild));

        // Assert - DOM structure (the component contract): a div carrying the md mode + ion-avatar class.
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-avatar");
    }

    [Fact]
    public void IonAvatar_UsesIosClass_OnIosPlatform()
    {
        // Arrange
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        // Act
        var cut = Context.Render<IonAvatar>(parameters =>
            parameters.Add(nameof(IonAvatar.ChildContent), MinimalChild));

        // Assert
        cut.Root.Class.ShouldBe("ios ion-avatar");
    }

    [Fact]
    public void IonAvatar_RendersChildContent()
    {
        // Act
        var cut = Context.Render<IonAvatar>(parameters =>
            parameters.Add(nameof(IonAvatar.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "img");
                builder.AddAttribute(1, "src", "https://example.com/avatar.svg");
                builder.CloseElement();
            })));

        // Assert - the avatar wraps its content verbatim (an <img> child, like the Ionic example).
        cut.Root.Children.Count.ShouldBe(1);
        var img = cut.Root.Children[0];
        img.TagName.ShouldBe("img");
        img.ShouldBeOfType<Miko.Core.DomElements.ImageElement>().Source.Value.ShouldBe("https://example.com/avatar.svg");
    }

    [Fact]
    public void IonAvatar_HasCircularBorderRadius_FromStyle()
    {
        // Key style assertion: a full-circle 50% border-radius, shared by both modes.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonAvatar>(parameters =>
            parameters.Add(nameof(IonAvatar.ChildContent), MinimalChild));

        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.BorderTopLeftRadius.ShouldBe(Miko.Common.Length.Percent(50));
        style.BorderTopRightRadius.ShouldBe(Miko.Common.Length.Percent(50));
        style.BorderBottomLeftRadius.ShouldBe(Miko.Common.Length.Percent(50));
        style.BorderBottomRightRadius.ShouldBe(Miko.Common.Length.Percent(50));
    }

    [Fact]
    public void IonAvatar_HasIntrinsicSize_Md()
    {
        // Key style assertion (the bug fix): the host has a non-zero intrinsic size so it is
        // actually visible. md is a 64px square (avatar.md.vars.scss $avatar-md-width).
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonAvatar>(parameters =>
            parameters.Add(nameof(IonAvatar.ChildContent), MinimalChild));

        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.Width.ShouldBe(Miko.Common.Length.Px(64));
        style.Height.ShouldBe(Miko.Common.Length.Px(64));
    }

    [Fact]
    public void IonAvatar_HasIntrinsicSize_Ios()
    {
        // ios is a 48px square (avatar.ios.vars.scss $avatar-ios-width).
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonAvatar>(parameters =>
            parameters.Add(nameof(IonAvatar.ChildContent), MinimalChild));

        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.Width.ShouldBe(Miko.Common.Length.Px(48));
        style.Height.ShouldBe(Miko.Common.Length.Px(48));
    }

    [Fact]
    public void IonAvatar_RendersAtIntrinsicSize_InLayout()
    {
        // BoxModel assertion: the host actually lays out at its intrinsic 64px square (md),
        // i.e. it is no longer collapsed to a 0x0 box.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonAvatar>(parameters =>
            parameters.Add(nameof(IonAvatar.ChildContent), MinimalChild));

        var box = cut.GetBoxModel(cut.Root);
        box.ShouldNotBeNull();
        box.Content.Width.ShouldBe(64f);
        box.Content.Height.ShouldBe(64f);
    }

    [Fact]
    public void IonAvatar_IsBlockLevel_FromStyle()
    {
        // Key style assertion: the host is block-level (Ionic's `:host { display: block }`).
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonAvatar>(parameters =>
            parameters.Add(nameof(IonAvatar.ChildContent), MinimalChild));

        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.Display.ShouldBe(Miko.Common.Display.Block);
    }

    [Fact]
    public void IonAvatar_ImgChild_FillsAndClipsToCircle()
    {
        // Key style assertion: an <img> child fills the avatar (100% width/height), is rounded to
        // the same circle, and clips its own overflow (Ionic's ::slotted(img) rules).
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonAvatar>(parameters =>
            parameters.Add(nameof(IonAvatar.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "img");
                builder.CloseElement();
            })));

        var img = cut.Root.Children[0];
        var style = cut.GetComputedStyle(img);
        style.ShouldNotBeNull();
        style.Width.ShouldBe(Miko.Common.Length.Percent(100));
        style.Height.ShouldBe(Miko.Common.Length.Percent(100));
        style.BorderTopLeftRadius.ShouldBe(Miko.Common.Length.Percent(50));
        style.OverflowX.ShouldBe(Miko.Common.Overflow.Hidden);
        style.OverflowY.ShouldBe(Miko.Common.Overflow.Hidden);
    }

    [Fact]
    public void IonAvatar_ImgChild_FillsHostSquare_SoCircularClipCoversIt()
    {
        // The circular clip (overflow:hidden + 50% radius) only matters if the <img> actually fills
        // the host square — a smaller img would leave the corners empty regardless. This ties the
        // render-layer rounded-clip fix to the avatar: the img lays out at the full 64px host box, so
        // the rounded overflow clip trims its square content into a circle.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonAvatar>(parameters =>
            parameters.Add(nameof(IonAvatar.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "img");
                builder.CloseElement();
            })));

        var img = cut.Root.Children[0];

        // The img fills the host's intrinsic 64px square...
        var imgBox = cut.GetBoxModel(img);
        imgBox.ShouldNotBeNull();
        imgBox.Content.Width.ShouldBe(64f);
        imgBox.Content.Height.ShouldBe(64f);

        // ...and carries the geometry that drives the circular clip: hidden overflow + 50% radius.
        var style = cut.GetComputedStyle(img);
        style.ShouldNotBeNull();
        style.OverflowX.ShouldBe(Miko.Common.Overflow.Hidden);
        style.OverflowY.ShouldBe(Miko.Common.Overflow.Hidden);
        style.BorderTopLeftRadius.ShouldBe(Miko.Common.Length.Percent(50));
        style.BorderBottomRightRadius.ShouldBe(Miko.Common.Length.Percent(50));
    }

    [Fact]
    public void IonAvatar_SharedCircularGeometry_AcrossModes()
    {
        // Both modes render a circular block host (only the intrinsic size differs: md 64 / ios 48).
        foreach (var platform in new[] { Miko.Platform.HostPlatform.Android, Miko.Platform.HostPlatform.Ios })
        {
            UsePlatform(platform);
            Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

            var cut = Context.Render<IonAvatar>(parameters =>
                parameters.Add(nameof(IonAvatar.ChildContent), MinimalChild));

            var style = cut.GetComputedStyle(cut.Root);
            style.ShouldNotBeNull();
            style.Display.ShouldBe(Miko.Common.Display.Block);
            style.BorderTopLeftRadius.ShouldBe(Miko.Common.Length.Percent(50));
        }
    }
}
