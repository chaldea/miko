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

    private class TestPage : Element { public override string TagName => "test-page"; }
    private class AboutPage : Element { public override string TagName => "about-page"; }

    [Route("/annotated")]
    private class AnnotatedPage : Element { public override string TagName => "annotated-page"; }
}
