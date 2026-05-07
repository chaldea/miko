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
    private float _density;

    public MikoGLView(Context context) : base(context)
    {
        _density = context.Resources?.DisplayMetrics?.Density ?? 1f;
    }

    protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        // 物理像素转逻辑像素（dp），让布局以 dp 为单位计算
        float logicalWidth = e.BackendRenderTarget.Width / _density;
        float logicalHeight = e.BackendRenderTarget.Height / _density;

        if (!_initialized)
        {
            var root = new MikoApp1.App().Build();
            var styleSheets = new List<Miko.Styling.StyleSheet> { BootstrapStyles.CreateBootstrapStyleSheet() };
            _engine.Initialize(root, styleSheets, canvas, logicalWidth, logicalHeight);
            _initialized = true;
        }

        canvas.Clear(SKColors.White);
        canvas.Save();
        canvas.Scale(_density);  // 缩放 canvas，使 1 逻辑像素 = density 物理像素
        _engine.Render(canvas);
        canvas.Restore();
    }
}
