using SkiaSharp;

namespace Miko.Platform.Video;

/// <summary>YUV→RGB 的色彩矩阵系数。窄带（limited/video range）输入。</summary>
public enum VideoColorSpace
{
    /// <summary>BT.601，标清内容与多数摄像头输出。</summary>
    Bt601,

    /// <summary>BT.709，高清内容（720p 及以上）的常规选择。</summary>
    Bt709,
}

/// <summary>
/// 把 NV12 双平面合成为一张可绘制的 RGB 图像，转换在 GPU 上由 SkSL shader 完成。
///
/// <para>
/// **为什么需要它**：系统硬解器（Media Foundation / VAAPI / VideoToolbox / MediaCodec）
/// 原生输出 NV12，而 SkiaSharp 3.119.1 **没有导出** <c>SKYUVAInfo</c> / <c>SKImage.FromTextures</c>
/// 多平面 API（已用反射确认），因此 ISSUE-058 设想的「双平面交给 Skia 内部 shader」不可行。
/// 这里用公开可用的构件重建同等能力：Y 平面包成 <see cref="SKColorType.R8Unorm"/>、
/// UV 平面包成 <see cref="SKColorType.Rg88"/>，各自作为 child shader 传给
/// <see cref="SKRuntimeEffect"/>，由自写 SkSL 做矩阵变换。像素始终不回到 CPU。
/// </para>
///
/// <para>
/// 线程约束：所有方法只在渲染线程调用（Skia 的 <c>GRContext</c> 非线程安全）。
/// </para>
/// </summary>
public static class Nv12FrameComposer
{
    // 采样两张纹理并做 YUV→RGB。窄带输入：Y 归一化后减 16/255，UV 减 128/255。
    // 若 uv 平面只有 r 通道可用（部分驱动对 Rg88 的处理差异），g 退化为 r 会导致偏色，
    // 因此这里显式取 .rg 两通道。
    private const string Sksl = """
        uniform shader yPlane;
        uniform shader uvPlane;
        uniform half3x3 yuvToRgb;
        uniform half2 uvScale;

        half4 main(float2 coord) {
            half y  = yPlane.eval(coord).r;
            half2 uv = uvPlane.eval(coord * uvScale).rg;

            half3 yuv = half3(y - 0.0627451, uv.r - 0.5019608, uv.g - 0.5019608);
            half3 rgb = yuvToRgb * yuv;
            return half4(saturate(rgb), 1.0);
        }
        """;

    private static SKRuntimeEffect? _effect;
    private static string? _effectError;

    /// <summary>
    /// 惰性编译 SkSL。编译失败（旧 Skia / 不支持 runtime effect）时返回 <c>null</c>，
    /// 调用方据此回退到 CPU 转换，不抛异常。
    /// </summary>
    private static SKRuntimeEffect? GetEffect()
    {
        if (_effect != null) return _effect;
        if (_effectError != null) return null;    // 已知失败，不重复编译

        _effect = SKRuntimeEffect.CreateShader(Sksl, out var error);
        if (_effect == null)
            _effectError = string.IsNullOrEmpty(error) ? "unknown SkSL compile error" : error;

        return _effect;
    }

    /// <summary>SkSL 编译失败原因，供宿主记录日志。未失败时为 <c>null</c>。</summary>
    public static string? ShaderError => _effectError;

    /// <summary>
    /// 用 Y / UV 两张图像合成一张 RGB 图像。两张输入图像可以是 GPU 纹理包装（零拷贝）
    /// 或 CPU 光栅图像（回退），本方法不关心其来源。
    /// </summary>
    /// <param name="yImage">全分辨率亮度平面，单通道。</param>
    /// <param name="uvImage">半分辨率交错色度平面，双通道。</param>
    /// <param name="width">输出宽（亮度平面尺寸）。</param>
    /// <param name="height">输出高。</param>
    /// <param name="grContext">GPU 上下文；<c>null</c> 时在 CPU surface 上合成。</param>
    /// <param name="colorSpace">色彩矩阵，按分辨率选择（见 <see cref="SelectColorSpace"/>）。</param>
    /// <returns>合成后的图像；shader 不可用时返回 <c>null</c>。调用方拥有返回值并负责释放。</returns>
    public static SKImage? Compose(
        SKImage yImage,
        SKImage uvImage,
        int width,
        int height,
        GRContext? grContext,
        VideoColorSpace colorSpace = VideoColorSpace.Bt709)
    {
        ArgumentNullException.ThrowIfNull(yImage);
        ArgumentNullException.ThrowIfNull(uvImage);
        if (width <= 0 || height <= 0) return null;

        var effect = GetEffect();
        if (effect == null) return null;

        using var yShader = yImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp,
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
        using var uvShader = uvImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp,
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));

        var uniforms = new SKRuntimeEffectUniforms(effect)
        {
            // half3x3 以列主序上传，与 SkSL 的矩阵约定一致。
            ["yuvToRgb"] = ColumnMajorMatrix(colorSpace),
            // UV 平面是半分辨率：采样坐标需缩放 0.5，使其覆盖同一画面区域。
            ["uvScale"] = new[] { 0.5f, 0.5f },
        };

        var children = new SKRuntimeEffectChildren(effect)
        {
            ["yPlane"] = yShader,
            ["uvPlane"] = uvShader,
        };

        using var shader = effect.ToShader(uniforms, children);
        if (shader == null) return null;

        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);

        // GPU 上下文可用时在显存上合成（真零拷贝路径的终点）；否则退到 CPU surface。
        using var surface = grContext != null
            ? SKSurface.Create(grContext, budgeted: true, info)
            : SKSurface.Create(info);

        if (surface == null) return null;

        using var paint = new SKPaint { Shader = shader };
        surface.Canvas.DrawRect(new SKRect(0, 0, width, height), paint);

        return surface.Snapshot();
    }

    /// <summary>
    /// 按分辨率选择色彩矩阵：高清内容（高度 ≥ 720）用 BT.709，其余用 BT.601。
    /// 这是容器未声明色彩空间时的通行启发式，与浏览器/播放器一致。
    /// </summary>
    public static VideoColorSpace SelectColorSpace(int height)
        => height >= 720 ? VideoColorSpace.Bt709 : VideoColorSpace.Bt601;

    /// <summary>
    /// 窄带 YUV→RGB 矩阵，列主序展开。
    /// Y 系数 1.164 = 255/219，补偿窄带亮度只用 16..235 的范围。
    /// </summary>
    private static float[] ColumnMajorMatrix(VideoColorSpace colorSpace) => colorSpace switch
    {
        // BT.601: R = 1.164Y + 1.596V, G = 1.164Y - 0.392U - 0.813V, B = 1.164Y + 2.017U
        VideoColorSpace.Bt601 =>
        [
            1.164f,  1.164f,  1.164f,   // 第 1 列：Y 对 R/G/B 的贡献
            0.000f, -0.392f,  2.017f,   // 第 2 列：U
            1.596f, -0.813f,  0.000f,   // 第 3 列：V
        ],

        // BT.709: R = 1.164Y + 1.793V, G = 1.164Y - 0.213U - 0.533V, B = 1.164Y + 2.112U
        _ =>
        [
            1.164f,  1.164f,  1.164f,
            0.000f, -0.213f,  2.112f,
            1.793f, -0.533f,  0.000f,
        ],
    };
}
