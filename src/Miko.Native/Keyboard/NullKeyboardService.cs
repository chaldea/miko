namespace Miko.Native.Keyboard;

/// <summary>未注册平台实现时的 <see cref="IKeyboardService"/> 占位实现，调用即抛。</summary>
public sealed class NullKeyboardService : NativeServiceBase, IKeyboardService
{
    public Task ShowAsync() => UnsupportedAsync();
    public Task HideAsync() => UnsupportedAsync();
    public Task SetAccessoryBarVisibleAsync(bool isVisible) => UnsupportedAsync();
    public Task SetScrollAsync(bool isDisabled) => UnsupportedAsync();
    public Task SetStyleAsync(KeyboardStyleOptions options) => UnsupportedAsync();
    public Task SetResizeModeAsync(KeyboardResizeOptions options) => UnsupportedAsync();
    public Task<KeyboardResizeOptions> GetResizeModeAsync() => UnsupportedAsync<KeyboardResizeOptions>();

    public event Action<KeyboardInfo>? OnKeyboardWillShow { add { } remove { } }
    public event Action<KeyboardInfo>? OnKeyboardDidShow { add { } remove { } }
    public event Action? OnKeyboardWillHide { add { } remove { } }
    public event Action? OnKeyboardDidHide { add { } remove { } }
}
