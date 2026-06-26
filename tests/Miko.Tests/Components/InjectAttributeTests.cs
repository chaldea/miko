using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Core;
using Miko.Routing;
using Shouldly;

namespace Miko.Tests.Components;

/// <summary>
/// ISSUE-069: Components instantiated through <see cref="RenderTreeBuilder.OpenComponent{T}"/>
/// — i.e. anything that's not the top-level page rendered by <see cref="RouteView"/> — must also
/// be able to resolve <c>[Inject]</c> properties from the application's service provider.
/// <para>
/// In Blazor every component is built by a <c>ComponentFactory</c> that uses the
/// <see cref="IServiceProvider"/>; Miko historically did so only inside RouteView. This test
/// suite verifies the ambient <c>ComponentServiceScope</c> bridges that gap.
/// </para>
/// </summary>
public class InjectAttributeTests
{
    private interface ICounterService { int Next(); }

    private sealed class CounterService : ICounterService
    {
        private int _n;
        public int Next() => ++_n;
    }

    // Receives the service via [Inject] and renders its current value.
    private sealed class InjectedChildComponent : ComponentBase
    {
        [Inject] public ICounterService? Counter { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, Counter == null ? "<no-inject>" : Counter.Next().ToString());
            builder.CloseElement();
        }
    }

    // A non-public [Inject] property — Blazor and our RouteView both support this.
    private sealed class NonPublicInjectComponent : ComponentBase
    {
        [Inject] internal ICounterService? Counter { get; set; }

        public ICounterService? GetCounter() => Counter;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.CloseElement();
        }
    }

    // Reads the injected service inside OnInitialized to assert timing.
    private sealed class LifecycleInjectComponent : ComponentBase
    {
        [Inject] public ICounterService? Counter { get; set; }
        public int? SeenInOnInitialized { get; private set; }

        protected override void OnInitialized()
            => SeenInOnInitialized = Counter?.Next();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.CloseElement();
        }
    }

    // Parent that renders an injected child through OpenComponent<T> — exactly what the Razor
    // compiler emits for <InjectedChildComponent />.
    private sealed class ParentRenderingInjectedChild : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.OpenComponent<InjectedChildComponent>(1);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }

    // Two-level nesting: an outer parent renders an inner parent that renders the injected child.
    private sealed class GrandparentComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.OpenComponent<ParentRenderingInjectedChild>(1);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }

    // Re-renders itself on demand; used to assert that re-render keeps the injected service.
    private sealed class ReRenderingHostComponent : ComponentBase
    {
        private int _tick;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // The injected child is the only child element; the tick counter is rendered into
            // an additional <span> sibling so we can distinguish it. (AddContent on the open
            // div would set its TextContent, not produce a child.)
            builder.OpenElement(0, "div");
            builder.OpenElement(1, "span");
            builder.AddContent(2, $"tick:{_tick}");
            builder.CloseElement();
            builder.OpenComponent<InjectedChildComponent>(3);
            builder.CloseComponent();
            builder.CloseElement();
        }

        public void Bump()
        {
            _tick++;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Page rendered through <see cref="RouteView"/> that nests an [Inject]-bearing child via
    /// <see cref="RenderTreeBuilder.OpenComponent{T}"/>. Exercises the full path: RouteView pushes
    /// the provider, ComponentBase re-pushes it for descendants.
    /// </summary>
    private sealed class PageWithInjectedChild : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.OpenComponent<InjectedChildComponent>(1);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }

    private static IServiceProvider BuildProvider(ICounterService? counter = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICounterService>(counter ?? new CounterService());
        return services.BuildServiceProvider();
    }

    [Fact]
    public void NonPageComponent_NoAmbientScope_LeavesInjectPropertyNull()
    {
        // Sanity baseline: building a component directly (no ambient scope) yields no service.
        var child = new InjectedChildComponent();
        var element = child.Build();

        child.Counter.ShouldBeNull();
        element.TextContent.ShouldBe("<no-inject>");
    }

    [Fact]
    public void OpenComponent_ResolvesInjectFromAmbientScope()
    {
        // Direct ambient push: emulates RouteView's behaviour without spinning up a Router.
        var provider = BuildProvider();
        ParentRenderingInjectedChild parent;
        Element element;
        using (ComponentServiceScope.Push(provider))
        {
            parent = new ParentRenderingInjectedChild();
            element = parent.Build();
        }

        // The nested child created via OpenComponent<T> received the service.
        var childDiv = element.Children[0];
        childDiv.TextContent.ShouldBe("1");
    }

    [Fact]
    public void OpenComponent_GrandchildAlsoResolvesInject()
    {
        // Two-level nesting: the provider must propagate through every ancestor's Build.
        var provider = BuildProvider();
        Element element;
        using (ComponentServiceScope.Push(provider))
        {
            element = new GrandparentComponent().Build();
        }

        // Outer div > inner div > injected child div
        var injectedDiv = element.Children[0].Children[0];
        injectedDiv.TextContent.ShouldBe("1");
    }

    [Fact]
    public void Inject_AvailableInOnInitialized()
    {
        // Like Blazor: [Inject] is resolved before OnInitialized runs.
        var provider = BuildProvider();
        LifecycleInjectComponent component;
        using (ComponentServiceScope.Push(provider))
        {
            // OpenComponent<T>() inside a parent is what really exercises the ambient lookup,
            // but the public surface still requires the scope; reuse the parent harness.
            var parent = new ParentForLifecycle();
            parent.Build();
            component = parent.Child!;
        }

        component.SeenInOnInitialized.ShouldBe(1);
    }

    private sealed class ParentForLifecycle : ComponentBase
    {
        public LifecycleInjectComponent? Child;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // Replicate the compiler's OpenComponent<T> path but capture the instance so the
            // test can inspect SeenInOnInitialized after Build returns.
            Child = new LifecycleInjectComponent();
            builder.AttachElement(Child.Build());
        }
    }

    [Fact]
    public void NonPublicInjectProperty_IsResolved()
    {
        var provider = BuildProvider();
        var parent = new NonPublicInjectParent();
        using (ComponentServiceScope.Push(provider))
            parent.Build();

        parent.Child!.GetCounter().ShouldNotBeNull();
    }

    private sealed class NonPublicInjectParent : ComponentBase
    {
        public NonPublicInjectComponent? Child;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Child = new NonPublicInjectComponent();
            builder.AttachElement(Child.Build());
        }
    }

    [Fact]
    public void OpenComponent_UnregisteredService_LeavesPropertyNullAndDoesNotThrow()
    {
        // A provider with no ICounterService registration: build must not throw, and the
        // property must remain null (we don't crash like Blazor; existing RouteView contract).
        var provider = new ServiceCollection().BuildServiceProvider();
        InjectedChildComponent? captured = null;
        Element? element = null;

        Should.NotThrow(() =>
        {
            using (ComponentServiceScope.Push(provider))
            {
                var parent = new GenericParent<InjectedChildComponent>(c => captured = c);
                element = parent.Build();
            }
        });

        captured.ShouldNotBeNull();
        captured!.Counter.ShouldBeNull();
        // GenericParent attaches the child's element directly (no own wrapper), so the build
        // result IS the child's <div>. With no service registered it renders the sentinel text.
        element!.TextContent.ShouldBe("<no-inject>");
    }

    private sealed class GenericParent<TChild> : ComponentBase
        where TChild : ComponentBase, new()
    {
        private readonly Action<TChild>? _onBuilt;
        public GenericParent(Action<TChild>? onBuilt = null) => _onBuilt = onBuilt;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var instance = new TChild();
            _onBuilt?.Invoke(instance);
            builder.AttachElement(instance.Build());
        }
    }

    [Fact]
    public void OpenComponent_InjectedServiceSurvivesStateHasChangedRebuilds()
    {
        // After the host re-renders, the freshly-created nested child must again receive the
        // injected service — the host's captured provider is re-pushed on every rebuild.
        var provider = BuildProvider();
        var host = new ReRenderingHostComponent();

        Element root;
        using (ComponentServiceScope.Push(provider))
            root = host.Build();

        // Children layout: [0] tick <span>, [1] injected child <div>.
        root.Children[1].TextContent.ShouldBe("1");

        // Outside the ambient scope: re-rendering must still inject (host re-pushes _services).
        host.Bump();
        // After Bump, the previous child is discarded; the new child gets a fresh inject.
        root.Children[1].TextContent.ShouldBe("2");
    }

    [Fact]
    public void Scope_UnwindsAfterBuild()
    {
        // The provider stack must be drained after the build completes, so a subsequent
        // ambient-less build does not see a leftover frame.
        var provider = BuildProvider();
        using (ComponentServiceScope.Push(provider))
            new ParentRenderingInjectedChild().Build();

        var orphan = new InjectedChildComponent();
        orphan.Build();
        orphan.Counter.ShouldBeNull();
    }

    [Fact]
    public void InjectAndCascadingParameter_BothResolvedOnSameComponent()
    {
        // A component can declare both [Inject] (DI) and [CascadingParameter] (ambient value)
        // and both must be populated before OnInitialized runs.
        var provider = BuildProvider();

        DualConsumer? captured = null;
        var children = (RenderFragment)(builder =>
        {
            captured = new DualConsumer();
            builder.AttachElement(captured.Build());
        });

        var cascading = new CascadingValue<string>
        {
            Value = "ambient",
            ChildContent = children,
        };

        using (ComponentServiceScope.Push(provider))
            cascading.Build();

        captured.ShouldNotBeNull();
        captured!.Counter.ShouldNotBeNull();
        captured.Theme.ShouldBe("ambient");
    }

    private sealed class DualConsumer : ComponentBase
    {
        [Inject] public ICounterService? Counter { get; set; }
        [CascadingParameter] public string? Theme { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.CloseElement();
        }
    }

    [Fact]
    public void RouteView_NestedComponent_GetsInjectedServiceFromContainer()
    {
        // End-to-end: a page rendered through RouteView nests an [Inject]-bearing child via
        // OpenComponent<T>. The child must resolve its service from the same container that
        // built the page.
        var services = new ServiceCollection();
        services.AddSingleton<ICounterService, CounterService>();
        var provider = services.BuildServiceProvider();

        var router = new Router();
        router.MapRoute<PageWithInjectedChild>("/");

        var routeView = new RouteView(router, new NavigationManager(), defaultLayout: null, provider);
        var tree = routeView.Render("/");

        // div > div (the injected child) — the child rendered the counter value 1.
        tree.Children[0].TextContent.ShouldBe("1");
    }

    [Fact]
    public void OpenComponent_InjectionUsesSameInstance_AcrossSiblings_WhenServiceIsSingleton()
    {
        // Sanity: two sibling components rendered under the same parent both resolve from the
        // ambient provider — singleton services should be the same instance.
        var provider = BuildProvider();

        TwoChildrenParent parent;
        using (ComponentServiceScope.Push(provider))
        {
            parent = new TwoChildrenParent();
            parent.Build();
        }

        parent.A!.Counter.ShouldNotBeNull();
        parent.B!.Counter.ShouldNotBeNull();
        parent.A.Counter.ShouldBeSameAs(parent.B.Counter);
    }

    private sealed class TwoChildrenParent : ComponentBase
    {
        public InjectedChildComponent? A;
        public InjectedChildComponent? B;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            A = new InjectedChildComponent();
            builder.AttachElement(A.Build());
            B = new InjectedChildComponent();
            builder.AttachElement(B.Build());
        }
    }
}
