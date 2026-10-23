using Miko.Ionic.Tests.Fixtures;
using Miko.Ionic.Tests.Fixtures.Routed;
using Miko.Routing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// <c>Href</c> navigates on every component that has it (P0-C1). IonItem and IonBreadcrumb always
/// did; IonButton, IonFabButton, IonItemOption and IonCard accepted the parameter and only rendered
/// an anchor, so the same markup navigated on one component and silently did nothing on the next.
/// Driven through a real routed app: a tap on the page enters through the interaction controller.
/// </summary>
public sealed class HrefNavigationEndToEndTests : IDisposable
{
    private readonly RoutedApp _app = new(("/", typeof(HrefPageFixture)), ("/detail", typeof(DetailPageFixture)));

    public void Dispose() => _app.Dispose();

    [Theory]
    [InlineData("href-button")]
    [InlineData("href-fab")]
    [InlineData("href-option")]
    [InlineData("href-card")]
    public void Should_navigate_forward_with_the_mode_transition_when_tapped(string component)
    {
        _app.Tap(component);

        _app.Navigation.CurrentPath.ShouldBe("/detail");
        _app.Navigation.History.ShouldBe(new[] { "/", "/detail" });
        var navigation = _app.LastNavigation.ShouldNotBeNull();
        navigation.Direction.ShouldBe(NavigationDirection.Forward);
        navigation.Transition.ShouldBeSameAs(IonicPageTransitions.MdPageTransition.Push);

        _app.Render();
        _app.Root.FindByClass("detail-page").ShouldNotBeEmpty();
    }

    [Fact]
    public void Should_render_an_anchor_as_the_native_surface_when_href_is_set()
    {
        _app.Single("href-button").FindByClass("button-native").ShouldHaveSingleItem().TagName.ShouldBe("a");
        _app.Single("href-fab").FindByClass("button-native").ShouldHaveSingleItem().TagName.ShouldBe("a");
        _app.Single("href-option").FindByClass("button-native").ShouldHaveSingleItem().TagName.ShouldBe("a");
        _app.Single("href-card").FindByClass("card-native").ShouldHaveSingleItem().TagName.ShouldBe("a");
    }

    [Fact]
    public void Should_clear_the_history_without_a_transition_when_router_direction_is_root()
    {
        _app.Tap("href-root-button");

        _app.Navigation.History.ShouldBe(new[] { "/detail" });
        var navigation = _app.LastNavigation.ShouldNotBeNull();
        navigation.Direction.ShouldBe(NavigationDirection.Root);
        navigation.Transition.ShouldBeNull();
    }
}
