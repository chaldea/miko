using Miko.Common;
using Miko.Components;
using Miko.Events;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonInfiniteScrollTests : IonicComponentTestBase
{
    [Fact]
    public void IonInfiniteScroll_RendersEnabledDom()
    {
        var cut = Context.Render<IonInfiniteScroll>();

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-infinite-scroll infinite-scroll-enabled");
    }

    [Fact]
    public void IonInfiniteScroll_DisabledOmitsEnabledClass()
    {
        var cut = Context.Render<IonInfiniteScroll>(p => p.Add(nameof(IonInfiniteScroll.Disabled), true));

        cut.Root.Class.ShouldBe("md ion-infinite-scroll");
    }

    [Fact]
    public void IonInfiniteScroll_LoadingStampsClass()
    {
        var cut = Context.Render<IonInfiniteScroll>(p => p.Add(nameof(IonInfiniteScroll.Loading), true));

        cut.Root.ShouldHaveClass("infinite-scroll-loading");
    }

    [Fact]
    public void IonInfiniteScroll_TopPositionStampsClass()
    {
        var cut = Context.Render<IonInfiniteScroll>(p => p.Add(nameof(IonInfiniteScroll.Position), "top"));

        cut.Root.ShouldHaveClass("infinite-scroll-top");
    }

    [Fact]
    public void IonInfiniteScroll_InvokesOnInfinite_WhenThresholdCrossed()
    {
        var invoked = false;
        var cut = Context.Render<IonInfiniteScroll>(p =>
        {
            p.Add(nameof(IonInfiniteScroll.Threshold), "10px");
            p.Add(nameof(IonInfiniteScroll.OnInfinite), EventCallback.Factory.Create(this, () => invoked = true));
        });

        cut.Root.OnScroll!.Invoke(new ScrollEventArgs { Target = cut.Root, DeltaY = 12, ScrollTop = 12 });

        invoked.ShouldBeTrue();
    }

    [Fact]
    public void IonInfiniteScroll_DoesNotInvoke_WhenDisabled()
    {
        var invoked = false;
        var cut = Context.Render<IonInfiniteScroll>(p =>
        {
            p.Add(nameof(IonInfiniteScroll.Disabled), true);
            p.Add(nameof(IonInfiniteScroll.OnInfinite), EventCallback.Factory.Create(this, () => invoked = true));
        });

        cut.Root.OnScroll!.Invoke(new ScrollEventArgs { Target = cut.Root, DeltaY = 100, ScrollTop = 100 });

        invoked.ShouldBeFalse();
    }

    [Fact]
    public void IonInfiniteScrollContent_RendersLoadingDom()
    {
        var cut = Context.Render<IonInfiniteScrollContent>(p =>
        {
            p.Add(nameof(IonInfiniteScrollContent.LoadingSpinner), "crescent");
            p.Add(nameof(IonInfiniteScrollContent.LoadingText), "Loading more");
        });

        cut.Root.Class.ShouldBe("md ion-infinite-scroll-content infinite-scroll-content-md");
        cut.FindByClass("infinite-loading").ShouldHaveSingleItem();
        cut.FindByClass("infinite-loading-spinner").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("Loading more");
    }

    [Fact]
    public void InfiniteScrollStyles_LoadingStateShowsLoading()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonInfiniteScroll>(p =>
        {
            p.Add(nameof(IonInfiniteScroll.Loading), true);
            p.AddChildContent(builder =>
            {
                builder.OpenComponent<IonInfiniteScrollContent>(0);
                builder.AddComponentParameter(1, nameof(IonInfiniteScrollContent.LoadingText), "Loading");
                builder.CloseComponent();
            });
        });

        var loading = cut.FindByClass("infinite-loading").Single();
        cut.GetComputedStyle(loading)!.Display.ShouldBe(Display.Block);
    }
}
