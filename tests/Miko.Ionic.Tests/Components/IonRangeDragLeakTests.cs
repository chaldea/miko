using Miko.Components;
using Miko.Core;
using Miko.Diagnostics;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;
using Shouldly;
using SkiaSharp;
using Xunit.Abstractions;

namespace Miko.Ionic.Tests.Components;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GcSensitiveTestCollection
{
    public const string Name = "GC-sensitive tests";
}

/// <summary>
/// 现场问题回归：<c>RangePage</c> 上来回拖动 IonRange 滑块时内存持续上涨、dotMemory 中 G2
/// 每拖一次近乎翻倍。
///
/// <para>根因在 <c>ComponentBase.TransferLayoutBox</c> 沿用了上一代的子 LayoutBox 列表，使每次
/// 重渲染都把前一代整棵树挂在新树下面（详见 <c>Miko.Tests</c> 的 PointerCaptureLeakTests）。
/// 这里用真实的 IonRange + <c>@bind-Value</c> 复现现场形态，确认拖动产生的中间代可被回收。</para>
/// </summary>
[Collection(GcSensitiveTestCollection.Name)]
public class IonRangeDragLeakTests : IDisposable
{
    private const float Width = 320;
    private const float Height = 100;
    private readonly SKBitmap _bitmap = new((int)Width, (int)Height);
    private readonly SKCanvas _canvas;
    private readonly ITestOutputHelper _output;

    public IonRangeDragLeakTests(ITestOutputHelper output)
    {
        _output = output;
        _canvas = new SKCanvas(_bitmap);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Fact]
    public void DraggingKnob_DoesNotRetainIntermediateGenerations()
    {
        var page = new HostPage();
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRootComponent(page.Build);
        var app = builder.Build();
        app.Controller.Initialize(_canvas, Width, Height);
        app.Engine.Render(_canvas);

        var track = FindTrack(app);
        var before = LayoutAllocationDiagnostics.ForceFullCollectionAndCaptureHeap();

        // Desktop hosts normally deliver an unpressed move before the press. That move populates
        // the controller's hover chain; it must not pin every subsequently superseded DOM tree.
        app.Controller.OnPointerMove(track.left + track.width * 0.05f, track.y);
        app.Controller.OnPointerDown(track.left + track.width * 0.05f, track.y, MouseButton.Left);

        // 来回拖动，逐步记录每一代的 knob 元素。
        const int moves = 24;
        var generations = new List<WeakReference>();
        for (int i = 1; i <= moves; i++)
        {
            float ratio = 0.05f + (i % 2 == 0 ? 0.8f : 0.2f) * (i / (float)moves) + i * 0.01f;
            app.Controller.OnPointerMove(track.left + track.width * Math.Clamp(ratio, 0f, 1f), track.y);

            var knob = app.Engine.GetRoot()!.FindByClass("range-knob").FirstOrDefault();
            if (knob != null) generations.Add(new WeakReference(knob));
        }

        page.Changes.Count.ShouldBeGreaterThan(5, "拖动应实际改变值，否则没有触发重渲染");
        generations.Count.ShouldBeGreaterThan(5);

        app.Engine.Render(_canvas);
        var after = LayoutAllocationDiagnostics.ForceFullCollectionAndCaptureHeap();

        int alive = generations.Count(w => w.IsAlive);
        _output.WriteLine(
            "forced-GC retention: oldGenerationsAlive={0}/{1}, managed={2:N0}->{3:N0} B, " +
            "heap={4:N0}->{5:N0} B, fragmented={6:N0}->{7:N0} B, gen2={8:N0}->{9:N0} B, " +
            "LOH={10:N0}->{11:N0} B, POH={12:N0}->{13:N0} B",
            alive, generations.Count,
            before.ManagedBytes, after.ManagedBytes,
            before.HeapSizeBytes, after.HeapSizeBytes,
            before.FragmentedBytes, after.FragmentedBytes,
            before.Gen2SizeBytes, after.Gen2SizeBytes,
            before.LargeObjectHeapSizeBytes, after.LargeObjectHeapSizeBytes,
            before.PinnedObjectHeapSizeBytes, after.PinnedObjectHeapSizeBytes);
        alive.ShouldBeLessThanOrEqualTo(3,
            $"拖动 {moves} 步后仍有 {alive} 代 IonRange 子树存活，说明每次重渲染都在累积旧树");
    }

