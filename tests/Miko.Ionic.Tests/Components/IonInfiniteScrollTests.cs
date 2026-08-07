using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Styling;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonInfiniteScrollTests : IonicComponentTestBase
{
    /// <summary>
    /// Renders an infinite scroll with a known host height, so the threshold formula (which
    /// subtracts the host's own height, Ionic's <c>infiniteHeight</c>) has real geometry to work
    /// with. Without a height the component early-returns, mirroring infinite-scroll.tsx.
    /// </summary>
    private ComponentUnderTest RenderWithHeight(
        Action<ComponentParameterBuilder<IonInfiniteScroll>>? configure = null,
        float height = HostHeight)
    {
        return Context.Render<IonInfiniteScroll>(p =>
        {
            p.Add(nameof(IonInfiniteScroll.Style), new Style { Height = Length.Px(height) });
            configure?.Invoke(p);
        });
    }

    private const float HostHeight = 50f;

    /// <summary>
    /// Builds a scroll event for a container scrolled to <paramref name="scrollTop"/>. Defaults
    /// describe a 1000px-tall content inside a 500px viewport, so the maximum scrollTop is 500.
    /// </summary>
    private static ScrollEventArgs Scroll(
        Element target,
        float scrollTop,
        float scrollHeight = 1000f,
        float clientHeight = 500f)
        => new()
        {
            Target = target,
            DeltaY = scrollTop,
            ScrollTop = scrollTop,
            ScrollHeight = scrollHeight,
            ClientHeight = clientHeight,
        };

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
        var cut = RenderWithHeight(p =>
        {
            p.Add(nameof(IonInfiniteScroll.Threshold), "100px");
            p.Add(nameof(IonInfiniteScroll.OnInfinite),
                EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, _ => invoked = true));
        });

        // distance = 1000 - 50 - 480 - 100 - 500 = -130 < 0 → fires.
        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 480));

        invoked.ShouldBeTrue();
    }

    [Fact]
    public void IonInfiniteScroll_DoesNotInvoke_WhenAboveThreshold()
    {
        var invoked = false;
        var cut = RenderWithHeight(p =>
        {
            p.Add(nameof(IonInfiniteScroll.Threshold), "100px");
            p.Add(nameof(IonInfiniteScroll.OnInfinite),
                EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, _ => invoked = true));
        });

        // distance = 1000 - 50 - 100 - 100 - 500 = 250 >= 0 → still far from the bottom.
        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 100));

        invoked.ShouldBeFalse();
    }

    [Fact]
    public void IonInfiniteScroll_PercentThreshold_ScalesWithClientHeight()
    {
        var invoked = false;
        var cut = RenderWithHeight(p =>
        {
            p.Add(nameof(IonInfiniteScroll.Threshold), "15%");
            p.Add(nameof(IonInfiniteScroll.OnInfinite),
                EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, _ => invoked = true));
        });

        // 15% of a 500px viewport = 75px.
        // distance = 1000 - 50 - 300 - 75 - 500 = 75 >= 0 → no fire.
        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 300));
        invoked.ShouldBeFalse();

        // distance = 1000 - 50 - 400 - 75 - 500 = -25 < 0 → fires.
        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 400));
        invoked.ShouldBeTrue();
    }

    [Fact]
    public void IonInfiniteScroll_DoesNotInvoke_WhenDisabled()
    {
        var invoked = false;
        var cut = RenderWithHeight(p =>
        {
            p.Add(nameof(IonInfiniteScroll.Disabled), true);
            p.Add(nameof(IonInfiniteScroll.OnInfinite),
                EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, _ => invoked = true));
        });

        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 500));

        invoked.ShouldBeFalse();
    }

    [Fact]
    public void IonInfiniteScroll_DoesNotInvoke_WhenHostHasNoHeight()
    {
        // infinite-scroll.tsx: "if there is no height of this element then do nothing".
        var invoked = false;
        var cut = Context.Render<IonInfiniteScroll>(p =>
        {
            p.Add(nameof(IonInfiniteScroll.OnInfinite),
                EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, _ => invoked = true));
        });

        cut.Root.OffsetHeight.ShouldBe(0f);
        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 500));

        invoked.ShouldBeFalse();
    }

    [Fact]
    public void IonInfiniteScroll_FiresOnlyOnce_UntilCompleteIsCalled()
    {
        // didFire: continuing to scroll inside the threshold band must not re-emit.
        var count = 0;
        IonInfiniteScrollCustomEvent? captured = null;
        var cut = RenderWithHeight(p =>
        {
            p.Add(nameof(IonInfiniteScroll.Threshold), "100px");
            p.Add(nameof(IonInfiniteScroll.OnInfinite),
                EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, e =>
                {
                    count++;
                    captured = e;
                }));
        });

        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 480));
        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 490));
        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 500));

        count.ShouldBe(1);

        // Complete() re-arms it for the next crossing.
        captured.ShouldNotBeNull();
        captured!.Complete();

        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 500));
        count.ShouldBe(2);
    }

    [Fact]
    public void IonInfiniteScroll_EventArgs_TargetIsTheEmittingComponent()
    {
        IonInfiniteScrollCustomEvent? captured = null;
        var cut = RenderWithHeight(p =>
        {
            p.Add(nameof(IonInfiniteScroll.Threshold), "100px");
            p.Add(nameof(IonInfiniteScroll.OnInfinite),
                EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, e => captured = e));
        });

        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 480));

        captured.ShouldNotBeNull();
        captured!.Target.ShouldBeOfType<IonInfiniteScroll>();
    }

    [Fact]
    public void IonInfiniteScroll_StampsLoadingClass_WhileHandlerRuns()
    {
        var cut = RenderWithHeight(p =>
        {
            p.Add(nameof(IonInfiniteScroll.Threshold), "100px");
            p.Add(nameof(IonInfiniteScroll.OnInfinite),
                EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, _ => { }));
        });

        cut.Root.ShouldNotHaveClass("infinite-scroll-loading");

        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 480));

        cut.Root.ShouldHaveClass("infinite-scroll-loading");
    }

    [Fact]
    public void IonInfiniteScroll_CompleteClearsLoadingClass()
    {
        IonInfiniteScrollCustomEvent? captured = null;
        var cut = RenderWithHeight(p =>
        {
            p.Add(nameof(IonInfiniteScroll.Threshold), "100px");
            p.Add(nameof(IonInfiniteScroll.OnInfinite),
                EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, e => captured = e));
        });

        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 480));
        cut.Root.ShouldHaveClass("infinite-scroll-loading");

        captured!.Complete();

        cut.Root.ShouldNotHaveClass("infinite-scroll-loading");
    }

    [Fact]
    public void IonInfiniteScroll_TopPosition_FiresNearTheTop()
    {
        var invoked = false;
        var cut = RenderWithHeight(p =>
        {
            p.Add(nameof(IonInfiniteScroll.Position), "top");
            p.Add(nameof(IonInfiniteScroll.Threshold), "100px");
            p.Add(nameof(IonInfiniteScroll.OnInfinite),
                EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, _ => invoked = true));
        });

        // top formula: scrollTop - infiniteHeight - threshold.
        // 400 - 50 - 100 = 250 >= 0 → far from the top, no fire.
        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 400));
        invoked.ShouldBeFalse();

        // 100 - 50 - 100 = -50 < 0 → fires.
        cut.Root.OnScroll!.Invoke(Scroll(cut.Root, 100));
        invoked.ShouldBeTrue();
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
