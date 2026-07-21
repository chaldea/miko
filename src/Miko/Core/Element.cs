using Miko.Core.DomElements;
using Miko.Events;
using Miko.Layout;
using Miko.Styling;

namespace Miko.Core;

/// <summary>
/// 元素基类
/// </summary>
public abstract class Element
{
    // 全局 DOM/样式变更版本号：任何影响样式匹配或布局结果的修改（结构、文本、class/id、
    // 行内样式替换、元素状态、图片内禀尺寸等）都会使其递增。布局引擎据此判断上一次
    // 布局结果是否仍然有效——版本未变且视口/样式表未变时整棵布局树可直接复用（ISSUE-096）。
    private static long s_mutationVersion;

    /// <summary>当前全局变更版本号（单调递增）。</summary>
    public static long MutationVersion => Interlocked.Read(ref s_mutationVersion);

    /// <summary>
    /// 递增全局变更版本号。由元素自身的变更入口自动调用；引擎在元素外完成的
    /// 布局相关写入（动画帧值、图片内禀尺寸等）也应调用，否则下一帧可能复用过期布局。
    /// </summary>
    internal static void BumpMutationVersion() => Interlocked.Increment(ref s_mutationVersion);

    private string? _id;
    private string? _class;
    private Style? _style;

    public string? Id
    {
        get => _id;
        set { if (_id != value) { _id = value; IsDirty = true; BumpMutationVersion(); } }
    }

    public string? Class
    {
        get => _class;
        set { if (_class != value) { _class = value; IsDirty = true; BumpMutationVersion(); } }
    }
    public List<Element> Children { get; set; } = new();
    public Element? Parent { get; private set; }

    internal void SetParent(Element parent)
    {
        Parent = parent;
    }
    /// <summary>
    /// 行内样式。替换整个对象会递增变更版本号；但直接改写其属性（<c>Style.Width = ...</c>）
    /// 不会被追踪——引擎内这样做的只有 AnimationManager（已显式递增版本号），
    /// 用户代码若直接改写属性，需随后调用 <c>MikoEngine.InvalidateElement</c> 触发重排。
    /// </summary>
    public Style? Style
    {
        get => _style;
        set { if (!ReferenceEquals(_style, value)) { _style = value; IsDirty = true; BumpMutationVersion(); } }
    }

    // TextContent 的原始存储。仅由 TextNode（承载真实文本）与 TextContent facade 直接访问。
    // 普通元素不应直接写入此字段——文本应作为 TextNode 子节点存在，见 ISSUE-086。
    private string? _rawTextContent;

    /// <summary>
    /// 原始文本存储，供 <see cref="DomElements.TextNode"/> 及 <see cref="TextContent"/> facade 内部使用。
    /// </summary>
    internal string? RawTextContent
    {
        get => _rawTextContent;
        set => _rawTextContent = value;
    }

    /// <summary>
    /// 元素的直接文本内容（便利外观）。
    ///
    /// 自 ISSUE-086 起，文本以有序的 <see cref="DomElements.TextNode"/> 子节点形式存放，以保留
    /// 文本与标签的交错顺序。为兼容既有代码，此属性保留 string 语义：
    /// <list type="bullet">
    /// <item>get：拼接所有直接子 <see cref="DomElements.TextNode"/> 的文本；无文本子节点时返回 null。</item>
    /// <item>set：移除现有文本子节点，若值非空则重建单个前置文本节点（等价旧「文本在前」语义）。</item>
    /// </list>
    /// <see cref="DomElements.TextNode"/> 自身重写此逻辑，直接读写其 <see cref="RawTextContent"/>。
    /// </summary>
    public virtual string? TextContent
    {
        get
        {
            // 快速路径：无子节点。
            if (Children.Count == 0) return null;

            string? single = null;
            System.Text.StringBuilder? sb = null;
            bool any = false;
            foreach (var child in Children)
            {
                if (child is TextNode tn)
                {
                    any = true;
                    if (sb != null)
                    {
                        sb.Append(tn.Text);
                    }
                    else if (single != null)
                    {
                        sb = new System.Text.StringBuilder(single);
                        sb.Append(tn.Text);
                    }
                    else
                    {
                        single = tn.Text;
                    }
                }
            }

            if (!any) return null;
            return sb?.ToString() ?? single;
        }
        set
        {
            // 移除已有的文本节点。
            Children.RemoveAll(c => c is TextNode);
            if (!string.IsNullOrEmpty(value))
            {
                // 重建为单个前置文本节点，保持旧「文本排在子元素之前」的语义。
                var textNode = new TextNode(value);
                textNode.SetParent(this);
                Children.Insert(0, textNode);
            }
            IsDirty = true;
            BumpMutationVersion();
        }
    }

    internal Dictionary<PseudoElementType, Style>? PseudoElementStyles { get; set; }

    // 布局后的盒子模型引用
    internal LayoutBox? LayoutBox { get; set; }

    // 产生该元素的组件的清理回调（组件被替换/丢弃时调用，用于退订事件等）。
    // 以委托而非组件引用形式保存，避免 Core 反向依赖 Components 类型。
    internal Action? DisposeCallback { get; set; }

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
            BumpMutationVersion();
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
            BumpMutationVersion();
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

    /// <summary>
    /// 元素文本是否可被用户选择，反映 CSS <c>user-select</c>（<c>none</c> → 不可选）。
    /// 优先读取已计算样式（<c>user-select</c> 已随级联继承，故父级 <c>none</c> 也会传递到此）；
    /// 布局尚未产生计算样式时回退到父元素链，缺省视为可选。
    /// <para>供交互层在实现文本选择/拖选时查询：命中 <c>user-select: none</c> 的元素不应开始或
    /// 扩展选择（见 <see cref="Platform.MikoInteractionController"/>）。</para>
    /// </summary>
    public bool IsSelectable
    {
        get
        {
            var computed = LayoutBox?.ComputedStyle;
            if (computed != null)
                return computed.UserSelect != Miko.Common.UserSelect.None;
            // 无计算样式时回退到父链（构造期/未布局场景）。
            return Parent?.IsSelectable ?? true;
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
        BumpMutationVersion();
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
            BumpMutationVersion();
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
