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
/// ISSUE-153 (IonAlertInput): a prompt alert reported the inputs' original values instead of what
/// was typed, ignored CssClass, rendered "textarea" as a single-line field, and a disabled text
/// field stayed editable. Driven through the real app pipeline: IonApp &gt; IonPage &gt; IonContent
/// &gt; IonAlert, typing through <see cref="MikoInteractionController"/>.
/// </summary>
public class IonAlertPromptTests : IDisposable
{
    private const int W = 390;
    private const int H = 844;

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;

    public IonAlertPromptTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private sealed class PromptPage : ComponentBase
    {
        internal static object? Data;

        private readonly IonAlertInput[] _inputs =
        [
            new() { Type = "text", Placeholder = "Name", Value = "initial", CssClass = "probe-name" },
            new() { Type = "textarea", Placeholder = "Notes", CssClass = "probe-notes" },
            new() { Type = "text", Value = "locked", Disabled = true, CssClass = "probe-locked" },
        ];

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
                        var alert = c.OpenComponent<IonAlert>();
                        alert.IsOpen = true;
                        alert.Header = "Prompt";
                        alert.Inputs = _inputs;
                        alert.Buttons = [new() { Text = "OK", Role = "confirm" }];
                        alert.OnDidDismiss = EventCallback.Factory.Create<IonOverlayDismissEventArgs>(
                            this, e => Data = e.Data);
                        c.CloseComponent();
                    };
                    p.CloseComponent();
                };
                a.CloseComponent();
            };
            builder.CloseComponent();
        }
    }

    private MikoAppContext BuildApp()
    {
        PromptPage.Data = null;
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRouter(router => router.MapRoute("/", typeof(PromptPage)));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        app.Engine.Render(_canvas);
        return app;
    }

    private static Element Find(MikoAppContext app, string cls)
        => app.Engine.GetRoot()!.FindByClass(cls).Single();

    private void Type(MikoAppContext app, string cls, string text)
    {
        var target = Find(app, cls);
        if (target is ITextEditable editable) editable.MoveCursorToEnd();
        app.Controller.SetFocus(target);
        app.Controller.OnTextInput(text);
        app.Engine.Render(_canvas);
    }

    [Fact]
    public void TypedText_IsReportedInTheDismissData()
    {
        var app = BuildApp();
        Type(app, "probe-name", "!");
        Type(app, "probe-notes", "two");

        var ok = Find(app, "alert-button");
        ok.OnClick!.Invoke(new MouseEventArgs { Target = ok });

        PromptPage.Data.ShouldBeAssignableTo<IEnumerable<string?>>()!
            .ShouldBe(["initial!", "two", "locked"]);
    }

    [Fact]
    public void Textarea_IsAMultilineField_AndCssClassIsApplied()
    {
        var app = BuildApp();

        Find(app, "probe-notes").ShouldBeOfType<TextAreaElement>().Placeholder.ShouldBe("Notes");
        Find(app, "probe-name").ShouldBeOfType<InputElement>().Placeholder.ShouldBe("Name");
    }

    [Fact]
    public void DisabledTextField_CannotBeEdited()
    {
        var app = BuildApp();
        var locked = (InputElement)Find(app, "probe-locked");
        locked.IsEditable.ShouldBeFalse();

        app.Controller.SetFocus(locked);
        app.Controller.OnTextInput("x");

        ((InputElement)Find(app, "probe-locked")).Value.ShouldBe("locked");
    }
}
