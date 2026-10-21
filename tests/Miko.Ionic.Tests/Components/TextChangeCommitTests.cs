using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Ionic.Tests.Fixtures;
using Miko.Platform;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ISSUE-153 problem 6: <c>IonInput.OnChange</c> / <c>IonTextarea.OnChange</c> never fired.
/// Both listen to the native element's <c>@onchange</c>, but the controller dispatched
/// <c>change</c> only for checkboxes, radios, selects and ranges. A text control commits its value
/// — and fires <c>change</c> — when it loses focus with a value different from the one it had on
/// focus, or when Enter is pressed in a single-line control (HTML and Ionic's <c>ionChange</c>).
/// </summary>
public class TextChangeCommitTests : IDisposable
{
    private const int W = 390;
    private const int H = 844;

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;

    public TextChangeCommitTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    /// <summary>IonApp &gt; IonPage &gt; IonContent &gt; IonList &gt; IonItem &gt; IonInput / IonTextarea.</summary>
    private sealed class FormPage : ComponentBase
    {
        internal static readonly List<string> Log = new();

        public string? Bound { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var app = builder.OpenComponent<IonApp>();
            app.ChildContent = b =>
            {
                var page = b.OpenComponent<IonPage>();
                page.ChildContent = b2 =>
                {
                    var content = b2.OpenComponent<IonContent>();
                    content.ChildContent = b3 =>
                    {
                        var list = b3.OpenComponent<IonList>();
                        list.ChildContent = b4 =>
                        {
                            var item1 = b4.OpenComponent<IonItem>();
                            item1.ChildContent = b5 =>
                            {
                                var input = b5.OpenComponent<IonInput>();
                                input.Label = "Name";
                                input.Class = "name";
                                input.Value = Bound;
                                input.ValueChanged = EventCallback.Factory.Create<string?>(this, v => Bound = v);
                                input.OnChange = EventCallback.Factory.Create<string?>(this, v => Log.Add($"input:{v}"));
                                b5.CloseComponent();
                            };
                            b4.CloseComponent();

                            var item2 = b4.OpenComponent<IonItem>();
                            item2.ChildContent = b5 =>
                            {
                                var textarea = b5.OpenComponent<IonTextarea>();
                                textarea.Label = "Notes";
                                textarea.Class = "notes";
                                textarea.OnChange = EventCallback.Factory.Create<string?>(this, v => Log.Add($"textarea:{v}"));
                                b5.CloseComponent();
                            };
                            b4.CloseComponent();
                        };
                        b3.CloseComponent();
                    };
                    b2.CloseComponent();
                };
                b.CloseComponent();
            };
            builder.CloseComponent();
        }
    }

    private MikoAppContext BuildApp()
    {
        FormPage.Log.Clear();
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRouter(router => router.MapRoute("/", typeof(FormPage)));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        app.Engine.Render(_canvas);
        return app;
    }

    private static Element Native(MikoAppContext app, string hostClass, string nativeClass)
        => app.Engine.GetRoot()!.FindByClass(hostClass).Single().FindByClass(nativeClass).Single();

    private void Type(MikoAppContext app, Element target, string text)
    {
        app.Controller.SetFocus(target);
        app.Engine.Render(_canvas);
        app.Controller.OnTextInput(text);
        app.Engine.Render(_canvas);
    }

    [Fact]
    public void IonInput_OnChange_FiresOnBlurAfterEditing()
    {
        var app = BuildApp();
        Type(app, Native(app, "name", "native-input"), "Ada");

        FormPage.Log.ShouldBeEmpty("typing alone does not commit");

        app.Controller.SetFocus(null);

        FormPage.Log.ShouldBe(["input:Ada"]);
    }

    [Fact]
    public void IonInput_OnChange_DoesNotFireWhenValueIsUnchanged()
    {
        var app = BuildApp();
        app.Controller.SetFocus(Native(app, "name", "native-input"));
        app.Controller.SetFocus(null);

        FormPage.Log.ShouldBeEmpty();
    }

    [Fact]
    public void IonInput_OnChange_FiresOnEnter_ThenNotAgainOnBlur()
    {
        var app = BuildApp();
        Type(app, Native(app, "name", "native-input"), "Ada");

        app.Controller.OnKeyDown(MikoKey.Enter, MikoKeyModifiers.None);
        FormPage.Log.ShouldBe(["input:Ada"]);

        app.Engine.Render(_canvas);
        app.Controller.SetFocus(null);
        FormPage.Log.ShouldBe(["input:Ada"], "the value was already committed by Enter");
    }

    [Fact]
    public void IonInput_BlurAfterChange_StillClearsFocusOnTheLiveInstance()
    {
        // The change callback re-renders the page; the blur must land on the replacement.
        var app = BuildApp();
        Type(app, Native(app, "name", "native-input"), "Ada");

        app.Controller.SetFocus(null);
        app.Engine.Render(_canvas);

        Native(app, "name", "native-input").HasState(ElementState.Focus).ShouldBeFalse();
    }

    [Fact]
    public void IonTextarea_OnChange_FiresOnBlur_AndEnterInsertsANewline()
    {
        var app = BuildApp();
        var textarea = Native(app, "notes", "native-textarea");
        Type(app, textarea, "line");
        app.Controller.OnKeyDown(MikoKey.Enter, MikoKeyModifiers.None);

        FormPage.Log.ShouldBeEmpty("Enter in a multi-line control inserts a newline, it does not commit");

        app.Engine.Render(_canvas);
        app.Controller.SetFocus(null);

        FormPage.Log.ShouldBe(["textarea:line\n"]);
    }

    /// <summary>
    /// Element-level <c>&lt;input @bind&gt;</c> / <c>&lt;textarea @bind&gt;</c> default to the change
    /// event, so they never wrote back either.
    /// </summary>
    [Fact]
    public void PlainBind_WritesBackOnBlur()
    {
        PlainInputBindFixture.InputText = null;
        PlainInputBindFixture.AreaText = null;
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseRouter(router => router.MapRoute("/", typeof(PlainInputBindFixture)));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        app.Engine.Render(_canvas);

        Type(app, app.Engine.GetRoot()!.FindByClass("plain-input").Single(), "hi");
        Type(app, app.Engine.GetRoot()!.FindByClass("plain-textarea").Single(), "there");
        PlainInputBindFixture.InputText.ShouldBe("hi", "focusing the textarea blurs the input");

        app.Controller.SetFocus(null);
        PlainInputBindFixture.AreaText.ShouldBe("there");
    }
}
