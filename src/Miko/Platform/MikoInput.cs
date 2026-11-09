namespace Miko.Platform;

/// <summary>
/// 平台无关的按键枚举（物理键）。每个平台实现层负责将原生按键映射到该枚举，映射不到的为 <see cref="Unknown"/>。
/// <para>文本控件只用到编辑键；完整键位（字母、数字、方向、修饰键……）供需要原始按键的组件使用
/// （远程桌面、游戏），经 <see cref="Events.KeyboardEventArgs.Code"/> 交给聚焦元素。
/// 字母与标点按美式键盘命名，与布局无关：输入的文字另走文本输入通道。</para>
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

    // ---- Appended for full keyboards (remote desktop, games). Existing values above keep their numbers. ----

    // Navigation / editing
    Up,
    Down,
    PageUp,
    PageDown,
    Insert,

    // Modifiers and locks. Meta is the Windows key / ⌘ Command.
    MetaLeft,
    MetaRight,
    CapsLock,
    NumLock,
    ScrollLock,
    PrintScreen,
    Pause,
    Menu,

    Space,

    // Letters and digits: physical keys named by the US layout.
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,

    // Punctuation keys, named by their unshifted US character.
    Grave,          // `
    Minus,          // -
    Equal,          // =
    LeftBracket,    // [
    RightBracket,   // ]
    Backslash,      // \
    Semicolon,      // ;
    Apostrophe,     // '
    Comma,          // ,
    Period,         // .
    Slash,          // /

    // Keypad (KeypadEnter maps to Enter)
    Keypad0, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,
    KeypadDecimal,
    KeypadDivide,
    KeypadMultiply,
    KeypadSubtract,
    KeypadAdd,
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

    /// <summary>The Windows key / ⌘ Command.</summary>
    Meta = 1 << 3,
}
