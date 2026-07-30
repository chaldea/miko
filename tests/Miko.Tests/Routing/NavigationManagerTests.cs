using Miko.Animation;
using Miko.Routing;
using Shouldly;

namespace Miko.Tests.Routing;

public class NavigationManagerTests
{
    [Fact]
    public void NavigateTo_Default_ShouldPushHistory_AndRaiseForwardEvent()
    {
        var nav = new NavigationManager();
        NavigationEventArgs? args = null;
        nav.LocationChanged += a => args = a;

        nav.NavigateTo("/list");

        nav.CurrentPath.ShouldBe("/list");
        nav.History.ShouldBe(new[] { "/", "/list" });
        nav.CanGoBack.ShouldBeTrue();

        args.ShouldNotBeNull();
        args!.FromPath.ShouldBe("/");
        args.ToPath.ShouldBe("/list");
        args.Direction.ShouldBe(NavigationDirection.Forward);
        args.Transition.ShouldBeNull();
    }

    [Fact]
    public void NavigateTo_SamePath_ShouldBeNoOp()
    {
        var nav = new NavigationManager();
        nav.NavigateTo("/list");

        int eventCount = 0;
        nav.LocationChanged += _ => eventCount++;

        nav.NavigateTo("/list");
        nav.NavigateTo("/list", NavigationDirection.Root);

        eventCount.ShouldBe(0);
        nav.History.ShouldBe(new[] { "/", "/list" });
    }

    [Fact]
    public void NavigateTo_Root_ShouldClearHistory()
    {
        var nav = new NavigationManager();
        nav.NavigateTo("/list");
        nav.NavigateTo("/detail");
        nav.CanGoBack.ShouldBeTrue();

        NavigationEventArgs? args = null;
        nav.LocationChanged += a => args = a;

        // Tab 切换：根级导航，历史栈重置
        nav.NavigateTo("/tab2", NavigationDirection.Root);

        nav.CurrentPath.ShouldBe("/tab2");
        nav.History.ShouldBe(new[] { "/tab2" });
        nav.CanGoBack.ShouldBeFalse();
        args!.Direction.ShouldBe(NavigationDirection.Root);
    }

    [Fact]
    public void NavigateTo_Back_ShouldPopCurrentEntry()
    {
        var nav = new NavigationManager();
        nav.NavigateTo("/list");
        nav.NavigateTo("/detail");

        NavigationEventArgs? args = null;
        nav.LocationChanged += a => args = a;

        nav.NavigateTo("/list", NavigationDirection.Back);

        nav.CurrentPath.ShouldBe("/list");
        nav.History.ShouldBe(new[] { "/", "/list" });
        args!.Direction.ShouldBe(NavigationDirection.Back);
        args.FromPath.ShouldBe("/detail");
        args.ToPath.ShouldBe("/list");
    }

    [Fact]
    public void NavigateBack_ShouldPopToPreviousEntry()
    {
        var nav = new NavigationManager();
        nav.NavigateTo("/list");
        nav.NavigateTo("/detail");

        NavigationEventArgs? args = null;
        nav.LocationChanged += a => args = a;

        nav.NavigateBack().ShouldBeTrue();

        nav.CurrentPath.ShouldBe("/list");
        nav.History.ShouldBe(new[] { "/", "/list" });
        args!.Direction.ShouldBe(NavigationDirection.Back);
    }

    [Fact]
    public void NavigateBack_WithoutHistory_ShouldReturnFalse_AndNotNavigate()
    {
        var nav = new NavigationManager();
        int eventCount = 0;
        nav.LocationChanged += _ => eventCount++;

        nav.NavigateBack().ShouldBeFalse();

        eventCount.ShouldBe(0);
        nav.CurrentPath.ShouldBe("/");
        nav.CanGoBack.ShouldBeFalse();
    }

    [Fact]
    public void NavigateTo_WithTransition_ShouldPassTransitionThroughEventArgs()
    {
        var nav = new NavigationManager();
        var transition = new StubTransition();

        NavigationEventArgs? args = null;
        nav.LocationChanged += a => args = a;

        nav.NavigateTo("/detail", NavigationDirection.Forward, transition);

        args!.Transition.ShouldBeSameAs(transition);
    }

    private sealed class StubTransition : NavigationTransition
    {
        public override float Duration => 0.3f;
        public override void Apply(NavigationTransitionContext context, float progress) { }
    }
}
