using Miko.Core.DomElements;
using Miko.Events;
using Miko.Ionic.Tests.Fixtures;
using Shouldly;

namespace Miko.Ionic.Tests;

/// <summary>
/// ISSUE-136 冒烟验证：一整页真实 Ionic 组件树经强类型构建路径渲染。
///
/// <para>逐组件的单测覆盖不到「整页真实嵌套」这一层——而组件库正是在这个规模上才暴露问题
/// （见 style-matching-rule-index 与 verify-with-real-component-structure 的教训）。这个
/// fixture 一次渲染里同时包含：带参数的嵌套组件、ChildContent 与具名插槽、循环产生的列表、
/// 事件处理器、字面量 <c>type</c> 的 input、以及与组件交错的普通标记元素。</para>
/// </summary>
public class SmokePageTests : IonicComponentTestBase
{
    [Fact]
    public void FullPage_RendersCompleteTree()
    {
        var cut = Context.Render<SmokePageFixture>();

        // 页面骨架到位。
        cut.Root.FindByClass("ion-app").ShouldNotBeEmpty();
        cut.Root.FindByClass("ion-page").ShouldNotBeEmpty();
        cut.Root.FindByClass("ion-header").ShouldNotBeEmpty();
        cut.Root.FindByClass("ion-content").ShouldNotBeEmpty();

        // 与组件交错的普通标记元素照旧出现在正确位置。
        cut.Root.FindByClass("smoke-body").ShouldHaveSingleItem().ShouldBeOfType<DivElement>();
        cut.Root.FindByClass("smoke-note").ShouldHaveSingleItem()
            .ShouldBeOfType<ParagraphElement>()
            .TextContent.ShouldBe("plain markup between components");
    }

    [Fact]
    public void FullPage_LoopedItems_CarryTheirParameters()
    {
        var cut = Context.Render<SmokePageFixture>();

        // 循环里的三个 IonItem 各自拿到自己的下标（组件参数直接赋值）。
        // 注意文本是 "Item0" 而非 "Item 0"：Miko 有意丢弃 <pre> 之外的纯空白标记内容
        // （见 ComponentRuntimeNodeWriter.WriteHtmlContent 与 ISSUE-098），因此
        // "Item @index" 中间那段空白不进 DOM。这是既有行为，与本次改动无关。
        var labels = cut.Root.FindByClass("ion-label");
        labels.Count.ShouldBe(3);
        labels.Select(l => l.TextContent).ShouldBe(["Item0", "Item1", "Item2"]);

        // IonBadge 的 Color 参数也要送达（体现为 ion-color-primary 类）。
        var badges = cut.Root.FindByClass("ion-badge");
        badges.Count.ShouldBe(3);
        badges.ShouldAllBe(b => b.HasClass("ion-color-primary"));
    }

    [Fact]
    public void FullPage_ButtonParameters_ReachTheComponent()
    {
        var cut = Context.Render<SmokePageFixture>();

        var button = cut.Root.FindByClass("ion-button").ShouldHaveSingleItem();
        // Color="primary" 与 Size="small" 都是字符串参数，经强类型赋值抵达组件。
        button.HasClass("ion-color-primary").ShouldBeTrue();
        button.HasClass("button-small").ShouldBeTrue();
    }

    [Fact]
    public void FullPage_LiteralInputType_ReachesElement()
    {
        var cut = Context.Render<SmokePageFixture>();

        var input = cut.Root.FindByClass("smoke-check").ShouldHaveSingleItem()
            .ShouldBeOfType<InputElement>();
        // type="checkbox" 是字面量，编译期已折叠成枚举常量。
        input.Type.ShouldBe(Miko.Common.InputType.Checkbox);
    }

    [Fact]
    public void FullPage_ButtonClick_InvokesHandlerThroughTypedSlot()
    {
        var taps = 0;
        var cut = Context.Render<SmokePageFixture>(
            p => p.Add(nameof(SmokePageFixture.OnTapped), (Action)(() => taps++)));

        cut.GetTextContent().ShouldContain("Tapped0");

        // IonButton 暴露的是 EventCallback 参数 OnClick（不是 DOM 的 @onclick），页面通过它
        // 接收点击；组件内部再把自己原生 button 的 @onclick 转发过来
        // （见 ionic-native-ua-reset）。点击原生 button 应当一路抵达页面的处理器。
        var native = cut.Root.FindByClass("button-native").ShouldHaveSingleItem();
        native.OnClick.ShouldNotBeNull();

        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        // 事件穿过 IonButton 的转发抵达页面组件的处理器。
        // （页面重渲染会换掉整棵子树，cut.Root 仍指向旧根，故不在此断言 DOM 文本——
        //  重渲染本身由 Miko.Tests 的 StateHasChanged 用例覆盖。）
        taps.ShouldBe(1);
    }

    [Fact]
    public void FullPage_LayoutProducesNonZeroGeometry()
    {
        // 渲染成功不等于布局成功：确认整页真的排出了非零尺寸（空树也能"渲染通过"）。
        var cut = Context.Render<SmokePageFixture>();

        var body = cut.Root.FindByClass("smoke-body").ShouldHaveSingleItem();
        var box = cut.GetBoxModel(body);
        box.ShouldNotBeNull();
        box!.MarginBox.Width.ShouldBeGreaterThan(0);
        box.MarginBox.Height.ShouldBeGreaterThan(0);
    }
}
