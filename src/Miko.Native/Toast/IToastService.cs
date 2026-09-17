namespace Miko.Native.Toast;

/// <summary>
/// Toast 提示。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/native/toast#api">Toast API</see>。
/// 支持平台：Android、iOS。
/// </summary>
public interface IToastService
{
    /// <summary>显示一条 Toast。</summary>
    Task ShowAsync(ToastOptions options);
}
