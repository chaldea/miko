using Miko.Core.DomElements;
using Miko.Testing;
using Shouldly;

namespace Miko.Razor.Tests;

/// <summary>
/// ISSUE-100 端到端验证：组件基类 <c>ComponentBase</c> 声明的 <c>[Parameter] ChildContent</c>
/// 对派生组件依然有效——即使派生组件又声明了别的 <c>RenderFragment</c> 参数（如 Start / End），
/// 也可以用显式 <c>&lt;ChildContent&gt;</c> 元素设置默认插槽内容，而无需在派生组件中重复声明
/// <c>ChildContent</c>。
///
/// 历史缺陷：<c>ComponentTagHelperDescriptorProvider.GetProperties</c> 沿用了 Blazor 的优化，
/// 在遍历继承链遇到 <c>ComponentBase</c> 时提前 break（Blazor 的 ComponentBase 无 [Parameter]）。
/// 但 Miko 的 <c>ComponentBase</c> 声明了 <c>ChildContent</c>，被跳过后编译器不会为其合成
/// child-content tag helper，导致 <c>&lt;ChildContent&gt;</c> 被视为未知内容并报 RZ9996。
/// </summary>
public class RazorInheritedChildContentTests : IDisposable
{
    private readonly TestContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void ExplicitChildContent_AlongsideNamedFragments_Compiles_AndRenders()
    {
        // 若此 fixture 不能编译，测试工程整体无法构建——能运行到断言即已证明 RZ9996 不再出现。
        var cut = _ctx.Render<InheritedChildContentFixture>();

        var root = cut.Root.ShouldBeOfType<DivElement>();
        root.HasClass("mf-button").ShouldBeTrue();

        // 三个插槽各自的内容都被正确投影：Start / 继承而来的 ChildContent / End。
        var start = root.FindByClass("mf-start").ShouldHaveSingleItem();
        Text(start).ShouldBe("S");

        var body = root.FindByClass("mf-body").ShouldHaveSingleItem();
        Text(body).ShouldBe("Body");

        var end = root.FindByClass("mf-end").ShouldHaveSingleItem();
        Text(end).ShouldBe("E");
    }

    private static string Text(Miko.Core.Element element)
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
