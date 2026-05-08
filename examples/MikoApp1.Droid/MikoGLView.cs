using Android.Content;
using Miko.Core;
using Miko.Examples.Bootstrap;
using Miko.Examples.Bootstrap.Examples;
using Miko.Fonts;
using Miko.Routing;
using SkiaSharp;
using SkiaSharp.Views.Android;

namespace MikoApp1.Droid;

public class MikoGLView : SKGLSurfaceView
{
    private readonly MikoEngine _engine = new();
    private bool _initialized;
    private float _density;

    public MikoGLView(Context context) : base(context)
    {
        _density = context.Resources?.DisplayMetrics?.Density ?? 1f;
        Android.Util.Log.Info("MikoGLView", $"Screen density: {_density}, Physical size: {context.Resources?.DisplayMetrics?.WidthPixels}×{context.Resources?.DisplayMetrics?.HeightPixels}");
    }

    protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float logicalWidth = e.BackendRenderTarget.Width / _density;
        float logicalHeight = e.BackendRenderTarget.Height / _density;

        if (!_initialized)
        {
            using var fontStream = typeof(MikoApp1.App).Assembly.GetManifestResourceStream("MikoApp1.Resources.Fonts.bootstrap-icons.woff2");
            if (fontStream != null)
                FontManager.Instance.RegisterFont("bootstrap-icons", fontStream);

            var router = new Router();
            router.ScanAssemblies(typeof(ButtonExample).Assembly);
            var navManager = new NavigationManager();
            var routeView = new RouteView(router, navManager, typeof(MikoApp1.MainLayout));
            var root = routeView.Render("/");
            var styleSheets = new List<Miko.Styling.StyleSheet>
            {
                BootstrapStyles.CreateBootstrapStyleSheet(),
                MikoApp1.MainLayout.CreateLayoutStyleSheet()
            };
            _engine.Initialize(root, styleSheets, canvas, logicalWidth, logicalHeight);
            _initialized = true;
        }

        canvas.Clear(SKColors.White);
        canvas.Save();
        canvas.Scale(_density);
        _engine.Render(canvas);
        canvas.Restore();
    }
}
