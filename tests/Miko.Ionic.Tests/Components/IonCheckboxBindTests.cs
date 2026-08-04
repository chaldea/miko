using Miko.Events;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ISSUE-115：<c>@bind-Checked</c> 绑定到 <c>IonCheckbox</c>（跨程序集）。
/// <para>
/// 这是 issue 报告的原始场景。之所以单列一组测试：case #4（组件 <c>Foo</c>/<c>FooChanged</c>
/// 配对）依赖<em>当前处理程序集</em>的组件描述符，因此"组件与消费方同处一个程序集"能通过、
/// 而"组件来自引用程序集"仍然失效是完全可能的——两条路径必须分别覆盖。
/// </para>
/// </summary>
public class IonCheckboxBindTests : IonicComponentTestBase
{
    [Fact]
    public void BindChecked_PassesInitialValue_ToIonCheckbox()
    {
        var cut = Context.Render<IonCheckboxBindFixture>();

        // 未选中：host 不应带 checkbox-checked 类。
        var host = cut.FindByClass("ion-checkbox").ShouldHaveSingleItem();
        host.HasClass("checkbox-checked").ShouldBeFalse();
    }

    [Fact]
    public void BindChecked_WritesBackToParentField_WhenToggled()
    {
        // 绑定失效时 @bind-Checked 会作为名为 "@bind-Checked" 的字面属性被静默丢弃，
        // 父字段永远保持 false —— 这正是本测试要拦住的回归。
        var component = new IonCheckboxBindFixture();
        var cut = Context.RenderElement(component.Build());

        component.CheckedValue.ShouldBeFalse();

        var wrapper = cut.FindByClass("checkbox-wrapper").ShouldHaveSingleItem();
        wrapper.OnClick!.Invoke(new MouseEventArgs { Target = wrapper });

        component.CheckedValue.ShouldBeTrue();
    }
}
