using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ISSUE-121 续：<c>@bind-Value</c> 下的光标与兄弟输入框状态。
///
/// <para><c>@bind-Value</c> 让<b>页面</b>成为 EventCallback 的 receiver（生成的代码是
/// <c>Factory.Create&lt;string&gt;(this, …)</c>，<c>this</c> 即页面），于是每次按键重渲染的是
/// <b>整个页面</b>，而不只是那个 IonInput。两个现场表现由此而来：</para>
/// <list type="number">
/// <item>带 <c>@bind-Value</c> 的输入框光标不跟随内容。</item>
/// <item>在无 bind 的第二个输入框里打字后，回到第一个（带 bind 的）输入框打字，第二个会被清空。</item>
/// </list>
/// <para>结构照搬现场：一个 IonItem/IonInput 带 bind，另一个不带。</para>
/// </summary>
public class IonInputBindCaretTests : IDisposable
{
    private const float W = 390;
    private const float H = 844;

    private readonly SKBitmap _bitmap = new((int)W, (int)H);
    private readonly SKCanvas _canvas;

    public IonInputBindCaretTests()
    {
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    /// <summary>
    /// 手写出编译器为 <c>@bind-Value="_firstName"</c> 生成的等价形态：Value 参数 +
    /// 以<b>页面</b>为 receiver 的 ValueChanged 回调（见 Demo_razor.g.cs）。
    /// 第二个 IonInput 不带 bind，用于复现问题 2。
    /// </summary>
    private sealed class BindPage : ComponentBase
    {
        public string? FirstName { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var __c3 = builder.OpenComponent<IonApp>();
            __c3.ChildContent = (RenderFragment)(b =>
            {
                var __c4 = b.OpenComponent<IonPage>();
                __c4.ChildContent = (RenderFragment)(b2 =>
                {
                    var __c5 = b2.OpenComponent<IonContent>();
                    __c5.ChildContent = (RenderFragment)(b3 =>
                    {
                        var __c6 = b3.OpenComponent<IonList>();
                        __c6.ChildContent = (RenderFragment)(b4 =>
                        {
                            // 第一项：带 @bind-Value（receiver = 页面）。
                            var __c7 = b4.OpenComponent<IonItem>();
                            __c7.ChildContent = (RenderFragment)(b5 =>
                            {
                                var __c1 = b5.OpenComponent<IonInput>();
                                __c1.Label = "First Name";
                                __c1.LabelPlacement = "stacked";
                                __c1.Class = "bound";
                                __c1.Value = FirstName;
                                __c1.ValueChanged = 
                                    EventCallback.Factory.Create<string?>(this, v => FirstName = v);
                                b5.CloseComponent();
                            });
                            b4.CloseComponent();

                            // 第二项：无 bind（IonInput 自己持有值）。
                            var __c8 = b4.OpenComponent<IonItem>();
                            __c8.ChildContent = (RenderFragment)(b5 =>
                            {
                                var __c2 = b5.OpenComponent<IonInput>();
                                __c2.Label = "Last Name";
                                __c2.LabelPlacement = "stacked";
                                __c2.Class = "unbound";
                                b5.CloseComponent();
                            });
                            b4.CloseComponent();
                        });
                        b3.CloseComponent();
                    });
                    b2.CloseComponent();
                });
                b.CloseComponent();
            });
            builder.CloseComponent();
        }
    }

    private MikoAppContext BuildApp()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRouter(router => router.MapRoute("/", typeof(BindPage)));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        app.Engine.Render(_canvas);
        return app;
    }

    /// <summary>按宿主 ion-input 的 class 取当前在场的那个原生 input（每次重渲染都是新实例）。</summary>
    private static InputElement NativeInput(MikoAppContext app, string hostClass)
        => app.Engine.GetRoot()!
            .FindByClass(hostClass)
            .Single()
            .FindByClass("native-input")
            .OfType<InputElement>()
            .Single();

    /// <summary>探测一个命中指定输入框的坐标（LayoutBox 是 internal，只能经公开命中测试找）。</summary>
    private static (float x, float y) FindPoint(MikoAppContext app, string hostClass)
    {
        var target = NativeInput(app, hostClass);
        for (float y = 2; y < H; y += 2)
        for (float x = 2; x < W; x += 2)
        {
            if (ReferenceEquals(app.Engine.HitTest(x, y), target))
                return (x, y);
        }
        throw new InvalidOperationException($"未能命中 .{hostClass} 内的 .native-input");
    }

    private static void ClickAndType(MikoAppContext app, string hostClass, string text, SKCanvas canvas)
    {
        var (x, y) = FindPoint(app, hostClass);
        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);
        app.Engine.Render(canvas);
        app.Controller.OnTextInput(text);
        app.Engine.Render(canvas);
    }

    [Fact]
    public void Bound_Input_CaretFollowsContent()
    {
        // 问题 1：带 @bind-Value 的输入框，光标应停在内容末尾。
        var app = BuildApp();

        ClickAndType(app, "bound", "abc", _canvas);

        var live = NativeInput(app, "bound");
        live.Value.ShouldBe("abc");
        live.CursorPosition.ShouldBe(3);
        live.HasState(ElementState.Focus).ShouldBeTrue();
    }

    [Fact]
    public void Bound_Input_KeepsTypingIntoOneField()
    {
        // 逐字输入：每次按键都重渲染整个页面，光标必须持续跟随。
        var app = BuildApp();

        var (x, y) = FindPoint(app, "bound");
        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);

        foreach (var ch in "hello")
        {
            app.Engine.Render(_canvas);
            app.Controller.OnTextInput(ch.ToString());
        }
        app.Engine.Render(_canvas);

        var live = NativeInput(app, "bound");
        live.Value.ShouldBe("hello");
        live.CursorPosition.ShouldBe(5);
    }

    [Fact]
    public void TypingInBoundInput_DoesNotClearUnboundSibling()
    {
        // 问题 2：先在无 bind 的输入框打字，再回到带 bind 的输入框打字，
        // 前者的内容不应被清空——它的值只活在元素上，页面级重渲染不得把它冲掉。
        var app = BuildApp();

        ClickAndType(app, "unbound", "last", _canvas);
        NativeInput(app, "unbound").Value.ShouldBe("last");

        ClickAndType(app, "bound", "first", _canvas);

        NativeInput(app, "bound").Value.ShouldBe("first");
        NativeInput(app, "unbound").Value.ShouldBe("last");
    }

    [Fact]
    public void BoundValue_FlowsBackToThePageField()
    {
        // 双向绑定本身仍然成立：输入写回页面字段。
        var app = BuildApp();

        ClickAndType(app, "bound", "xy", _canvas);

        var page = app.Engine.GetRoot()!;
        page.ShouldNotBeNull();
        NativeInput(app, "bound").Value.ShouldBe("xy");
    }
}
