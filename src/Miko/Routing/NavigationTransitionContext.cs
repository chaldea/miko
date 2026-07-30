using Miko.Core;

namespace Miko.Routing;

/// <summary>
/// 页面转场上下文（ISSUE-108）：标识参与转场的两棵页面树，并承载每帧的图层绘制状态。
/// <para>
/// 转场期间引擎把旧页面（leaving）与新页面（entering）作为两个叠放图层绘制，
/// 每层应用本上下文中的整体偏移（像素）与不透明度。转场效果在
/// <see cref="NavigationTransition.Apply"/> 中逐帧写入这些值——偏移/透明度只影响
/// 绘制，不参与布局，也不会改写元素样式，因此转场不触发逐帧重排。
/// </para>
/// <para>
/// 转场期间命中测试始终作用于 entering 页面树（leaving 页面不可交互）。
/// </para>
/// </summary>
public sealed class NavigationTransitionContext
{
    public NavigationTransitionContext(
        Element leavingElement,
        Element enteringElement,
        NavigationDirection direction,
        string fromPath,
        string toPath,
        float viewportWidth,
        float viewportHeight)
    {
        LeavingElement = leavingElement;
        EnteringElement = enteringElement;
        Direction = direction;
        FromPath = fromPath;
        ToPath = toPath;
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
    }

    /// 旧页面（离开页）的根元素。其布局树在转场期间被保留并作为 leaving 图层绘制。
    public Element LeavingElement { get; }

    /// 新页面（进入页）的根元素，即引擎当前根。
    public Element EnteringElement { get; }

    /// 本次导航的方向。
    public NavigationDirection Direction { get; }

    /// 导航前的路径。
    public string FromPath { get; }

    /// 导航到的路径。
    public string ToPath { get; }

    /// 视口宽度（逻辑像素）。转场效果据此把"页面宽度百分比"换算为像素偏移。
    public float ViewportWidth { get; }

    /// 视口高度（逻辑像素）。
    public float ViewportHeight { get; }

    /// leaving 图层的水平偏移（像素，正值向右）。
    public float LeavingOffsetX { get; set; }

    /// leaving 图层的垂直偏移（像素，正值向下）。
    public float LeavingOffsetY { get; set; }

    /// leaving 图层的不透明度（0..1）。
    public float LeavingOpacity { get; set; } = 1f;

    /// entering 图层的水平偏移（像素，正值向右）。
    public float EnteringOffsetX { get; set; }

    /// entering 图层的垂直偏移（像素，正值向下）。
    public float EnteringOffsetY { get; set; }

    /// entering 图层的不透明度（0..1）。
    public float EnteringOpacity { get; set; } = 1f;

    /// <summary>
    /// 叠放次序：true = entering 层在 leaving 层之下（先绘制 entering）。
    /// 典型场景是返回（<see cref="NavigationDirection.Back"/>）转场——旧页面向右滑出，
    /// 露出下方的新页面。默认 false：新页面盖在旧页面之上（前进滑入）。
    /// </summary>
    public bool EnteringBelow { get; set; }
}
