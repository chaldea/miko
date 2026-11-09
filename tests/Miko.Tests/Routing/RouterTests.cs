using Miko.Components;
using Miko.Core;
using Miko.Routing;
using Shouldly;

namespace Miko.Tests.Routing;

public class RouterTests
{
    [Fact]
    public void MapRoute_Generic_RegistersRoute()
    {
        var router = new Router();
        router.MapRoute<TestPage>("/test");

        router.Resolve("/test").ShouldBe(typeof(TestPage));
    }

    [Fact]
    public void MapRoute_Type_RegistersRoute()
    {
        var router = new Router();
        router.MapRoute("/about", typeof(AboutPage));

        router.Resolve("/about").ShouldBe(typeof(AboutPage));
    }

    [Fact]
    public void MapRoute_MultipleRoutes_AllResolvable()
    {
        var router = new Router();
        router.MapRoute<TestPage>("/");
        router.MapRoute<AboutPage>("/about");

        router.Resolve("/").ShouldBe(typeof(TestPage));
        router.Resolve("/about").ShouldBe(typeof(AboutPage));
    }

    [Fact]
    public void MapRoute_CaseInsensitive()
    {
        var router = new Router();
        router.MapRoute<TestPage>("/Home");

        router.Resolve("/home").ShouldBe(typeof(TestPage));
    }

    [Fact]
    public void Resolve_UnknownPath_ReturnsNull()
    {
        var router = new Router();
        router.MapRoute<TestPage>("/test");

        router.Resolve("/unknown").ShouldBeNull();
    }

    [Fact]
    public void ScanAssemblies_FindsRouteAttributes()
    {
        var router = new Router();
        router.ScanAssemblies(typeof(AnnotatedPage).Assembly);

        router.Resolve("/annotated").ShouldBe(typeof(AnnotatedPage));
    }

    [Fact]
    public void MapRoute_AndScanAssemblies_BothWork()
    {
        var router = new Router();
        router.ScanAssemblies(typeof(AnnotatedPage).Assembly);
        router.MapRoute<TestPage>("/manual");

        router.Resolve("/annotated").ShouldBe(typeof(AnnotatedPage));
        router.Resolve("/manual").ShouldBe(typeof(TestPage));
    }

    // ---- Route parameters (ISSUE-164) ----

    [Fact]
    public void Match_ReturnsTheRouteAndTypedValues()
    {
        var router = new Router();
        router.MapRoute<UserPage>("/user/{id:int}/{option:bool?}");

        var match = router.Match("/user/42/true").ShouldNotBeNull();

        match.ComponentType.ShouldBe(typeof(UserPage));
        match.Route.Template.ShouldBe("/user/{id:int}/{option:bool?}");
        match.Parameters["id"].ShouldBe(42);
        match.Parameters["option"].ShouldBe(true);
    }

    [Fact]
    public void Match_UnknownPath_ReturnsNull()
    {
        var router = new Router();
        router.MapRoute<UserPage>("/user/{id:int}");

        router.Match("/user/abc").ShouldBeNull();
        router.Resolve("/user/abc").ShouldBeNull();
    }

    [Fact]
    public void Match_IgnoresTheQueryString()
    {
        var router = new Router();
        router.MapRoute<UserPage>("/user/{id:int}");

        router.Match("/user/42?tab=posts").ShouldNotBeNull().Parameters["id"].ShouldBe(42);
    }

    [Fact]
    public void Match_PrefersLiteral_ThenConstrained_ThenUnconstrained_ThenCatchAll_RegardlessOfOrder()
    {
        // Registered worst-first: registration order must not decide (Blazor's precedence does).
        var router = new Router();
        router.MapRoute<FilePage>("/user/{*rest}");
        router.MapRoute<TestPage>("/user/{name}");
        router.MapRoute<UserPage>("/user/{id:int}");
        router.MapRoute<AboutPage>("/user/profile");

        router.Resolve("/user/profile").ShouldBe(typeof(AboutPage));
        router.Resolve("/user/42").ShouldBe(typeof(UserPage));
        router.Resolve("/user/john").ShouldBe(typeof(TestPage));
        router.Resolve("/user/john/posts").ShouldBe(typeof(FilePage));
        router.Resolve("/user").ShouldBe(typeof(FilePage));
    }

    [Fact]
    public void Match_PrefersTheShorterTemplate_WhenSegmentsTie()
    {
        var router = new Router();
        router.MapRoute<SettingsPage>("/user/{id}/{tab?}");
        router.MapRoute<UserPage>("/user/{id}");

        router.Resolve("/user/1").ShouldBe(typeof(UserPage));
        router.Resolve("/user/1/posts").ShouldBe(typeof(SettingsPage));
    }

    [Fact]
    public void Match_KeepsRegistrationOrder_ForEqualPrecedence()
    {
        var router = new Router();
        router.MapRoute<UserPage>("/user/{id}");
        router.MapRoute<TestPage>("/user/{name}");

        router.Resolve("/user/x").ShouldBe(typeof(UserPage));
    }

    [Fact]
    public void Match_SeesRoutesAddedAfterAnEarlierMatch()
    {
        var router = new Router();
        router.MapRoute<TestPage>("/user/{name}");
        router.Resolve("/user/profile").ShouldBe(typeof(TestPage));

        router.MapRoute<AboutPage>("/user/profile");

        router.Resolve("/user/profile").ShouldBe(typeof(AboutPage));
    }

    [Fact]
    public void MapRoute_InvalidTemplate_ThrowsAtRegistration()
    {
        var router = new Router();

        Should.Throw<ArgumentException>(() => router.MapRoute<UserPage>("/user/{id:number}"));
        Should.Throw<ArgumentException>(() => router.MapRoute("/files/{*path}/x", typeof(FilePage)));
        router.Routes.ShouldBeEmpty();
    }

    [Fact]
    public void ResolvePageKey_IsTheComponentType_ForARouteWithoutParameters()
    {
        // ISSUE-144: one page at several templates keeps one scroll snapshot.
        var router = new Router();
        router.MapRoute<TestPage>("/");
        router.MapRoute<TestPage>("/home");

        router.ResolvePageKey("/").ShouldBe(typeof(TestPage).FullName);
        router.ResolvePageKey("/home").ShouldBe(typeof(TestPage).FullName);
        router.ResolvePageKey("/missing").ShouldBeNull();
    }

    [Fact]
    public void ResolvePageKey_DistinguishesParameterValues()
    {
        // /user/1 and /user/2 are one component showing different content: going from one
        // detail page to another must not restore the first one's scroll position.
        var router = new Router();
        router.MapRoute<UserPage>("/user/{id:int}");
        router.MapRoute<UserPage>("/member/{ID:int}");

        var first = router.ResolvePageKey("/user/1");
        first.ShouldNotBe(router.ResolvePageKey("/user/2"));
        first.ShouldBe(router.ResolvePageKey("/user/1?tab=posts"));
        first.ShouldBe(router.ResolvePageKey("/member/1"), "same component, same values → same page");
    }

    private class TestPage : Element { public override string TagName => "test-page"; }
    private class AboutPage : Element { public override string TagName => "about-page"; }
    private class UserPage : Element { public override string TagName => "user-page"; }
    private class FilePage : Element { public override string TagName => "file-page"; }
    private class SettingsPage : Element { public override string TagName => "settings-page"; }

    [Route("/annotated")]
    private class AnnotatedPage : Element { public override string TagName => "annotated-page"; }
}
