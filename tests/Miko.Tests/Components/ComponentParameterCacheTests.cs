using Miko.Components;
using Miko.Core.DomElements;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Miko.Tests.Components;

/// <summary>
/// ISSUE-136：级联参数与 <c>[Inject]</c> 的反射描述符按组件类型缓存。
///
/// <para>此前 <see cref="ComponentBase.Build"/> 每次渲染都执行
/// <c>GetProperties(Public | NonPublic | Instance)</c> 并对每个属性做一次
/// <c>GetCustomAttribute</c>；而 <c>StateHasChanged</c> 每帧都会渲染。缓存把这份工作
/// 降到「每类型一次」，属性写入也改走预先构建的委托而非 <c>PropertyInfo.SetValue</c>。</para>
///
/// <para>这些用例锁定的是<b>行为不变</b>：缓存只是加速，解析语义必须与逐次反射时完全一致
/// ——尤其是重复渲染、无匹配提供者、只读属性、以及非公开属性这几种情况。</para>
/// </summary>
public class ComponentParameterCacheTests
{
    // -----------------------------------------------------------------
    // 级联参数
    // -----------------------------------------------------------------

    [Fact]
    public void CascadingParameter_IsResolved_OnEveryRender()
    {
        // 缓存是按类型的，不能因为「这个类型解析过了」就跳过后续实例/后续渲染的赋值。
        var first = RenderWithCascadingValue("alpha");
        var second = RenderWithCascadingValue("beta");

        first.ShouldBe("alpha");
        second.ShouldBe("beta");
    }

    [Fact]
    public void CascadingParameter_Rerender_PicksUpNewValue()
    {
        var host = new CascadingHost { Value = "first" };
        host.Build().TextContent.ShouldBe("first");

        host.Value = "second";
        host.Rerender();

        host.Child.ShouldNotBeNull();
        host.Child!.Received.ShouldBe("second");
    }

    [Fact]
    public void CascadingParameter_WithNoProvider_KeepsDefault()
    {
        // 无匹配提供者时属性保持默认值（不得被写成 null 或抛错）。
        var component = new CascadingChild { Received = "untouched" };

        component.Build();

        component.Received.ShouldBe("untouched");
    }

    [Fact]
    public void CascadingParameter_NamedValue_MatchesByName()
    {
        var host = new NamedCascadingHost();
        host.Build();

        host.Child.ShouldNotBeNull();
        host.Child!.Named.ShouldBe("by-name");
    }

    [Fact]
    public void CascadingParameter_NonPublicProperty_IsResolved()
    {
        // 旧实现用 NonPublic 绑定标志，非公开的级联属性也要赋值。
        var host = new NonPublicCascadingHost();
        host.Build();

        host.Child.ShouldNotBeNull();
        host.Child!.Exposed.ShouldBe("private-ok");
    }

    [Fact]
    public void CascadingParameter_ReadOnlyProperty_IsSkipped()
    {
        // 只读属性不可写，必须跳过而不是抛异常。
        var host = new ReadOnlyCascadingHost();

        Should.NotThrow(() => host.Build());
    }

    // -----------------------------------------------------------------
    // [Inject]
    // -----------------------------------------------------------------

    [Fact]
    public void Inject_ResolvesRegisteredService()
    {
        var services = new ServiceCollection()
            .AddSingleton(new Greeter("hi"))
            .BuildServiceProvider();

        using (ComponentServiceScope.Push(services))
        {
            var component = new InjectedComponent();
            component.Build();

            component.Greeter.ShouldNotBeNull();
            component.Greeter!.Message.ShouldBe("hi");
        }
    }

    [Fact]
    public void Inject_UnregisteredService_LeavesPropertyNull()
    {
        // 未注册的服务被容忍（保持 RouteView 的既有行为），不抛异常。
        var services = new ServiceCollection().BuildServiceProvider();

        using (ComponentServiceScope.Push(services))
        {
            var component = new InjectedComponent();

            Should.NotThrow(() => component.Build());
            component.Greeter.ShouldBeNull();
        }
    }

    [Fact]
    public void Inject_ResolvesOncePerInstance_AcrossRerenders()
    {
        // 注入只在首次 Build 解析；后续重建保持同一实例（对齐 Blazor）。
        var services = new ServiceCollection()
            .AddSingleton(new Greeter("once"))
            .BuildServiceProvider();

        using (ComponentServiceScope.Push(services))
        {
            var component = new InjectedComponent();
            component.Build();
            var resolved = component.Greeter;

            component.Build();

            component.Greeter.ShouldBeSameAs(resolved);
        }
    }

    // -----------------------------------------------------------------
    // 继承而来的参数（ISSUE-140）
    // -----------------------------------------------------------------

    [Fact]
    public void CascadingParameter_InheritedProtectedProperty_IsResolved()
    {
        // Ionic 的 IonicComponentBase 把 CascadingTheme 等注入点声明为 protected，由几十个
        // 组件继承。裁剪器的 NonPublicProperties 只保留「该类型自己声明的」非公开属性，
        // 不上溯基类——所以入口标注必须带 NonPublicPropertiesWithInherited，否则 AOT 下
        // 这些继承来的槽位全被裁掉，级联上下文永远是 null（ISSUE-140 的三号症状）。
        var host = new InheritedCascadingHost();
        host.Build();

        host.Child.ShouldNotBeNull();
        host.Child!.ExposedFromBase.ShouldBe("inherited-ok");
    }

