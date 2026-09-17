using Miko.Core.DomElements;
using Miko.Testing;
using Shouldly;

namespace Miko.Razor.Tests;

/// <summary>
/// ISSUE-136 端到端验证：Razor 编译器发射<b>强类型</b>构建调用，而不再是「序号 + 字符串名」。
///
/// <para>旧的发射形态让运行时重做一遍编译期已知的事：标签名查字典取工厂委托、属性名过约
/// 70 分支的 switch + 类型测试链、组件参数走 <c>GetProperty</c> + <c>SetValue</c> 反射（无缓存，
/// 值类型还要装箱）。新形态直接写出具体元素/组件类型与属性赋值。</para>
///
/// <para>这里既断言<b>发射出来的代码形状</b>（读生成的 .g.cs——靠行为断言无法区分两条路径，
/// 它们结果相同），也断言<b>行为不变</b>（渲染出的 DOM 与事件照旧工作）。</para>
/// </summary>
public class RazorTypedEmitTests : IDisposable
{
    private readonly TestContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    // -----------------------------------------------------------------
    // 发射的代码形状
    // -----------------------------------------------------------------

    [Fact]
    public void Elements_AreOpenedWithConcreteType()
    {
        var code = GeneratedCode("TypedEmitFixture");

        code.ShouldContain("OpenElement<global::Miko.Core.DomElements.DivElement>()");
        code.ShouldContain("OpenElement<global::Miko.Core.DomElements.ButtonElement>()");
        code.ShouldContain("OpenElement<global::Miko.Core.DomElements.SpanElement>()");
        code.ShouldContain("OpenElement<global::Miko.Core.DomElements.InputElement>()");

        // 旧的字符串标签路径不应再出现在发射结果里。
        code.ShouldNotContain("OpenElement(0, \"div\")");
        code.ShouldNotContain("OpenElement(0, \"button\")");
    }

    [Fact]
    public void ElementAttributes_AreAssignedDirectly()
    {
        var code = GeneratedCode("TypedEmitFixture");

        // class 是直接属性赋值，不再经 AddAttribute 的名字 switch。
        code.ShouldContain(".Class = \"typed-host\"");
        code.ShouldContain(".Class = \"typed-button\"");

        // style 走 SetStyle 辅助方法：它有 Style 与 object? 两个重载，后者保留了旧 switch
        // 「非 Style 值静默忽略」的行为（个别组件传的是 CSS 字符串）。
        code.ShouldContain("global::Miko.Components.RenderTreeBuilder.SetStyle(");

        code.ShouldNotContain("AddAttribute(0, \"class\"");
        code.ShouldNotContain("\"class\",");
        code.ShouldNotContain("\"style\",");
    }

    [Fact]
    public void LiteralInputType_IsFoldedToEnumConstant()
    {
        var code = GeneratedCode("TypedEmitFixture");

        // type="checkbox" 是字面量，编译期即可折叠，省掉运行时 ToLowerInvariant + switch。
        code.ShouldContain(".Type = global::Miko.Common.InputType.Checkbox;");
        code.ShouldNotContain("ParseInputType(\"checkbox\")");
    }

    [Fact]
    public void EventHandler_IsAssignedToTypedSlot()
    {
        var code = GeneratedCode("TypedEmitFixture");

        // @onclick 写进 Element.OnClick 槽位（经 ToHandler 包装 EventCallback）。
        code.ShouldContain(".OnClick = global::Miko.Components.RenderTreeBuilder.ToHandler(");
        code.ShouldNotContain("\"onclick\",");
    }

    [Fact]
    public void ComponentParameters_AreAssignedDirectly()
    {
        var code = GeneratedCode("TypedEmitFixture");

        code.ShouldContain("OpenComponent<global::Miko.Razor.Tests.");
        code.ShouldContain(".Label = \"child\"");
        // 值类型参数直接赋给 int 属性。RuntimeHelpers.TypeCheck<T> 是恒等函数，只为编译期
        // 类型检查而存在，不产生装箱。
        code.ShouldContain(".Count = global::Miko.Components.CompilerServices.RuntimeHelpers.TypeCheck<global::System.Int32>(");
        code.ShouldContain(".ChildContent = (global::Miko.Components.RenderFragment)(");

        // 反射路径（按名字 GetProperty + SetValue）不应再被发射。
        code.ShouldNotContain("AddComponentParameter(");
        // 旧的 AddAttribute 路径会对值类型显式装箱；新路径不再有 (object) 强转。
        code.ShouldNotContain("(object)(");
    }

