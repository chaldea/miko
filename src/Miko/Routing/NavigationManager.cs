namespace Miko.Routing;

/// <summary>
/// 导航管理器：维护当前路径与导航历史栈，并在位置变化时广播
/// <see cref="NavigationEventArgs"/>（含方向与可选的页面转场效果，ISSUE-108）。
/// <para>
/// 方向语义对齐 Ionic：<see cref="NavigationDirection.Forward"/> 压栈（列表 → 详情）、
/// <see cref="NavigationDirection.Back"/> 出栈返回、<see cref="NavigationDirection.Root"/>
/// 清空栈的根级切换（如 Tab 切换，通常不配转场）。具体的转场效果由调用方
/// （组件库/应用）按方向提供，引擎只负责执行。
/// </para>
/// </summary>
public class NavigationManager
{
    private readonly List<string> _history = new() { "/" };
    private string _currentPath = "/";

    public string CurrentPath => _currentPath;

    /// <summary>导航历史栈（栈顶为当前路径）。</summary>
    public IReadOnlyList<string> History => _history;

    /// <summary>是否可以返回（历史栈中当前路径之下还有条目）。</summary>
    public bool CanGoBack => _history.Count > 1;

    public event Action<NavigationEventArgs>? LocationChanged;

    /// <summary>以前进（压栈）方向导航，无转场。等价于 <c>NavigateTo(path, NavigationDirection.Forward)</c>。</summary>
    public void NavigateTo(string path) => NavigateTo(path, NavigationDirection.Forward);

    /// <summary>
    /// 按指定方向导航到 <paramref name="path"/>，并可选地附带页面转场效果。
    /// 目标路径与当前路径相同则为 no-op（不触发事件、不改变历史栈）。
    /// </summary>
    public void NavigateTo(string path, NavigationDirection direction, NavigationTransition? transition = null)
    {
        if (_currentPath == path) return;

        var from = _currentPath;
        switch (direction)
        {
            case NavigationDirection.Forward:
                _history.Add(path);
                break;

            case NavigationDirection.Back:
                // 出栈当前页；目标页应与暴露出的栈顶一致，不一致时以目标为准补栈。
                if (_history.Count > 1) _history.RemoveAt(_history.Count - 1);
                if (_history[^1] != path) _history.Add(path);
                break;

            case NavigationDirection.Root:
                _history.Clear();
                _history.Add(path);
                break;
        }

        _currentPath = path;
        LocationChanged?.Invoke(new NavigationEventArgs
        {
            FromPath = from,
            ToPath = path,
            Direction = direction,
            Transition = transition
        });
    }

    /// <summary>
    /// 返回上一页（出栈）。历史栈中无可返回条目时返回 false 且不触发导航。
    /// </summary>
    public bool NavigateBack(NavigationTransition? transition = null)
    {
        if (!CanGoBack) return false;

        NavigateTo(_history[^2], NavigationDirection.Back, transition);
        return true;
    }
}
