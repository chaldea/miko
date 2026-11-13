using Miko.Routing;
using Shouldly;

namespace Miko.Tests.Routing;

public class NavigationStateTests
{
    [Fact]
    public void Should_Return_Default_When_State_Is_Absent_Or_Has_A_Different_Type()
    {
        var nav = new NavigationManager();

        nav.CurrentState.ShouldBeNull();
        nav.GetState<User>().ShouldBeNull();
        nav.GetState<int>().ShouldBe(0);
        nav.GetState<int?>().ShouldBeNull();

        nav.NavigateTo("/b", new User());

        nav.GetState<string>().ShouldBeNull();
        nav.GetState<int>().ShouldBe(0);
    }

    [Fact]
    public void Should_Pass_The_Original_Object_And_Keep_Shared_Edits()
    {
        var nav = new NavigationManager();
        var user = new User { Name = "Alice" };
        user.Friend = user;

        nav.NavigateTo("/b", user);

        nav.CurrentPath.ShouldBe("/b");
        nav.CurrentState.ShouldBeSameAs(user);
        nav.GetState<User>().ShouldBeSameAs(user);
        nav.GetState<object>().ShouldBeSameAs(user);
        nav.GetState<User>()!.Friend.ShouldBeSameAs(user);
        nav.GetState<User>()!.Name = "Bob";
        user.Name.ShouldBe("Bob");
    }

    [Fact]
    public void Should_Support_Value_Types_Without_Conversion()
    {
        var nav = new NavigationManager();

        nav.NavigateTo("/number", state: 42);

        nav.GetState<int>().ShouldBe(42);
        nav.GetState<int?>().ShouldBe(42);
        nav.GetState<long?>().ShouldBeNull();
    }

