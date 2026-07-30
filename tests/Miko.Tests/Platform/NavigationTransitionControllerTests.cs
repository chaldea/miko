using Miko.Animation;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Routing;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Platform;

/// <summary>
/// 页面转场的控制器级测试（ISSUE-108）：验证 MikoInteractionController 把导航事件中的
/// 转场效果接入引擎，以及帧推进对"稳态空闲后的首帧大 deltaTime"的钳制——
/// 空闲期间宿主跳帧不渲染（ISSUE-096），恢复出帧的第一帧 deltaTime 包含整段空闲时长，
/// 不钳制会让该帧刚启动的转场直接跳到完成态（示例中表现为转场"不生效"）。
/// </summary>
public class NavigationTransitionControllerTests : IDisposable
{
    private const float W = 200;
    private const float H = 100;

    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly MikoAppContext _context;

    public NavigationTransitionControllerTests()
    {
        _bitmap = new SKBitmap((int)W, (int)H);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);

        var builder = MikoAppBuilder.CreateDefault();
        builder.UseRouter(router =>
        {
            router.MapRoute("/", typeof(RedPage));
            router.MapRoute("/detail", typeof(GreenPage));
        });
        _context = builder.Build();
        _context.Controller.Initialize(_canvas, W, H);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Fact]
    public void Rebuild_AfterNavigateWithTransition_ShouldStartEngineTransition()
    {
        var nav = _context.Services.GetService(typeof(NavigationManager)) as NavigationManager;
        nav.ShouldNotBeNull();

        nav!.NavigateTo("/detail", NavigationDirection.Forward, new StubSlideTransition());
        _context.Controller.Rebuild(_canvas, W, H);

        _context.Engine.IsNavigationTransitionActive.ShouldBeTrue();
    }

    [Fact]
    public void Rebuild_AfterNavigateWithoutTransition_ShouldNotStartEngineTransition()
    {
        var nav = (NavigationManager)_context.Services.GetService(typeof(NavigationManager))!;

        nav.NavigateTo("/detail");
        _context.Controller.Rebuild(_canvas, W, H);

        _context.Engine.IsNavigationTransitionActive.ShouldBeFalse();
    }

    [Fact]
    public void Update_WithHugeDeltaAfterIdle_ShouldAdvanceByNominalFrameStep_InsteadOfJumping()
    {
        var nav = (NavigationManager)_context.Services.GetService(typeof(NavigationManager))!;
        nav.NavigateTo("/detail", NavigationDirection.Forward, new StubSlideTransition());
        _context.Controller.Rebuild(_canvas, W, H);

        // 模拟稳态空闲数秒后恢复出帧的第一帧：deltaTime 携带整段空闲时长。
        // 恢复帧应按标称帧步长（1/60s）推进——1s 转场仅前进约 1.7%，既不跳完也不跳过 10%。
        _context.Controller.Update(5f);

        _context.Engine.IsNavigationTransitionActive.ShouldBeTrue();

        // progress = 1/60 → entering 偏移 = 200 * (1 - 1/60) ≈ 196.7px：仅右缘约 3px 露出新页面
        _context.Engine.Render(_canvas);
        _bitmap.GetPixel(150, 50).ShouldBe(SKColors.Red);
        _bitmap.GetPixel(198, 50).ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void Update_ShouldAdvanceTransitionGradually_UntilCompletion()
    {
        var nav = (NavigationManager)_context.Services.GetService(typeof(NavigationManager))!;
        nav.NavigateTo("/detail", NavigationDirection.Forward, new StubSlideTransition());
        _context.Controller.Rebuild(_canvas, W, H);

        // 60fps 的正常帧步长：约 1 秒完成
        for (int i = 0; i < 70; i++)
            _context.Controller.Update(1f / 60f);

        _context.Engine.IsNavigationTransitionActive.ShouldBeFalse();
    }

    private sealed class RedPage : ComponentBase
    {
        public override Element Build() => new DivElement
        {
            Style = new Style { Width = Length.Px(W), Height = Length.Px(H), BackgroundColor = Color.FromRgb(255, 0, 0) }
        };
    }

    private sealed class GreenPage : ComponentBase
    {
        public override Element Build() => new DivElement
        {
            Style = new Style { Width = Length.Px(W), Height = Length.Px(H), BackgroundColor = Color.FromRgb(0, 255, 0) }
        };
    }

    private sealed class StubSlideTransition : NavigationTransition
    {
        public override float Duration => 1f;
        public override TimingFunction TimingFunction => TimingFunction.Linear;

        public override void Apply(NavigationTransitionContext context, float progress)
            => context.EnteringOffsetX = context.ViewportWidth * (1f - progress);
    }
}
