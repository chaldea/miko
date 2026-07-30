namespace Miko.Routing;

/// <summary>
/// 导航方向（语义对齐 Ionic 的 router direction）：
/// <list type="bullet">
/// <item><see cref="Forward"/>：压栈前进（如 列表 → 详情），通常伴随"新页面滑入"转场。</item>
/// <item><see cref="Back"/>：出栈返回（详情 → 列表），通常伴随反向转场。</item>
/// <item><see cref="Root"/>：清空历史栈的根级切换（如 Tab 切换），通常不做转场。</item>
/// </list>
/// </summary>
public enum NavigationDirection
{
    Forward,
    Back,
    Root
}
