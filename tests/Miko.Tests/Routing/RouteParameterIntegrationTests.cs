using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Routing;
using Shouldly;

namespace Miko.Tests.Routing;

/// <summary>
/// ISSUE-164: <see cref="RouteView"/> assigns route values to the page's
/// <see cref="ParameterAttribute"/> properties, as Blazor's router does.
/// </summary>
public class RouteParameterIntegrationTests
{
    private static RouteView CreateRouteView(Router router, Type? layout = null)
        => new(router, new NavigationManager(), layout, new ServiceCollection().BuildServiceProvider());

    private static string Text(Element root, string className) => root.FindByClass(className).Single().TextContent!;

    [Fact]
    public void Should_Assign_Typed_Values_From_The_Issue_Template()
    {
        var router = new Router();
        router.MapRoute<UserPage>("/user/{id:int}/{option:bool?}");

        var root = CreateRouteView(router).Render("/user/42/true");

        Text(root, "id").ShouldBe("42");
        Text(root, "option").ShouldBe("True");
    }

    [Fact]
    public void Should_Keep_The_Property_Initializer_When_An_Optional_Parameter_Is_Absent()
    {
        var router = new Router();
        router.MapRoute<UserPage>("/user/{id:int}/{option:bool?}");

        var root = CreateRouteView(router).Render("/user/42");

        Text(root, "id").ShouldBe("42");
        Text(root, "option").ShouldBe("False");
    }

    [Fact]
    public void Should_Assign_A_Catch_All_As_A_String()
    {
        var router = new Router();
        router.MapRoute<CatchAllPage>("/catch-all/{*pageRoute}");

        var root = CreateRouteView(router).Render("/catch-all/docs/2024/report.pdf");

        Text(root, "route").ShouldBe("docs/2024/report.pdf");
    }

    [Fact]
    public void Should_Leave_An_Empty_Catch_All_Unassigned()
    {
        var router = new Router();
        router.MapRoute<CatchAllPage>("/catch-all/{*pageRoute}");

        var root = CreateRouteView(router).Render("/catch-all");

        Text(root, "route").ShouldBe("(none)");
    }

    [Fact]
    public void Should_Match_Parameter_Names_Case_Insensitively()
    {
        // Blazor: {userId} fills UserId.
        var router = new Router();
        router.MapRoute<PostPage>("/user/{userId:int}/post/{POSTID:long}");

        var root = CreateRouteView(router).Render("/user/7/post/123");

        Text(root, "user").ShouldBe("7");
        Text(root, "post").ShouldBe("123");
    }

    [Fact]
    public void Should_Assign_To_Nullable_And_Object_Properties()
    {
        var router = new Router();
        router.MapRoute<NullablePage>("/n/{id:int}/{name}");

        var root = CreateRouteView(router).Render("/n/5/ann");

        Text(root, "id").ShouldBe("5");
        Text(root, "name").ShouldBe("ann");
    }

    [Fact]
    public void Should_Assign_Values_Before_The_Lifecycle_Runs()
    {
        var router = new Router();
        router.MapRoute<LifecyclePage>("/life/{id:int}");

        var root = CreateRouteView(router).Render("/life/9");

        Text(root, "seen").ShouldBe("init=9 set=9");
    }

    [Fact]
    public void Should_Assign_Values_When_The_Page_Is_Rendered_Inside_A_Layout()
    {
        var router = new Router();
        router.MapRoute<UserPage>("/user/{id:int}/{option:bool?}");

        var root = CreateRouteView(router, typeof(RootLayout)).Render("/user/3/true");

        root.Class.ShouldBe("layout");
        Text(root, "id").ShouldBe("3");
        Text(root, "option").ShouldBe("True");
    }

    [Fact]
    public void Should_Leave_Parameters_The_Route_Does_Not_Supply()
    {
        // A page registered at a literal route as well keeps its defaults there.
        var router = new Router();
        router.MapRoute<UserPage>("/me");

        var root = CreateRouteView(router).Render("/me");

        Text(root, "id").ShouldBe("0");
    }

    [Fact]
    public void Should_Throw_When_The_Value_Type_Does_Not_Fit_The_Property()
    {
        // Blazor does not convert: an unconstrained {id} is a string and cannot fill an int.
        var router = new Router();
        router.MapRoute<UserPage>("/user/{id}");

        var error = Should.Throw<InvalidOperationException>(() => CreateRouteView(router).Render("/user/42"));

        error.Message.ShouldContain("'/user/{id}'");
        error.Message.ShouldContain("String");
        error.Message.ShouldContain("Int32");
        error.Message.ShouldContain("{id:int}");
    }

    [Fact]
    public void Should_Throw_When_The_Page_Has_No_Property_For_A_Route_Parameter()
    {
        var router = new Router();
        router.MapRoute<UserPage>("/user/{id:int}/{slug}");

        var error = Should.Throw<InvalidOperationException>(() => CreateRouteView(router).Render("/user/1/hello"));

        error.Message.ShouldContain("'slug'");
        error.Message.ShouldContain(typeof(UserPage).FullName!);
    }

    [Fact]
    public void Should_Not_Fill_A_Property_Without_The_Parameter_Attribute()
    {
        var router = new Router();
        router.MapRoute<PlainPropertyPage>("/plain/{id:int}");

        Should.Throw<InvalidOperationException>(() => CreateRouteView(router).Render("/plain/1"))
            .Message.ShouldContain("no [Parameter] property named 'id'");
    }

    // ---- Pages ----

    private static void Line(RenderTreeBuilder builder, string className, string text)
    {
        var span = builder.OpenElement<SpanElement>();
        span.Class = className;
        builder.AddContent(text);
        builder.CloseElement();
    }

    private sealed class UserPage : ComponentBase
    {
        [Parameter] public int Id { get; set; }
        [Parameter] public bool Option { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            Line(builder, "id", Id.ToString());
            Line(builder, "option", Option.ToString());
            builder.CloseElement();
        }
    }

    private sealed class CatchAllPage : ComponentBase
    {
        [Parameter] public string? PageRoute { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            Line(builder, "route", PageRoute ?? "(none)");
            builder.CloseElement();
        }
    }

    private sealed class PostPage : ComponentBase
    {
        [Parameter] public int UserId { get; set; }
        [Parameter] public long PostId { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            Line(builder, "user", UserId.ToString());
            Line(builder, "post", PostId.ToString());
            builder.CloseElement();
        }
    }

    private sealed class NullablePage : ComponentBase
    {
        [Parameter] public int? Id { get; set; }
        [Parameter] public object? Name { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            Line(builder, "id", Id?.ToString() ?? "null");
            Line(builder, "name", Name?.ToString() ?? "null");
            builder.CloseElement();
        }
    }

    private sealed class LifecyclePage : ComponentBase
    {
        private int _atInit = -1;
        private int _atSet = -1;

        [Parameter] public int Id { get; set; }

        protected override void OnInitialized() => _atInit = Id;
        protected override void OnParametersSet() => _atSet = Id;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            Line(builder, "seen", $"init={_atInit} set={_atSet}");
            builder.CloseElement();
        }
    }

    private sealed class PlainPropertyPage : ComponentBase
    {
        public int Id { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            builder.CloseElement();
        }
    }

    private sealed class RootLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var root = builder.OpenElement<DivElement>();
            root.Class = "layout";
            Body?.Invoke(builder);
            builder.CloseElement();
        }
    }
}
