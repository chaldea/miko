using Miko.Common;
using Miko.Platform.Video;
using SkiaSharp;

namespace Miko.Core.DomElements;

/// <summary>
/// 图片元素。<see cref="Source"/> 经统一资源管理器异步加载并解码为 <see cref="Bitmap"/>，
/// 加载完成前可渲染 <see cref="Placeholder"/> 占位内容。
/// </summary>
public class ImageElement : Element
{
    public override string TagName => "img";

    /// <summary>图片源（file:// / res:// / http(s):// / data: / 裸路径）。</summary>
    public MediaSource Source { get; set; }

    /// <summary>占位图源（本地文件 / res://）。真实图片就绪前渲染，标准浏览器无此属性，为引擎扩展。</summary>
    public string? Placeholder { get; set; }

    /// <summary>已解码的位图（由引擎异步加载后写入）。</summary>
    public SKBitmap? Bitmap { get; internal set; }

    /// <summary>占位图已解码的位图（首张真实图前绘制）。</summary>
    internal SKBitmap? PlaceholderBitmap { get; set; }

    /// <summary>
    /// replaced 元素内禀尺寸（CSS 像素），加载完成后由引擎写入并触发重排。未知时为 null。
    /// </summary>
    internal int? IntrinsicWidth { get; set; }
    internal int? IntrinsicHeight { get; set; }
}

/// <summary>
/// 视频元素。Miko 只负责把"当前帧"作为一张 GPU 图像合成进盒模型；
/// 解复用与编解码由平台后端（<see cref="IVideoBackend"/>）实现。
/// <para>
/// 它是一个 replaced 元素，行为对齐 <see cref="ImageElement"/>：完全参与
/// Block/Flex/定位/overflow 裁剪/opacity/transform 链路，其它元素可覆盖在其上。
/// </para>
/// </summary>
public class VideoElement : Element
{
    public override string TagName => "video";

    // ---- 声明式属性（HTML 风格）-------------------------------------------
    public MediaSource Source { get; set; }
    public bool AutoPlay { get; set; }
    public bool Loop { get; set; }
    public bool Muted { get; set; }

    /// <summary>是否渲染默认控件层。预留扩展点，当前版本不绘制原生控件。</summary>
    public bool Controls { get; set; }

    /// <summary>首帧到达前显示的占位图源（与 <see cref="ImageElement"/> 同路径解码）。</summary>
    public string? Poster { get; set; }

    // ---- 引擎内部状态 ------------------------------------------------------

    /// <summary>
    /// 由渲染引擎在首次见到该元素时通过 <see cref="IVideoBackend"/> 创建并赋值；
    /// 元素从树中移除时由引擎 <see cref="IVideoSession.Dispose"/>。
    /// </summary>
    internal IVideoSession? Session { get; set; }

    /// <summary>占位图已解码的位图（首帧前绘制）。</summary>
    internal SKBitmap? PosterBitmap { get; set; }

    /// <summary>
    /// replaced 元素内禀尺寸，会话 <c>Loaded</c> 后由引擎写入并触发重排。
    /// 未知时为 null，布局回退到默认 300×150（HTML 规范）。
    /// </summary>
    internal int? IntrinsicWidth { get; set; }
    internal int? IntrinsicHeight { get; set; }
}
