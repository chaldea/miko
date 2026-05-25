using Miko.Events;
using Miko.Layout;
using Miko.Styling;

namespace Miko.Core;

/// <summary>
/// 元素基类
/// </summary>
public abstract class Element
{
    public string? Id { get; set; }
    public string? Class { get; set; }
    public List<Element> Children { get; set; } = new();
    public Element? Parent { get; private set; }

    internal void SetParent(Element parent)
    {
        Parent = parent;
    }
    public Style? Style { get; set; }
    public string? TextContent { get; set; }

    internal Dictionary<PseudoElementType, Style>? PseudoElementStyles { get; set; }

    // 布局后的盒子模型引用
    internal LayoutBox? LayoutBox { get; set; }

    // 脏标记
    internal bool IsDirty { get; set; }

    // 事件监听器列表
    private readonly List<EventListener> _eventListeners = new();

    // 便捷事件处理器属性
    public MikoEventHandler<MouseEventArgs>? OnClick { get; set; }
    public MikoEventHandler<MouseEventArgs>? OnMouseEnter { get; set; }
    public MikoEventHandler<MouseEventArgs>? OnMouseLeave { get; set; }
    public MikoEventHandler<MouseEventArgs>? OnMouseDown { get; set; }
    public MikoEventHandler<MouseEventArgs>? OnMouseUp { get; set; }
    public MikoEventHandler<FocusEventArgs>? OnFocus { get; set; }
    public MikoEventHandler<FocusEventArgs>? OnBlur { get; set; }
    public MikoEventHandler<ChangeEventArgs>? OnChange { get; set; }
    public MikoEventHandler<ScrollEventArgs>? OnScroll { get; set; }
    public MikoEventHandler<KeyboardEventArgs>? OnKeyDown { get; set; }
    public MikoEventHandler<InputEventArgs>? OnInput { get; set; }

    /// <summary>
    /// 添加事件监听器
    /// </summary>
    public void AddEventListener<T>(string eventType, MikoEventHandler<T> handler) where T : MikoEventArgs
    {
        _eventListeners.Add(new EventListener
        {
            EventType = eventType,
            Handler = handler
        });
    }

    /// <summary>
    /// 移除事件监听器
    /// </summary>
    public void RemoveEventListener<T>(string eventType, MikoEventHandler<T> handler) where T : MikoEventArgs
    {
        _eventListeners.RemoveAll(l => l.EventType == eventType && l.Handler.Equals(handler));
    }

    /// <summary>
    /// 获取指定类型的事件监听器
    /// </summary>
    internal IEnumerable<EventListener> GetEventListeners(string eventType)
    {
        return _eventListeners.Where(l => l.EventType == eventType);
    }

    // 元素状态
    public ElementState State { get; private set; } = ElementState.None;

    /// <summary>
    /// 设置状态标志
    /// </summary>
    public void SetState(ElementState state)
    {
        if ((State & state) != state)
        {
            State |= state;
            IsDirty = true;
        }
    }

    /// <summary>
    /// 清除状态标志
    /// </summary>
    public void ClearState(ElementState state)
    {
        if ((State & state) != ElementState.None)
        {
            State &= ~state;
            IsDirty = true;
        }
    }

    /// <summary>
    /// 检查是否有指定状态
    /// </summary>
    public bool HasState(ElementState state)
    {
        return (State & state) == state;
    }

    /// <summary>
    /// 检查元素是否被禁用（包括检查父元素链）
    /// </summary>
    public bool IsDisabled
    {
        get
        {
            if (HasState(ElementState.Disabled)) return true;
            return Parent?.IsDisabled ?? false;
        }
    }

    public abstract string TagName { get; }

    /// <summary>
    /// 添加子元素
    /// </summary>
    public void AddChild(Element child)
    {
        if (child.Parent != null)
        {
            child.Parent.RemoveChild(child);
        }

        Children.Add(child);
        child.Parent = this;
        IsDirty = true;
    }

    /// <summary>
    /// 移除子元素
    /// </summary>
    public bool RemoveChild(Element child)
    {
        if (Children.Remove(child))
        {
            child.Parent = null;
            IsDirty = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 查找元素（通过ID）
    /// </summary>
    public Element? FindById(string id)
    {
        if (Id == id) return this;

        foreach (var child in Children)
        {
            var found = child.FindById(id);
            if (found != null) return found;
        }

        return null;
    }

    /// <summary>
    /// 查找元素（通过Class）
    /// </summary>
    public List<Element> FindByClass(string className)
    {
        var results = new List<Element>();

        if (Class != null && Class.Split(' ').Contains(className))
        {
            results.Add(this);
        }

        foreach (var child in Children)
        {
            results.AddRange(child.FindByClass(className));
        }

        return results;
    }

    /// <summary>
    /// 查找元素（通过标签名）
    /// </summary>
    public List<Element> FindByTagName(string tagName)
    {
        var results = new List<Element>();

        if (TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase))
        {
            results.Add(this);
        }

        foreach (var child in Children)
        {
            results.AddRange(child.FindByTagName(tagName));
        }

        return results;
    }

    /// <summary>
    /// 检查是否有指定的class
    /// </summary>
    public bool HasClass(string className)
    {
        return Class != null && Class.Split(' ').Contains(className);
    }

    public override string ToString() => $"<{TagName} id=\"{Id}\" class=\"{Class}\">";
}