    [Fact]
    public void Should_Publish_State_Before_Notifying_Listeners_And_Preserve_Transition()
    {
        var nav = new NavigationManager();
        var user = new User();
        var transition = new StubTransition();
        NavigationEventArgs? notification = null;
        nav.LocationChanged += args =>
        {
            nav.CurrentPath.ShouldBe("/b");
            nav.GetState<User>().ShouldBeSameAs(user);
            args.State.ShouldBeSameAs(user);
            notification = args;
        };

        nav.NavigateTo("/b", user, NavigationDirection.Forward, transition);

        notification.ShouldNotBeNull();
        notification.FromPath.ShouldBe("/");
        notification.ToPath.ShouldBe("/b");
        notification.Direction.ShouldBe(NavigationDirection.Forward);
        notification.Transition.ShouldBeSameAs(transition);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_Restore_State_When_Returning(bool explicitPath)
    {
        var nav = new NavigationManager();
        var user = new User();
        nav.NavigateTo("/a", user);
        nav.NavigateTo("/b", new User());
        NavigationEventArgs? notification = null;
        nav.LocationChanged += args =>
        {
            nav.GetState<User>().ShouldBeSameAs(user);
            notification = args;
        };

        if (explicitPath)
            nav.NavigateTo("/a", NavigationDirection.Back);
        else
            nav.NavigateBack().ShouldBeTrue();

        nav.CurrentPath.ShouldBe("/a");
        nav.History.ShouldBe(new[] { "/", "/a" });
        notification!.State.ShouldBeSameAs(user);
        notification.Direction.ShouldBe(NavigationDirection.Back);
    }

    [Fact]
    public void Should_Keep_State_Per_History_Entry_For_Repeated_Paths()
    {
        var nav = new NavigationManager();
        var first = new User();
        var second = new User();
        nav.NavigateTo("/a", first);
        nav.NavigateTo("/b");
        nav.NavigateTo("/a", second);

        nav.GetState<User>().ShouldBeSameAs(second);
        nav.History.ShouldBe(new[] { "/", "/a", "/b", "/a" });
        nav.NavigateBack().ShouldBeTrue();
        nav.CurrentState.ShouldBeNull();
        nav.NavigateBack().ShouldBeTrue();
        nav.GetState<User>().ShouldBeSameAs(first);
        nav.NavigateBack().ShouldBeTrue();
        nav.CurrentState.ShouldBeNull();
    }

    [Theory]
    [InlineData(NavigationDirection.Forward)]
    [InlineData(NavigationDirection.Root)]
    public void Should_Start_Without_State_When_None_Is_Passed(NavigationDirection direction)
    {
        var nav = new NavigationManager();
        nav.NavigateTo("/a", new User());

        nav.NavigateTo("/b", direction);

        nav.CurrentState.ShouldBeNull();
        nav.GetState<User>().ShouldBeNull();
    }

    [Fact]
    public void Should_Reset_History_And_State_On_Root_Navigation()
    {
        var nav = new NavigationManager();
        var user = new User();
        nav.NavigateTo("/a", new User());
        nav.NavigateTo("/b", new User());

        nav.NavigateTo("/root", user, NavigationDirection.Root);

        nav.History.ShouldBe(new[] { "/root" });
        nav.CanGoBack.ShouldBeFalse();
        nav.NavigateBack().ShouldBeFalse();
        nav.GetState<User>().ShouldBeSameAs(user);
        nav.NavigateTo("/a");
        nav.CurrentState.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_Allow_Explicit_State_Replacement_Or_Clear_On_Back(bool clear)
    {
        var nav = new NavigationManager();
        var replacement = clear ? null : new User();
        nav.NavigateTo("/a", new User());
        nav.NavigateTo("/b", new User());

        nav.NavigateTo("/a", replacement, NavigationDirection.Back);

        nav.CurrentState.ShouldBeSameAs(replacement);
        nav.History.ShouldBe(new[] { "/", "/a" });
        nav.NavigateTo("/c");
        nav.NavigateBack().ShouldBeTrue();
        nav.CurrentState.ShouldBeSameAs(replacement);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_Use_Only_The_Supplied_State_For_A_Back_Fallback(bool withState)
    {
        var nav = new NavigationManager();
        var fallback = new User();
        nav.NavigateTo("/a", new User());
        nav.NavigateTo("/b", new User());

        if (withState)
            nav.NavigateTo("/fallback", fallback, NavigationDirection.Back);
        else
            nav.NavigateTo("/fallback", NavigationDirection.Back);

        nav.History.ShouldBe(new[] { "/", "/a", "/fallback" });
        nav.CurrentState.ShouldBeSameAs(withState ? fallback : null);
    }

    [Theory]
    [InlineData(NavigationDirection.Forward)]
    [InlineData(NavigationDirection.Back)]
    [InlineData(NavigationDirection.Root)]
    public void Should_Preserve_Same_Path_No_Op_Including_State(NavigationDirection direction)
    {
        var nav = new NavigationManager();
        var user = new User();
        nav.NavigateTo("/a", user);
        var notifications = 0;
        nav.LocationChanged += _ => notifications++;

        nav.NavigateTo("/a", direction);
        nav.NavigateTo("/a", new User(), direction);
        nav.NavigateTo("/a", state: null, direction: direction);

        notifications.ShouldBe(0);
        nav.CurrentState.ShouldBeSameAs(user);
        nav.History.ShouldBe(new[] { "/", "/a" });
    }

    [Fact]
    public void Should_Not_Reuse_Popped_State_When_Visiting_The_Path_Again()
    {
        var nav = new NavigationManager();
        nav.NavigateTo("/a", new User());
        nav.NavigateBack().ShouldBeTrue();

        nav.NavigateTo("/a");

        nav.CurrentState.ShouldBeNull();
    }

    [Fact]
    public void Should_Keep_Managers_Isolated()
    {
        var first = new NavigationManager();
        var second = new NavigationManager();
        first.NavigateTo("/a", new User());

        second.NavigateTo("/a");

        second.CurrentState.ShouldBeNull();
        first.GetState<User>().ShouldNotBeNull();
    }

    [Fact]
    public void Should_Expose_A_Live_Read_Only_Path_History()
    {
        var nav = new NavigationManager();
        var paths = nav.History;

        nav.NavigateTo("/a", new User());
        paths.ShouldBe(new[] { "/", "/a" });
        paths[^1].ShouldBe("/a");
        nav.NavigateBack();
        paths.ShouldBe(new[] { "/" });
    }

    private sealed class User
    {
        public string Name { get; set; } = "";
        public User? Friend { get; set; }
    }

    private sealed class StubTransition : NavigationTransition
    {
        public override float Duration => 0.3f;
        public override void Apply(NavigationTransitionContext context, float progress) { }
    }
}
