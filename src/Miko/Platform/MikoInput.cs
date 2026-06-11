namespace Miko.Platform;

/// <summary>
/// 平台无关的按键枚举。每个平台实现层负责将原生按键映射到该枚举。
/// 仅包含 Miko 交互逻辑实际使用到的按键，其余统一为 <see cref="Unknown"/>。
/// </summary>
public enum MikoKey
{
    Unknown = 0,

    // 文本编辑相关
    Backspace,
    Delete,
    Left,
    Right,
    Home,
    End,

    // 常用控制键
    Enter,
    Tab,
    Escape,

    // 修饰键
    ControlLeft,
    ControlRight,
    ShiftLeft,
    ShiftRight,
    AltLeft,
    AltRight,

    // 功能键
    F1,
    F2,
    F3,
    F4,
    F5,
    F6,
    F7,
    F8,
    F9,
    F10,
    F11,
    F12,
}

/// <summary>
/// 平台无关的键盘修饰键标志。
/// </summary>
[Flags]
public enum MikoKeyModifiers
{
    None = 0,
    Control = 1 << 0,
    Shift = 1 << 1,
    Alt = 1 << 2,
}
