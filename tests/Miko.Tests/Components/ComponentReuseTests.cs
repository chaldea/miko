using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Shouldly;

namespace Miko.Tests.Components;

/// <summary>
/// ISSUE-154 BUG-01 / FEAT-01：父组件刷新时，身份不变的子组件复用原实例——C# 字段、订阅、进行中的
/// 异步工作都保留；OnInitialized 只执行一次，参数更新走 OnParametersSet，实例最终出局时才 OnDispose。
///
/// <para>身份 = 调用点（编译器给每个 OpenComponent 的序号）+ 所在的片段/带键元素作用域 +
/// <c>@key</c>（无键时按同一调用点的出现次序）。这些组件按 Razor 编译器的发射形态手写
/// （<c>OpenComponent&lt;T&gt;(seq, key)</c> 后直接给参数赋值）。</para>
/// </summary>
public class ComponentReuseTests
{
    /// <summary>记录生命周期调用的子组件。</summary>
    private sealed class Probe : ComponentBase
    {
        [Parameter] public string? Label { get; set; }
        [Parameter] public string? Note { get; set; }

        public int Initialized;
        public int ParametersSet;
        public int Disposed;
        public int Clicks;

        protected override void OnInitialized() => Initialized++;
        protected override void OnParametersSet() => ParametersSet++;
        protected override void OnDispose() => Disposed++;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var div = builder.OpenElement<DivElement>();
            div.Class = "probe";
            div.OnClick = RenderTreeBuilder.ToHandler(EventCallback.Factory.Create<MouseEventArgs>(this, () => Clicks++));
            builder.AddContent($"{Label}:{Clicks}");
            builder.CloseElement();
        }
    }

    /// <summary>可编程的父组件：每次渲染调用 <see cref="Render"/>，测试据此改变标记形状。</summary>
    private sealed class Host : ComponentBase
    {
        public Action<RenderTreeBuilder> Render { get; set; } = _ => { };
        public void Refresh() => StateHasChanged();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            Render(builder);
            builder.CloseElement();
        }
    }

    /// <summary>挂到一个父元素下，使 StateHasChanged 走有父节点的替换分支。</summary>
    private static (Host Host, DivElement Container) Mount(Action<RenderTreeBuilder> render)
    {
        var host = new Host { Render = render };
        var container = new DivElement();
        container.AddChild(host.Build());
        return (host, container);
    }

    private static Probe OpenProbe(RenderTreeBuilder builder, int seq, string label, object? key = null)
    {
        var probe = builder.OpenComponent<Probe>(seq, key);
        probe.Label = label;
        builder.CloseComponent();
        return probe;
    }

    private static List<string> ProbeTexts(Element root)
        => root.FindByClass("probe").Select(e => e.TextContent ?? string.Empty).ToList();

    [Fact]
    public void Should_reuse_child_instance_and_keep_its_state_when_parent_rerenders()
    {
        var instances = new List<Probe>();
        var label = "a";
        var (host, container) = Mount(b => instances.Add(OpenProbe(b, 1, label)));

        var first = instances.Single();
        first.Clicks = 3;            // C# 字段上的状态
        label = "b";
        host.Refresh();
        host.Refresh();

        instances.Count.ShouldBe(3);
        instances.ShouldAllBe(p => ReferenceEquals(p, first));
        first.Initialized.ShouldBe(1);
        first.ParametersSet.ShouldBe(3);
        first.Disposed.ShouldBe(0);
        ProbeTexts(container).ShouldBe(new[] { "b:3" });
    }

    [Fact]
    public void Should_keep_unassigned_parameters_and_overwrite_assigned_ones()
    {
        var instances = new List<Probe>();
        var passNote = true;
        var (host, _) = Mount(b =>
        {
            var probe = b.OpenComponent<Probe>(1, null);
            probe.Label = "x";
            if (passNote) probe.Note = "from parent";
            b.CloseComponent();
            instances.Add(probe);
        });

        passNote = false;
        host.Refresh();

        instances[^1].Note.ShouldBe("from parent");
    }

    [Fact]
    public void Should_dispose_child_once_when_parent_stops_rendering_it()
    {
        Probe? probe = null;
        var show = true;
        var (host, container) = Mount(b => { if (show) probe = OpenProbe(b, 1, "a"); });

        show = false;
        host.Refresh();
        host.Refresh();

        probe!.Disposed.ShouldBe(1);
        ProbeTexts(container).ShouldBeEmpty();
    }

    /// <summary>
    /// 条件块出现/消失不应让后面调用点的实例错位：两个调用点的序号不同，各自认领各自的实例。
    /// </summary>
    [Fact]
    public void Should_not_shift_instances_when_a_preceding_conditional_component_toggles()
    {
        var conditional = new List<Probe>();
        var always = new List<Probe>();
        var show = false;
        var (host, _) = Mount(b =>
        {
            if (show) conditional.Add(OpenProbe(b, 1, "conditional"));
            always.Add(OpenProbe(b, 2, "always"));
        });
        always[0].Clicks = 7;

        show = true;
        host.Refresh();
        show = false;
        host.Refresh();

        always.ShouldAllBe(p => ReferenceEquals(p, always[0]));
        always[0].Clicks.ShouldBe(7);
        conditional.Single().Disposed.ShouldBe(1);
    }

    [Fact]
    public void Should_reuse_unkeyed_list_items_by_position()
    {
        var items = new List<string> { "a", "b", "c" };
        var rendered = new List<List<Probe>>();
        var (host, _) = Mount(b =>
        {
            var current = new List<Probe>();
            foreach (var item in items) current.Add(OpenProbe(b, 1, item));
            rendered.Add(current);
        });

        items.RemoveAt(0);
        host.Refresh();

        // 无键：按位置匹配——前两个实例被复用（拿到新的标签），第三个出局。
        rendered[1][0].ShouldBeSameAs(rendered[0][0]);
        rendered[1][1].ShouldBeSameAs(rendered[0][1]);
        rendered[1][0].Label.ShouldBe("b");
        rendered[0][2].Disposed.ShouldBe(1);
    }

    [Fact]
    public void Should_follow_keys_when_list_items_are_reordered_removed_and_inserted()
    {
        var items = new List<string> { "a", "b", "c" };
        var byKey = new Dictionary<string, List<Probe>>();
        var (host, container) = Mount(b =>
        {
            foreach (var item in items)
            {
                var probe = OpenProbe(b, 1, item, key: item);
                if (!byKey.TryGetValue(item, out var list)) byKey[item] = list = new();
                list.Add(probe);
            }
        });
        byKey["a"][0].Clicks = 1;
        byKey["b"][0].Clicks = 2;
        byKey["c"][0].Clicks = 3;

        items = new List<string> { "c", "a", "d" };   // 重排、删除 b、插入 d
        host.Refresh();

        byKey["a"].ShouldAllBe(p => ReferenceEquals(p, byKey["a"][0]));
        byKey["c"].ShouldAllBe(p => ReferenceEquals(p, byKey["c"][0]));
        byKey["b"].Single().Disposed.ShouldBe(1);
        byKey["d"].Single().Initialized.ShouldBe(1);
        ProbeTexts(container).ShouldBe(new[] { "c:3", "a:1", "d:0" });
        byKey["a"][0].Disposed.ShouldBe(0);
    }

    [Fact]
    public void Should_reject_duplicate_component_keys()
    {
        var host = new Host
        {
            Render = b =>
            {
                OpenProbe(b, 1, "first", key: 42);
                OpenProbe(b, 1, "second", key: 42);
            },
        };

        Should.Throw<InvalidOperationException>(() => host.Build()).Message.ShouldContain("42");
    }

    [Fact]
    public void Should_reject_duplicate_element_keys_among_siblings()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement<DivElement>();
        builder.OpenElement<DivElement>();
        builder.SetKey("k");
        builder.CloseElement();
        builder.OpenElement<DivElement>();

        Should.Throw<InvalidOperationException>(() => builder.SetKey("k"));
    }

    /// <summary>带键元素给其内部的组件划定身份作用域：列表项里的组件随项移动。</summary>
    [Fact]
    public void Should_scope_components_inside_keyed_elements()
    {
        var items = new List<string> { "a", "b" };
        var byItem = new Dictionary<string, List<Probe>>();
        var (host, _) = Mount(b =>
        {
            foreach (var item in items)
            {
                b.OpenElement<LiElement>();
                b.SetKey(item);
                var probe = OpenProbe(b, 1, item);    // 组件本身无键
                b.CloseElement();
                if (!byItem.TryGetValue(item, out var list)) byItem[item] = list = new();
                list.Add(probe);
            }
        });

        items.Reverse();
        host.Refresh();

        byItem["a"][1].ShouldBeSameAs(byItem["a"][0]);
        byItem["b"][1].ShouldBeSameAs(byItem["b"][0]);
    }

    /// <summary>片段里的组件带着定义它的文件的序号；片段调用点作用域让它们不与渲染方自己的组件混淆。</summary>
    [Fact]
    public void Should_not_confuse_fragment_components_with_the_renderers_own()
    {
        var own = new List<Probe>();
        var fromFragment = new List<Probe>();
        var showOwn = false;
        RenderFragment fragment = b => fromFragment.Add(OpenProbe(b, 1, "fragment"));
        var (host, _) = Mount(b =>
        {
            b.AddContent(5, fragment);
            if (showOwn) own.Add(OpenProbe(b, 1, "own"));   // 同类型、同序号，但不在片段作用域里
        });

        showOwn = true;
        host.Refresh();

        fromFragment[1].ShouldBeSameAs(fromFragment[0]);
        own.Single().ShouldNotBeSameAs(fromFragment[0]);
    }

    [Fact]
    public void Should_dispose_old_and_create_new_instance_when_component_type_changes()
    {
        var useProbe = true;
        Probe? probe = null;
        DisposalCounter? other = null;
        var (host, _) = Mount(b =>
        {
            if (useProbe) probe = OpenProbe(b, 1, "probe");
            else
            {
                other = b.OpenComponent<DisposalCounter>(1, null);
                b.CloseComponent();
            }
        });

        useProbe = false;
        host.Refresh();

        probe!.Disposed.ShouldBe(1);
        other.ShouldNotBeNull();
    }

    private sealed class DisposalCounter : ComponentBase
    {
        public int Disposed;
        protected override void OnDispose() => Disposed++;
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<SpanElement>();
            builder.CloseElement();
        }
    }

    /// <summary>
    /// 在 OnInitialized 里订阅服务事件的组件：父级反复刷新后仍只有一个订阅者，出局时退订。
    /// 此前每次父级刷新都新建实例并重新订阅（旧实例靠拆解退订）。
    /// </summary>
    [Fact]
    public void Should_keep_a_single_subscription_across_parent_rerenders()
    {
        var service = new CounterService();
        var show = true;
        var (host, _) = Mount(b =>
        {
            if (!show) return;
            var subscriber = b.OpenComponent<Subscriber>(1, null);
            subscriber.Service = service;
            b.CloseComponent();
        });

        for (int i = 0; i < 5; i++) host.Refresh();
        service.SubscriberCount.ShouldBe(1);

        show = false;
        host.Refresh();
        service.SubscriberCount.ShouldBe(0);
    }

    private sealed class CounterService
    {
        public event Action? Changed;
        public int SubscriberCount => Changed?.GetInvocationList().Length ?? 0;
    }

    private sealed class Subscriber : ComponentBase
    {
        [Parameter] public CounterService? Service { get; set; }
        protected override void OnInitialized() => Service!.Changed += OnChanged;
        protected override void OnDispose() => Service!.Changed -= OnChanged;
        private void OnChanged() { }
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<SpanElement>();
            builder.CloseElement();
        }
    }

    /// <summary>
    /// 进行中的异步初始化跨过父级刷新后在同一实例上完成并重渲染——此前旧实例被丢弃，
    /// 它的完成通知被忽略，新实例又从头开始一次异步初始化。
    /// </summary>
    [Fact]
    public void Should_let_pending_async_initialization_complete_on_the_reused_instance()
    {
        // 没有同步上下文时，异步生命周期的完成通知内联执行，用例无需等待。
        var previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            var gate = new TaskCompletionSource();
            var instances = new List<AsyncLoader>();
            var (host, container) = Mount(b =>
            {
                var loader = b.OpenComponent<AsyncLoader>(1, null);
                loader.Gate = gate.Task;
                b.CloseComponent();
                instances.Add(loader);
            });

            host.Refresh();
            container.FindByClass("loader").Single().TextContent.ShouldBe("loading");

            gate.SetResult();

            instances.ShouldAllBe(l => ReferenceEquals(l, instances[0]));
            instances[0].Loads.ShouldBe(1);
            container.FindByClass("loader").Single().TextContent.ShouldBe("loaded");
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    private sealed class AsyncLoader : ComponentBase
    {
        [Parameter] public Task? Gate { get; set; }
        public int Loads;
        private bool _loaded;

        protected override async Task OnInitializedAsync()
        {
            Loads++;
            await Gate!;
            _loaded = true;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var span = builder.OpenElement<SpanElement>();
            span.Class = "loader";
            builder.AddContent(_loaded ? "loaded" : "loading");
            builder.CloseElement();
        }
    }

    /// <summary>
    /// 写在组件上、匹配不到参数的 <c>@onclick</c> 挂到组件根元素上：复用的实例每次父级刷新都会重新收到
    /// 它，不能越积越多（点一次触发 N 次）。
    /// </summary>
    [Fact]
    public void Should_not_accumulate_host_event_listeners_on_a_reused_instance()
    {
        var clicks = 0;
        var callback = EventCallback.Factory.Create<MouseEventArgs>(new object(), () => clicks++);
        var (host, container) = Mount(b =>
        {
            b.OpenComponent<DisposalCounter>(1, null);
            b.AddComponentParameter(2, "onclick", callback);
            b.CloseComponent();
        });

        host.Refresh();
        host.Refresh();

        var root = container.FindByTagName("span").Single();
        new Miko.Events.EventDispatcher().Dispatch(root, EventTypes.Click, new MouseEventArgs { Target = root });
        clicks.ShouldBe(1);
    }

    /// <summary>
    /// 通过名字设置参数的路径（类型推断的泛型组件）在 CloseComponent 时才挑实例，因而 SetKey 写在
    /// OpenComponent 之后同样有效。
    /// </summary>
    [Fact]
    public void Should_honour_keys_set_after_a_name_based_open()
    {
        var items = new List<string> { "a", "b" };
        var captured = new Dictionary<string, List<object>>();
        var (host, _) = Mount(b =>
        {
            foreach (var item in items)
            {
                b.OpenComponent<Probe>(1);
                b.AddComponentParameter(2, nameof(Probe.Label), item);
                b.SetKey(item);
                b.AddComponentReferenceCapture(3, c =>
                {
                    if (!captured.TryGetValue(item, out var list)) captured[item] = list = new();
                    list.Add(c);
                });
                b.CloseComponent();
            }
        });

        items.Reverse();
        host.Refresh();

        captured["a"][1].ShouldBeSameAs(captured["a"][0]);
        captured["b"][1].ShouldBeSameAs(captured["b"][0]);
        ((Probe)captured["a"][1]).Label.ShouldBe("a");
    }

    /// <summary>
    /// 每次都交回同一个保留元素的组件（MikoPlayer 的形态）：复用不会让拆解回调越串越长，
    /// 出局时恰好拆解一次。
    /// </summary>
    [Fact]
    public void Should_dispose_a_retained_element_component_exactly_once()
    {
        RetainedRoot? retained = null;
        var show = true;
        var (host, _) = Mount(b =>
        {
            if (!show) return;
            retained = b.OpenComponent<RetainedRoot>(1, null);
            b.CloseComponent();
        });

        for (int i = 0; i < 5; i++) host.Refresh();
        retained!.Disposed.ShouldBe(0);

        show = false;
        host.Refresh();
        retained.Disposed.ShouldBe(1);
    }

    private sealed class RetainedRoot : ComponentBase
    {
        private readonly DivElement _root = new() { Class = "retained" };
        public int Disposed;
        protected override void OnDispose() => Disposed++;
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddElement(0, _root);
    }

    /// <summary>
    /// 子组件自己的事件处理器先触发了父级重渲染（父级复用并重建了它），随后处理器结束时不必再
    /// 单独重渲染子组件——那一遍会抹掉祖先构建后置处理给子组件根元素加的类。
    /// </summary>
    [Fact]
    public void Should_keep_classes_an_ancestor_added_when_the_child_rerenders_on_its_own()
    {
        Probe? probe = null;
        var (host, container) = Mount(b =>
        {
            probe = OpenProbe(b, 1, "a");
        });
        // 祖先构建后置处理（IonItem 给子按钮加 button-small 的形态）。
        container.FindByClass("probe").Single().Class += " stamped-by-ancestor";

        var root = container.FindByClass("probe").Single();
        root.OnClick!.Invoke(new MouseEventArgs { Target = root });

        var rebuilt = container.FindByClass("probe").Single();
        rebuilt.ShouldNotBeSameAs(root);
        rebuilt.TextContent.ShouldBe("a:1");
        rebuilt.HasClass("stamped-by-ancestor").ShouldBeTrue();
    }

    /// <summary>
    /// 一个处理器里先后回报两个绑定值（IonCheckbox：先 IndeterminateChanged 再 CheckedChanged）。
    /// 父级的重渲染要等处理器的同步部分结束才进行，否则第一个回调就让父级把尚未回报的旧值写回
    /// 正在运行的同一个实例。
    /// </summary>
    [Fact]
    public void Should_render_the_parent_once_after_a_handler_reported_every_bound_value()
    {
        var parent = new TwoValueParent();
        var container = new DivElement();
        container.AddChild(parent.Build());

        var toggle = container.FindByClass("toggle").Single();
        toggle.OnClick!.Invoke(new MouseEventArgs { Target = toggle });

        parent.Checked.ShouldBeTrue();
        parent.Partial.ShouldBeFalse();
        container.FindByClass("toggle").Single().TextContent.ShouldBe("checked=True partial=False");
    }

    private sealed class TwoValueParent : ComponentBase
    {
        public bool Checked;
        public bool Partial = true;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var child = builder.OpenComponent<TwoValueChild>(1, null);
            child.Checked = Checked;
            child.CheckedChanged = EventCallback.Factory.Create<bool>(this, v => Checked = v);
            child.Partial = Partial;
            child.PartialChanged = EventCallback.Factory.Create<bool>(this, v => Partial = v);
            builder.CloseComponent();
        }
    }

    private sealed class TwoValueChild : ComponentBase
    {
        [Parameter] public bool Checked { get; set; }
        [Parameter] public EventCallback<bool> CheckedChanged { get; set; }
        [Parameter] public bool Partial { get; set; }
        [Parameter] public EventCallback<bool> PartialChanged { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var div = builder.OpenElement<DivElement>();
            div.Class = "toggle";
            div.OnClick = RenderTreeBuilder.ToHandler(EventCallback.Factory.Create<MouseEventArgs>(this, Toggle));
            builder.AddContent($"checked={Checked} partial={Partial}");
            builder.CloseElement();
        }

        private async Task Toggle()
        {
            Checked = !Checked;
            Partial = false;
            await PartialChanged.InvokeAsync(false);
            await CheckedChanged.InvokeAsync(Checked);
        }
    }

    /// <summary>焦点等运行时状态在带键列表重排后跟随对应的项，而不是停在原来的位置上。</summary>
    [Fact]
    public void Should_carry_focus_by_key_when_keyed_elements_are_reordered()
    {
        var items = new List<string> { "a", "b", "c" };
        var (host, container) = Mount(b =>
        {
            foreach (var item in items)
            {
                var input = b.OpenElement<InputElement>();
                b.SetKey(item);
                input.Id = item;
                b.CloseElement();
            }
        });
        container.FindById("b")!.SetState(ElementState.Focus);

        // b 从第 2 位移到第 1 位：按位置配对会把焦点交给此刻排在第 2 位的 c。
        items.Add(items[0]);
        items.RemoveAt(0);
        host.Refresh();

        container.FindById("b")!.HasState(ElementState.Focus).ShouldBeTrue();
        container.FindById("a")!.HasState(ElementState.Focus).ShouldBeFalse();
        container.FindById("c")!.HasState(ElementState.Focus).ShouldBeFalse();
    }
}
