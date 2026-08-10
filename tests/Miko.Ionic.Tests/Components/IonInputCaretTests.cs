using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Routing;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ISSUE-121 端到端回归测试：真实 <see cref="IonInput"/> 在完整应用上下文（Ionic 样式表 +
/// 交互控制器）下的焦点与光标行为。
///
/// <para>现场表现：点击 <c>IonInput</c>，组件确实获得焦点，但不显示光标；输入文字能看到内容，
/// 光标却不跟随。直接用裸 <c>&lt;input/&gt;</c> 一切正常。差别在于 <c>IonInput</c> 的 label 挂了
/// <c>@onclick</c>、input 挂了 <c>@oninput</c>——这些处理器会重渲染整棵子树，把焦点与光标位置
/// 连同旧的元素实例一起丢掉。</para>
///
/// <para>刻意搭真实组件而非等价的手写结构：该问题依赖 <c>IonInput</c> 的处理器与嵌套形状，
/// 手写骨架容易复现不出来。</para>
/// </summary>
public class IonInputCaretTests : IDisposable
{
    private const float W = 390;
    private const float H = 844;

    private readonly SKBitmap _bitmap = new((int)W, (int)H);
    private readonly SKCanvas _canvas;

    public IonInputCaretTests()
    {
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private sealed class InputPage : Miko.Components.ComponentBase
    {
        protected override void BuildRenderTree(Miko.Components.RenderTreeBuilder builder)
        {
            builder.OpenComponent<IonApp>(0);
            builder.AddAttribute(1, "ChildContent", (Miko.Components.RenderFragment)(b =>
            {
                b.OpenComponent<IonPage>(0);
                b.AddAttribute(1, "ChildContent", (Miko.Components.RenderFragment)(b2 =>
                {
                    b2.OpenComponent<IonContent>(0);
                    b2.AddAttribute(1, "ChildContent", (Miko.Components.RenderFragment)(b3 =>
                    {
                        b3.OpenComponent<IonInput>(0);
                        b3.CloseComponent();
                    }));
                    b2.CloseComponent();
                }));
                b.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private MikoAppContext BuildApp()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRouter(router => router.MapRoute("/", typeof(InputPage)));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        app.Engine.Render(_canvas);
        return app;
    }

    /// <summary>当前在树中的原生 input（每次重渲染都是新实例，必须重新查找）。</summary>
    private static InputElement NativeInput(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("native-input").OfType<InputElement>().Single();

    /// <summary>探测出一个命中原生 input 的坐标（LayoutBox 是 internal，只能经公开命中测试找）。</summary>
    private static (float x, float y) FindInputPoint(MikoAppContext app)
    {
        for (float y = 2; y < H; y += 2)
        for (float x = 2; x < W; x += 2)
        {
            if (app.Engine.HitTest(x, y) is InputElement hit && hit.HasClass("native-input"))
                return (x, y);
        }
        throw new InvalidOperationException("未能在页面上命中 .native-input");
    }

    [Fact]
    public void Click_FocusesTheLiveNativeInput()
    {
        var app = BuildApp();
        var (x, y) = FindInputPoint(app);

        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);

        // 关键断言：焦点落在<b>在场</b>的 input 上。修复前 label 的 @onclick 已经重建了子树，
        // 焦点留在脱离树的旧实例上，渲染遍历新实例，于是光标不画。
        NativeInput(app).HasState(ElementState.Focus).ShouldBeTrue();
    }

    [Fact]
    public void Typing_CaretFollowsContent()
    {
        var app = BuildApp();
        var (x, y) = FindInputPoint(app);

        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);
        app.Engine.Render(_canvas);

        app.Controller.OnTextInput("hello");

        var live = NativeInput(app);
        live.Value.ShouldBe("hello");
        // 修复前恒为 0：@oninput 每次都重建子树，光标位置随旧实例被丢弃。
        live.CursorPosition.ShouldBe(5);
        live.HasState(ElementState.Focus).ShouldBeTrue();

        // 输入必须调度重绘，否则稳态空闲会跳过帧生产，光标仍然不出现（ISSUE-104）。
        app.Engine.HasPendingVisualWork.ShouldBeTrue();
    }

    [Fact]
    public void ArrowAndBackspace_EditTheLiveInput()
    {
        var app = BuildApp();
        var (x, y) = FindInputPoint(app);

        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);
        app.Controller.OnTextInput("abc");
        app.Engine.Render(_canvas);

        app.Controller.OnKeyDown(MikoKey.Left, MikoKeyModifiers.None);
        NativeInput(app).CursorPosition.ShouldBe(2);

        app.Controller.OnKeyDown(MikoKey.Backspace, MikoKeyModifiers.None);

        var live = NativeInput(app);
        live.Value.ShouldBe("ac");   // 光标在 2，删除其前的 'b'
        live.CursorPosition.ShouldBe(1);
    }
}
