using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests;

/// <summary>
/// 回归测试：反复点击 <c>ion-segment-button</c> 切换分段，被换下的那些代不得存活。
///
/// <para><b>现场症状</b>（人工复审报告）：来回点击分段按钮时 G2 从约 10 MB 稳定涨到 70 MB，
/// dotMemory 里 force GC 也放不掉，快照显示占用方是样式对象。表面看像"池化的
/// <c>ComputedStyle</c> 只出借不归还"，实则相反——池是平的（实测 <c>RentNew=0</c>、
/// 出借与归还都是 21,300 次）。真正被钉住的是<b>整棵旧的元素树</b>，样式只是跟着它一起活。</para>
///
/// <para><b>根因</b>见 <see cref="Animation.AnimationManager"/> 里三个应用器字段的注释：
/// <c>Track*Change</c> 开头的 <c>RemoveAll(t =&gt; t.Element == element ...)</c> 谓词捕获了
/// <c>element</c>，同作用域内新建的应用器 lambda 于是与它<b>共享同一个闭包实例</b>，
/// 即便应用器自己一个元素也没提。应用器被存进长期存活的 <c>ActiveTransition</c>，就把那个元素
/// 连同 <c>Parent</c> 链上的整棵旧树钉住；而 <c>PruneDetachedTargets</c> 按
/// <c>transition.Element</c> 剪枝，该字段已被 <c>MigrateSupersededTargets</c> 前推到在场实例，
/// 条目看着"健康"因而剪不掉。</para>
///
/// <para><b>判定方式</b>：对每次点击后的分段按钮留弱引用，强制压缩回收后要求除最后一代外
/// 全部已被回收。不用"堆字节数"判定——那既受同进程其它用例干扰，也分不清缓存与泄漏。</para>
/// </summary>
public class SegmentRetentionTests
{
    private sealed class PlatformOnlyServices : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => serviceType == typeof(IPlatformInfo) ? new PlatformInfo(HostPlatform.Android) : null;
    }

    /// <summary>三个分段按钮 + 一段随选中值变化的内容，贴近 Anime 示例的 Home 页结构。</summary>
    private sealed class SegmentPage : ComponentBase
    {
        private string _category = "a";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var root = builder.OpenElement<DivElement>();
            root.Class = "page";

            var segment = builder.OpenComponent<IonSegment>();
            segment.Value = _category;
            segment.ValueChanged = EventCallback.Factory.Create<string>(this, value =>
            {
                _category = value;
                StateHasChanged();
            });
            segment.ChildContent = b =>
            {
                foreach (var name in new[] { "a", "b", "c" })
                {
                    var button = b.OpenComponent<IonSegmentButton>();
                    button.Value = name;
                    button.ChildContent = lb => lb.AddContent(name);
                    b.CloseComponent();
                }
            };
            builder.CloseComponent();

            for (int i = 0; i < 10; i++)
            {
                var item = builder.OpenElement<DivElement>();
                item.Class = $"row-{_category}-{i}";
                builder.AddContent("item");
                builder.CloseElement();
            }

            builder.CloseElement();
        }
    }

    /// <summary>
    /// 只取坐标，绝不把 <see cref="Element"/> 引用带回调用方——哪怕只留住第 0 代的一个按钮，
    /// 它的 <c>SupersededBy</c> 前向链就会让此后每一代都可达，测试于是永远"发现泄漏"。
    /// 单独成方法是因为 Release 下内联的 foreach 临时栈槽同样足以钉住它。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (float X, float Y)[] ButtonCenters(MikoEngine engine)
    {
        var centers = new List<(float, float)>();
        foreach (var button in engine.GetRoot()!.FindByTagName("button"))
        {
            var rect = button.LayoutBox!.BoxModel.BorderBox;
            centers.Add((rect.Left + rect.Width / 2, rect.Top + rect.Height / 2));
        }
        return centers.ToArray();
    }

    /// <summary>同上：采样只留弱引用，不得把元素本身带出方法。</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SampleButtons(MikoEngine engine, List<WeakReference> sink)
    {
        foreach (var button in engine.GetRoot()!.FindByTagName("button"))
            sink.Add(new WeakReference(button));
    }

    private static void ForceCompactingCollect()
    {
        for (int i = 0; i < 3; i++)
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    [Fact]
    public void SwitchingSegments_ShouldNotRetainReplacedGenerations()
    {
        const int clicks = 200;
        var samples = new List<WeakReference>();
        int buttonsPerGeneration = 0;
        int alive = 0;

        // 会话整体放在一个独立方法里，结束后栈上不再有任何指向元素的槽。
        [MethodImpl(MethodImplOptions.NoInlining)]
        void RunSession()
        {
            var page = new SegmentPage();
            var options = new MikoAppOptions
            {
                RootComponentFactory = () => page.Build(),
                StyleSheets = { IonicStyleSheetFactory.CreateAllModes() },
            };

            var engine = new MikoEngineBuilder().Build();
            var dispatcher = new MikoDispatcher();
            var controller = new MikoInteractionController(
                Options.Create(options), new PlatformOnlyServices(), engine,
                new EventDispatcher(), dispatcher,
                new HotReloadService(NullLogger<HotReloadService>.Instance),
                NullLogger<MikoInteractionController>.Instance);

            using var surface = SKSurface.Create(new SKImageInfo(400, 800));
            controller.Initialize(surface.Canvas, 400, 800);
            engine.Render(surface.Canvas);

            var centers = ButtonCenters(engine);
            centers.Length.ShouldBe(3, "分段应渲染出三个按钮，否则下面点的是空气");
            buttonsPerGeneration = centers.Length;

            for (int i = 0; i < clicks; i++)
            {
                var (x, y) = centers[i % centers.Length];
                controller.OnPointerDown(x, y, MouseButton.Left);
                controller.OnPointerUp(x, y, MouseButton.Left);
                dispatcher.Drain();      // 宿主每帧都排空调度队列
                engine.Render(surface.Canvas);
                SampleButtons(engine, samples);
            }

            // **必须在会话仍存活时判定**：泄漏的形态是「引擎/动画管理器还活着，却攥着早已
            // 被换下的那些代」。等会话整体出栈再测，泄漏链自己也变成垃圾，什么都测不出来。
            ForceCompactingCollect();
            alive = samples.Count(reference => reference.IsAlive);

            GC.KeepAlive(controller);
            GC.KeepAlive(engine);
        }

        RunSession();

        // 只有当前在场的那一代（3 个按钮）允许存活，之前每一代都必须已被回收。
        alive.ShouldBeLessThanOrEqualTo(buttonsPerGeneration,
            $"{alive}/{samples.Count} 个分段按钮在强制压缩回收后仍存活（每代 {buttonsPerGeneration} 个，" +
            $"共 {clicks} 代）——过渡条目的应用器闭包又把旧树钉住了，见 AnimationManager 的应用器字段注释");
    }
}
