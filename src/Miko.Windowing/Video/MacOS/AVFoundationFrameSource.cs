using System.Runtime.Versioning;
using Miko.Platform.Video;

namespace Miko.Windowing.Video.MacOS;

/// <summary>
/// AVFoundation 的帧源。包装逻辑全部继承自 <see cref="VideoFrameSourceBase"/>：
/// NV12 双平面经 SkSL shader 在 GPU 上转 RGB。
///
/// <para>
/// **零拷贝现状**：完整零拷贝需 <c>CVOpenGLTextureCacheCreateTextureFromImage</c>
/// 把 <c>CVPixelBuffer</c>（IOSurface 支撑）映射为 <c>GL_TEXTURE_RECTANGLE</c>，
/// 这要求纹理缓存与渲染线程的 CGL 上下文绑定。当前实现走
/// 「VideoToolbox 硬解 + NV12 上传 + GPU 侧色彩转换」；后续接纹理缓存时，
/// 只需让会话改推 <see cref="VideoFrameBuffer.FromTextures"/>（<c>Target</c> 传
/// <c>GL_TEXTURE_RECTANGLE</c>），本类与渲染侧都不必改动。
/// </para>
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class AVFoundationFrameSource : VideoFrameSourceBase;
