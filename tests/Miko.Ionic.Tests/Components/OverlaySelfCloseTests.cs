using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ISSUE-153: an overlay that closes itself without <c>@bind-IsOpen</c> re-renders only itself. That
/// path runs the markup but not OnParametersSet, where the host class was computed, so the closed
/// state never reached the class: an action sheet kept <c>action-sheet-mounted</c>
/// (pointer-events:auto) after its leave animation and went on blocking taps on the page.
/// Driven through the real app with frames pumped until the leave animation completes.
/// </summary>
public class OverlaySelfCloseTests : IDisposable
{
    private const int W = 390;
    private const int H = 844;

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;

    public OverlaySelfCloseTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private sealed class SheetPage : ComponentBase
    {
        internal static bool WithApp;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (WithApp)
            {
                var app = builder.OpenComponent<IonApp>();
                app.ChildContent = Page;
                builder.CloseComponent();
            }
            else
            {
                Page(builder);
            }
        }

        private static void Page(RenderTreeBuilder builder)
        {
            var page = builder.OpenComponent<IonPage>();
            page.ChildContent = p =>
            {
                var content = p.OpenComponent<IonContent>();
                content.ChildContent = c =>
                {
                    var target = c.OpenElement<Miko.Core.DomElements.DivElement>();
                    target.Class = "page-target";
                    target.Style = new Miko.Styling.Style
                    {
                        Height = Miko.Common.Length.Px(H),
                    };
                    c.CloseElement();

                    var sheet = c.OpenComponent<IonActionSheet>();
                    sheet.IsOpen = true;
                    sheet.Header = "Albums";
                    sheet.Buttons = [new() { Text = "Delete", Role = "destructive" }];
                    c.CloseComponent();
                };
                p.CloseComponent();
            };
            builder.CloseComponent();
        }
    }

    private MikoAppContext BuildApp(bool withApp)
    {
        SheetPage.WithApp = withApp;
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(c => c.Platform = HostPlatform.Android);
        builder.UseRouter(router => router.MapRoute("/", typeof(SheetPage)));
        var app = builder.Build();
        app.Controller.Initialize(_canvas, W, H);
        Frames(app, 30);
        return app;
    }

    private void Frames(MikoAppContext app, int count)
    {
        for (var i = 0; i < count; i++)
            app.Controller.RenderFrame(_canvas, W, H, 1f / 30f, c => app.Engine.Render(c));
    }

    private static bool IsInside(Element element, Element ancestor)
    {
        for (var node = element; node is not null; node = node.Parent)
            if (ReferenceEquals(node, ancestor)) return true;
        return false;
    }

    [Theory]
    [InlineData(true)]   // in IonApp's overlay layer
    [InlineData(false)]  // the inline fallback without an IonApp
    public void ActionSheet_ClosedByItsOwnButton_StopsBlockingThePage(bool withApp)
    {
        var app = BuildApp(withApp);
        var root = app.Engine.GetRoot()!;
        var button = root.FindByClass("action-sheet-button").First();
        button.OnClick!.Invoke(new MouseEventArgs { Target = button });

        Frames(app, 60); // well past the leave animation

        var hosts = app.Engine.GetRoot()!.FindByClass("ion-action-sheet");
        hosts.ShouldAllBe(host => host.HasClass("overlay-hidden"));

        var hit = app.Engine.HitTest(W / 2f, H / 2f);
        hit.ShouldNotBeNull();
        IsInside(hit!, app.Engine.GetRoot()!.FindByClass("page-target").Single())
            .ShouldBeTrue($"the page takes the tap again, hit {hit!.TagName}.{hit.Class}");
    }
}
