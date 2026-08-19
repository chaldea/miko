using Miko.Core;

namespace Miko.Events;

/// <summary>
/// 事件处理器委托
/// </summary>
public delegate void MikoEventHandler<in T>(T args) where T : MikoEventArgs;

/// <summary>
/// 事件监听器注册信息
/// </summary>
internal class EventListener
{
    public required string EventType { get; init; }
    public required Delegate Handler { get; init; }
}

/// <summary>
/// 事件分发器，支持事件冒泡
/// </summary>
public class EventDispatcher
{
    /// <summary>
    /// 向目标元素分发事件（支持冒泡）
    /// </summary>
    public void Dispatch<T>(Element target, string eventType, T args) where T : MikoEventArgs
    {
        // 禁用的元素不接收事件（除了mouseleave）
        if (target.IsDisabled && eventType != EventTypes.MouseLeave)
            return;

        args.CurrentTarget = target;

        // 构建祖先链用于冒泡
        var ancestors = new List<Element>();
        var current = target.Parent;
        while (current != null)
        {
            ancestors.Add(current);
            current = current.Parent;
        }

        // 目标阶段 - 在目标上调用处理器
        InvokeHandlers(target, eventType, args);
        InvokeConvenienceHandler(target, eventType, args);

        // 冒泡阶段（从下往上）
        if (args.Bubbles && !args.IsPropagationStopped)
        {
            foreach (var ancestor in ancestors)
            {
                if (args.IsPropagationStopped) break;

                args.CurrentTarget = ancestor;
                InvokeHandlers(ancestor, eventType, args);
                InvokeConvenienceHandler(ancestor, eventType, args);
            }
        }
    }

    /// <summary>
    /// 向 <paramref name="source"/> 的子孙元素分发事件（不含 <paramref name="source"/> 自身）。
    /// <para>
    /// 冒泡只能让祖先感知事件，但滚动这类事件的语义相反：真正滚动的是容器，而关心它的组件
    /// （如 ion-infinite-scroll）往往是容器的后代。DOM 中这类组件通过在滚动元素上注册监听器
    /// 解决，Miko 的组件拿不到祖先引用，因此由引擎在目标+冒泡之后额外向下通知一次。
    /// </para>
    /// <para>
    /// <paramref name="shouldPrune"/> 返回 true 的子树会被整体跳过（含该元素本身），用于
    /// 剪掉嵌套的独立滚动容器——外层滚动不应触发内层容器内部的监听器。
    /// </para>
    /// </summary>
    public void DispatchToDescendants<T>(
        Element source,
        string eventType,
        T args,
        Func<Element, bool>? shouldPrune = null) where T : MikoEventArgs
    {
        // 先把接收者收集成快照，再逐个回调：处理器很可能调用 StateHasChanged 重建 DOM
        // （ion-infinite-scroll 触发时就会），边遍历边回调会让 Children 在枚举中被改写。
        //
        // 只收集<b>确实订阅了该事件</b>的元素。滚动是每帧路径，而绝大多数子树里一个监听器都
        // 没有——此时 targets 保持为 null，整趟遍历零分配。
        List<Element>? targets = null;
        CollectTargets(source, eventType, ref targets, shouldPrune);
        if (targets == null) return;

        foreach (var target in targets)
        {
            if (args.IsPropagationStopped) return;

            args.CurrentTarget = target;
            InvokeHandlers(target, eventType, args);
            InvokeConvenienceHandler(target, eventType, args);
        }
    }

    private static void CollectTargets(
        Element element,
        string eventType,
        ref List<Element>? targets,
        Func<Element, bool>? shouldPrune)
    {
        var children = element.Children;
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (shouldPrune != null && shouldPrune(child)) continue;

            // 与 Dispatch 保持一致：禁用的元素不接收事件（向下派发不涉及 mouseleave）。
            if (!child.IsDisabled && child.HasListenerFor(eventType))
                (targets ??= new List<Element>()).Add(child);

            CollectTargets(child, eventType, ref targets, shouldPrune);
        }
    }

    private static void InvokeHandlers<T>(Element element, string eventType, T args) where T : MikoEventArgs
    {
        foreach (var listener in element.GetEventListeners(eventType))
        {
            if (listener.Handler is MikoEventHandler<T> handler)
            {
                handler(args);
            }
        }
    }

    private static void InvokeConvenienceHandler<T>(Element element, string eventType, T args) where T : MikoEventArgs
    {
        switch (eventType)
        {
            case EventTypes.Click when args is MouseEventArgs mouseArgs:
                element.OnClick?.Invoke(mouseArgs);
                break;
            case EventTypes.MouseEnter when args is MouseEventArgs mouseArgs:
                element.OnMouseEnter?.Invoke(mouseArgs);
                break;
            case EventTypes.MouseLeave when args is MouseEventArgs mouseArgs:
                element.OnMouseLeave?.Invoke(mouseArgs);
                break;
            case EventTypes.MouseDown when args is MouseEventArgs mouseArgs:
                element.OnMouseDown?.Invoke(mouseArgs);
                break;
            case EventTypes.MouseUp when args is MouseEventArgs mouseArgs:
                element.OnMouseUp?.Invoke(mouseArgs);
                break;
            case EventTypes.MouseMove when args is MouseEventArgs mouseArgs:
                element.OnMouseMove?.Invoke(mouseArgs);
                break;
            case EventTypes.Focus when args is FocusEventArgs focusArgs:
                element.OnFocus?.Invoke(focusArgs);
                break;
            case EventTypes.Blur when args is FocusEventArgs focusArgs:
                element.OnBlur?.Invoke(focusArgs);
                break;
            case EventTypes.Change when args is ChangeEventArgs changeArgs:
                element.OnChange?.Invoke(changeArgs);
                break;
            case EventTypes.Scroll when args is ScrollEventArgs scrollArgs:
                element.OnScroll?.Invoke(scrollArgs);
                break;
            case EventTypes.KeyDown when args is KeyboardEventArgs keyArgs:
                element.OnKeyDown?.Invoke(keyArgs);
                break;
            case EventTypes.Input when args is InputEventArgs inputArgs:
                element.OnInput?.Invoke(inputArgs);
                break;
        }
    }
}
