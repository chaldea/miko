using System.Runtime.Versioning;
using Miko.Platform.Video;

namespace Miko.Windowing.Video.Linux;

/// <summary>
/// GStreamer 的帧源。包装逻辑全部继承自 <see cref="VideoFrameSourceBase"/>：
/// NV12 双平面经 SkSL shader 在 GPU 上转 RGB。
///
/// <para>
/// **零拷贝现状**：完整零拷贝需要 <c>vaExportSurfaceHandle</c> 导出 DMA-BUF FD，
/// 再经 <c>eglCreateImageKHR</c> + <c>glEGLImageTargetTexture2DOES</c> 绑定为 GL 纹理。
/// 这条路径要求宿主使用 EGL（Silk.NET 在 Linux 上可能是 GLX）并在渲染线程持有上下文，
/// 且需 VAAPI 与 EGL 扩展同时可用。当前实现走「GStreamer 硬解 + NV12 上传 + GPU 侧色彩转换」，
/// 后续接 DMA-BUF 时只需让会话改推 <see cref="VideoFrameBuffer.FromTextures"/>。
/// </para>
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class GStreamerFrameSource : VideoFrameSourceBase;
