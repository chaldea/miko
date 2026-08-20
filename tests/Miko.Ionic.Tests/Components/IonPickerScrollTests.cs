using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Styling;
using Miko.Testing;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// 端到端复现 <c>issues/ion-picker.md</c> 问题 1：picker 是真正的滚轮——
/// <c>MikoEngine.ScrollBy</c> 滚动 <c>.picker-opts</c> 后选中居中项，点击某个 option 会把
/// 该 option 滚到滚轮中心。这依赖真实的 <see cref="MikoEngine"/>（驱动滚动与居中），因此
/// 采用与 <see cref="IonInfiniteScrollIntegrationTests"/> 相同的集成测试形态。
/// </summary>
public class IonPickerScrollTests : IDisposable
{
    private const float ViewportW = 400f;
    private const float ViewportH = 600f;

    private readonly SKSurface _surface =
        SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

    public void Dispose() => _surface.Dispose();

    /// <summary>
    /// 承载页：一个 <see cref="IonPicker"/> 包一个 <see cref="IonPickerColumn"/>，三个 option。
    /// 与 issue 示例页结构一致。
    /// </summary>
    private sealed class PickerHost : ComponentBase
    {
        [Parameter] public EventCallback<string> OnChange { get; set; }
        [Parameter] public string? Value { get; set; }
        [Parameter] public bool DisableMiddle { get; set; }

        private static readonly string[] Values = { "javascript", "typescript", "csharp" };

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<IonPicker>(0);
            builder.AddComponentParameter(1, nameof(IonPicker.ChildContent), (RenderFragment)(picker =>
            {
                picker.OpenComponent<IonPickerColumn>(0);
                picker.AddComponentParameter(1, nameof(IonPickerColumn.ValueChanged), OnChange);
                picker.AddComponentParameter(2, nameof(IonPickerColumn.Value), Value);
                picker.AddComponentParameter(3, nameof(IonPickerColumn.ChildContent), Options());
                picker.CloseComponent();
            }));
            builder.CloseComponent();
        }

