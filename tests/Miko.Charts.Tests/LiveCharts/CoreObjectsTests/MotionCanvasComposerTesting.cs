// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.CoreObjectsTests;

using LiveChartsCore.Motion;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class MotionCanvasComposerTesting
{
    public MotionCanvasComposerTesting() => CoreTestsSetup.Ensure();

    // Regression for #2029. WPF's TabControl raises Loaded on a chart that lives
    // in a not-yet-selected tab (size 0), then raises Loaded *again* — with no
    // Unloaded between them — once the tab is reparented into the active content
    // host. Without an idempotency guard the composer's Initialize re-subscribed
    // OnPaintSurface and FrameRequest → DrawFrame, so every paint applied the
    // HiDPI Canvas.Scale twice (e.g. 1.75² ≈ 3) and the chart rendered at
    // multiple-times the intended scale.
    [Fact]
    public void Initialize_IsIdempotent_WithoutInterveningDispose()
    {
        var renderMode = new SpyRenderMode();
        var ticker = new SpyFrameTicker();
        var composer = new MotionCanvasComposer(renderMode, ticker);
        var canvas = new CoreMotionCanvas();

        composer.Initialize(canvas);
        composer.Initialize(canvas);

        renderMode.InitializeCount.ShouldBe(1, "InitializeRenderMode must run once.");
        ticker.InitializeCount.ShouldBe(1, "InitializeTicker must run once.");
        renderMode.FrameRequestSubscriberCount.ShouldBe(1, "FrameRequest must have a single subscriber.");
    }

    [Fact]
    public void Dispose_AfterDoubleInitialize_RestoresCleanState()
    {
        var renderMode = new SpyRenderMode();
        var ticker = new SpyFrameTicker();
        var composer = new MotionCanvasComposer(renderMode, ticker);
        var canvas = new CoreMotionCanvas();

        composer.Initialize(canvas);
        composer.Initialize(canvas);
        composer.Dispose(canvas);

        renderMode.FrameRequestSubscriberCount.ShouldBe(0, "Dispose must unsubscribe FrameRequest.");
        renderMode.DisposeCount.ShouldBe(1, "DisposeRenderMode must run once.");
        ticker.DisposeCount.ShouldBe(1, "DisposeTicker must run once.");

        // After a full Dispose, Initialize must work again (re-entering the
        // Initialize → Dispose → Initialize cycle on a future tab switch).
        composer.Initialize(canvas);

        renderMode.InitializeCount.ShouldBe(2);
        renderMode.FrameRequestSubscriberCount.ShouldBe(1);
    }

    private sealed class SpyRenderMode : IRenderMode
    {
        private CoreMotionCanvas.FrameRequestHandler? _frameRequest;

        public int InitializeCount { get; private set; }
        public int DisposeCount { get; private set; }
        public int FrameRequestSubscriberCount =>
            _frameRequest?.GetInvocationList().Length ?? 0;

        public event CoreMotionCanvas.FrameRequestHandler FrameRequest
        {
            add => _frameRequest += value;
            remove => _frameRequest -= value;
        }

        public void InitializeRenderMode(CoreMotionCanvas canvas) => InitializeCount++;
        public void DisposeRenderMode() => DisposeCount++;
        public void InvalidateRenderer() { }
    }

    private sealed class SpyFrameTicker : IFrameTicker
    {
        public int InitializeCount { get; private set; }
        public int DisposeCount { get; private set; }

        public void InitializeTicker(CoreMotionCanvas canvas, IRenderMode renderMode) => InitializeCount++;
        public void DisposeTicker() => DisposeCount++;
    }
}
