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
    private readonly List<HistoryEntry> _history = new() { new("/", null) };
    private readonly HistoryPaths _historyPaths;

    public NavigationManager() => _historyPaths = new HistoryPaths(_history);

    public string CurrentPath => _history[^1].Path;

    /// <summary>
    /// The current history entry's in-memory state, or null. Objects are held by reference,
    /// without serialization, and are not persisted in the URL or across application restarts.
    /// Store application data, not page components, elements, layout boxes or delegates that
    /// capture them: anything reachable from state remains alive with its history entry.
    /// Pages subscribing to state events should unsubscribe in OnDispose.
    /// </summary>
    public object? CurrentState => _history[^1].State;

    /// <summary>
    /// Returns the current state when it is assignable to <typeparamref name="T"/>;
    /// otherwise returns default. Reading state does not consume it or convert its type.
    /// </summary>
    public T? GetState<T>() => CurrentState is T state ? state : default;

    /// <summary>导航历史栈（栈顶为当前路径）。</summary>
    public IReadOnlyList<string> History => _historyPaths;

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
        => NavigateToCore(path, direction, transition, null, hasState: false);

    /// <summary>
    /// Navigates with state held by reference for the destination history entry. State is
    /// available before <see cref="LocationChanged"/> fires and before the page initializes.
    /// On Back, explicitly supplied state replaces the destination's saved state (null clears it).
    /// Navigating to the current path is a no-op, including its state.
    /// </summary>
    public void NavigateTo(string path, object? state, NavigationDirection direction = NavigationDirection.Forward,
        NavigationTransition? transition = null)
        => NavigateToCore(path, direction, transition, state, hasState: true);

    private void NavigateToCore(string path, NavigationDirection direction, NavigationTransition? transition,
        object? state, bool hasState)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (CurrentPath == path) return;

        var from = CurrentPath;
        switch (direction)
        {
            case NavigationDirection.Forward:
                _history.Add(new HistoryEntry(path, state));
                break;

            case NavigationDirection.Back:
                // 出栈当前页；目标页应与暴露出的栈顶一致，不一致时以目标为准补栈。
                if (_history.Count > 1) _history.RemoveAt(_history.Count - 1);
                if (_history[^1].Path != path)
                    _history.Add(new HistoryEntry(path, state));
                else if (hasState)
                    _history[^1] = new HistoryEntry(path, state);
                break;

            case NavigationDirection.Root:
                _history.Clear();
                _history.Add(new HistoryEntry(path, state));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown navigation direction.");
        }

        LocationChanged?.Invoke(new NavigationEventArgs
        {
            FromPath = from,
            ToPath = path,
            Direction = direction,
            Transition = transition,
            State = CurrentState
        });
    }

    /// <summary>
    /// 返回上一页（出栈）。历史栈中无可返回条目时返回 false 且不触发导航。
    /// </summary>
    public bool NavigateBack(NavigationTransition? transition = null)
    {
        if (!CanGoBack) return false;

        NavigateTo(_history[^2].Path, NavigationDirection.Back, transition);
        return true;
    }

    private readonly record struct HistoryEntry(string Path, object? State);

    // Keep the public path-only history as a live, read-only view of the entries.
    private sealed class HistoryPaths(List<HistoryEntry> entries) : IReadOnlyList<string>
    {
        public int Count => entries.Count;
        public string this[int index] => entries[index].Path;

        public IEnumerator<string> GetEnumerator()
        {
            // A List<HistoryEntry>.Enumerator would keep a copy of State across yield,
            // retaining it even after Back/Root removes that entry from history.
            for (var i = 0; i < entries.Count; i++)
                yield return entries[i].Path;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
