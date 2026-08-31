using System.Runtime.Versioning;
using Miko.Platform.Video;

namespace Miko.Windowing.Video.Windows;

/// <summary>
/// Media Foundation 的帧源。全部包装逻辑继承自 <see cref="VideoFrameSourceBase"/>：
/// NV12 双平面经 SkSL shader 在 GPU 上转 RGB，RGB32 回退格式直接成像。
///
/// <para>
/// **零拷贝现状**：Media Foundation 的硬解输出是 <c>ID3D11Texture2D</c>。要让 Skia 的
/// OpenGL <c>GRContext</c> 直接采样它，需要 <c>WGL_NV_DX_interop2</c>
/// （<c>wglDXOpenDeviceNV</c> / <c>wglDXRegisterObjectNV</c> / <c>wglDXLockObjectsNV</c>），
/// 该扩展依赖显卡驱动支持，且注册与锁定都必须在持有 GL 上下文的渲染线程执行 ——
/// 也就是说需要把 D3D 纹理句柄跨线程送到渲染线程再注册。
/// </para>
/// <para>
/// 当前实现停在「系统硬解 + NV12 平面上传 + GPU 侧色彩转换」：解码与色彩转换都不占 CPU，
/// CPU 只搬运一次 NV12（约 RGBA 的 3/8 体积）。这是本类型作为
/// <see cref="VideoFrameSourceBase"/> 子类的意义 —— 后续接上 DX-interop 时，只需让
/// 会话改推 <see cref="VideoFrameBuffer.FromTextures"/>，本类与渲染侧都不必改动。
/// </para>
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class MediaFoundationFrameSource : VideoFrameSourceBase;
