using Miko.Common;
using Miko.Animation;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Styling;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonRefresherTests : IonicComponentTestBase
{
    private static RenderFragment Refresher(EventCallback onRefresh, bool disabled,
        int pullMin, int? pullMax) => builder =>
    {
        builder.OpenComponent<IonRefresher>(0);
        builder.AddComponentParameter(1, nameof(IonRefresher.OnRefresh), onRefresh);
        builder.AddComponentParameter(2, nameof(IonRefresher.Disabled), disabled);
        builder.AddComponentParameter(3, nameof(IonRefresher.PullMin), pullMin);
        if (pullMax is not null)
            builder.AddComponentParameter(4, nameof(IonRefresher.PullMax), pullMax);
        builder.OpenComponent<IonRefresherContent>(5);
        builder.CloseComponent();
        builder.CloseComponent();
    };

    private ComponentUnderTest RenderInContent(EventCallback onRefresh = default,
        bool disabled = false, int pullMin = 60, int? pullMax = null)
        => Context.Render<IonContent>(parameters =>
        {
            parameters.Add(nameof(IonContent.Fixed), Refresher(onRefresh, disabled, pullMin, pullMax));
            parameters.AddChildContent(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "style", new Style { Height = Length.Px(1000) });
                builder.CloseElement();
            });
        });

    [Fact]
    public void IonRefresher_RendersInactiveDom()
    {
        var cut = Context.Render<IonRefresher>();

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-refresher refresher-md");
    }

    [Theory]
    [InlineData("pulling", "refresher-active refresher-pulling")]
    [InlineData("ready", "refresher-active refresher-ready")]
    [InlineData("refreshing", "refresher-active refresher-refreshing")]
    [InlineData("cancelling", "refresher-active refresher-cancelling")]
    [InlineData("completing", "refresher-active refresher-completing")]
    public void IonRefresher_StampsStateClasses(string state, string expectedClasses)
    {
        var cut = Context.Render<IonRefresher>(p => p.Add(nameof(IonRefresher.State), state));

        foreach (var cls in expectedClasses.Split(' '))
            cut.Root.ShouldHaveClass(cls);
    }

    [Fact]
    public void IonRefresher_DisabledStampsClassAndNoActive()
    {
        var cut = Context.Render<IonRefresher>(p =>
        {
            p.Add(nameof(IonRefresher.Disabled), true);
            p.Add(nameof(IonRefresher.State), "refreshing");
        });

        cut.Root.ShouldHaveClass("refresher-disabled");
        cut.Root.ShouldNotHaveClass("refresher-active");
    }

    [Fact]
    public void IonRefresherContent_RendersPullingAndRefreshingBlocks()
    {
        var cut = Context.Render<IonRefresherContent>(p =>
        {
            p.Add(nameof(IonRefresherContent.PullingText), "Pull");
            p.Add(nameof(IonRefresherContent.RefreshingText), "Refresh");
        });

        cut.Root.Class.ShouldBe("md ion-refresher-content");
        cut.FindByClass("refresher-pulling").ShouldHaveSingleItem();
        cut.FindByClass("refresher-refreshing").ShouldHaveSingleItem();
        cut.GetTextContent().ShouldContain("Pull");
        cut.GetTextContent().ShouldContain("Refresh");
    }

    [Fact]
    public void IonRefresherContent_UsesIosClass()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonRefresherContent>();

        cut.Root.Class.ShouldBe("ios ion-refresher-content");
    }

    [Fact]
    public void RefresherStyles_ShowPullingContent_WhenPulling()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonRefresher>(p =>
        {
            p.Add(nameof(IonRefresher.State), "pulling");
            p.AddChildContent(builder =>
            {
                builder.OpenComponent<IonRefresherContent>(0);
                builder.AddComponentParameter(1, nameof(IonRefresherContent.PullingText), "Pull");
                builder.CloseComponent();
            });
        });

        cut.GetComputedStyle(cut.Root)!.Display.ShouldBe(Display.Block);
        var pulling = cut.FindByClass("refresher-pulling")
            .Single(e => e.Class == "refresher-pulling");
        cut.GetComputedStyle(pulling)!.Display.ShouldBe(Display.Block);
    }

    [Fact]
    public void RefresherStyles_ShowRefreshingContent_WhenRefreshing()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonRefresher>(p =>
        {
            p.Add(nameof(IonRefresher.State), "refreshing");
            p.AddChildContent(builder =>
            {
                builder.OpenComponent<IonRefresherContent>(0);
                builder.AddComponentParameter(1, nameof(IonRefresherContent.RefreshingText), "Refresh");
                builder.CloseComponent();
            });
        });

        var refreshing = cut.FindByClass("refresher-refreshing")
            .Single(e => e.Class == "refresher-refreshing");
        cut.GetComputedStyle(refreshing)!.Display.ShouldBe(Display.Block);
    }

    [Fact]
    public void PullingContent_DownFromTop_ShowsRefresherAndMovesScrollContent()
    {
        var cut = RenderInContent();
        var inner = cut.FindByClass("inner-scroll").ShouldHaveSingleItem();

        Drag(inner, 20, 60, release: false);

        cut.FindByClass("ion-refresher").ShouldHaveSingleItem().ShouldHaveClass("refresher-pulling");
        TranslateY(inner).ShouldBe(40f);
    }

    [Fact]
    public void ReleasingBeforePullMin_CancelsPullAndRestoresContent()
    {
        var cut = RenderInContent();
        var inner = cut.FindByClass("inner-scroll").ShouldHaveSingleItem();

        Drag(inner, 20, 55);

        cut.FindByClass("ion-refresher").ShouldHaveSingleItem().ShouldNotHaveClass("refresher-active");
        TranslateY(inner).ShouldBe(0f);
    }

    [Fact]
    public void ReleasingAtPullMin_BeginsRefreshOnceAndSnapsContentToThreshold()
    {
        var refreshes = 0;
        var cut = RenderInContent(EventCallback.Factory.Create(this, () => refreshes++));
        var inner = cut.FindByClass("inner-scroll").ShouldHaveSingleItem();

        Drag(inner, 20, 90);

        refreshes.ShouldBe(1);
        cut.FindByClass("ion-refresher").ShouldHaveSingleItem().ShouldHaveClass("refresher-refreshing");
        TranslateY(inner).ShouldBe(60f);
    }

    [Fact]
    public void PullingPastPullMax_BeginsRefreshBeforeRelease()
    {
        var refreshes = 0;
        var cut = RenderInContent(EventCallback.Factory.Create(this, () => refreshes++), pullMin: 50, pullMax: 80);
        var inner = cut.FindByClass("inner-scroll").ShouldHaveSingleItem();

        Drag(inner, 20, 110, release: false);

        refreshes.ShouldBe(1);
        cut.FindByClass("ion-refresher").ShouldHaveSingleItem().ShouldHaveClass("refresher-refreshing");
        TranslateY(inner).ShouldBe(50f);
    }

    [Fact]
    public void PullGesture_DoesNotStartWhenContentIsScrolledOrDisabled()
    {
        var cut = RenderInContent();
        var inner = cut.FindByClass("inner-scroll").ShouldHaveSingleItem();
        cut.FindLayoutBox(inner)!.ScrollTop = 20;
        Drag(inner, 20, 100, release: false);
        cut.FindByClass("ion-refresher").ShouldHaveSingleItem().ShouldNotHaveClass("refresher-active");

        var disabled = RenderInContent(disabled: true);
        var disabledInner = disabled.FindByClass("inner-scroll").ShouldHaveSingleItem();
        Drag(disabledInner, 20, 100, release: false);
        disabled.FindByClass("ion-refresher").ShouldHaveSingleItem().ShouldNotHaveClass("refresher-active");
    }

    [Fact]
    public void HorizontalDragAndPointerCancel_DoNotLeaveRefresherActive()
    {
        var cut = RenderInContent();
        var inner = cut.FindByClass("inner-scroll").ShouldHaveSingleItem();
        Drag(inner, 20, 30, toX: 100, release: false);
        cut.FindByClass("ion-refresher").ShouldHaveSingleItem().ShouldNotHaveClass("refresher-active");

        var cancelled = RenderInContent();
        var cancelledInner = cancelled.FindByClass("inner-scroll").ShouldHaveSingleItem();
        Drag(cancelledInner, 20, 70, release: false);
        new EventDispatcher().Dispatch(cancelledInner, EventTypes.PointerCancel,
            PointerArgs(cancelledInner, 10, 70, pressed: false));
        cancelled.FindByClass("ion-refresher").ShouldHaveSingleItem().ShouldNotHaveClass("refresher-active");
        TranslateY(cancelledInner).ShouldBe(0f);
    }

    private static void Drag(Element target, float fromY, float toY, float toX = 10, bool release = true)
    {
        var dispatcher = new EventDispatcher();
        dispatcher.Dispatch(target, EventTypes.PointerDown, PointerArgs(target, 10, fromY, pressed: true));
        dispatcher.Dispatch(target, EventTypes.PointerMove, PointerArgs(target, toX, toY, pressed: true));
        if (release)
            dispatcher.Dispatch(target, EventTypes.PointerUp, PointerArgs(target, toX, toY, pressed: false));
    }

    private static PointerEventArgs PointerArgs(Element target, float x, float y, bool pressed) => new()
    {
        Target = target,
        X = x,
        Y = y,
        Button = MouseButton.Left,
        IsButtonPressed = pressed,
        PointerType = PointerType.Touch,
    };

    private static float TranslateY(Element element)
        => element.Style?.Transform?.Value.Functions
            .OfType<TransformFunction.TranslateY>().LastOrDefault()?.Y.Value ?? 0f;
}