        private RenderFragment Options() => builder =>
        {
            var seq = 0;
            foreach (var v in Values)
            {
                var captured = v;
                builder.OpenComponent<IonPickerColumnOption>(seq++);
                builder.AddComponentParameter(seq++, nameof(IonPickerColumnOption.Value), captured);
                builder.AddComponentParameter(seq++, nameof(IonPickerColumnOption.Disabled),
                    DisableMiddle && captured == "typescript");
                builder.AddComponentParameter(seq++, nameof(IonPickerColumnOption.ChildContent),
                    (RenderFragment)(b => b.AddContent(0, captured)));
                builder.CloseComponent();
            }
        };
    }

    /// <summary>
    /// Builds the DOM with a real <see cref="MikoEngine"/> registered in DI so the column can reach
    /// it (for centering), then initializes that same engine over the built DOM.
    /// </summary>
    private (Element Root, MikoEngine Engine) BuildAndInitialize(PickerHost host)
    {
        var engine = new MikoEngine();

        using var context = new TestContext
        {
            ViewportWidth = ViewportW,
            ViewportHeight = ViewportH,
        };
        context.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(HostPlatform.Android));
        context.Services.AddSingleton<MikoEngine>(engine);
        var ionicStyles = IonicStyleSheetFactory.CreateAllModes();
        context.AddStyleSheet(ionicStyles);

        var cut = context.Render<PickerHost>(p =>
        {
            p.Add(nameof(PickerHost.OnChange), host.OnChange);
            p.Add(nameof(PickerHost.Value), host.Value);
            p.Add(nameof(PickerHost.DisableMiddle), host.DisableMiddle);
        });
        var root = cut.Root;

        // TestContext performs its own layout and consumes InitialScrollTop before this integration
        // engine is initialized. Replay that already-applied offset into the engine under test.
        var opts = root.FindByClass("picker-opts").Single();
        if (cut.FindLayoutBox(opts)?.ScrollTop is > 0f and var initialScrollTop)
            opts.InitialScrollTop = initialScrollTop;

        engine.Initialize(
            root,
            new List<StyleSheet> { ionicStyles },
            _surface.Canvas,
            ViewportW,
            ViewportH);

        return (root, engine);
    }

    private static Miko.Layout.LayoutBox FindScroller(MikoEngine engine)
        => FindLayout(engine.GetCurrentLayout()!, "picker-opts").ShouldNotBeNull();

    private static Miko.Layout.LayoutBox? FindLayout(Miko.Layout.LayoutBox box, string className)
    {
        if (box.Element.HasClass(className)) return box;
        foreach (var child in box.Children)
        {
            var found = FindLayout(child, className);
            if (found != null) return found;
        }
        return null;
    }

    private static List<Element> FindByClass(MikoEngine engine, string className)
    {
        var result = new List<Element>();
        Walk(engine.GetCurrentLayout()!.Element, className, result);
        return result;
    }

    private static void Walk(Element element, string className, List<Element> result)
    {
        if (element.HasClass(className)) result.Add(element);
        foreach (var child in element.Children) Walk(child, className, result);
    }

    [Fact]
    public void ScrollingTheWheel_SelectsTheCenteredOption()
    {
        string? changed = null;
        var host = new PickerHost
        {
            OnChange = EventCallback.Factory.Create<string>(this, v => changed = v),
        };

        var (_, engine) = BuildAndInitialize(host);

        // 34px rows + 3 leading spacers: scrollTop 53 centers option index 1 ("typescript").
        engine.ScrollBy(ViewportW / 2, 100, 0, 53).ShouldBeTrue();

        changed.ShouldBe("typescript");
    }

    [Fact]
    public void ScrollingTheWheel_AtTheTop_SelectsTheFirstOption()
    {
        string? changed = null;
        var host = new PickerHost
        {
            OnChange = EventCallback.Factory.Create<string>(this, v => changed = v),
        };

        var (_, engine) = BuildAndInitialize(host);

        // scrollTop 19 centers option index 0 ("javascript").
        engine.ScrollBy(ViewportW / 2, 100, 0, 19).ShouldBeTrue();

        changed.ShouldBe("javascript");
    }

    [Fact]
    public void ClickingAnOption_CentersItInTheWheel()
    {
        var host = new PickerHost();

        var (_, engine) = BuildAndInitialize(host);

        // Click the second option ("typescript") — it must scroll to the wheel center (scrollTop 53).
        var option = FindByClass(engine, "ion-picker-column-option")[1];
        option.OnClick!.Invoke(new MouseEventArgs { Target = option });

        FindScroller(engine).ScrollTop.ShouldBe(53f);
    }

    [Fact]
    public void InitialValue_IsCenteredOnFirstLayout()
    {
        var (_, engine) = BuildAndInitialize(new PickerHost { Value = "csharp" });

        FindScroller(engine).ScrollTop.ShouldBe(87f);
    }

    [Fact]
    public void MouseWheelDelta_SnapsTheSelectedOptionToCenter()
    {
        string? changed = null;
        var host = new PickerHost
        {
            OnChange = EventCallback.Factory.Create<string>(this, value => changed = value),
        };
        var (_, engine) = BuildAndInitialize(host);

        // Desktop hosts use 40px per wheel notch. The nearest row is index 1, whose center is 53px.
        engine.ScrollBy(ViewportW / 2, 100, 0, 40).ShouldBeTrue();

        changed.ShouldBe("typescript");
        FindScroller(engine).ScrollTop.ShouldBe(53f);
    }

    [Fact]
    public void SmallScrollWithinSelectedRow_SnapsBackToItsCenter()
    {
        var (_, engine) = BuildAndInitialize(new PickerHost { Value = "typescript" });

        engine.ScrollBy(ViewportW / 2, 100, 0, 10).ShouldBeTrue();

        FindScroller(engine).ScrollTop.ShouldBe(53f);
    }

    [Fact]
    public void ScrollingToDisabledOption_SelectsAndSnapsToNextEnabledOption()
    {
        string? changed = null;
        var host = new PickerHost
        {
            DisableMiddle = true,
            OnChange = EventCallback.Factory.Create<string>(this, value => changed = value),
        };
        var (_, engine) = BuildAndInitialize(host);

        engine.ScrollBy(ViewportW / 2, 100, 0, 40).ShouldBeTrue();

        changed.ShouldBe("csharp");
        FindScroller(engine).ScrollTop.ShouldBe(87f);
    }

    [Fact]
    public void PickerWheel_HidesClassicScrollbarButRemainsScrollable()
    {
        var (_, engine) = BuildAndInitialize(new PickerHost());
        var scroller = FindScroller(engine);

        scroller.HasVerticalScrollbar.ShouldBeTrue();
        scroller.ShowsVerticalScrollbar.ShouldBeFalse();
        engine.HitTestScrollbar(scroller.BoxModel.PaddingBox.Right - 1, 100).ShouldBeNull();
        engine.ScrollBy(ViewportW / 2, 100, 0, 40).ShouldBeTrue();
    }
}
