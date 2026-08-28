using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Hosting;
using Miko.Layout;
using Miko.Platform;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Platform;

/// <summary>
/// 重渲染世代回收回归（现场：IonRange 拖动滑块时 dotMemory 显示 G2 每拖一次近乎翻倍）。
///
/// <para>成因在 <c>ComponentBase.TransferLayoutBox</c>：它曾直接 <c>Children =
/// oldElement.LayoutBox.Children</c>，使新元素的 LayoutBox 握着<b>上一代</b>的子 LayoutBox，
/// 而子盒的 <c>.Element</c> 指向上一代元素、后者再经 <c>SupersededBy</c> 指向下一代。每次重渲染
/// 就多套一层，整条历史全部可达且 GC 无法回收。拖动是最容易触发的场景：每个 mousemove 都经
/// <c>@bind-Value</c> 重渲染一次。</para>
///
/// <para>这里用弱引用 + 真实 GC 直接度量「旧代是否可回收」，不依赖对泄漏路径的猜测。</para>
/// </summary>
public class PointerCaptureLeakTests
{
    private const float Width = 200;
    private const float Height = 120;

    /// <summary>拖动产生的中间代元素树必须可被回收，不能随拖动步数累积。</summary>
    [Fact]
    public void Drag_DoesNotRetainIntermediateElementGenerations()
    {
        using var bitmap = new SKBitmap((int)Width, (int)Height);
        using var canvas = new SKCanvas(bitmap);

        var page = new SliderPage();
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseRootComponent(page.Build);
        var app = builder.Build();
        app.Controller.Initialize(canvas, Width, Height);
        app.Engine.Render(canvas);

        app.Controller.OnPointerDown(10, 10, MouseButton.Left);

        // 每一步拖动后记录该代的 knob 元素（弱引用），随后继续拖动把它变成「中间代」。
        const int moves = 30;
        var generations = new List<WeakReference>();
        for (int i = 1; i <= moves; i++)
        {
            app.Controller.OnPointerMove(10 + i * 2, 10);
            var knob = app.Engine.GetRoot()!.FindByClass("knob").SingleOrDefault();
            if (knob != null) generations.Add(new WeakReference(knob));
        }

        page.Moves.ShouldBe(moves, "每步移动都应触发一次重渲染，否则该用例没测到累积");
        generations.Count.ShouldBe(moves);

        // 再渲染一帧，确保最后一代也已被新树取代、布局缓存指向最新树。
        app.Engine.Render(canvas);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // 除最后一两代（当前在场 / 被引擎作为上一帧状态持有）外，中间代都应已回收。
        int alive = generations.Count(w => w.IsAlive);
        alive.ShouldBeLessThanOrEqualTo(3,
            $"拖动 {moves} 步后仍有 {alive} 代旧元素树存活，说明每步都在累积整棵旧树");
    }

    /// <summary>
    /// 诊断用对照：完全相同的重渲染次数，但<b>不按下</b>指针（无捕获）。
    /// 若此例通过而上例失败，泄漏的持有者就是 pointer 捕获引用而非重渲染本身。
    /// </summary>
    [Fact]
    public void HoverOnlyRerenders_DoNotRetainGenerations()
    {
        using var bitmap = new SKBitmap((int)Width, (int)Height);
        using var canvas = new SKCanvas(bitmap);

        var page = new SliderPage();
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseRootComponent(page.Build);
        var app = builder.Build();
        app.Controller.Initialize(canvas, Width, Height);
        app.Engine.Render(canvas);

        const int moves = 30;
        var generations = new List<WeakReference>();
        for (int i = 1; i <= moves; i++)
        {
            app.Controller.OnPointerMove(10 + i * 2, 10);
            var knob = app.Engine.GetRoot()!.FindByClass("knob").SingleOrDefault();
            if (knob != null) generations.Add(new WeakReference(knob));
        }

        app.Engine.Render(canvas);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        int alive = generations.Count(w => w.IsAlive);
        alive.ShouldBeLessThanOrEqualTo(3,
            $"无指针捕获时仍有 {alive} 代存活，说明持有者不是捕获引用");
    }

    /// <summary>修复不得破坏拖动本身：事件仍须投递、值仍须跟随。</summary>
    [Fact]
    public void Drag_StillDeliversMovesToLiveElement()
    {
        using var bitmap = new SKBitmap((int)Width, (int)Height);
        using var canvas = new SKCanvas(bitmap);

        var page = new SliderPage();
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseRootComponent(page.Build);
        var app = builder.Build();
        app.Controller.Initialize(canvas, Width, Height);
        app.Engine.Render(canvas);

        app.Controller.OnPointerDown(10, 10, MouseButton.Left);
        app.Controller.OnPointerMove(20, 10);
        app.Controller.OnPointerMove(30, 10);
        page.Moves.ShouldBe(2);

        // 拖动过程中 knob 仍在树中且位置随之更新。
        var knob = app.Engine.GetRoot()!.FindByClass("knob").SingleOrDefault();
        knob.ShouldNotBeNull();

        app.Controller.OnPointerUp(30, 10, MouseButton.Left);
        int afterRelease = page.Moves;
        app.Controller.OnPointerMove(40, 10);
        page.Moves.ShouldBe(afterRelease + 1);
    }

    /// <summary>
    /// 模拟 IonRange 的形态：一棵有嵌套的子树（容器 / 轨道 / 滑块），
    /// mousemove 改状态并重渲染整页，每帧产出一棵全新的元素树。
    /// </summary>
    private sealed class SliderPage : ComponentBase
    {
        public int Moves { get; private set; }
        private float _pct;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "page");
            builder.AddAttribute(2, "style", new Style
            {
                Width = Length.Px(Width),
                Height = Length.Px(Height),
                BackgroundColor = Color.FromRgb(240, 240, 240)
            });
            builder.AddAttribute(3, "onmousemove",
                EventCallback.Factory.Create<MouseEventArgs>(this, args =>
                {
                    Moves++;
                    _pct = Math.Clamp(args.OffsetX / Width * 100f, 0f, 100f);
                    StateHasChanged();
                }));

            builder.OpenElement(4, "div");
            builder.AddAttribute(5, "class", "track");
            builder.AddAttribute(6, "style", new Style
            {
                Width = Length.Percent(100),
                Height = Length.Px(4),
                BackgroundColor = Color.FromRgb(180, 180, 180)
            });

            builder.OpenElement(7, "div");
            builder.AddAttribute(8, "class", "knob");
            builder.AddAttribute(9, "style", new Style
            {
                Position = Position.Relative,
                Left = Length.Percent(_pct),
                Width = Length.Px(12),
                Height = Length.Px(12),
                BackgroundColor = Color.FromRgb(40, 100, 200)
            });
            builder.AddContent(10, $"{(int)_pct}");
            builder.CloseElement();

            builder.CloseElement();
            builder.CloseElement();
        }
    }
}
