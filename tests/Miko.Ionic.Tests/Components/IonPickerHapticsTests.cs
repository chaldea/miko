using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Styling;
using Miko.Testing;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// The picker wheel's "tick" (ISSUE-149): a selection opens on the first scroll of a gesture, every
/// change of the centered option ticks once, and the selection closes once the wheel has been still
/// for a moment. Scrolling needs a real <see cref="MikoEngine"/>, so these follow the integration
/// shape of <see cref="IonPickerScrollTests"/>.
/// </summary>
public class IonPickerHapticsTests : IDisposable
{
    private const float ViewportW = 400f;
    private const float ViewportH = 600f;

    // The column closes a selection 250ms after the last scroll; allow plenty of slack on CI.
    private static readonly TimeSpan EndTimeout = TimeSpan.FromSeconds(5);

    private readonly SKSurface _surface =
        SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

    private readonly RecordingHapticFeedback _haptics = new();

    public void Dispose() => _surface.Dispose();

    private static readonly string[] Values = { "javascript", "typescript", "csharp" };

    /// <summary>One picker, one column, three options. Value changes go to an outside callback.</summary>
    private sealed class PickerHost : ComponentBase
    {
        [Parameter] public string? Value { get; set; }
        [Parameter] public bool Disabled { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var picker = builder.OpenComponent<IonPicker>();
            picker.ChildContent = (RenderFragment)(b =>
            {
                var column = b.OpenComponent<IonPickerColumn>();
                column.Value = Value;
                column.Disabled = Disabled;
                column.ChildContent = Options;
                b.CloseComponent();
            });
            builder.CloseComponent();
        }
    }

    /// <summary>
    /// Binds the column two-way, as <c>@bind-Value</c> does: every tick re-renders this host, which
    /// recreates the column mid-gesture (ISSUE-138 constraint 1).
    /// </summary>
    private sealed class BindingHost : ComponentBase
    {
        private string? _value = "javascript";

        /// <summary>Every value the column reported, in order.</summary>
        [Parameter] public List<string>? Reported { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var picker = builder.OpenComponent<IonPicker>();
            picker.ChildContent = (RenderFragment)(b =>
            {
                var column = b.OpenComponent<IonPickerColumn>();
                column.Value = _value;
                column.ValueChanged = EventCallback.Factory.Create<string>(this, value =>
                {
                    _value = value;
                    Reported?.Add(value);
                });
                column.ChildContent = Options;
                b.CloseComponent();
            });
            builder.CloseComponent();
        }
    }

    private static readonly RenderFragment Options = builder =>
    {
        var seq = 0;
        foreach (var value in Values)
        {
            var captured = value;
            builder.OpenComponent<IonPickerColumnOption>(seq++);
            builder.AddComponentParameter(seq++, nameof(IonPickerColumnOption.Value), captured);
            builder.AddComponentParameter(seq++, nameof(IonPickerColumnOption.ChildContent),
                (RenderFragment)(b => b.AddContent(0, captured)));
            builder.CloseComponent();
        }
    };

    private MikoEngine BuildAndInitialize<THost>(Action<ComponentParameterBuilder<THost>>? parameters = null)
        where THost : ComponentBase, new()
    {
        var engine = new MikoEngineBuilder().Build();
        var platform = new PlatformInfo(HostPlatform.Ios) { Haptics = _haptics };

        using var context = new TestContext { ViewportWidth = ViewportW, ViewportHeight = ViewportH };
        context.Services.AddSingleton<IPlatformInfo>(platform);
        context.Services.AddSingleton<IPlatformService>(platform);
        context.Services.AddSingleton<MikoEngine>(engine);
        var ionicStyles = IonicStyleSheetFactory.CreateAllModes();
        context.AddStyleSheet(ionicStyles);

        var cut = context.Render(parameters);
        var opts = cut.Root.FindByClass("picker-opts").Single();
        if (cut.FindLayoutBox(opts)?.ScrollTop is > 0f and var initialScrollTop)
            opts.InitialScrollTop = initialScrollTop;

        engine.Initialize(cut.Root, new List<StyleSheet> { ionicStyles }, _surface.Canvas, ViewportW, ViewportH);
        return engine;
    }

    private static Miko.Layout.LayoutBox FindScroller(MikoEngine engine)
        => FindLayout(engine.GetCurrentLayout()!, "picker-opts").ShouldNotBeNull();

    private static Miko.Layout.LayoutBox? FindLayout(Miko.Layout.LayoutBox box, string className)
    {
        if (box.Element.HasClass(className)) return box;
        foreach (var child in box.Children)
            if (FindLayout(child, className) is { } found) return found;
        return null;
    }

