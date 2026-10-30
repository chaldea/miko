using Miko.Common;

namespace Miko.Graphics;

/// <summary>
/// 绘制目标的抽象：引擎绘制、自定义绘制（<see cref="Core.DomElements.CanvasElement"/>）与宿主合成
/// 都只通过它画图（ISSUE-162）。
///
/// <para><b>为什么不直接给 <c>SKCanvas</c></b>：渲染后端（目前是 Skia）被封闭在核心库内部，
/// 组件库、平台宿主与应用都只依赖这里的抽象，后端将来可以替换而不波及它们。
/// 绘制所需的资源（<see cref="TextBlock"/>、<see cref="Image"/>）同样经抽象创建，
/// 见 <see cref="IGraphicsFactory"/>（容器中的服务，也可由 <see cref="Graphics"/> 取得）。</para>
///
/// <para><b>坐标</b>：逻辑像素，原点在左上角。宿主已按设备密度缩放，
/// 实际的设备像素倍率见 <see cref="DeviceScale"/>。</para>
///
/// <para><b>状态</b>：变换与裁剪是栈式状态，<see cref="Save"/> 压栈、<see cref="Restore"/> 出栈，
/// 与 HTML Canvas / Skia 的约定相同。几何图形（矩形、圆、弧、线）与文本一律抗锯齿；
/// 线条端点为平头（butt）。</para>
///
/// <para><b>线程</b>：只在渲染线程使用；回调中拿到的上下文属于引擎，不得保存到回调之外。</para>
///
/// <para>引擎只能绘制到由 Miko 创建的上下文（<see cref="RenderSurface.Context"/>、
/// <see cref="WindowRenderTarget"/> 等）；自行实现本接口可以用来在测试中记录组件的绘制调用，
/// 但不能交给 <c>MikoEngine.Render</c>。</para>
/// </summary>
public interface IDrawingContext
{
    /// <summary>创建本上下文可绘制的资源（文本、图像）。</summary>
    IGraphicsFactory Graphics { get; }

    /// <summary>
    /// 当前变换下一个逻辑像素对应多少个设备像素（桌面通常为 1，高密度屏为 2~4）。
    /// 取两个轴向缩放的较大者，旋转/倾斜也计入。
    /// </summary>
    float DeviceScale { get; }

    /// <summary>保存当前的变换与裁剪，返回保存前的栈深度（供 <see cref="RestoreToCount"/> 使用）。</summary>
    int Save();

    /// <summary>恢复到最近一次 <see cref="Save"/> 时的状态。</summary>
    void Restore();

    /// <summary>恢复到 <paramref name="count"/> 对应的栈深度（<see cref="Save"/> 的返回值）。</summary>
    void RestoreToCount(int count);

    void Translate(float dx, float dy);

    void Scale(float sx, float sy);

    /// <summary>绕当前原点顺时针旋转。</summary>
    void Rotate(float degrees);

    /// <summary>与当前裁剪区求交（不抗锯齿，边界按像素对齐）。</summary>
    void ClipRect(RectF rect);

    /// <summary>与圆角矩形求交（抗锯齿）。</summary>
    void ClipRoundRect(RectF rect, float radius);

    /// <summary>以 <paramref name="color"/> 覆盖整个裁剪区（不混合）。</summary>
    void Clear(Color color);

    void FillRect(RectF rect, Color color);

    void StrokeRect(RectF rect, Color color, float strokeWidth);

    void FillRoundRect(RectF rect, float radius, Color color);

    void FillCircle(float centerX, float centerY, float radius, Color color);

    /// <summary>
    /// 描一段椭圆弧（内切于 <paramref name="oval"/>）。角度单位为度，0° 指向右方，正值顺时针。
    /// </summary>
    void StrokeArc(RectF oval, float startAngle, float sweepAngle, Color color, float strokeWidth);

    void StrokeLine(float x0, float y0, float x1, float y1, Color color, float strokeWidth);

    /// <summary>在基线 <paramref name="baseline"/> 处绘制文本，<paramref name="x"/> 为文本左边缘。</summary>
    void DrawText(TextBlock text, float x, float baseline, Color color);

    /// <summary>描绘文本字形的轮廓（如弹幕的描边），位置约定同 <see cref="DrawText"/>。</summary>
    void StrokeText(TextBlock text, float x, float baseline, Color color, float strokeWidth);

    /// <summary>
    /// 把整张图缩放绘制到 <paramref name="destination"/>。
    /// <paramref name="tint"/> 非空时只取图像的 alpha 作为遮罩、以该颜色填充
    /// （单色图标的 <c>fill: currentColor</c>）。矢量图像按目标尺寸直接绘制，任何缩放下都清晰。
    /// </summary>
    void DrawImage(Image image, RectF destination, ImageSampling sampling = ImageSampling.Auto, Color? tint = null);
}
