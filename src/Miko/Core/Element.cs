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

    /// <summary>
    /// 元素边框盒的宽度（对应 DOM 的 offsetWidth）。尚未布局时为 0。
    /// </summary>
    public float OffsetWidth => LayoutBox?.BoxModel.BorderBox.Width ?? 0f;

    /// <summary>
    /// 元素边框盒的高度（对应 DOM 的 offsetHeight）。尚未布局时为 0。
    /// </summary>
    public float OffsetHeight => LayoutBox?.BoxModel.BorderBox.Height ?? 0f;

    // 产生该元素的组件的清理回调（组件被替换/丢弃时调用，用于退订事件等）。
    // 以委托而非组件引用形式保存，避免 Core 反向依赖 Components 类型。
    internal Action? DisposeCallback { get; set; }

    /// <summary>
    /// 组件重渲染时接替本元素位置的新实例（由 <c>ComponentBase.TransferRuntimeState</c> 写入）。
    /// <para>组件的每次重渲染都产出<b>全新</b>的元素实例替换整棵子树，而交互状态（焦点、
    /// 文本光标位置）活在实例上，控制器也按引用缓存焦点/拖拽目标。该指针把旧实例转发到
    /// 在场实例，使这些缓存能重新指向正确的元素——否则事件处理器一旦触发重渲染，焦点就
    /// 留在已脱离树的旧实例上（ISSUE-121：点了输入框却不画光标）。</para>
    /// </summary>
    internal Element? SupersededBy { get; set; }

    /// <summary>
    /// 沿 <see cref="SupersededBy"/> 链取回当前仍在树中的实例；本元素未被替换时返回自身。
    /// </summary>
    internal Element ResolveSuperseded()
    {
        var current = this;
        while (current.SupersededBy is { } next)
            current = next;
        // 路径压缩：反复重渲染会把链越接越长，逐次遍历既慢又让整条链上的旧实例都活着。
        if (!ReferenceEquals(current, this))
            SupersededBy = current;
        return current;
    }

    /// <summary>
    /// 从被本元素替换掉的旧实例上接过交互运行时状态（ISSUE-121）。
    /// <para>只搬迁「由交互产生、不由组件参数重新写入」的状态：交互状态标志位（焦点/悬停/
    /// 按下）。<see cref="ElementState.Disabled"/> 不在其中——它由组件按参数每次重新标注，
    /// 搬迁反而会让 <c>Disabled</c> 参数转为 false 后仍卡在禁用态。</para>
    /// <para>子类可重写以追加自己的状态（如输入控件的光标位置）。</para>
    /// </summary>
    internal virtual void CopyInteractionStateFrom(Element old)
    {
        // 静默置位：本元素尚未进入布局树，样式尚未解析，标脏无意义；随后的重排会照常
        // 按新状态级联（这条路径本身就发生在一次重渲染中间）。
        var carried = old.State & (ElementState.Focus | ElementState.Hover | ElementState.Active);
        if (carried != ElementState.None)
            SetState(carried, invalidate: false);
    }

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

    /// <summary>
    /// 是否订阅了该事件——便捷属性（OnClick/OnScroll…）或 AddEventListener 注册的监听器。
    /// 供每帧路径（如滚动的向下派发）零分配地筛掉绝大多数无监听器的元素。
    /// </summary>
    internal bool HasListenerFor(string eventType)
    {
        for (int i = 0; i < _eventListeners.Count; i++)
        {
            if (_eventListeners[i].EventType == eventType) return true;
        }

        return eventType switch
        {
            EventTypes.Click => OnClick != null,
            EventTypes.MouseEnter => OnMouseEnter != null,
            EventTypes.MouseLeave => OnMouseLeave != null,
            EventTypes.MouseDown => OnMouseDown != null,
            EventTypes.MouseUp => OnMouseUp != null,
            EventTypes.Focus => OnFocus != null,
            EventTypes.Blur => OnBlur != null,
            EventTypes.Change => OnChange != null,
            EventTypes.Scroll => OnScroll != null,
            EventTypes.KeyDown => OnKeyDown != null,
            EventTypes.Input => OnInput != null,
            _ => false,
        };
    }

    // 元素状态
    public ElementState State { get; private set; } = ElementState.None;

    /// <summary>
    /// 设置状态标志
    /// </summary>
    public void SetState(ElementState state) => SetState(state, invalidate: true);

    /// <summary>
    /// 设置状态标志。<paramref name="invalidate"/> 为 false 时仅更新标志位，
    /// 不标脏也不递增 MutationVersion——仅用于调用方已确知该状态变化不可能影响
    /// 样式匹配/布局结果的场景（如悬停元素与所有 :hover 规则无关，见 ISSUE-104
    /// 问题1），避免一次无谓的全量重排。
    /// </summary>
    internal void SetState(ElementState state, bool invalidate)
    {
        if ((State & state) != state)
        {
            State |= state;
            if (invalidate)
            {
                IsDirty = true;
                BumpMutationVersion();
            }
        }
    }

    /// <summary>
    /// 清除状态标志
    /// </summary>
    public void ClearState(ElementState state) => ClearState(state, invalidate: true);

    /// <summary>见 <see cref="SetState(ElementState, bool)"/>。</summary>
    internal void ClearState(ElementState state, bool invalidate)
    {
        if ((State & state) != ElementState.None)
        {
            State &= ~state;
            if (invalidate)
            {
                IsDirty = true;
                BumpMutationVersion();
            }
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

        if (HasClass(className))
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
    /// 检查是否有指定的 class。
    /// </summary>
    /// <remarks>
    /// 样式解析的最热路径（ISSUE-113）：每帧对每个元素测试每条规则，
    /// <see cref="ClassSelector"/> 最终都落到这里——一个 287 元素 × 1868 条规则的
    /// Ionic 页面单帧就有约 53 万次调用。旧实现 <c>Class.Split(' ').Contains(...)</c>
    /// 每次调用都分配一个 <c>string[]</c> 加每个 token 的子串再套一层 LINQ 枚举器，
    /// 单帧因此产生约 60 MB 垃圾，触发持续的 gen0 回收与可见卡顿。
    /// 此处改为零分配的 span 分词：按 CSS 的空白语义（空格/制表/换行皆为分隔符）
    /// 逐 token 比较，不产生任何中间对象。
    /// </remarks>
    public bool HasClass(string className)
    {
        return ContainsClassToken(_class, className);
    }

    /// <summary>
    /// 判断空白分隔的 <paramref name="classList"/> 中是否含有 <paramref name="token"/>，
    /// 全程不分配（见 <see cref="HasClass"/>）。
    /// </summary>
    internal static bool ContainsClassToken(string? classList, string token)
    {
        if (string.IsNullOrEmpty(classList) || string.IsNullOrEmpty(token)) return false;

        // 快速路径：整个 class 串就是该 token（最常见的单类名元素）。
        if (string.Equals(classList, token, StringComparison.Ordinal)) return true;

        ReadOnlySpan<char> remaining = classList.AsSpan();
        ReadOnlySpan<char> needle = token.AsSpan();

        while (!remaining.IsEmpty)
        {
            // 跳过前导空白。
            int start = 0;
            while (start < remaining.Length && char.IsWhiteSpace(remaining[start])) start++;
            if (start >= remaining.Length) break;
            remaining = remaining[start..];

            // 取出一个 token。
            int end = 0;
            while (end < remaining.Length && !char.IsWhiteSpace(remaining[end])) end++;

            if (remaining[..end].SequenceEqual(needle)) return true;

            remaining = remaining[end..];
        }

        return false;
    }

    public override string ToString() => $"<{TagName} id=\"{Id}\" class=\"{Class}\">";
}