    // Polls without blocking a thread: test classes run in parallel, and a blocked or spinning
    // thread starves the timer-driven tests elsewhere in the suite.
    private static async Task<bool> WaitUntilAsync(Func<bool> condition)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.Elapsed > EndTimeout) return false;
            await Task.Delay(10);
        }
        return true;
    }

    private async Task WaitForSelectionEndAsync()
        => (await WaitUntilAsync(() => _haptics.Count("end") >= 1))
            .ShouldBeTrue($"selection did not end; calls: {string.Join(", ", _haptics.Calls)}");

    [Fact]
    public void ScrollingOntoAnotherOption_StartsASelectionAndTicksOnce()
    {
        var engine = BuildAndInitialize<PickerHost>();

        // 34px rows + 3 leading spacers: scrollTop 53 centers option index 1.
        engine.ScrollBy(ViewportW / 2, 100, 0, 53).ShouldBeTrue();

        _haptics.Calls.ShouldBe(new[] { "start", "changed" });
    }

    [Fact]
    public void ScrollingWithinTheSelectedOption_DoesNotTick()
    {
        var engine = BuildAndInitialize<PickerHost>(p => p.Add(nameof(PickerHost.Value), "typescript"));

        // The wheel snaps back to the same option: a gesture, but no gear click.
        engine.ScrollBy(ViewportW / 2, 100, 0, 10).ShouldBeTrue();

        _haptics.Count("changed").ShouldBe(0);
        _haptics.Count("start").ShouldBe(1);
    }

    [Fact]
    public async Task Selection_EndsOnceTheWheelStops()
    {
        var engine = BuildAndInitialize<PickerHost>();

        engine.ScrollBy(ViewportW / 2, 100, 0, 53).ShouldBeTrue();
        await WaitForSelectionEndAsync();

        _haptics.Calls.ShouldBe(new[] { "start", "changed", "end" });
    }

    [Fact]
    public async Task ConsecutiveScrolls_ShareOneSelection_AndTickPerOption()
    {
        var engine = BuildAndInitialize<PickerHost>();

        engine.ScrollBy(ViewportW / 2, 100, 0, 53).ShouldBeTrue();  // → typescript
        engine.ScrollBy(ViewportW / 2, 100, 0, 34).ShouldBeTrue();  // → csharp
        await WaitForSelectionEndAsync();

        _haptics.Calls.ShouldBe(new[] { "start", "changed", "changed", "end" });
    }

    [Fact]
    public void TappingAnOption_TicksOnce()
    {
        var engine = BuildAndInitialize<PickerHost>();

        var option = engine.GetCurrentLayout()!.Element.FindByClass("ion-picker-column-option")[2];
        option.OnClick!.Invoke(new MouseEventArgs { Target = option });

        _haptics.Calls.ShouldBe(new[] { "start", "changed" });
    }

    [Fact]
    public void DisabledColumn_IsSilent()
    {
        var engine = BuildAndInitialize<PickerHost>(p => p.Add(nameof(PickerHost.Disabled), true));

        engine.ScrollBy(ViewportW / 2, 100, 0, 53);

        _haptics.Calls.ShouldBeEmpty();
    }

    /// <summary>
    /// A slow finger drag moves the wheel a few pixels per event. The wheel must follow it — ticking
    /// as each option passes the center — instead of snapping back to the selected option on every
    /// event, and the bound value is committed once, when the finger lifts. Driven through the real
    /// interaction controller, as the Android and iOS hosts do.
    /// </summary>
    [Fact]
    public void SlowTouchDrag_FollowsTheFinger_TicksPerOption_AndCommitsOnRelease()
    {
        var reported = new List<string>();
        var engine = BuildAndInitialize<BindingHost>(p => p.Add(nameof(BindingHost.Reported), reported));
        using var services = new ServiceCollection().BuildServiceProvider();
        var input = new MikoInteractionController(
            Microsoft.Extensions.Options.Options.Create(new MikoAppOptions()), services, engine, new EventDispatcher(), new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance), NullLogger<MikoInteractionController>.Instance);

        var wheel = FindScroller(engine).BoxModel.BorderBox;
        float x = wheel.X + wheel.Width / 2, y = wheel.Y + wheel.Height / 2;
        input.OnPointerDown(x, y, MouseButton.Left, PointerType.Touch);
        // Finger up by 90px in 3px steps: the content scrolls down from "javascript" (scrollTop 19).
        for (var moved = 3f; moved <= 90f; moved += 3f)
        {
            input.OnPointerMove(x, y - moved);
            engine.Render(_surface.Canvas);
        }

        // Past "typescript" onto "csharp" without snapping, and nothing committed yet.
        _haptics.Count("changed").ShouldBe(2);
        reported.ShouldBeEmpty();
        FindScroller(engine).ScrollTop.ShouldNotBe(87f);

        input.OnPointerUp(x, y - 90f, MouseButton.Left, PointerType.Touch);
        engine.Render(_surface.Canvas);

        reported.ShouldBe(new[] { "csharp" });
        FindScroller(engine).ScrollTop.ShouldBe(87f);
    }

    /// <summary>
    /// With two-way binding the column is rebuilt on every tick. Each instance closes the selection
    /// it opened (on dispose), so selections stay balanced and every option change still ticks once.
    /// </summary>
    [Fact]
    public async Task TwoWayBinding_RecreatesTheColumnPerTick_SelectionsStayBalanced()
    {
        var engine = BuildAndInitialize<BindingHost>();

        // The host starts on "javascript" (scrollTop 19); each 34px row moves the wheel one option.
        engine.ScrollBy(ViewportW / 2, 100, 0, 34).ShouldBeTrue();  // → typescript, host re-renders
        engine.Render(_surface.Canvas);
        engine.ScrollBy(ViewportW / 2, 100, 0, 34).ShouldBeTrue();  // → csharp on the new column
        engine.Render(_surface.Canvas);

        _haptics.Count("changed").ShouldBe(2);
        (await WaitUntilAsync(() => _haptics.Count("end") == _haptics.Count("start")))
            .ShouldBeTrue($"unbalanced selection; calls: {string.Join(", ", _haptics.Calls)}");
    }
}
