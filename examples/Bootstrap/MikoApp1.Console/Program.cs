using Microsoft.Extensions.DependencyInjection;
using Miko.Core;
using Miko.Examples.Bootstrap;
using Miko.Examples.Bootstrap.Examples;
using Miko.Common;
using Miko.Fonts;
using Miko.Hosting;
using Miko.Routing;
using MikoApp1;

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

var engine = new MikoEngineBuilder().Build();
using var surface = engine.Graphics.CreateSurface(width, height);
var canvas = surface.Context;
canvas.Clear(Color.White);

engine.Initialize(root, styleSheets, canvas, width, height);
engine.Render(canvas);

var outputPath = Path.Combine(AppContext.BaseDirectory, "output.png");
File.WriteAllBytes(outputPath, surface.EncodePng());

Console.WriteLine($"Rendered to: {outputPath}");
