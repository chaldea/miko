using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Ionic.Tests.Fixtures;
using Miko.Layout;
using Miko.Platform;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests;

/// <summary>
/// Release-1.0.0 A2 end to end with real Ionic components (IonPage › IonHeader / IonContent /
/// IonFooter): when the soft keyboard opens, the footer moves above it and the focused field that
/// it would cover scrolls into view; closing the keyboard restores the page.
/// </summary>
public class KeyboardAvoidanceTests : IDisposable
{
    private const float W = 390, H = 844, Keyboard = 320, SafeBottom = 24;

    private readonly SKBitmap _bitmap = new((int)W, (int)H);
    private readonly SKCanvas _canvas;
    private readonly MikoAppContext _app;

    public KeyboardAvoidanceTests()
    {
        _canvas = new SKCanvas(_bitmap);
        var sheet = new StyleSheet();
        sheet.Add(new CssObject { [".spacer"] = new() { Display = Display.Block, Height = Length.Px(900) } });

        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.AddStyleSheet(sheet);
        builder.UseRouter(router => router.MapRoute("/", typeof(KeyboardFormFixture)));
        _app = builder.Build();
        _app.Controller.Initialize(_canvas, W, H);
        _app.Controller.SetSafeAreaInsets(0, 24, 0, SafeBottom);
        Frame();
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private void Frame() => _app.Controller.RenderFrame(_canvas, W, H, 1f / 60f, _app.Engine.Render);

    private Element Single(string className) => _app.Engine.GetRoot()!.FindByClass(className).Single();

    private InputElement NativeInput => Single("last-field").FindByClass("native-input").OfType<InputElement>().Single();

    /// <summary>Border box on screen: layout position minus the scroll offsets of every ancestor.</summary>
    private RectF ScreenBox(Element element)
    {
        var box = FindBox(_app.Engine.GetCurrentLayout()!, element)!.BoxModel.BorderBox;
        float scroll = 0;
        for (var p = element.Parent; p != null; p = p.Parent)
            scroll += p.ScrollTop;
        return new RectF(box.Left, box.Top - scroll, box.Width, box.Height);
    }

    private static LayoutBox? FindBox(LayoutBox box, Element element)
    {
        if (box.Element == element) return box;
        foreach (var child in box.Children)
            if (FindBox(child, element) is { } found) return found;
        return null;
    }

    [Fact]
    public void Should_keep_the_footer_and_the_focused_field_above_the_keyboard()
    {
        var footer = Single("footer-title");
        ScreenBox(NativeInput).Top.ShouldBeGreaterThan(H, "the field starts below the fold");

        _app.Controller.SetFocus(NativeInput);
        _app.Controller.SetKeyboardInset(Keyboard);
        Frame();
        Frame();

        var keyboardTop = H - Keyboard;
        ScreenBox(footer).Bottom.ShouldBeLessThanOrEqualTo(keyboardTop + 0.5f, "the footer sits on the keyboard");
        var field = ScreenBox(NativeInput);
        field.Bottom.ShouldBeLessThanOrEqualTo(keyboardTop - ScreenBox(footer).Height + 0.5f,
            "the focused field is visible between the header and the footer");
        field.Top.ShouldBeGreaterThan(0f);
    }

    [Fact]
    public void Should_restore_the_footer_when_the_keyboard_closes()
    {
        var footer = Single("footer-title");
        var before = ScreenBox(footer).Bottom;

        _app.Controller.SetKeyboardInset(Keyboard);
        Frame();
        _app.Controller.SetKeyboardInset(0);
        Frame();

        ScreenBox(footer).Bottom.ShouldBe(before, 0.5f);
    }
}
