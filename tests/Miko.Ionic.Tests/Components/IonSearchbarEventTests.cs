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
/// <see cref="IonSearchbar"/> 的回调事件面（对齐 searchbar.tsx 的 ionInput / ionChange /
/// ionClear / ionCancel / ionFocus / ionBlur）与 <c>@bind-Value</c> 支持。
///
/// <para>全部搭在真实应用上下文里驱动真实交互（点击 / 键入 / 失焦），而不是直接调处理器：
/// 事件能否到达组件本身就是被测内容——引擎的事件分发、冒泡与焦点管理都在链路上
/// （见 [[verify-with-real-component-structure]]）。</para>
/// </summary>
public class IonSearchbarEventTests : IDisposable
{
    private const float W = 390;
    private const float H = 844;

    private readonly SKBitmap _bitmap = new((int)W, (int)H);
    private readonly SKCanvas _canvas;

    public IonSearchbarEventTests()
    {
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    // --- 测试宿主页 -------------------------------------------------------------------------

    /// <summary>
    /// 承载一个 IonSearchbar 并记录其所有回调。<c>@bind-Value</c> 形态（ValueChanged 的 receiver
    /// 是祖先）刻意保留，因为它会引发额外一轮重渲染（ISSUE-121）。
    /// </summary>
    private sealed class HostPage : Miko.Components.ComponentBase
    {
        public string? Bound { get; set; }
        public string? ShowClearButton { get; set; }
        public string? ShowCancelButton { get; set; }
        public bool BindValue { get; set; } = true;

        public readonly List<string?> InputEvents = new();
        public readonly List<string?> ChangeEvents = new();
        public int ClearCount;
        public int CancelCount;
        public int FocusCount;
        public int BlurCount;

        protected override void BuildRenderTree(Miko.Components.RenderTreeBuilder builder)
        {
            builder.OpenComponent<IonApp>(0);
            builder.AddAttribute(1, "ChildContent", (Miko.Components.RenderFragment)(b =>
            {
                b.OpenComponent<IonPage>(0);
                b.AddAttribute(1, "ChildContent", (Miko.Components.RenderFragment)(b2 =>
                {
                    b2.OpenComponent<IonContent>(0);
                    b2.AddAttribute(1, "ChildContent", (Miko.Components.RenderFragment)(BuildSearchbar));
                    b2.CloseComponent();
                }));
                b.CloseComponent();
            }));
            builder.CloseComponent();
        }

        private void BuildSearchbar(Miko.Components.RenderTreeBuilder b)
        {
            b.OpenComponent<IonSearchbar>(0);
            if (BindValue)
            {
                b.AddAttribute(1, nameof(IonSearchbar.Value), Bound);
                b.AddAttribute(2, nameof(IonSearchbar.ValueChanged),
                    Miko.Components.EventCallback.Factory.Create<string?>(this, v => Bound = v));
            }
            if (ShowClearButton is not null)
                b.AddAttribute(3, nameof(IonSearchbar.ShowClearButton), ShowClearButton);
            if (ShowCancelButton is not null)
                b.AddAttribute(4, nameof(IonSearchbar.ShowCancelButton), ShowCancelButton);

            b.AddAttribute(5, nameof(IonSearchbar.OnInput),
                Miko.Components.EventCallback.Factory.Create<string?>(this, v => InputEvents.Add(v)));
            b.AddAttribute(6, nameof(IonSearchbar.OnChange),
                Miko.Components.EventCallback.Factory.Create<string?>(this, v => ChangeEvents.Add(v)));
            b.AddAttribute(7, nameof(IonSearchbar.OnClear),
                Miko.Components.EventCallback.Factory.Create(this, () => ClearCount++));
            b.AddAttribute(8, nameof(IonSearchbar.OnCancel),
                Miko.Components.EventCallback.Factory.Create(this, () => CancelCount++));
            b.AddAttribute(9, nameof(IonSearchbar.OnFocus),
                Miko.Components.EventCallback.Factory.Create(this, () => FocusCount++));
            b.AddAttribute(10, nameof(IonSearchbar.OnBlur),
                Miko.Components.EventCallback.Factory.Create(this, () => BlurCount++));
            b.CloseComponent();
        }
    }

    // --- 基础设施 ---------------------------------------------------------------------------

    private (MikoAppContext app, HostPage page) BuildApp(Action<HostPage>? configure = null)
    {
        var page = new HostPage();
        configure?.Invoke(page);

        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRootComponent(page.Build);
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        app.Engine.Render(_canvas);
        return (app, page);
    }

    private static InputElement SearchInput(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("searchbar-input").OfType<InputElement>().Single();

    /// <summary>
    /// 搜索框所在的纵向范围：它是页面里唯一的内容，贴着内容区顶部。扫描限定在这条带内
    /// （而非整页逐像素），否则一次未命中的探测要做十万次命中测试——足以拖慢整个测试程序集，
    /// 把并行跑着的、依赖真实计时器的用例（IonLoading 自动消失）挤成偶发失败。
    /// </summary>
    private const float SearchbarBandHeight = 120;

    /// <summary>探测一个命中指定 class 元素的坐标（LayoutBox 是 internal，只能经公开命中测试找）。</summary>
    private static (float x, float y)? FindPoint(MikoAppContext app, string className)
    {
        for (float y = 2; y < SearchbarBandHeight; y += 2)
        for (float x = 2; x < W; x += 2)
        {
            var hit = app.Engine.HitTest(x, y);
            while (hit is not null)
            {
                if (hit.HasClass(className)) return (x, y);
                hit = hit.Parent;
            }
        }
        return null;
    }

    private static void Click(MikoAppContext app, float x, float y)
    {
        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);
    }

    /// <summary>点击命中指定 class 的第一个位置；命中不到即失败（说明布局或揭示条件不对）。</summary>
    private static void ClickOn(MikoAppContext app, string className)
    {
        var point = FindPoint(app, className);
        point.ShouldNotBeNull($"未能在页面上命中 .{className}");
        Click(app, point!.Value.x, point.Value.y);
    }

    private void FocusField(MikoAppContext app)
    {
        ClickOn(app, "searchbar-input");
        app.Engine.Render(_canvas);
    }

    // --- ionInput / @bind-Value -------------------------------------------------------------

    [Fact]
    public void Typing_RaisesOnInput_AndFlowsBackThroughValueChanged()
    {
        var (app, page) = BuildApp();
        FocusField(app);

        app.Controller.OnTextInput("ab");

        // 一次 OnTextInput（一次已组合的文本输入）对应一次 ionInput，携带完整新值
        // ——控制器插入全部字符后才分发，不是逐字符发。
        page.InputEvents.ShouldBe(new[] { "ab" });
        // ValueChanged 让 @bind-Value 生效：值回流到祖先字段。
        page.Bound.ShouldBe("ab");

        // 逐次键入则逐次发。
        page.InputEvents.Clear();
        app.Controller.OnTextInput("c");
        app.Controller.OnTextInput("d");
        page.InputEvents.ShouldBe(new[] { "abc", "abcd" });
    }

    [Fact]
    public void Typing_DoesNotRaiseOnChange()
    {
        var (app, page) = BuildApp();
        FocusField(app);

        app.Controller.OnTextInput("ab");

        // ionChange 只在提交时发（失焦 / 清除 / 取消），不随每次键入发出。
        page.ChangeEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Backspace_RaisesOnInput()
    {
        var (app, page) = BuildApp();
        FocusField(app);
        app.Controller.OnTextInput("ab");
        page.InputEvents.Clear();

        app.Controller.OnKeyDown(MikoKey.Backspace, MikoKeyModifiers.None);

        page.InputEvents.ShouldBe(new[] { "a" });
        page.Bound.ShouldBe("a");
    }

    // --- ionFocus / ionBlur -----------------------------------------------------------------

    [Fact]
    public void Focus_RaisesOnFocus()
    {
        var (app, page) = BuildApp();

        FocusField(app);

        page.FocusCount.ShouldBe(1);
        page.BlurCount.ShouldBe(0);
        SearchInput(app).HasState(ElementState.Focus).ShouldBeTrue();
    }

    [Fact]
    public void Blur_RaisesOnBlur()
    {
        var (app, page) = BuildApp();
        FocusField(app);

        // 点到搜索框外：焦点转移，触发 blur。
        Click(app, W - 2, H - 2);
        app.Engine.Render(_canvas);

        page.BlurCount.ShouldBe(1);
        SearchInput(app).HasState(ElementState.Focus).ShouldBeFalse();
    }

    // --- ionChange 的提交语义 ---------------------------------------------------------------

    [Fact]
    public void Blur_AfterEditing_RaisesOnChangeOnce()
    {
        var (app, page) = BuildApp();
        FocusField(app);
        app.Controller.OnTextInput("ab");

        Click(app, W - 2, H - 2);   // 失焦提交
        app.Engine.Render(_canvas);

        page.ChangeEvents.ShouldBe(new[] { "ab" });
    }

    [Fact]
    public void Blur_WithoutEditing_DoesNotRaiseOnChange()
    {
        var (app, page) = BuildApp();
        FocusField(app);

        Click(app, W - 2, H - 2);   // 只聚焦再失焦，值没变
        app.Engine.Render(_canvas);

        // 对齐 searchbar.tsx onBlur：只有 focusedValue != value 才补发 ionChange。
        page.ChangeEvents.ShouldBeEmpty();
        page.BlurCount.ShouldBe(1);
    }

    // --- 清除按钮 ---------------------------------------------------------------------------

    [Fact]
    public void ClearButton_ClearsValue_AndRaisesClearThenInput()
    {
        var (app, page) = BuildApp();
        FocusField(app);
        app.Controller.OnTextInput("ab");
        app.Engine.Render(_canvas);
        page.InputEvents.Clear();

        ClickOn(app, "searchbar-clear-button");

        page.ClearCount.ShouldBe(1);
        // 清空同时发一次 ionInput（searchbar.tsx onClearInput → emitInputChange）。
        page.InputEvents.ShouldBe(new[] { string.Empty });
        page.Bound.ShouldBe(string.Empty);
        SearchInput(app).Value.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 未绑定 <c>Value</c> 时清除按钮不会出现：揭示规则依赖宿主的 <c>searchbar-has-value</c>，
    /// 而该类由 <c>Value</c> 参数推导，用户键入只写在元素上、不回流参数。与浏览器里 Ionic 的差别
    /// 源于它把 value 存在组件自身可变属性上（<c>@Prop({ mutable: true })</c>）。
    /// <para>因此需要清除按钮的场景必须绑定 Value（<c>@bind-Value</c> 或显式 ValueChanged）。
    /// 这里把这个约束固定下来，避免日后误以为它是 bug 而去掉 has-value 条件。</para>
    /// </summary>
    [Fact]
    public void ClearButton_StaysHidden_WhenValueUnbound()
    {
        var (app, _) = BuildApp(p => p.BindValue = false);
        FocusField(app);
        app.Controller.OnTextInput("ab");
        app.Engine.Render(_canvas);

        // 元素确实有了文本，但宿主没有 has-value，清除按钮仍是 display:none（命不中）。
        SearchInput(app).Value.ShouldBe("ab");
        Host(app).ShouldNotHaveClass("searchbar-has-value");
        FindPoint(app, "searchbar-clear-button").ShouldBeNull();
    }

    [Fact]
    public void ClearButton_ClearsElementText_NotJustTheParameter()
    {
        // 绑定场景下清除必须同时落到元素上：用户键入的文本活在元素里，只改参数清不掉它。
        var (app, _) = BuildApp();
        FocusField(app);
        app.Controller.OnTextInput("ab");
        app.Engine.Render(_canvas);
        SearchInput(app).Value.ShouldBe("ab");

        ClickOn(app, "searchbar-clear-button");
        app.Engine.Render(_canvas);

        SearchInput(app).Value.ShouldBe(string.Empty);
        SearchInput(app).CursorPosition.ShouldBe(0);
    }

    // --- 取消按钮 ---------------------------------------------------------------------------

    [Fact]
    public void CancelButton_RaisesCancel_ClearsValue_AndDropsFocus()
    {
        var (app, page) = BuildApp(p => p.ShowCancelButton = "always");
        FocusField(app);
        app.Controller.OnTextInput("ab");
        app.Engine.Render(_canvas);

        ClickOn(app, "searchbar-cancel-button");
        app.Engine.Render(_canvas);

        page.CancelCount.ShouldBe(1);
        page.Bound.ShouldBe(string.Empty);
        SearchInput(app).Value.ShouldBe(string.Empty);
        Host(app).ShouldNotHaveClass("searchbar-has-focus");
    }

    private static Element Host(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("ion-searchbar").Single();
}
