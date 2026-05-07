#if ANDROID
using Android.Content;
using Miko.Core;
using Miko.Examples.Bootstrap;
using MikoApp1.Components;
using SkiaSharp;
using SkiaSharp.Views.Android;

namespace MikoApp1.Platforms.Android;

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
            var root = new App().Build();
            var styleSheets = new List<Miko.Styling.StyleSheet> { BootstrapStyles.CreateBootstrapStyleSheet() };
            _engine.Initialize(root, styleSheets, canvas, e.BackendRenderTarget.Width, e.BackendRenderTarget.Height);
            _initialized = true;
            return;
        }

        canvas.Clear(SKColors.White);
        _engine.Render(canvas);
    }
}
#endif
