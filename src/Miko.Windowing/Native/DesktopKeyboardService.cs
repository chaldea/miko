using Miko.Native;
using Miko.Native.Keyboard;
using Miko.Platform;

namespace Miko.Windowing.Native;

/// <summary>
/// 桌面键盘能力，桥接引擎已有的 <see cref="IInputMethodService"/>。
/// <para>
/// 桌面有物理键盘、没有可显隐的软键盘：<c>ShowAsync</c> 转发给 IME 端点（对有输入面板的
/// 平板模式有意义），<c>HideAsync</c> 则通过清空文本客户端来收起输入面板。
/// 其余成员在原始文档中即标注为 iOS 专有，桌面上抛 <see cref="PlatformNotSupportedException"/>。
/// </para>
/// </summary>
internal sealed class DesktopKeyboardService : NativeServiceBase, IKeyboardService
{
    private readonly IInputMethodService _inputMethod;

    public DesktopKeyboardService(IInputMethodService inputMethod) => _inputMethod = inputMethod;

    public Task ShowAsync()
    {
        _inputMethod.ShowKeyboard();
        return Task.CompletedTask;
    }

    public Task HideAsync()
    {
        // 清空文本客户端即告诉平台「当前无可编辑目标」，输入面板随之收起。
        _inputMethod.SetState(null);
        return Task.CompletedTask;
    }

    /// <summary>iOS 专有（iPhone 键盘辅助栏）。</summary>
    public Task SetAccessoryBarVisibleAsync(bool isVisible) => UnsupportedAsync();

    /// <summary>iOS 专有（WebView 滚动开关）。</summary>
    public Task SetScrollAsync(bool isDisabled) => UnsupportedAsync();

    /// <summary>iOS 专有（键盘外观样式）。</summary>
    public Task SetStyleAsync(KeyboardStyleOptions options) => UnsupportedAsync();

    /// <summary>iOS 专有（键盘调整模式）。</summary>
    public Task SetResizeModeAsync(KeyboardResizeOptions options) => UnsupportedAsync();

    /// <summary>iOS 专有（键盘调整模式）。</summary>
    public Task<KeyboardResizeOptions> GetResizeModeAsync() => UnsupportedAsync<KeyboardResizeOptions>();

    /// <summary>桌面软键盘不上报显隐，这些事件永不触发。</summary>
    public event Action<KeyboardInfo>? OnKeyboardWillShow { add { } remove { } }
    public event Action<KeyboardInfo>? OnKeyboardDidShow { add { } remove { } }
    public event Action? OnKeyboardWillHide { add { } remove { } }
    public event Action? OnKeyboardDidHide { add { } remove { } }
}
