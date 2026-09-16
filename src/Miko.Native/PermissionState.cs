namespace Miko.Native;

/// <summary>
/// 原生权限状态，对应 Capacitor 的 <c>PermissionState</c>。
/// 相机、定位、通知、文件等能力的权限查询/请求统一返回本枚举。
/// </summary>
public enum PermissionState
{
    /// <summary>尚未请求过，调用时会弹出系统授权提示。</summary>
    Prompt,

    /// <summary>用户此前拒绝过，再次请求前应先向用户说明用途（Android 的 rationale）。</summary>
    PromptWithRationale,

    /// <summary>已授权。</summary>
    Granted,

    /// <summary>已拒绝；通常需要用户前往系统设置手动开启。</summary>
    Denied,
}
