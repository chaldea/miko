// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Layout;
using Miko.Platform;
using Miko.Routing;
using Miko.Windowing.Video;

namespace Anime.Verification;

/// <summary>Drives the real shared application using the platform controller and an offscreen surface.</summary>
internal sealed class AppHarness : IDisposable
{
    private readonly MikoAppContext _context;
    private RenderSurface _surface;

    public AppHarness(string? output, bool ios = false)
    {
        Output = Path.GetFullPath(output ?? "artifacts/anime/verification");
        Directory.CreateDirectory(Output);
        _context = App.CreateContext(builder =>
        {
            builder.UseSystemVideo();
            if (ios)
            {
                builder.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(HostPlatform.Ios));
            }
        });
        _surface = _context.Engine.Graphics.CreateSurface(390, 844);
        _context.RegisterFonts();
        _context.Controller.Initialize(_surface.Context, _surface.Width, _surface.Height);
    }

    public string Output { get; }
    public int Captures { get; private set; }
    public string Route => Service<NavigationManager>().CurrentPath;
    public Element Root => _context.Engine.GetRoot() ?? throw new InvalidOperationException("Missing root.");
    public string Text => AllText(Root);
    public T Service<T>() where T : notnull => _context.Services.GetRequiredService<T>();
    public IReadOnlyList<Element> Find(string className) => Root.FindByClass(className);

    public void Navigate(string route)
    {
        Service<NavigationManager>().NavigateTo(route);
        Pump();
    }

    public void Resize(int width, int height)
    {
        _surface.Dispose();
        _surface = _context.Engine.Graphics.CreateSurface(width, height);
        _context.Controller.SetViewportSize(width, height);
        Pump();
    }

    public void Pump(int count = 20)
    {
        for (var frame = 0; frame < count; frame++)
        {
            _context.Controller.RenderFrame(_surface.Context, _surface.Width, _surface.Height, 1f / 60f, _context.Engine.Render);
            Thread.Sleep(10);
        }
    }

    public void Capture(string name)
    {
        Pump();
        File.WriteAllBytes(Path.Combine(Output, $"{name}.png"), _surface.EncodePng());
        var boxes = Boxes(_context.Engine.GetCurrentLayout()!).ToArray();
        File.WriteAllLines(Path.Combine(Output, $"{name}.layout.txt"), boxes.Select(box =>
            $"{box.Element.TagName}.{box.Element.Class}: {box.BoxModel.BorderBox}"));
        Require(boxes.All(b => float.IsFinite(b.BoxModel.BorderBox.Width) && float.IsFinite(b.BoxModel.BorderBox.Height)), "Non-finite layout.");
        Require(boxes.Any(b => b.Element.HasClass("ion-content") && b.BoxModel.BorderBox.Height > 100), "Collapsed content.");
        Captures++;
        Console.WriteLine($"Rendered {name}");
    }

    public void ClickText(string className, string text) =>
        Click(Find(className).First(e => AllText(e).Contains(text, StringComparison.Ordinal)));

    public void ClickClass(string className) => Click(Find(className).First());

    public RectF Bounds(Element element) => Box(element).BoxModel.BorderBox;

    public void Click(Element host)
    {
        var target = host.OnClick is not null ? host : Descendants(host).First(e => e.OnClick is not null);
        var rect = Bounds(target);
        Require(rect.Width > 0 && rect.Height > 0, $"Zero-size target {host.Class}");
        var x = rect.X + rect.Width / 2;
        var y = rect.Y + rect.Height / 2;
        for (var ancestor = target.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            var ancestorBox = Boxes(_context.Engine.GetCurrentLayout()!).FirstOrDefault(b => ReferenceEquals(b.Element, ancestor));
            x -= ancestorBox?.ScrollLeft ?? 0;
            y -= ancestorBox?.ScrollTop ?? 0;
        }
        Require(x >= 0 && x < _surface.Width && y >= 0 && y < _surface.Height, $"Target outside viewport: {host.Class} ({x}, {y})");
        var hit = _context.Engine.HitTest(x, y);
        Require(hit is not null && (ReferenceEquals(hit, target) || Descendants(target).Contains(hit)),
            $"Pointer missed {host.Class}; hit {hit?.TagName}.{hit?.Class} at ({x}, {y}).");
        _context.Controller.OnPointerDown(x, y, MouseButton.Left);
        _context.Controller.OnPointerUp(x, y, MouseButton.Left);
        Pump();
    }

    public void Enter(string className, string text)
    {
        var input = Descendants(Find(className).Single()).OfType<InputElement>().Single();
        _context.Controller.SetFocus(input);
        input.MoveCursorToEnd();
        // Use the same editing path as keyboard/IME input, including two-way bindings.
        var length = input.Value?.Length ?? 0;
        for (var index = 0; index < length; index++)
        {
            _context.Controller.OnKeyDown(MikoKey.Backspace, MikoKeyModifiers.None);
        }
        _context.Controller.OnTextInput(text);
        _context.Controller.SetFocus(null);
        Pump();
    }

    public void Dispose()
    {
        _context.Engine.DisposeVideoSessions();
        (_context.Services as IDisposable)?.Dispose();
        _surface.Dispose();
    }

    private LayoutBox Box(Element element) => Boxes(_context.Engine.GetCurrentLayout()!).First(b => ReferenceEquals(b.Element, element));
    private static IEnumerable<Element> Descendants(Element element) => element.Children.SelectMany(e => new[] { e }.Concat(Descendants(e)));
    public static string AllText(Element element) => element.TextContent + string.Concat(element.Children.Select(AllText));
    private static IEnumerable<LayoutBox> Boxes(LayoutBox box) => new[] { box }.Concat(box.Children.SelectMany(Boxes));

    public static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
