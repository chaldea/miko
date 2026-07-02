using Miko.Common;
using Miko.Components;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonRefresherTests : IonicComponentTestBase
{
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
            cut.Root.Class.ShouldContain(cls);
    }

    [Fact]
    public void IonRefresher_DisabledStampsClassAndNoActive()
    {
        var cut = Context.Render<IonRefresher>(p =>
        {
            p.Add(nameof(IonRefresher.Disabled), true);
            p.Add(nameof(IonRefresher.State), "refreshing");
        });

        cut.Root.Class.ShouldContain("refresher-disabled");
        cut.Root.Class.ShouldNotContain("refresher-active");
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
}
