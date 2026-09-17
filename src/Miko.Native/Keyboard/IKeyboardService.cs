namespace Miko.Native.Keyboard;

/// <summary>
/// 键盘显示、隐藏和事件。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/native/keyboard#api">Keyboard API</see>。
/// 支持平台：Android、iOS、Windowing。
/// <para>
/// 与引擎内部的 <c>IInputMethodService</c> 的分工：后者是**引擎**用来驱动 IME 组合与文本提交的
/// 内部端点；本接口是**应用**用来主动显隐键盘、读取键盘高度的公开能力。平台实现通常桥接到前者。
/// </para>
/// </summary>
public interface IKeyboardService
{
    /// <summary>显示键盘（Android）。</summary>
    Task ShowAsync();

    /// <summary>隐藏键盘。</summary>
    Task HideAsync();

    /// <summary>设置 iPhone 键盘辅助栏是否可见。</summary>
    Task SetAccessoryBarVisibleAsync(bool isVisible);

    /// <summary>设置 iOS 视图滚动是否禁用。</summary>
    Task SetScrollAsync(bool isDisabled);

    /// <summary>设置 iOS 键盘样式。</summary>
    Task SetStyleAsync(KeyboardStyleOptions options);

    /// <summary>设置 iOS 键盘调整模式。</summary>
    Task SetResizeModeAsync(KeyboardResizeOptions options);

    /// <summary>获取当前键盘调整模式。</summary>
    Task<KeyboardResizeOptions> GetResizeModeAsync();

    /// <summary>键盘即将显示。</summary>
    event Action<KeyboardInfo>? OnKeyboardWillShow;

    /// <summary>键盘已经显示。</summary>
    event Action<KeyboardInfo>? OnKeyboardDidShow;

    /// <summary>键盘即将隐藏。</summary>
    event Action? OnKeyboardWillHide;

    /// <summary>键盘已经隐藏。</summary>
    event Action? OnKeyboardDidHide;
}
