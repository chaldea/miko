using Miko.Components;
using Miko.Core;
using Shouldly;

namespace Miko.Tests.Components;

public class CascadingValueTests
{
    // ---------------------------------------------------------------------------------------
    // Test components. Each child receives a cascading value and renders it as text content so
    // the test can assert on the produced Element. They're driven through RenderTreeBuilder the
    // same way generated Razor code would: OpenComponent / AddComponentParameter / CloseComponent.
    // ---------------------------------------------------------------------------------------

    private interface IGreeter { string Hello(); }

    private sealed class Greeter : IGreeter
    {
        public string Message = "hi";
        public string Hello() => Message;
    }

    // Receives a string by type.
    private sealed class StringConsumer : ComponentBase
    {
        [CascadingParameter] public string? Msg { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, Msg ?? "<null>");
            builder.CloseElement();
        }
    }

    // Receives a named int.
    private sealed class NamedConsumer : ComponentBase
    {
        [CascadingParameter(Name = "B")] public int Value { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, Value.ToString());
            builder.CloseElement();
        }
    }

    // Receives a service by interface (assignability match).
    private sealed class ServiceConsumer : ComponentBase
    {
        [CascadingParameter] public IGreeter? Greeter { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, Greeter?.Hello() ?? "none");
            builder.CloseElement();
        }
    }

    // Reads the cascading value during OnInitialized (timing test).
    private sealed class LifecycleConsumer : ComponentBase
    {
        [CascadingParameter] public string? Msg { get; set; }
        public string? SeenInOnInitialized { get; private set; }

        protected override void OnInitialized() => SeenInOnInitialized = Msg;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, Msg ?? "<null>");
            builder.CloseElement();
        }
    }

    // Helper: builds a CascadingValue<TValue> wrapping a child produced by `childFragment`.
    private static Element BuildProvider<TValue>(TValue value, string? name, RenderFragment childFragment)
    {
        var provider = new CascadingValue<TValue>
        {
            Value = value,
            Name = name,
            ChildContent = childFragment,
        };
        return provider.Build();
    }

    // Helper: a RenderFragment that renders a single child component of type TChild.
    private static RenderFragment Child<TChild>(out Func<TChild?> captured)
        where TChild : ComponentBase, new()
    {
        TChild? instance = null;
        captured = () => instance;
        return builder =>
        {
            // Replicate the compiler's nested-component path so the child's Build() runs inside
            // the provider's push scope. We instantiate explicitly to capture the instance for
            // assertions (OpenComponent<T> would create its own we couldn't reach).
            instance = new TChild();
            var element = instance.Build();
            builder.AttachElement(element);
        };
    }

    [Fact]
    public void ResolvesByType()
    {
        var root = BuildProvider<string>("hello", null, Child<StringConsumer>(out var child));

        // CascadingValue is transparent: it renders no wrapper element, so a single child becomes
        // the build result directly (the consumer's <div> with the resolved text).
        root.TagName.ShouldBe("div");
        root.TextContent.ShouldBe("hello");
        child()!.Msg.ShouldBe("hello");
    }

    [Fact]
    public void ResolvesByName_PicksMatchingProvider()
    {
        // <CascadingValue Name="A" Value="1"><CascadingValue Name="B" Value="2"><NamedConsumer/>
        var consumerFragment = Child<NamedConsumer>(out var consumer);

        var inner = (RenderFragment)(builder =>
        {
            var b = new CascadingValue<int> { Value = 2, Name = "B", ChildContent = consumerFragment };
            builder.AttachElement(b.Build());
        });

        BuildProvider<int>(1, "A", inner);

        consumer()!.Value.ShouldBe(2);
    }

    [Fact]
    public void InnerProviderShadowsOuter()
    {
        // Outer string "outer", inner string "inner"; the deepest consumer sees "inner".
        var consumerFragment = Child<StringConsumer>(out var consumer);

        var innerProvider = (RenderFragment)(builder =>
        {
            var b = new CascadingValue<string> { Value = "inner", ChildContent = consumerFragment };
            builder.AttachElement(b.Build());
        });

        BuildProvider<string>("outer", null, innerProvider);

        consumer()!.Msg.ShouldBe("inner");
    }

    [Fact]
    public void NoProvider_LeavesParameterDefault_AndDoesNotThrow()
    {
        var consumer = new StringConsumer();
        Element? element = null;
        Should.NotThrow(() => element = consumer.Build());

        consumer.Msg.ShouldBeNull();
        element!.TextContent.ShouldBe("<null>");
    }

    [Fact]
    public void ResolvesByInterface_AssignabilityMatch()
    {
        var greeter = new Greeter { Message = "howdy" };
        var root = BuildProvider<Greeter>(greeter, null, Child<ServiceConsumer>(out var child));

        child()!.Greeter.ShouldBeSameAs(greeter);
        // CascadingValue is transparent: the single child is the build result directly.
        root.TagName.ShouldBe("div");
        root.TextContent.ShouldBe("howdy");
    }

    [Fact]
    public void CascadingValue_AvailableInOnInitialized()
    {
        BuildProvider<string>("early", null, Child<LifecycleConsumer>(out var child));

        child()!.SeenInOnInitialized.ShouldBe("early");
    }

    [Fact]
    public void NullValue_StillResolvesByDeclaredType()
    {
        // Value is null but declared as string; the consumer's Msg should be set to null
        // (resolved), and the build must not leak a frame or throw.
        BuildProvider<string?>(null, null, Child<StringConsumer>(out var child));

        // Resolved (matched by declared type) and set to null; build did not throw.
        child()!.Msg.ShouldBeNull();
    }

    [Fact]
    public void Stack_UnwindsAfterBuild()
    {
        // After a provider builds, no frame should remain on the ambient stack: an independent
        // consumer built afterwards must NOT pick up the previous provider's value.
        BuildProvider<string>("scoped", null, Child<StringConsumer>(out _));

        var afterwards = new StringConsumer();
        afterwards.Build();

        afterwards.Msg.ShouldBeNull();
    }

    [Fact]
    public void TransparentWrapper_DoesNotEmitSpan()
    {
        // Regression for ISSUE-066 问题1: CascadingValue must not wrap its child in a <span>.
        var root = BuildProvider<string>("x", null, Child<StringConsumer>(out _));

        root.ShouldBeOfType<Miko.Core.DomElements.DivElement>();
        root.FindByTagName("span").ShouldBeEmpty();
    }

    [Fact]
    public void MultipleChildren_ProduceTransparentFragment_StillResolvingValue()
    {
        // Two sibling consumers under one CascadingValue: the provider stays transparent (no span),
        // and the multiple roots are carried by a transparent FragmentElement. Both consumers
        // resolve the cascading value.
        StringConsumer? a = null, b = null;
        RenderFragment children = builder =>
        {
            a = new StringConsumer();
            builder.AttachElement(a.Build());
            b = new StringConsumer();
            builder.AttachElement(b.Build());
        };

        var root = BuildProvider<string>("shared", null, children);

        root.ShouldBeOfType<Miko.Core.DomElements.FragmentElement>();
        root.Children.Count.ShouldBe(2);
        root.Children[0].TextContent.ShouldBe("shared");
        root.Children[1].TextContent.ShouldBe("shared");
        a!.Msg.ShouldBe("shared");
        b!.Msg.ShouldBe("shared");
    }
}
