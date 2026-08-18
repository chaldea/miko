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
/// 真实应用上下文（Ionic 样式表 + 交互控制器）下 <see cref="IonSearchbar"/> 的焦点与输入行为。
///
/// <para>现场表现：点击搜索框拿不到焦点，因而无法输入。与 <see cref="IonInputCaretTests"/> 同类，
/// 但根因不同：searchbar 的原生 input 是 <see cref="InputType.Search"/>，而点击聚焦
/// （<c>HandleInputClick</c>）与键盘编辑（<c>InputElement.IsEditable</c>）此前都只认 Text/Password。</para>
///
/// <para>刻意搭真实组件而非手写等价结构：问题依赖 IonSearchbar 实际写入的 input 类型与嵌套形状。</para>
/// </summary>
public class IonSearchbarFocusTests : IDisposable
{
    private const float W = 390;
    private const float H = 844;

    private readonly SKBitmap _bitmap = new((int)W, (int)H);
    private readonly SKCanvas _canvas;

    public IonSearchbarFocusTests()
    {
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private sealed class SearchbarPage : Miko.Components.ComponentBase
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
                        b3.OpenComponent<IonSearchbar>(0);
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
        builder.UseRouter(router => router.MapRoute("/", typeof(SearchbarPage)));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        app.Engine.Render(_canvas);
        return app;
    }

    /// <summary>搜索框所在的纵向范围：它是页面里唯一的内容，贴着内容区顶部。</summary>
    private const float SearchbarBandHeight = 120;

    /// <summary>当前在树中的原生 input（重渲染会换实例，必须重新查找）。</summary>
    private static InputElement SearchInput(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("searchbar-input").OfType<InputElement>().Single();

    /// <summary>探测一个命中搜索输入框的坐标（LayoutBox 是 internal，只能经公开命中测试找）。</summary>
    private static (float x, float y) FindInputPoint(MikoAppContext app)
    {
        // 只扫搜索框所在的顶部窄带，别逐像素扫全页：一次探测十万次命中测试会拖慢整个程序集，
        // 把并行跑着的、依赖真实计时器的用例（IonLoading 自动消失）挤成偶发失败。
        for (float y = 2; y < SearchbarBandHeight; y += 2)
        for (float x = 2; x < W; x += 2)
        {
            if (app.Engine.HitTest(x, y) is InputElement hit && hit.HasClass("searchbar-input"))
                return (x, y);
        }
        throw new InvalidOperationException("未能在页面上命中 .searchbar-input");
    }

    [Fact]
    public void Click_FocusesTheSearchInput()
    {
        var app = BuildApp();
        var (x, y) = FindInputPoint(app);

        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);

        // 修复前：HandleInputClick 的 switch 不认 InputType.Search，点击既不聚焦也不移动光标。
        SearchInput(app).HasState(ElementState.Focus).ShouldBeTrue();
    }

    [Fact]
    public void Typing_WritesIntoTheSearchInput()
    {
        var app = BuildApp();
        var (x, y) = FindInputPoint(app);

        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);
        app.Engine.Render(_canvas);

        app.Controller.OnTextInput("miko");

        var live = SearchInput(app);
        // 修复前：IsEditable 排除 Search，键盘输入被整体丢弃。
        live.Value.ShouldBe("miko");
        live.CursorPosition.ShouldBe(4);
        live.HasState(ElementState.Focus).ShouldBeTrue();

        // 输入必须调度重绘，否则稳态空闲会跳过帧生产，光标与文字都不出现（ISSUE-104）。
        app.Engine.HasPendingVisualWork.ShouldBeTrue();
    }

    [Fact]
    public void ArrowAndBackspace_EditTheSearchInput()
    {
        var app = BuildApp();
        var (x, y) = FindInputPoint(app);

        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);
        app.Controller.OnTextInput("abc");
        app.Engine.Render(_canvas);

        app.Controller.OnKeyDown(MikoKey.Left, MikoKeyModifiers.None);
        SearchInput(app).CursorPosition.ShouldBe(2);

        app.Controller.OnKeyDown(MikoKey.Backspace, MikoKeyModifiers.None);

        var live = SearchInput(app);
        live.Value.ShouldBe("ac");   // 光标在 2，删除其前的 'b'
        live.CursorPosition.ShouldBe(1);
    }

    /// <summary>
    /// 未绑定 Value 的搜索框，其文本只活在元素上。祖先重渲染会重建一个 Value 为 null 的新
    /// IonSearchbar 实例，若 Build() 把 null 当成「声明了空值」写下去，用户键入的内容就被清空
    /// （ISSUE-121 问题 2，IonInput 已按此规避）。
    /// </summary>
    [Fact]
    public void AncestorRerender_KeepsTypedText_WhenValueUnbound()
    {
        var page = new RerenderablePage();
        var app = BuildAppFrom(page.Build);
        var (x, y) = FindInputPoint(app);

        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);
        app.Controller.OnTextInput("miko");
        app.Engine.Render(_canvas);

        // 祖先因自身状态变化重渲染，把 IonSearchbar 整棵子树换成新实例。
        page.ForceRender();

        SearchInput(app).Value.ShouldBe("miko");
    }

    /// <summary>宿主页：可主动触发一次与输入无关的重渲染（等价于祖先状态变化）。</summary>
    private sealed class RerenderablePage : Miko.Components.ComponentBase
    {
        public void ForceRender() => StateHasChanged();

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
                        b3.OpenComponent<IonSearchbar>(0);
                        b3.CloseComponent();
                    }));
                    b2.CloseComponent();
                }));
                b.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private MikoAppContext BuildAppFrom(Func<Element> rootFactory)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRootComponent(rootFactory);
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        app.Engine.Render(_canvas);
        return app;
    }

    [Fact]
    public void DisabledSearchbar_ClickDoesNotFocus()
    {
        var app = BuildAppWith(disabled: true);

        var input = SearchInput(app);
        input.IsDisabled.ShouldBeTrue();

        // 禁用宿主是 pointer-events:none，命中测试落不到 input 上，点遍整个搜索框区域都不该聚焦。
        for (float y = 4; y < SearchbarBandHeight; y += 4)
        for (float x = 4; x < W; x += 8)
        {
            app.Controller.OnPointerDown(x, y, MouseButton.Left);
            app.Controller.OnPointerUp(x, y, MouseButton.Left);
        }

        SearchInput(app).HasState(ElementState.Focus).ShouldBeFalse();
    }

    private MikoAppContext BuildAppWith(bool disabled)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRouter(router => router.MapRoute("/",
            disabled ? typeof(DisabledSearchbarPage) : typeof(SearchbarPage)));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        app.Engine.Render(_canvas);
        return app;
    }

    private sealed class DisabledSearchbarPage : Miko.Components.ComponentBase
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
                        b3.OpenComponent<IonSearchbar>(0);
                        b3.AddAttribute(1, nameof(IonSearchbar.Disabled), true);
                        b3.CloseComponent();
                    }));
                    b2.CloseComponent();
                }));
                b.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
