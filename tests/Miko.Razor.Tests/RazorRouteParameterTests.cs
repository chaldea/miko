using Microsoft.Extensions.DependencyInjection;
using Miko.Core;
using Miko.Routing;
using Shouldly;

namespace Miko.Razor.Tests;

/// <summary>
/// ISSUE-164 end to end: <c>@page</c> directives with route parameters go through Miko's Razor
/// compiler into the generated <c>RegisterRoutes</c> (the AOT-safe path every app uses), and the
/// routed page receives the values in its <c>[Parameter]</c> properties.
/// </summary>
public class RazorRouteParameterTests
{
    private static Router GeneratedRouter()
    {
        var router = new Router();
        GeneratedRouteRegistration.RegisterRoutes(router);
        return router;
    }

    private static Element Render(string path)
        => new RouteView(GeneratedRouter(), new NavigationManager(), null, new ServiceCollection().BuildServiceProvider())
            .Render(path);

    private static string Text(Element root, string className) => root.FindByClass(className).Single().TextContent!;

    [Fact]
    public void GeneratedRegistration_KeepsTheTemplatesAsWritten()
    {
        var templates = GeneratedRouter().Routes
            .Where(r => r.ComponentType == typeof(RouteParameterFixture))
            .Select(r => r.Template);

        templates.ShouldBe(["/razor-user/{id:int}/{option:bool?}", "/razor-catch-all/{*pageRoute}"], ignoreOrder: true);
    }

    [Fact]
    public void Page_ReceivesTypedValues()
    {
        var root = Render("/razor-user/42/true");

        Text(root, "route-id").ShouldBe("42");
        Text(root, "route-option").ShouldBe("True");
        Text(root, "route-rest").ShouldBe("(none)");
    }

    [Fact]
    public void Page_KeepsDefaults_ForAnAbsentOptionalParameter()
    {
        var root = Render("/razor-user/7");

        Text(root, "route-id").ShouldBe("7");
        Text(root, "route-option").ShouldBe("False");
    }

    [Fact]
    public void Page_ReceivesTheCatchAll()
    {
        var root = Render("/razor-catch-all/a/b/c.txt");

        Text(root, "route-rest").ShouldBe("a/b/c.txt");
        Text(root, "route-id").ShouldBe("0");
    }

    [Fact]
    public void ConstraintMismatch_DoesNotRoute()
    {
        GeneratedRouter().Match("/razor-user/abc").ShouldBeNull();
        GeneratedRouter().Match("/razor-user/1/maybe").ShouldBeNull();
    }
}
