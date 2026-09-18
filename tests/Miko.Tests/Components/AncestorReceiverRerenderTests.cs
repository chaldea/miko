using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Shouldly;

namespace Miko.Tests.Components;

/// <summary>
/// ISSUE-121 续：当子组件的 <c>ValueChanged</c> 回调的 receiver 是<b>祖先</b>（<c>@bind-Value</c>
/// 生成的正是这种形态），一次按键会引发<b>两次</b>重渲染：
/// <list type="number">
/// <item>祖先的 StateHasChanged（回调 receiver 是它）——这次会把子组件的整棵子树换成新实例；</item>
/// <item>子组件自己的 StateHasChanged（它的 <c>@oninput</c> 回调 receiver 是它自己）——此时它的
/// <c>_rootElement</c> 已被上一步换掉，脱离了 DOM 树。</item>
/// </list>
/// <para>第二次重渲染作用在已脱离的旧树上，却仍会写 <c>SupersededBy</c> 转发指针，把控制器的
/// 焦点引用引向一棵看不见的树——光标于是停在中途不再跟随。</para>
/// </summary>
public class AncestorReceiverRerenderTests
{
    /// <summary>子组件：自己持有 @oninput 处理器，并把新值经 ValueChanged 上报。</summary>
    private sealed class FieldComponent : ComponentBase
    {
        [Parameter] public string? Value { get; set; }
        [Parameter] public EventCallback<string?> ValueChanged { get; set; }

        public int SelfRenders { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            SelfRenders++;
            var __e1 = builder.OpenElement<DivElement>();
            __e1.Class = "field";
            var __e2 = builder.OpenElement<InputElement>();
            __e2.Class = "native";
            global::Miko.Components.RenderTreeBuilder.SetInputValue(__e2, Value);
            __e2.OnInput = global::Miko.Components.RenderTreeBuilder.ToHandler(
                EventCallback.Factory.Create<InputEventArgs>(this, HandleInput));
            builder.CloseElement();
            builder.CloseElement();
        }

        private async Task HandleInput(InputEventArgs args)
        {
            Value = args.Data;
            await ValueChanged.InvokeAsync(args.Data);
        }
    }

    /// <summary>宿主页：以自身为 receiver 订阅 ValueChanged（等价于 @bind-Value）。</summary>
    private sealed class HostPage : ComponentBase
    {
        public string? Bound { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var __e3 = builder.OpenElement<DivElement>();
            __e3.Class = "page";
            var __c4 = builder.OpenComponent<FieldComponent>();
            __c4.Value = Bound;
            __c4.ValueChanged = 
                EventCallback.Factory.Create<string?>(this, v => Bound = v);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }

    private static InputElement LiveInput(Element root)
        => root.FindByClass("native").OfType<InputElement>().Single();

    [Fact]
    public void AncestorRerender_LeavesTheLiveInputReachable()
    {
        var page = new HostPage();
        var root = page.Build();

        var input = LiveInput(root);
        input.SetState(ElementState.Focus);
        input.InsertText("a");

        // 触发与真实按键相同的路径：元素的 oninput → 子组件 HandleInput → 祖先 ValueChanged。
        input.OnInput!.Invoke(new InputEventArgs { Target = input, Data = "a" });

        // 值确实回流到了页面字段（双向绑定成立）。
        page.Bound.ShouldBe("a");

        // 关键：从旧实例出发沿转发链走到的，必须是<b>当前树里</b>的那个 input。
        // 修复前子组件的第二次重渲染把链指向了一棵已脱离的树。
        var live = LiveInput(root);
        input.ResolveSuperseded().ShouldBeSameAs(live);
    }

    /// <summary>兄弟输入框：无任何 value 声明，其文本只活在元素上。</summary>
    private sealed class TwoFieldPage : ComponentBase
    {
        public string? Bound { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var __e5 = builder.OpenElement<DivElement>();
            __e5.Class = "page";

            var __e6 = builder.OpenElement<InputElement>();
            __e6.Class = "bound";
            global::Miko.Components.RenderTreeBuilder.SetInputValue(__e6, Bound);
            builder.CloseElement();

            // 无 value 属性声明。
            var __e7 = builder.OpenElement<InputElement>();
            __e7.Class = "unbound";
            builder.CloseElement();

            builder.CloseElement();
        }

        public void ForceRender() => StateHasChanged();
    }

    private static InputElement ByClass(Element root, string cls)
        => root.FindByClass(cls).OfType<InputElement>().Single();

    [Fact]
    public void AncestorRerender_KeepsTextOfAnUndeclaredSibling()
    {
        // 问题 2 的最小化：页面重渲染时，没有 value 声明的输入框其文本必须保留
        // ——对齐浏览器：缺席的 value 属性不会清空输入框。
        var page = new TwoFieldPage();
        var root = page.Build();

        ByClass(root, "unbound").InsertText("typed");
        page.Bound = "b";

        page.ForceRender();

        ByClass(root, "unbound").Value.ShouldBe("typed");
        ByClass(root, "bound").Value.ShouldBe("b");
    }

    [Fact]
    public void AncestorRerender_ExplicitEmptyDeclarationStillClears()
    {
        // 显式声明空串（如清除按钮）是组件的明确意图，必须照旧生效。
        var page = new TwoFieldPage();
        var root = page.Build();

        ByClass(root, "bound").InsertText("typed");
        page.Bound = string.Empty;

        page.ForceRender();

        ByClass(root, "bound").Value.ShouldBe(string.Empty);
    }

    [Fact]
    public void AncestorRerender_CarriesCaretPositionToTheLiveInput()
    {
        var page = new HostPage();
        var root = page.Build();

        var input = LiveInput(root);
        input.SetState(ElementState.Focus);
        input.InsertText("ab");
        input.CursorPosition.ShouldBe(2);

        input.OnInput!.Invoke(new InputEventArgs { Target = input, Data = "ab" });

        var live = LiveInput(root);
        live.Value.ShouldBe("ab");
        live.CursorPosition.ShouldBe(2);
        live.HasState(ElementState.Focus).ShouldBeTrue();
    }
}
