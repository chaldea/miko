using Miko.Common;
using Miko.Components;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonCardTests : IonicComponentTestBase
{
    private static readonly RenderFragment Body = builder => builder.AddContent(0, "Card body");
    private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

    [Fact]
    public void IonCard_RendersPlainCardDom()
    {
        var cut = Context.Render<IonCard>(p => p.Add(nameof(IonCard.ChildContent), Body));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-card");
        cut.GetTextContent().ShouldContain("Card body");
    }

    [Fact]
    public void IonCard_RendersButtonNative_WhenButton()
    {
        var cut = Context.Render<IonCard>(p =>
        {
            p.Add(nameof(IonCard.Button), true);
            p.Add(nameof(IonCard.ChildContent), Body);
        });

        cut.Root.Class.ShouldContain("ion-activatable");
        cut.Root.Children[0].TagName.ShouldBe("button");
        cut.Root.Children[0].Class.ShouldBe("card-native");
    }

    [Fact]
    public void IonCard_RendersAnchorNative_WhenHref()
    {
        var cut = Context.Render<IonCard>(p =>
        {
            p.Add(nameof(IonCard.Href), "/details");
            p.Add(nameof(IonCard.Target), "_blank");
            p.Add(nameof(IonCard.ChildContent), Body);
        });

        var anchor = cut.Root.Children[0].ShouldBeOfType<AnchorElement>();
        anchor.Class.ShouldBe("card-native");
        anchor.Href.ShouldBe("/details");
        anchor.Target.ShouldBe("_blank");
    }

    [Fact]
    public void IonCard_InvokesOnClick_WhenClickable()
    {
        var clicked = false;
        var cut = Context.Render<IonCard>(p =>
        {
            p.Add(nameof(IonCard.Button), true);
            p.Add(nameof(IonCard.OnClick), EventCallback.Factory.Create(this, () => clicked = true));
        });

        cut.Root.Children[0].OnClick!.Invoke(new MouseEventArgs { Target = cut.Root.Children[0] });

        clicked.ShouldBeTrue();
    }

    [Fact]
    public void IonCard_DoesNotInvokeOnClick_WhenDisabled()
    {
        var clicked = false;
        var cut = Context.Render<IonCard>(p =>
        {
            p.Add(nameof(IonCard.Button), true);
            p.Add(nameof(IonCard.Disabled), true);
            p.Add(nameof(IonCard.OnClick), EventCallback.Factory.Create(this, () => clicked = true));
        });

        cut.Root.Children[0].OnClick!.Invoke(new MouseEventArgs { Target = cut.Root.Children[0] });

        clicked.ShouldBeFalse();
        cut.Root.Class.ShouldContain("card-disabled");
    }

    [Fact]
    public void IonCard_DefaultStyle_UsesMdCardSurface()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonCard>();
        var style = cut.GetComputedStyle(cut.Root)!;

        style.MarginTop.ShouldBe(Length.Px(10));
        style.BorderTopLeftRadius.Value.ShouldBe(4f);
        style.BoxShadow.ShouldNotBeNull();
        style.BoxShadow!.Count.ShouldBe(3);
    }

    [Fact]
    public void IonCard_IosStyle_UsesLargerMarginAndRadius()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonCard>();
        var style = cut.GetComputedStyle(cut.Root)!;

        style.MarginTop.ShouldBe(Length.Px(24));
        style.BorderTopLeftRadius.Value.ShouldBe(8f);
    }

    [Fact]
    public void IonCardHeader_RendersAndStyles()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonCardHeader>(p => p.AddChildContent(Text("Header")));

        cut.Root.Class.ShouldBe("md ion-card-header ion-inherit-color");
        cut.GetComputedStyle(cut.Root)!.PaddingTop.ShouldBe(Length.Px(16));
    }

    [Fact]
    public void IonCardHeader_StampsTranslucentAndColor()
    {
        var cut = Context.Render<IonCardHeader>(p =>
        {
            p.Add(nameof(IonCardHeader.Translucent), true);
            p.Add(nameof(IonCardHeader.Color), "primary");
        });

        cut.Root.Class.ShouldContain("card-header-translucent");
        cut.Root.Class.ShouldContain("ion-color-primary");
    }

    [Fact]
    public void IonCardContent_RendersModeSpecificClassAndPadding()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonCardContent>(p => p.AddChildContent(Text("Content")));
        var style = cut.GetComputedStyle(cut.Root)!;

        cut.Root.Class.ShouldBe("md ion-card-content card-content-md");
        style.PaddingTop.ShouldBe(Length.Px(13));
        style.PaddingLeft.ShouldBe(Length.Px(16));
    }
}
