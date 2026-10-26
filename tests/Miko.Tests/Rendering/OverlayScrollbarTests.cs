using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Layout;
using Miko.Platform;
using Miko.Rendering;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// Overlay 滚动条（macOS 的默认样式，ISSUE-156）：平时不画也不拦截点击；容器一滚动就显现，
/// 停下后保持 <see cref="OverlayScrollbarActivity.HoldSeconds"/> 再淡出。
/// <para>保持期按真实时钟计且<b>不出帧</b>——画面不变，宿主照常空闲（ISSUE-096）；保持期结束时
/// 向调度器投递唤醒，按需绘制的宿主（Android）才会开始淡出。时钟经 DI 的 <see cref="TimeProvider"/>
/// 注入，用例里手动推进，不依赖真实等待。</para>
/// </summary>
public class OverlayScrollbarTests : IDisposable
{
    private const int W = 400;
    private const int H = 300;

    // 竖直 overlay 轨道占右边 11px（389..400），滑块左右各缩进 2px；滚动前滑块位于顶部。
    private const int BarX = 394;
    private const int ThumbY = 60;

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;
    private readonly ManualClock _clock = new();
    private readonly MikoDispatcher _dispatcher = new();

    public OverlayScrollbarTests()
    {
        _canvas = new SKCanvas(_bitmap);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Fact]
    public void Should_neither_paint_nor_hit_test_the_scrollbar_before_scrolling()
    {
        var engine = CreateEngine(ScrollbarStyle.Overlay);

        Paint(engine);

        Pixel(BarX, ThumbY).ShouldBe(SKColors.White);
        engine.HitTestScrollbar(BarX, ThumbY).ShouldBeNull("an invisible bar must not swallow the click");
        engine.HasVisibleOverlayScrollbars.ShouldBeFalse();
        engine.HasPendingRenderWork.ShouldBeFalse();
    }

    [Fact]
    public void Should_paint_and_hit_test_the_scrollbar_once_the_container_scrolls()
    {
        var engine = CreateEngine(ScrollbarStyle.Overlay);
        Paint(engine);

        engine.ScrollBy(200, 150, 0, 50).ShouldBeTrue();
        Paint(engine);

        engine.HasVisibleOverlayScrollbars.ShouldBeTrue();
        Pixel(BarX, ThumbY).Red.ShouldBeLessThan((byte)160, "the thumb is drawn at full opacity");
        engine.HitTestScrollbar(BarX, ThumbY)!.HitType.ShouldBe(MikoEngine.ScrollbarHitType.VerticalThumb);
    }

    [Fact]
    public void Should_let_the_host_idle_while_the_scrollbar_holds_then_fade_it_out()
    {
        var engine = CreateEngine(ScrollbarStyle.Overlay);
        Paint(engine);
        engine.ScrollBy(200, 150, 0, 50);
        Paint(engine);

        // 保持期：画面不变，不需要出帧。
        engine.HasPendingRenderWork.ShouldBeFalse("the hold must not keep the host rendering identical frames");
        _clock.Advance(TimeSpan.FromSeconds(0.9));
        engine.HasPendingRenderWork.ShouldBeFalse();
        Paint(engine);
        var full = Pixel(BarX, ThumbY).Red;
        full.ShouldBeLessThan((byte)160);

        // 淡出期：逐帧推进，滑块越来越淡。
        _clock.Advance(TimeSpan.FromSeconds(0.25)); // 1.15s：淡出过半
        engine.HasPendingRenderWork.ShouldBeTrue("the fade animates every frame");
        Paint(engine);
        var fading = Pixel(BarX, ThumbY).Red;
        fading.ShouldBeGreaterThan(full);
        fading.ShouldBeLessThan((byte)255);
        engine.HitTestScrollbar(BarX, ThumbY).ShouldNotBeNull("a bar that is still visible can be grabbed");

        // 完全淡出后：还要出一帧把它擦掉，此后宿主回到空闲。
        _clock.Advance(TimeSpan.FromSeconds(0.2)); // 1.35s
        engine.HasPendingRenderWork.ShouldBeTrue("one more frame erases the last faded thumb");
        Paint(engine);
        Pixel(BarX, ThumbY).ShouldBe(SKColors.White);
        engine.HasVisibleOverlayScrollbars.ShouldBeFalse();
        engine.HasPendingRenderWork.ShouldBeFalse();
        engine.HitTestScrollbar(BarX, ThumbY).ShouldBeNull();
    }

