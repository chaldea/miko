using Microsoft.Extensions.DependencyInjection;
using Miko.Core;
using Miko.Examples.Bootstrap;
using Miko.Examples.Bootstrap.Examples;
using Miko.Fonts;
using Miko.Routing;
using MikoApp1;
using SkiaSharp;

int width = 1024;
int height = 700;

// Register fonts from embedded resources
using var fontStream = typeof(App).Assembly.GetManifestResourceStream("MikoApp1.Resources.Fonts.bootstrap-icons.woff2");
if (fontStream != null)
    FontManager.Instance.RegisterFont("bootstrap-icons", fontStream);

var services = new ServiceCollection().BuildServiceProvider();
var router = new Router();
router.ScanAssemblies(typeof(ButtonExample).Assembly);
var navManager = new NavigationManager();
var routeView = new RouteView(router, navManager, typeof(MainLayout), services);

var root = routeView.Render("/");

var styleSheets = new List<Miko.Styling.StyleSheet>
{
    BootstrapStyles.CreateBootstrapStyleSheet(),
    MainLayout.CreateLayoutStyleSheet()
};

var engine = new MikoEngine();
using var surface = SKSurface.Create(new SKImageInfo(width, height));
var canvas = surface.Canvas;
canvas.Clear(SKColors.White);

engine.Initialize(root, styleSheets, canvas, width, height);
engine.Render(canvas);

var outputPath = Path.Combine(AppContext.BaseDirectory, "output.png");
using var image = surface.Snapshot();
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
using var stream = File.OpenWrite(outputPath);
data.SaveTo(stream);

Console.WriteLine($"Rendered to: {outputPath}");
