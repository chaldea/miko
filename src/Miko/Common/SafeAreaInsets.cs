namespace Miko.Common;

/// <summary>
/// 安全区边距（逻辑像素）。由平台宿主从 OS 系统栏（状态栏、导航栏）获取，
/// 传递给引擎以防止内容被系统 UI 遮盖。
/// </summary>
public readonly record struct SafeAreaInsets(float Left, float Top, float Right, float Bottom)
{
    /// <summary>无安全区（桌面平台或未初始化时的默认值）。</summary>
    public static readonly SafeAreaInsets Zero = new(0, 0, 0, 0);

    public bool IsZero => Left == 0 && Top == 0 && Right == 0 && Bottom == 0;
}