    [Fact]
    public void Should_wake_the_host_when_the_hold_ends()
    {
        // Android 按需绘制、不轮询：保持期不出帧，就得有人在淡出开始时把它叫醒。
        var engine = CreateEngine(ScrollbarStyle.Overlay);
        var posts = 0;
        _dispatcher.Posted += () => posts++;
        Paint(engine);

        engine.ScrollBy(200, 150, 0, 20);
        engine.ScrollBy(200, 150, 0, 20); // 每次滚动都把唤醒往后推
        Paint(engine);
        _clock.Advance(TimeSpan.FromSeconds(0.9));
        posts.ShouldBe(0);

        _clock.Advance(TimeSpan.FromSeconds(0.2));
        posts.ShouldBe(1);
        _dispatcher.HasPendingActions.ShouldBeTrue();
    }

    [Fact]
    public void Should_keep_the_scrollbar_visible_while_its_thumb_is_dragged()
    {
        var engine = CreateEngine(ScrollbarStyle.Overlay);
        Paint(engine);
        engine.ScrollBy(200, 150, 0, 50);
        Paint(engine);

        // 拖动滑块就是在用它：每次拖动都重新计时，哪怕这一步没有改变滚动位置，
        // 滚动条也不会在手底下淡出。
        _clock.Advance(TimeSpan.FromSeconds(0.9));
        var hit = engine.HitTestScrollbar(BarX, ThumbY);
        hit.ShouldNotBeNull();
        engine.DragVerticalThumb(hit!.Box, ThumbY, hit.ThumbOffset).ShouldBeFalse("the thumb did not move");
        _clock.Advance(TimeSpan.FromSeconds(0.9));
        engine.DragVerticalThumb(hit.Box, ThumbY, hit.ThumbOffset);
        _clock.Advance(TimeSpan.FromSeconds(0.9));
        Paint(engine);

        engine.HasPendingRenderWork.ShouldBeFalse();
        Pixel(BarX, ThumbY).Red.ShouldBeLessThan((byte)160);
    }

    [Fact]
    public void Should_paint_content_under_an_overlay_scrollbar()
    {
        // classic 滚动条占位，内容裁剪到它左侧；overlay 盖在内容上，内容一直画到容器边缘。
        var classic = CreateEngine(ScrollbarStyle.Classic, contentColor: Color.FromHex("0000ff"));
        Paint(classic);
        Pixel(BarX, 250).ShouldBe(new SKColor(0xF1, 0xF1, 0xF1), "the classic track");

        var overlay = CreateEngine(ScrollbarStyle.Overlay, contentColor: Color.FromHex("0000ff"));
        Paint(overlay);
        Pixel(BarX, 250).ShouldBe(SKColors.Blue);
    }

    [Theory]
    [InlineData(ScrollbarStyle.Classic, 800f - H + LayoutBox.ScrollbarThickness)]
    [InlineData(ScrollbarStyle.Overlay, 800f - H)]
    public void Should_not_extend_the_scroll_range_for_an_overlay_horizontal_scrollbar(
        ScrollbarStyle style, float maxScrollTop)
    {
        // classic 横向滚动条盖住视口底边，要多滚它的厚度才看得到末尾；overlay 不占位，不需要。
        var engine = CreateEngine(style, contentWidth: 800);
        Paint(engine);

        engine.ScrollBy(200, 150, 0, 10_000);

        engine.LastScrollResult.Box!.ScrollTop.ShouldBe(maxScrollTop);
    }

