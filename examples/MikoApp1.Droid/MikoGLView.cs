using Android.Content;
using Miko.Core;
using Miko.Examples.Bootstrap;
using SkiaSharp;
using SkiaSharp.Views.Android;

namespace MikoApp1.Droid;

public class MikoGLView : SKGLSurfaceView
{
    private readonly MikoEngine _engine = new();
    private bool _initialized;

    public MikoGLView(Context context) : base(context) { }

    protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        if (!_initialized)
        {
            var root = new MikoApp1.App().Build();
            var styleSheets = new List<Miko.Styling.StyleSheet> { BootstrapStyles.CreateBootstrapStyleSheet() };
            _engine.Initialize(root, styleSheets, canvas, e.BackendRenderTarget.Width, e.BackendRenderTarget.Height);
            _initialized = true;
            return;
        }
        canvas.Clear(SKColors.White);
        _engine.Render(canvas);
    }
}
