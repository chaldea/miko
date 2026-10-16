using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Animation;

/// <summary>
/// 回归测试：display:none 子树里的关键帧动画不得让宿主一直出帧（ISSUE-096 的稳态空闲）。
///
/// <para>现场是 <c>ion-infinite-scroll-content</c>：加载指示器平时由 display:none 隐藏，但它的
/// 无限循环动画照样被启动，<see cref="AnimationManager.HasActiveAnimations"/> 恒为真，整页于是一直以
/// 60fps 重绘。动画推进时的绘制失效还会给隐藏元素标脏、凭空再要一帧，所以单门禁
/// <see cref="MikoEngine.HasPendingVisualWork"/> 的动画判断不够，绘制失效也必须门禁。</para>
///
/// <para>条目本身保留：显示出来后接着播放，有其它原因出帧时也照常推进。</para>
/// </summary>
public sealed class HiddenAnimationIdleTests
{
    private const string Hidden = "hidden";

    private static (MikoEngine engine, DivElement wrapper, DivElement spinner, SKSurface surface) Build()
    {
        var spinner = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(20),
                Height = Length.Px(20),
                BackgroundColor = Color.Red,
                Animations = new List<KeyframeAnimation>
                {
                    new("spin-fade", 1f,
                        new Keyframe(0f, new Style { Opacity = 0f }),
                        new Keyframe(1f, new Style { Opacity = 1f }))
                    {
                        Infinite = true,
                        TimingFunction = TimingFunction.Linear,
                    },
                },
            },
        };
        // The animated element sits *below* the display:none element, as the spinner does.
        var wrapper = new DivElement { Class = Hidden, Children = { new DivElement { Children = { spinner } } } };
        var root = new DivElement { Style = new Style { Width = Length.Px(200), Height = Length.Px(100) } };
        root.AddChild(wrapper);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject { [$".{Hidden}"] = new() { Display = Display.None } });

        var engine = new MikoEngineBuilder().Build();
        var surface = SKSurface.Create(new SKImageInfo(200, 100));
        engine.Initialize(root, new List<StyleSheet> { sheet }, surface.Canvas, 200, 100);
        engine.Render(surface.Canvas);
        return (engine, wrapper, spinner, surface);
    }

    [Fact]
    public void AnimationInsideDisplayNone_DoesNotKeepTheEngineBusy()
    {
        var (engine, _, _, surface) = Build();
        using var _surface = surface;

        engine.AnimationManager.HasActiveAnimations.ShouldBeTrue("前提：动画条目已启动并保留");
        engine.HasPendingVisualWork.ShouldBeFalse("隐藏的动画画不出任何东西，宿主应当空闲");
    }

    [Fact]
    public void AdvancingAHiddenAnimation_DoesNotRequestAFrame()
    {
        // 别的原因出了一帧（滚动、其它动画）时，隐藏的动画照常推进；它的绘制失效不能给隐藏元素
        // 标脏——否则那块脏区又要来下一帧，下一帧再推进、再标脏，回路永不停止。
        var (engine, _, _, surface) = Build();
        using var _surface = surface;

        engine.AnimationManager.Update(0.25f);

        engine.HasPendingVisualWork.ShouldBeFalse();
    }

    [Fact]
    public void ShowingTheSubtree_RunsTheAnimation_AndHidingItAgainIdles()
    {
        var (engine, wrapper, spinner, surface) = Build();
        using var _surface = surface;

        wrapper.Class = null;
        engine.Render(surface.Canvas);

        engine.HasPendingVisualWork.ShouldBeTrue("显示出来的动画逐帧推进");
        var before = spinner.LayoutBox!.ComputedStyle.Opacity;
        engine.AnimationManager.Update(0.25f);
        engine.HasPendingVisualWork.ShouldBeTrue();
        engine.Render(surface.Canvas);
        spinner.LayoutBox!.ComputedStyle.Opacity.ShouldNotBe(before);

        wrapper.Class = Hidden;
        engine.Render(surface.Canvas);

        engine.HasPendingVisualWork.ShouldBeFalse("再次隐藏后回到空闲");
    }
}