    // -----------------------------------------------------------------
    // 行为不变
    // -----------------------------------------------------------------

    [Fact]
    public void TypedEmit_RendersExpectedTree()
    {
        var cut = _ctx.Render<TypedEmitFixture>();

        var host = cut.Root.ShouldBeOfType<DivElement>();
        host.HasClass("typed-host").ShouldBeTrue();
        // style="@_hostStyle" 传的是 Style 对象，应落到 Element.Style 上。
        host.Style.ShouldNotBeNull();

        cut.Root.FindByClass("typed-button").ShouldHaveSingleItem().ShouldBeOfType<ButtonElement>();
        cut.Root.FindByClass("typed-label").ShouldHaveSingleItem().ShouldBeOfType<SpanElement>();
    }

    [Fact]
    public void TypedEmit_LiteralInputType_ReachesElement()
    {
        var cut = _ctx.Render<TypedEmitFixture>();

        var input = cut.Root.FindByClass("typed-check").ShouldHaveSingleItem().ShouldBeOfType<InputElement>();
        input.Type.ShouldBe(Miko.Common.InputType.Checkbox);
    }

    [Fact]
    public void TypedEmit_ComponentParameters_ReachChild()
    {
        var cut = _ctx.Render<TypedEmitFixture>();

        cut.Root.FindByClass("typed-child-label").ShouldHaveSingleItem()
            .TextContent.ShouldBe("child");
        cut.Root.FindByClass("typed-child-count").ShouldHaveSingleItem()
            .TextContent.ShouldBe("7");
        // ChildContent 也要送达（作为组件参数直接赋值）。
        cut.Root.FindByClass("typed-slot").ShouldHaveSingleItem()
            .TextContent.ShouldBe("slot");
    }

    [Fact]
    public void TypedEmit_EventHandler_StillFires()
    {
        var cut = _ctx.Render<TypedEmitFixture>();

        var label = cut.Root.FindByClass("typed-label").ShouldHaveSingleItem();
        label.TextContent.ShouldBe("0");

        var button = cut.Root.FindByClass("typed-button").ShouldHaveSingleItem();
        button.OnClick.ShouldNotBeNull();
        button.OnClick!.Invoke(new Miko.Events.MouseEventArgs { Target = button });

        // 处理器经 EventCallback 触发，完成后自动重渲染（见 EventCallbackHelper）。
        cut.Root.FindByClass("typed-label").ShouldHaveSingleItem().TextContent.ShouldBe("1");
    }

    // -----------------------------------------------------------------

    /// <summary>
    /// Reads the .g.cs the Razor generator wrote for a fixture. The project sets
    /// <c>EmitCompilerGeneratedFiles</c> precisely so compiler output stays inspectable
    /// (the same mechanism ISSUE-115's @bind tests rely on).
    /// </summary>
    private static string GeneratedCode(string fixtureName)
    {
        var root = FindRepoRoot();
        var generated = Path.Combine(root, "tests", "Miko.Razor.Tests", "obj", "GeneratedFiles");

        Directory.Exists(generated).ShouldBeTrue(
            $"Generated sources not found at {generated}. EmitCompilerGeneratedFiles must stay enabled.");

        var file = Directory.EnumerateFiles(generated, $"{fixtureName}_razor.g.cs", SearchOption.AllDirectories)
            .FirstOrDefault();
        file.ShouldNotBeNull($"No generated file for {fixtureName} under {generated}.");

        return File.ReadAllText(file!);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "miko.slnx")))
            dir = dir.Parent;

        dir.ShouldNotBeNull("Could not locate the repository root (miko.slnx).");
        return dir!.FullName;
    }
}
