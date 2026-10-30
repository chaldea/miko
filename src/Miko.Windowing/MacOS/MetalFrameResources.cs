namespace Miko.Windowing.MacOS;

/// <summary>
/// 推迟释放「本帧 GPU 命令会读取、但渲染器并不持有」的原生资源，直到本帧的 Metal 命令执行完毕。
/// 与 iOS 宿主的同名类型同一约定（ISSUE-147）。
///
/// <para><b>为什么需要</b>：视频零拷贝把解码器交出的 <c>CVPixelBuffer</c> 经
/// <c>CVMetalTextureCache</c> 映射为 Metal 纹理。渲染器要到帧末 flush 才把采样这些纹理的命令
/// 提交给 GPU，GPU 执行还要更晚。CoreVideo 要求 <c>CVMetalTexture</c> 一直持有到 GPU 用完为止；
/// 提前释放，像素缓冲会回到解码器的缓冲池被下一帧覆写，画面串帧或撕裂。</para>
///
/// <para><b>释放时机</b>：<see cref="MetalWindowGraphics"/> 在提交呈现命令缓冲时取走本帧登记的资源，
/// 等该缓冲执行完毕后释放。同一命令队列上的缓冲按提交顺序执行，呈现缓冲排在本帧绘制命令之后，
/// 所以它完成时，本帧对这些纹理的读取也已完成。</para>
///
/// <para>按线程隔离：登记与取走都发生在同一个渲染线程上（取帧在绘制中、呈现在帧末），
/// 模拟器等宿主各自的渲染线程互不干扰。</para>
/// </summary>
internal static class MetalFrameResources
{
    [ThreadStatic] private static List<IDisposable>? t_pending;

    /// <summary>登记一个须活到当前帧 GPU 命令执行完毕的资源。</summary>
    public static void RetainUntilFrameCompletes(IDisposable resource)
        => (t_pending ??= []).Add(resource);

    /// <summary>取走当前线程本帧登记的资源（没有时为 <c>null</c>），由宿主接管其释放。</summary>
    public static List<IDisposable>? TakePending()
    {
        var pending = t_pending;
        t_pending = null;
        return pending;
    }
}
