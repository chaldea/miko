using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Platform;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Platform;

/// <summary>
/// Release-1.0.0 A2：软键盘遮挡。页面是手机上最常见的结构——纵向 flex 根、可滚动的内容区、
/// 底部固定的工具栏（等同 IonContent + IonFooter），输入框在内容区底部，键盘会盖住它。
/// </summary>
public class KeyboardInsetTests : IDisposable
{
    private const float Width = 390, Height = 844, Keyboard = 300;

    private readonly DivElement _root = new() { Class = "page" };
    private readonly DivElement _content = new() { Class = "content" };
    private readonly DivElement _footer = new() { Class = "footer" };
    private readonly InputElement _input = new() { Class = "field" };
    private readonly MikoEngine _engine = new MikoEngineBuilder().Build();
    private readonly MikoInteractionController _controller;
    private readonly SKSurface _surface = SKSurface.Create(new SKImageInfo((int)Width, (int)Height));

    public KeyboardInsetTests()
    {
        _content.Children.Add(new DivElement { Class = "spacer" });
        _content.Children.Add(_input);
        _root.Children.Add(_content);
        _root.Children.Add(_footer);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".page"] = new()
            {
                Display = Display.Flex, FlexDirection = FlexDirection.Column,
                Width = Length.Percent(100), Height = Length.Percent(100),
                PaddingBottom = Length.SafeAreaInsetBottom, BoxSizing = BoxSizing.BorderBox,
            },
            [".content"] = new() { FlexGrow = 1, FlexShrink = 1, FlexBasis = Length.Px(0), OverflowY = Overflow.Auto },
            [".spacer"] = new() { Display = Display.Block, Height = Length.Px(600) },
            [".field"] = new() { Display = Display.Block, Width = Length.Px(300), Height = Length.Px(40) },
            [".footer"] = new() { Display = Display.Block, Height = Length.Px(56), FlexShrink = 0 },
        });

        _controller = new MikoInteractionController(
            Options.Create(new MikoAppOptions { RootComponentFactory = () => _root, StyleSheets = { sheet } }),
            new EmptyServiceProvider(),
            _engine,
            new EventDispatcher(),
            new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance),
            NullLogger<MikoInteractionController>.Instance);
        _controller.Initialize(_surface.Canvas, Width, Height);
        _controller.SetSafeAreaInsets(0, 47, 0, 34);
        Frame();
    }

    public void Dispose() => _surface.Dispose();

    private void Frame() => _controller.RenderFrame(_surface.Canvas, Width, Height, 1f / 60f, _engine.Render);

    private static RectF Box(Element e) => e.LayoutBox!.BoxModel.BorderBox;

    private float InputScreenBottom => Box(_input).Bottom - _content.ScrollTop;

    [Fact]
    public void Should_lay_out_above_the_keyboard_and_drop_the_bottom_safe_area()
    {
        Box(_footer).Bottom.ShouldBe(Height - 34, 0.5f);

        _controller.SetKeyboardInset(Keyboard);
        Frame();

        // The footer sits on top of the keyboard: no home-indicator gap, it is under the keyboard.
        Box(_footer).Bottom.ShouldBe(Height - Keyboard, 0.5f);
        Box(_content).Bottom.ShouldBe(Height - Keyboard - 56, 0.5f);
    }

    [Fact]
    public void Should_scroll_the_focused_input_into_view_when_the_keyboard_opens()
    {
        _controller.SetFocus(_input);
        Frame();
        InputScreenBottom.ShouldBeLessThan(Box(_content).Bottom);   // visible before the keyboard

        _controller.SetKeyboardInset(Keyboard);
        Frame();   // relayout above the keyboard, then reveal
        Frame();   // paint the scrolled content

        _content.ScrollTop.ShouldBeGreaterThan(0f);
        InputScreenBottom.ShouldBeLessThanOrEqualTo(Box(_content).Bottom + 0.5f);
        (Box(_input).Top - _content.ScrollTop).ShouldBeGreaterThanOrEqualTo(Box(_content).Top);
    }

    [Fact]
    public void Should_not_scroll_when_nothing_is_focused()
    {
        _controller.SetKeyboardInset(Keyboard);
        Frame();
        Frame();

        _content.ScrollTop.ShouldBe(0f);
    }

    [Fact]
    public void Should_restore_the_layout_when_the_keyboard_closes()
    {
        _controller.SetKeyboardInset(Keyboard);
        Frame();
        _controller.SetKeyboardInset(0);
        Frame();

        Box(_footer).Bottom.ShouldBe(Height - 34, 0.5f);
        _engine.KeyboardInset.ShouldBe(0f);
    }

    [Fact]
    public void Should_reveal_an_input_focused_while_the_keyboard_is_open()
    {
        _controller.SetKeyboardInset(Keyboard);
        Frame();
        _content.ScrollTop.ShouldBe(0f);

        _controller.SetFocus(_input);   // e.g. "next field": the keyboard height does not change
        Frame();
        Frame();

        InputScreenBottom.ShouldBeLessThanOrEqualTo(Box(_content).Bottom + 0.5f);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
