using Miko.Common;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ISSUE-137 的端到端验证：真实的 <see cref="IonIcon"/> 组件树，画在按屏幕密度放大的画布上
/// （Android 真机 / 模拟器的渲染方式，见 <c>MikoSurfaceView.OnPaintSurface</c> 的
/// <c>canvas.Scale(_density)</c>），图标边缘必须与「直接按设备像素栅格化」同样锐利。
///
/// <para>此前 SVG 背景按<b>逻辑</b>尺寸（24×24）栅格化，再由 3× 画布放大插值到 72 设备像素，
/// 每个源像素的边缘被抹成 3 像素宽的渐变带 —— 这就是高分屏上图标发虚的成因。</para>
/// </summary>
public class IonIconHighDpiTests : IonicComponentTestBase
{
    private const float Density = 3f;   // Android xxhdpi
    private const int IconSize = 24;    // .ion-icon 的默认逻辑尺寸
    private const int Canvas = 96;      // 逻辑 32px 的画布，图标居中留白

    [Fact]
    public void IonIcon_OnHighDensityCanvas_ShouldNotBlurTheGlyphEdges()
    {
        int actualBlend = CountEdgePixels(RenderIcon(Density));

        // 参考：同样的设备像素分辨率，但画布不放大（引擎直接按 72 设备像素布局与栅格化），
        // 即「没有二次重采样」时的边缘锐度。
        int referenceBlend = CountEdgePixels(RenderIcon(scale: 1f, iconSize: (int)(IconSize * Density)));

        // 二次重采样会把过渡带展宽到约 Density 倍，中间灰阶像素数成倍增长。
        // 1.5 倍余量吸收抗锯齿实现差异与几何取整。
        actualBlend.ShouldBeLessThan((int)(referenceBlend * 1.5f));
    }

    /// <summary>
    /// 渲染一个真实的 <see cref="IonIcon"/>（含 Ionic 样式表与组件解析出的背景图）到
    /// <paramref name="scale"/> 倍画布上。
    /// </summary>
    private SKBitmap RenderIcon(float scale, int iconSize = IconSize)
    {
        var cut = Context.Render<IonIcon>(parameters => parameters
            .Add(nameof(IonIcon.Icon), "triangle")
            .Add(nameof(IonIcon.Style), new Style
            {
                Width = Length.Px(iconSize),
                Height = Length.Px(iconSize),
                // 模板图标以 color 着色，用纯黑与白底形成最大对比，便于统计过渡像素。
                Color = Color.FromRgb(0, 0, 0),
            }));

        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(iconSize), Height = Length.Px(iconSize) },
            Children = { cut.Root },
        };

        var bitmap = new SKBitmap(Canvas, Canvas);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            canvas.Scale(scale);

            float logical = Canvas / scale;
            var engine = new MikoEngineBuilder().Build();
            engine.Initialize(root, [IonicStyleSheetFactory.CreateAllModes()], canvas, logical, logical);
            engine.Render(canvas);
        }
        return bitmap;
    }

    /// <summary>统计既非纯黑也非纯白的过渡像素数（边缘模糊程度的代理指标）。</summary>
    private static int CountEdgePixels(SKBitmap bitmap)
    {
        using var _ = bitmap;
        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var p = bitmap.GetPixel(x, y);
                bool pureBlack = p.Red < 8 && p.Green < 8 && p.Blue < 8;
                bool pureWhite = p.Red > 247 && p.Green > 247 && p.Blue > 247;
                if (!pureBlack && !pureWhite) count++;
            }
        }
        return count;
    }
}
