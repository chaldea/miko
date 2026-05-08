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
        }
    }
}
