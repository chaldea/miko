using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ISSUE-153: <c>Maxlength</c> on IonInput / IonTextarea only fed the counter, and a read-only
/// field could not be focused — <c>.input-readonly</c> set pointer-events:none in place of the
/// native readonly. Real nesting (IonApp &gt; IonPage &gt; IonContent &gt; IonList &gt; IonItem),
/// taps and typing through <see cref="MikoInteractionController"/>.
/// </summary>
public class ReadonlyMaxlengthTests : IDisposable
{
    private const int W = 390;
    private const int H = 844;

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;

    public ReadonlyMaxlengthTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private sealed class FormPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var app = builder.OpenComponent<IonApp>();
            app.ChildContent = a =>
            {
                var page = a.OpenComponent<IonPage>();
                page.ChildContent = p =>
                {
                    var content = p.OpenComponent<IonContent>();
                    content.ChildContent = c =>
                    {
                        var list = c.OpenComponent<IonList>();
                        list.ChildContent = l =>
                        {
                            Item(l, i =>
                            {
                                var input = i.OpenComponent<IonInput>();
                                input.Class = "limited";
                                input.Maxlength = 3;
                                i.CloseComponent();
                            });
                            Item(l, i =>
                            {
                                var textarea = i.OpenComponent<IonTextarea>();
                                textarea.Class = "limited-area";
                                textarea.Maxlength = 4;
                                i.CloseComponent();
                            });
                            Item(l, i =>
                            {
                                var input = i.OpenComponent<IonInput>();
                                input.Class = "locked";
                                input.Readonly = true;
                                input.Value = "fixed";
                                i.CloseComponent();
                            });
                        };
                        c.CloseComponent();
                    };
                    p.CloseComponent();
                };
                a.CloseComponent();
            };
            builder.CloseComponent();
        }

        private static void Item(RenderTreeBuilder builder, RenderFragment content)
        {
            var item = builder.OpenComponent<IonItem>();
            item.ChildContent = content;
            builder.CloseComponent();
        }
    }

    private MikoAppContext BuildApp()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRouter(router => router.MapRoute("/", typeof(FormPage)));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        app.Engine.Render(_canvas);
        return app;
    }

    private static T Native<T>(MikoAppContext app, string host) where T : Element
        => app.Engine.GetRoot()!.FindByClass(host).Single().FindByTagName(
            typeof(T) == typeof(TextAreaElement) ? "textarea" : "input").OfType<T>().Single();

    [Fact]
    public void Maxlength_CutsOffTypedAndPastedText()
    {
        var app = BuildApp();

        app.Controller.SetFocus(Native<InputElement>(app, "limited"));
        app.Controller.OnTextInput("ab");
        app.Engine.Render(_canvas);
        app.Controller.OnTextInput("cdef");   // a paste: only one character still fits
        app.Engine.Render(_canvas);
        Native<InputElement>(app, "limited").Value.ShouldBe("abc");

        app.Controller.SetFocus(Native<TextAreaElement>(app, "limited-area"));
        app.Controller.OnTextInput("12345");
        app.Engine.Render(_canvas);
        Native<TextAreaElement>(app, "limited-area").Value.ShouldBe("1234");
    }

    [Fact]
    public void ReadonlyInput_TakesFocusOnTap_ButNotEdits()
    {
        var app = BuildApp();
        var locked = Native<InputElement>(app, "locked");
        var (x, y) = Center(app, locked);

        app.Controller.OnPointerDown(x, y, MouseButton.Left);
        app.Controller.OnPointerUp(x, y, MouseButton.Left);
        app.Engine.Render(_canvas);

        Native<InputElement>(app, "locked").HasState(ElementState.Focus).ShouldBeTrue("a read-only field can be focused");

        app.Controller.OnTextInput("x");
        app.Engine.Render(_canvas);
        Native<InputElement>(app, "locked").Value.ShouldBe("fixed");
    }

    private static (float x, float y) Center(MikoAppContext app, Element element)
    {
        var box = FindBox(app.Engine.GetCurrentLayout()!, element)!.BoxModel.BorderBox;
        return (box.X + box.Width / 2, box.Y + box.Height / 2);
    }

    private static Miko.Layout.LayoutBox? FindBox(Miko.Layout.LayoutBox box, Element element)
    {
        if (ReferenceEquals(box.Element, element)) return box;
        foreach (var child in box.Children)
            if (FindBox(child, element) is { } found) return found;
        return null;
    }
}
