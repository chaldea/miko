using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Routing;
using Shouldly;

namespace Miko.Tests.Routing;

public class NavigationStateIntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_Read_Shared_Form_State_In_Page_Initialization_And_After_Back(bool withLayout)
    {
        var nav = new NavigationManager();
        var router = new Router();
        router.MapRoute<FormPage>("/step/{id:int}");
        using var services = new ServiceCollection().AddSingleton(nav).BuildServiceProvider();
        var view = new RouteView(router, nav, withLayout ? typeof(FormLayout) : null, services);
        Element? rendered = null;
        nav.LocationChanged += args => rendered = view.Render(args.ToPath);
        var draft = new FormDraft { Name = "Alice" };

        nav.NavigateTo("/step/1", draft);
        rendered!.FindByClass("form-state").Single().TextContent.ShouldBe("1:Alice");

        draft.Name = "Bob";
        nav.NavigateTo("/step/2", draft);
        rendered!.FindByClass("form-state").Single().TextContent.ShouldBe("2:Bob");

        nav.NavigateBack().ShouldBeTrue();
        rendered!.FindByClass("form-state").Single().TextContent.ShouldBe("1:Bob");
        nav.GetState<FormDraft>().ShouldBeSameAs(draft);

        nav.NavigateTo("/step/3");
        rendered!.FindByClass("form-state").Single().TextContent.ShouldBe("3:(none)");
    }

    private sealed class FormDraft
    {
        public string Name { get; set; } = "";
    }

    private sealed class FormPage : ComponentBase
    {
        private FormDraft? _draft;

        [Parameter] public int Id { get; set; }

        protected override void OnInitialized() => _draft = NavigationManager!.GetState<FormDraft>();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>().Class = "form-state";
            builder.AddContent($"{Id}:{_draft?.Name ?? "(none)"}");
            builder.CloseElement();
        }
    }

    private sealed class FormLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            Body?.Invoke(builder);
            builder.CloseElement();
        }
    }
}
