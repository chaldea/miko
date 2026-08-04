using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Testing;
using Shouldly;

namespace Miko.Razor.Tests;

/// <summary>
/// ISSUE-115 端到端验证：<c>@bind-Xxx</c> 双向绑定。遵循 Blazor 契约的组件
/// （<c>T Xxx</c> 配 <c>EventCallback&lt;T&gt; XxxChanged</c>）应当支持 <c>@bind-Xxx</c>，
/// 组件回写新值时父组件的字段随之更新。
///
/// 历史缺陷（三处叠加，任一处都会让 <c>@bind</c> 完全失效）：
/// <list type="number">
///   <item>
///     Miko 运行时缺少编译器探测用的哨兵类型（<c>BindConverter</c> /
///     <c>BindElementAttribute</c> / <c>BindInputElementAttribute</c>），
///     <c>BindTagHelperDescriptorProvider</c> 因此直接 return，从不合成任何
///     <c>@bind-*</c> tag helper。
///   </item>
///   <item>
///     即使哨兵类型齐备，<c>TargetAssembly</c> 判断也会让"组件 <c>Foo</c>/<c>FooChanged</c>
///     配对"（case #4）只在处理声明 <c>BindConverter</c> 的程序集时运行；而该 case 依赖
///     <em>当前</em>程序集的组件描述符，于是消费方项目永远拿不到。
///   </item>
///   <item>
///     <c>MightContainTagHelpers</c> 仍沿用 Blazor 的 <c>Microsoft.AspNetCore.*</c> 前缀判断，
///     把 <c>Miko</c> 程序集整体跳过，元素级 <c>@bind</c> 映射（<c>BindAttributes</c>）无从发现。
///   </item>
/// </list>
/// 失效时的症状：<c>@bind-Checked</c> 原样作为名为 <c>"@bind-Checked"</c> 的字面属性传下去，
/// 运行时反射找不到同名属性而被静默丢弃——不报错，值也不回写。
/// </summary>
public class RazorTwoWayBindingTests : IDisposable
{
    private readonly TestContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    // -----------------------------------------------------------------
    // 组件级 @bind-Xxx（case #4：Foo / FooChanged 配对）
    // -----------------------------------------------------------------

    [Fact]
    public void BindToComponentParameter_PassesInitialValue_ToChild()
    {
        // 绑定必须双向：先确认"下行"——父字段的初始值到达子组件。
        var cut = _ctx.Render<BindLocalComponentFixture>();

        var target = cut.Root.FindByClass("bind-target").ShouldHaveSingleItem();
        target.HasClass("is-unchecked").ShouldBeTrue();
    }

    [Fact]
    public void BindToComponentParameter_WritesBack_WhenChildRaisesChanged()
    {
        // 关键断言（即 issue 描述的场景）：子组件回写后父组件字段随之改变。
        var component = new BindLocalComponentFixture();
        var cut = _ctx.RenderElement(component.Build());

        component.CheckedValue.ShouldBeFalse();

        var target = cut.Root.FindByClass("bind-target").ShouldHaveSingleItem();
        target.OnClick!.Invoke(new MouseEventArgs { Target = target });

        component.CheckedValue.ShouldBeTrue();
    }

    [Fact]
    public void BindToComponentParameter_PassesInitialValue_ForNonBooleanAndGenericValues()
    {
        // TValue 由编译器推断（走 RuntimeHelpers.CreateInferredEventCallback），
        // 泛型组件与非 bool 类型同样要能绑定；绑定失效时子组件收不到值，这里会是空串。
        var component = new BindGenericComponentFixture();
        var cut = _ctx.RenderElement(component.Build());

        component.NameValue.ShouldBe("start");

        var target = cut.Root.FindByClass("value-target").ShouldHaveSingleItem();
        TextOf(target).ShouldBe("start");
    }

    // -----------------------------------------------------------------
    // 元素级 @bind（case #1-#3：BindAttributes 驱动的 value/onchange 映射）
    // -----------------------------------------------------------------

    [Fact]
    public void BindToCheckbox_ProjectsBoundValue_OntoCheckedState()
    {
        // <input type="checkbox" @bind="_flag" /> 应降级为 checked 属性 + onchange binder。
        var cut = _ctx.Render<BindElementFixture>();

        var checkbox = cut.Root.FindByClass("cb").ShouldHaveSingleItem().ShouldBeOfType<InputElement>();
        checkbox.Type.ShouldBe(InputType.Checkbox);

        // 绑定值确实投影到元素上（字段初始为 true）。若 checked 走了无操作的
        // AddAttribute(int, string, bool) 重载，这里会是 false。
        checkbox.Checked.ShouldBeTrue();

        // onchange 处理器已挂上——binder 存在，change 事件才能回写。
        checkbox.OnChange.ShouldNotBeNull();
    }

    [Fact]
    public void BindToCheckbox_WritesBack_OnChangeEvent()
    {
        var component = new BindElementFixture();
        var cut = _ctx.RenderElement(component.Build());

        component.Flag.ShouldBeTrue();

        var checkbox = cut.Root.FindByClass("cb").ShouldHaveSingleItem().ShouldBeOfType<InputElement>();

        // 模拟 MikoInteractionController：先翻转元素状态，再派发 change。
        checkbox.Checked = false;
        checkbox.OnChange!.Invoke(new ChangeEventArgs { Target = checkbox });

        component.Flag.ShouldBeFalse();
    }

    [Fact]
    public void BindToTextInput_ProjectsValue_AndWritesBackOnChange()
    {
        var component = new BindElementFixture();
        var cut = _ctx.RenderElement(component.Build());

        var textbox = cut.Root.FindByClass("tb").ShouldHaveSingleItem().ShouldBeOfType<InputElement>();
        textbox.Value.ShouldBe("initial");

        textbox.Value = "edited";
        textbox.OnChange!.Invoke(new ChangeEventArgs { Target = textbox });

        component.Text.ShouldBe("edited");
    }

    [Fact]
    public void BindToTextArea_WritesBack_OnChange()
    {
        var component = new BindElementFixture();
        var cut = _ctx.RenderElement(component.Build());

        var textArea = cut.Root.FindByClass("ta").ShouldHaveSingleItem().ShouldBeOfType<TextAreaElement>();

        textArea.Value = "note body";
        textArea.OnChange!.Invoke(new ChangeEventArgs { Target = textArea });

        component.Note.ShouldBe("note body");
    }

    private static string TextOf(Element element)
    {
        var text = "";
        foreach (var child in element.Children)
        {
            if (child is TextNode textNode)
            {
                text += textNode.Text;
            }
        }
        return text;
    }
}
