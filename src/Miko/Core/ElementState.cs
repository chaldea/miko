namespace Miko.Core;

/// <summary>
/// 元素交互状态（可使用标志位组合）
/// </summary>
[Flags]
public enum ElementState
{
    None = 0,
    Hover = 1,      // 鼠标悬停在元素上
    Active = 2,     // 元素正在被激活（鼠标按下）
    Focus = 4,      // 元素获得键盘焦点
    Disabled = 8    // 元素被禁用
}
