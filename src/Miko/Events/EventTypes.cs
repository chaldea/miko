namespace Miko.Events;

/// <summary>
/// 标准事件类型名称常量
/// </summary>
public static class EventTypes
{
    public const string Click = "click";
    public const string MouseEnter = "mouseenter";
    public const string MouseLeave = "mouseleave";
    public const string MouseDown = "mousedown";
    public const string MouseUp = "mouseup";
    public const string MouseMove = "mousemove";
    public const string PointerDown = "pointerdown";
    public const string PointerUp = "pointerup";
    public const string PointerMove = "pointermove";
    public const string PointerCancel = "pointercancel";
    public const string LongPress = "longpress";
    public const string Focus = "focus";
    public const string Blur = "blur";
    public const string Change = "change";
    public const string Scroll = "scroll";
    public const string KeyDown = "keydown";
    public const string Input = "input";

    /// <summary>
    /// 系统返回键（ISSUE-138）。由 <see cref="Miko.Platform.BackButtonRegistry"/> 在返回被消费时
    /// 派发到对应的元素上，供需要自行响应返回的应用监听。硬件来源因平台而异：Android 的硬件/
    /// 手势返回、iOS 的左缘返回手势、桌面的 Escape。
    /// </summary>
    public const string BackButton = "backbutton";
}