    [Fact]
    public void Inject_InheritedProtectedProperty_IsResolved()
    {
        var services = new ServiceCollection()
            .AddSingleton(new Greeter("from-base"))
            .BuildServiceProvider();

        using (ComponentServiceScope.Push(services))
        {
            var component = new DerivedInjectedComponent();
            component.Build();

            component.ExposedGreeter.ShouldNotBeNull();
            component.ExposedGreeter!.Message.ShouldBe("from-base");
        }
    }

    [Fact]
    public void CascadingParameter_ValueTypeProperty_IsResolved()
    {
        // CreateSetter 的快路径只能绑定引用类型（委托绑定不会为 object 形参装箱），
        // 值类型属性走 PropertyInfo.SetValue 兜底——这条路径同样必须写入成功。
        var host = new ValueTypeCascadingHost();
        host.Build();

        host.Child.ShouldNotBeNull();
        host.Child!.Count.ShouldBe(42);
    }

    // -----------------------------------------------------------------
    // Fixtures
    // -----------------------------------------------------------------

    private static string? RenderWithCascadingValue(string value)
    {
        var host = new CascadingHost { Value = value };
        host.Build();
        return host.Child?.Received;
    }

    private sealed class CascadingHost : ComponentBase
    {
        public string? Value { get; set; }
        public CascadingChild? Child { get; private set; }

        public void Rerender() => NotifyStateChanged();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var cascade = builder.OpenComponent<CascadingValue<string>>();
            cascade.Value = Value;
            cascade.ChildContent = inner =>
            {
                Child = inner.OpenComponent<CascadingChild>();
                inner.CloseComponent();
            };
            builder.CloseComponent();
        }
    }

    private sealed class CascadingChild : ComponentBase
    {
        [CascadingParameter] public string? Received { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<SpanElement>();
            builder.AddContent(Received);
            builder.CloseElement();
        }
    }

    private sealed class NamedCascadingHost : ComponentBase
    {
        public NamedChild? Child { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var cascade = builder.OpenComponent<CascadingValue<string>>();
            cascade.Name = "tag";
            cascade.Value = "by-name";
            cascade.ChildContent = inner =>
            {
                Child = inner.OpenComponent<NamedChild>();
                inner.CloseComponent();
            };
            builder.CloseComponent();
        }
    }

    private sealed class NamedChild : ComponentBase
    {
        [CascadingParameter(Name = "tag")] public string? Named { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<SpanElement>();
            builder.CloseElement();
        }
    }

    private sealed class NonPublicCascadingHost : ComponentBase
    {
        public NonPublicChild? Child { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var cascade = builder.OpenComponent<CascadingValue<string>>();
            cascade.Value = "private-ok";
            cascade.ChildContent = inner =>
            {
                Child = inner.OpenComponent<NonPublicChild>();
                inner.CloseComponent();
            };
            builder.CloseComponent();
        }
    }

    private sealed class NonPublicChild : ComponentBase
    {
        [CascadingParameter] private string? Hidden { get; set; }

        public string? Exposed => Hidden;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<SpanElement>();
            builder.CloseElement();
        }
    }

    private sealed class ReadOnlyCascadingHost : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var cascade = builder.OpenComponent<CascadingValue<string>>();
            cascade.Value = "ignored";
            cascade.ChildContent = inner =>
            {
                inner.OpenComponent<ReadOnlyChild>();
                inner.CloseComponent();
            };
            builder.CloseComponent();
        }
    }

    private sealed class ReadOnlyChild : ComponentBase
    {
        [CascadingParameter] public string? ReadOnlyValue => null;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<SpanElement>();
            builder.CloseElement();
        }
    }

    private sealed class InjectedComponent : ComponentBase
    {
        [Inject] public Greeter? Greeter { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<SpanElement>();
            builder.CloseElement();
        }
    }

    // 模拟 Miko.Ionic.Components.IonicComponentBase：参数槽声明在基类上且非公开。
    private abstract class ParameterizedBase : ComponentBase
    {
        [CascadingParameter] protected string? ThemeFromBase { get; set; }

        [Inject] protected Greeter? GreeterFromBase { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<SpanElement>();
            builder.CloseElement();
        }
    }

    private sealed class InheritedCascadingChild : ParameterizedBase
    {
        public string? ExposedFromBase => ThemeFromBase;
    }

    private sealed class DerivedInjectedComponent : ParameterizedBase
    {
        public Greeter? ExposedGreeter => GreeterFromBase;
    }

    private sealed class InheritedCascadingHost : ComponentBase
    {
        public InheritedCascadingChild? Child { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var cascade = builder.OpenComponent<CascadingValue<string>>();
            cascade.Value = "inherited-ok";
            cascade.ChildContent = inner =>
            {
                Child = inner.OpenComponent<InheritedCascadingChild>();
                inner.CloseComponent();
            };
            builder.CloseComponent();
        }
    }

    private sealed class ValueTypeCascadingHost : ComponentBase
    {
        public ValueTypeChild? Child { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var cascade = builder.OpenComponent<CascadingValue<int>>();
            cascade.Value = 42;
            cascade.ChildContent = inner =>
            {
                Child = inner.OpenComponent<ValueTypeChild>();
                inner.CloseComponent();
            };
            builder.CloseComponent();
        }
    }

    private sealed class ValueTypeChild : ComponentBase
    {
        [CascadingParameter] public int Count { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<SpanElement>();
            builder.CloseElement();
        }
    }

    private sealed class Greeter(string message)
    {
        public string Message { get; } = message;
    }
}