    [Fact]
    public void DraggingKnob_ShouldReuseComputedStylesWhileRecreatingFrameObjects()
    {
        var page = new HostPage();
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRootComponent(page.Build);
        var app = builder.Build();
        app.Controller.Initialize(_canvas, Width, Height);
        app.Engine.Render(_canvas);

        var track = FindTrack(app);
        app.Controller.OnPointerDown(track.left + track.width * 0.05f, track.y, MouseButton.Left);

        // One changing frame fills the pool with the previous frame's ComputedStyle instances.
        app.Controller.OnPointerMove(track.left + track.width * 0.25f, track.y);
        app.Engine.Render(_canvas);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

        const int frames = 200;
        LayoutAllocationSnapshot metrics;
        LayoutAllocationDiagnostics.Begin();
        try
        {
            for (int i = 0; i < frames; i++)
            {
                float ratio = i % 2 == 0 ? 0.35f : 0.75f;
                app.Controller.OnPointerMove(track.left + track.width * ratio, track.y);
                app.Engine.Render(_canvas);
            }
        }
        finally
        {
            metrics = LayoutAllocationDiagnostics.End();
        }

        _output.WriteLine(
            "frames={0}, thread={1}, allocated={2:N0} B ({3:N0} B/frame), " +
            "computedStyle[new={4}, reused={5} (gen0={6}, gen1={7}, gen2={8}), returned={9}, " +
            "capacityRejected={10}, duplicateReturn={11}], " +
            "created[layoutBox={12}, element={13}, style={14}, pseudoComputedStyle={15}], " +
            "GC[gen0={16}, gen1={17}, gen2={18}]",
            frames,
            metrics.ThreadId,
            metrics.AllocatedBytes,
            metrics.AllocatedBytes / frames,
            metrics.ComputedStyleRentNew,
            metrics.ComputedStyleRentReused,
            metrics.ComputedStyleRentReusedGen0,
            metrics.ComputedStyleRentReusedGen1,
            metrics.ComputedStyleRentReusedGen2,
            metrics.ComputedStyleReturnAccepted,
            metrics.ComputedStyleReturnRejectedCapacity,
            metrics.ComputedStyleReturnAlreadyPooled,
            metrics.LayoutBoxCreated,
            metrics.ElementCreated,
            metrics.StyleCreated,
            metrics.PseudoComputedStyleCreated,
            metrics.Gen0Collections,
            metrics.Gen1Collections,
            metrics.Gen2Collections);

        metrics.ComputedStyleRentNew.ShouldBe(0,
            "after one warm-up relayout, ordinary element styles should come entirely from the pool");
        metrics.ComputedStyleRentReused.ShouldBeGreaterThan(0);
        metrics.ComputedStyleRentReusedGen2.ShouldBe(metrics.ComputedStyleRentReused,
            "the forced full GC should promote the warmed pool, proving Gen2 styles are rented again");
        metrics.ComputedStyleReturnAccepted.ShouldBe(metrics.ComputedStyleRentReused,
            "a stable-shape page should return as many ordinary computed styles as it rents");
        metrics.ComputedStyleReturnRejectedCapacity.ShouldBe(0);
        metrics.ComputedStyleReturnAlreadyPooled.ShouldBe(0);
        metrics.LayoutBoxCreated.ShouldBeGreaterThan(metrics.ComputedStyleRentReused,
            "layout and component state transfer still create fresh layout boxes despite style reuse");
        metrics.ElementCreated.ShouldBeGreaterThan(0,
            "the bound component should rebuild element subtrees while dragging");
        metrics.StyleCreated.ShouldBeGreaterThan(0,
            "IonRange creates fresh knob and active-bar inline styles for new values");
        metrics.AllocatedBytes.ShouldBeGreaterThan(0);
    }

    private static (float left, float width, float y) FindTrack(MikoAppContext app)
    {
        float bestLeft = 0, bestRight = 0, bestY = 0, bestWidth = 0;
        for (float y = 1; y < Height; y += 2)
        {
            float? first = null;
            float last = 0;
            for (float x = 0; x < Width; x++)
            {
                if (!IsInTrack(app.Engine.HitTest(x, y))) continue;
                first ??= x;
                last = x;
            }
            if (first is null || last - first.Value <= bestWidth) continue;
            bestLeft = first.Value;
            bestRight = last;
            bestY = y;
            bestWidth = last - first.Value;
        }
        bestWidth.ShouldBeGreaterThan(0f);
        return (bestLeft, bestRight - bestLeft, bestY);
    }

    private static bool IsInTrack(Element? element)
    {
        for (var current = element; current != null; current = current.Parent)
            if (current.HasClass("range-slider")) return true;
        return false;
    }

    /// <summary>与 RangePage 同形：以自身为 receiver 订阅 ValueChanged（即 @bind-Value）。</summary>
    private sealed class HostPage : ComponentBase
    {
        public double Value { get; set; } = 20;
        public List<double> Changes { get; } = new();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<IonRange>(0);
            builder.AddComponentParameter(1, nameof(IonRange.Value), Value);
            builder.AddComponentParameter(2, nameof(IonRange.Pin), true);
            builder.AddComponentParameter(3, nameof(IonRange.ValueChanged),
                EventCallback.Factory.Create<double>(this, value =>
                {
                    Value = value;
                    Changes.Add(value);
                }));
            builder.CloseComponent();
        }
    }
}
