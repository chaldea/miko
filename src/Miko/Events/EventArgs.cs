using Miko.Core;

namespace Miko.Events;

/// <summary>
/// 所有Miko事件的基类
/// </summary>
public class MikoEventArgs : EventArgs
{
    /// <summary>
    /// 事件的原始目标元素
    /// </summary>
    public Element Target { get; init; } = null!;

    /// <summary>
    /// 当前正在处理事件的元素（在冒泡过程中会改变）
    /// </summary>
    public Element? CurrentTarget { get; internal set; }

    /// <summary>
    /// 事件是否冒泡
    /// </summary>
    public bool Bubbles { get; init; } = true;

    /// <summary>
    /// 是否已停止传播
    /// </summary>
    public bool IsPropagationStopped { get; private set; }

    /// <summary>
    /// 是否已阻止默认行为
    /// </summary>
    public bool DefaultPrevented { get; private set; }

    /// <summary>
    /// 停止事件传播
    /// </summary>
    public void StopPropagation() => IsPropagationStopped = true;

    /// <summary>
    /// 阻止默认行为
    /// </summary>
    public void PreventDefault() => DefaultPrevented = true;
}

/// <summary>
/// 鼠标事件参数
/// </summary>
public class MouseEventArgs : MikoEventArgs
{
    /// <summary>
    /// 鼠标X坐标
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// 鼠标Y坐标
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// 鼠标按钮
    /// </summary>
    public MouseButton Button { get; init; }
}

/// <summary>
/// 焦点事件参数
/// </summary>
public class FocusEventArgs : MikoEventArgs
{
    /// <summary>
    /// 相关目标（失去焦点时为新焦点元素，获得焦点时为旧焦点元素）
    /// </summary>
    public Element? RelatedTarget { get; init; }
}

/// <summary>
/// 值变化事件参数
/// </summary>
public class ChangeEventArgs : MikoEventArgs
{
    /// <summary>
    /// 旧值
    /// </summary>
    public string? OldValue { get; init; }

    /// <summary>
    /// 新值
    /// </summary>
    public string? NewValue { get; init; }
}

/// <summary>
/// 鼠标按钮枚举
/// </summary>
public enum MouseButton
{
    Left,
    Middle,
    Right
}

/// <summary>
/// 键盘事件参数
/// </summary>
public class KeyboardEventArgs : MikoEventArgs
{
    public string Key { get; init; } = string.Empty;
    public bool CtrlKey { get; init; }
    public bool ShiftKey { get; init; }
    public bool AltKey { get; init; }
}

/// <summary>
/// 输入事件参数（文本输入）
/// </summary>
public class InputEventArgs : MikoEventArgs
{
    public string Data { get; init; } = string.Empty;
}

/// <summary>
/// 滚动事件参数
/// </summary>
public class ScrollEventArgs : MikoEventArgs
{
    /// <summary>
    /// 水平滚动增量（像素）
    /// </summary>
    public float DeltaX { get; init; }

    /// <summary>
    /// 垂直滚动增量（像素）
    /// </summary>
    public float DeltaY { get; init; }

    /// <summary>
    /// 当前滚动位置 X
    /// </summary>
    public float ScrollLeft { get; init; }

    /// <summary>
    /// 当前滚动位置 Y
    /// </summary>
    public float ScrollTop { get; init; }
}