    [Fact]
    public void Should_hide_overlay_scrollbars_when_the_style_changes()
    {
        var engine = CreateEngine(ScrollbarStyle.Overlay);
        Paint(engine);
        engine.ScrollBy(200, 150, 0, 50);
        engine.HasVisibleOverlayScrollbars.ShouldBeTrue();

        engine.ScrollbarStyle = ScrollbarStyle.None;
        Paint(engine);

        engine.HasVisibleOverlayScrollbars.ShouldBeFalse();
        engine.HitTestScrollbar(BarX, ThumbY).ShouldBeNull();
    }

    [Fact]
    public void Should_never_track_classic_or_hidden_scrollbars()
    {
        foreach (var style in new[] { ScrollbarStyle.Classic, ScrollbarStyle.None })
        {
            var engine = CreateEngine(style);
            Paint(engine);
            engine.ScrollBy(200, 150, 0, 50).ShouldBeTrue();

            engine.HasVisibleOverlayScrollbars.ShouldBeFalse();
            engine.HasPendingRenderWork.ShouldBeTrue("the scroll itself repaints");
            Paint(engine);
            engine.HasPendingRenderWork.ShouldBeFalse();
        }
    }

    [Fact]
    public void Should_follow_a_scroll_container_replaced_by_a_rerender()
    {
        // 组件重渲染用新实例替换滚动容器：滚动条跟到接替它的元素上，而不是闪没。
        var activity = new OverlayScrollbarActivity(_clock);
        var before = new DivElement();
        var after = new DivElement();
        activity.NoteActivity(before);
        before.SupersededBy = after;
        activity.BeginFrame();

        activity.GetOpacity(after).ShouldBe(1f);
    }

    // ================================ helpers ================================

    private MikoEngine CreateEngine(ScrollbarStyle style, Color? contentColor = null, float contentWidth = 0)
    {
        var engine = new MikoEngineBuilder()
            .ConfigureServices(s =>
            {
                s.AddSingleton<TimeProvider>(_clock);
                s.AddSingleton(_dispatcher);
            })
            .Build();
        engine.ScrollbarStyle = style;

        var content = new DivElement { Style = new Style { Height = Length.Px(800) } };
        if (contentColor is { } color) content.Style!.BackgroundColor = color;
        if (contentWidth > 0) content.Style!.Width = Length.Px(contentWidth);

        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(W),
                Height = Length.Px(H),
                OverflowY = Overflow.Auto,
                OverflowX = contentWidth > 0 ? Overflow.Auto : null,
                BackgroundColor = Color.FromHex("ffffff"),
            },
            Children = { content },
        };
        engine.Initialize(root, new List<StyleSheet>(), _canvas, W, H);
        return engine;
    }

    private void Paint(MikoEngine engine)
    {
        _canvas.Clear(SKColors.White);
        engine.Render(_canvas);
        _canvas.Flush();
    }

    private SKColor Pixel(int x, int y) => _bitmap.GetPixel(x, y);

    /// <summary>只在用例推进时才走的时钟；计时器到点同步触发。</summary>
    private sealed class ManualClock : TimeProvider
    {
        private readonly List<ManualTimer> _timers = new();
        private long _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            timer.Change(dueTime, period);
            return timer;
        }

        public void Advance(TimeSpan by)
        {
            _ticks += by.Ticks;
            foreach (var timer in _timers.ToArray())
            {
                if (timer.DueAt is { } due && due <= _ticks)
                {
                    timer.DueAt = null;
                    timer.Fire();
                }
            }
        }

        private sealed class ManualTimer(ManualClock clock, TimerCallback callback, object? state) : ITimer
        {
            public long? DueAt { get; set; }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                DueAt = dueTime == Timeout.InfiniteTimeSpan ? null : clock._ticks + dueTime.Ticks;
                if (!clock._timers.Contains(this)) clock._timers.Add(this);
                return true;
            }

            public void Fire() => callback(state);

            public void Dispose() => clock._timers.Remove(this);

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
