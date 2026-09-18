using Miko.Core.DomElements;
using Miko.Diagnostics;
using Miko.Events;
using Miko.Layout;
using Miko.Styling;

namespace Miko.Core;

/// <summary>
/// 元素基类
/// </summary>
public abstract class Element
{
    /// <summary>
    /// 本元素所属引擎的变更计数器（ISSUE-129）。由引擎在 <c>Initialize</c> / 每帧渲染时
    /// 沿树下发（见 <see cref="AssignOwner"/>）。
    ///
    /// <para>为 null 表示该元素尚未挂入任何引擎（构造期的游离元素）：此时它的变更静默
    /// 不计数——没有引擎在观察它，也就没有布局缓存需要失效。元素被挂入引擎时的
    /// <see cref="AssignOwner"/> 遍历本身就发生在一次结构变更之后，不会漏掉更新。</para>
    /// </summary>
    internal MutationTracker? Owner { get; private set; }

    /// <summary>
    /// 递增所属引擎的变更版本号。由元素自身的变更入口自动调用；引擎在元素外完成的
    /// 布局相关写入（动画帧值、图片内禀尺寸等）也应调用，否则下一帧可能复用过期布局。
    /// </summary>
    internal void BumpMutationVersion() => Owner?.Bump();

    /// <summary>
    /// 把本子树的归属设为 <paramref name="tracker"/>（ISSUE-129）。
    ///
    /// <para>由引擎在 <c>Initialize</c> 与每帧渲染时对根元素调用。<b>必须</b>是引擎侧的树遍历
    /// 而非只在 <see cref="AddChild"/> 里传播：<see cref="Children"/> 是公开的 List，
    /// 集合初始化器（<c>Children = { ... }</c>）、<see cref="TextContent"/> setter、
    /// 组件重渲染的子树替换（<c>ComponentBase.ReplaceElementContent</c>）都会绕过
    /// <see cref="AddChild"/> 直接写入该 List。</para>
    ///
    /// <para>刻意<b>不</b>在「本节点归属已正确」时剪枝：正因为上面那些路径能把新子节点直接
    /// 塞进一个归属已正确的父节点的 <see cref="Children"/> 里，剪枝会让这些新子树永远拿不到
    /// 归属，其后所有变更都静默丢失（布局缓存不失效 → 界面不更新）。遍历成本可以接受——
    /// 引擎每帧本来就要为动画对齐整树扫描一遍（<c>CollectDeclaredAnimations</c>），
    /// 本方法不增加渐近成本。</para>
    /// </summary>
    internal void AssignOwner(MutationTracker tracker)
    {
        Owner = tracker;
        foreach (var child in Children)
            child.AssignOwner(tracker);
    }

    /// <summary>
    /// 清除本子树的引擎归属（ISSUE-129）。在子树被移出 DOM 时调用：此后它的变更不再递增
    /// 任何引擎的版本号，回到「游离元素不记账」的初始语义。若该子树日后被挂回某个引擎，
    /// <see cref="AddChild"/> 或引擎的 <see cref="AssignOwner"/> 遍历会重新为它赋予归属。
    /// </summary>
    internal void ClearOwner()
    {
        Owner = null;
        foreach (var child in Children)
            child.ClearOwner();
    }

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
    /// <summary>
    /// 子节点集合。所有增删改都会自动设置父引用、下发引擎归属并使布局缓存失效
    /// （见 <see cref="ElementCollection"/>，ISSUE-129）——直接写这个集合与调用
    /// <see cref="AddChild"/> 同样安全。
    /// </summary>
    public ElementCollection Children { get; }

    protected Element()
    {
        LayoutAllocationDiagnostics.RecordElementCreated();
        Children = new ElementCollection(this);
    }

    public Element? Parent { get; private set; }

    internal void SetParent(Element parent)
    {
        Parent = parent;
    }

    /// <summary>断开父引用。供 <see cref="ElementCollection"/> 在子节点被移除/替换时调用。</summary>
    internal void ClearParent() => Parent = null;

    /// <summary>标记本元素需要重绘。供 <see cref="ElementCollection"/> 在结构变更时调用。</summary>
    internal void MarkDirty() => IsDirty = true;
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
            // 移除已有的文本节点。用不记账的原始写入，本 setter 末尾统一记账一次。
            Children.RemoveAllInternal(c => c is TextNode);
            if (!string.IsNullOrEmpty(value))
            {
                // 重建为单个前置文本节点，保持旧「文本排在子元素之前」的语义。
                var textNode = new TextNode(value);
                textNode.SetParent(this);
                // 该节点绕过 AddChild 直接写入集合，需自行继承引擎归属（ISSUE-129）。
                if (Owner != null) textNode.AssignOwner(Owner);
                Children.InsertInternal(0, textNode);
            }
            IsDirty = true;
            BumpMutationVersion();
        }
    }

    internal Dictionary<PseudoElementType, Style>? PseudoElementStyles { get; set; }

    // 布局后的盒子模型引用
    internal LayoutBox? LayoutBox { get; set; }

    private float? _initialScrollTop;

    /// <summary>
    /// Requests a vertical scroll offset for the next layout. The request is consumed once layout
    /// applies it. Components use this for an initial or externally-controlled scroll position.
    /// </summary>
    public float? InitialScrollTop
    {
        get => _initialScrollTop;
        set => _initialScrollTop = value;
    }

    internal float? ConsumeInitialScrollTop()
    {
        var value = _initialScrollTop;
        _initialScrollTop = null;
        return value;
    }

    /// <summary>
    /// 元素边框盒的宽度（对应 DOM 的 offsetWidth）。尚未布局时为 0。
    /// </summary>
    public float OffsetWidth => LayoutBox?.BoxModel.BorderBox.Width ?? 0f;

    /// <summary>
    /// 元素边框盒的高度（对应 DOM 的 offsetHeight）。尚未布局时为 0。
    /// </summary>
    public float OffsetHeight => LayoutBox?.BoxModel.BorderBox.Height ?? 0f;

    /// <summary>Current vertical scroll offset after layout, or zero before layout.</summary>
    public float ScrollTop => LayoutBox?.ScrollTop ?? 0f;

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
    public MikoEventHandler<MouseEventArgs>? OnMouseMove { get; set; }
    public MikoEventHandler<PointerEventArgs>? OnPointerDown { get; set; }
    public MikoEventHandler<PointerEventArgs>? OnPointerUp { get; set; }
    public MikoEventHandler<PointerEventArgs>? OnPointerMove { get; set; }
    public MikoEventHandler<PointerEventArgs>? OnPointerCancel { get; set; }
    public MikoEventHandler<PointerEventArgs>? OnLongPress { get; set; }
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
            EventTypes.MouseMove => OnMouseMove != null,
            EventTypes.PointerDown => OnPointerDown != null,
            EventTypes.PointerUp => OnPointerUp != null,
            EventTypes.PointerMove => OnPointerMove != null,
            EventTypes.PointerCancel => OnPointerCancel != null,
            EventTypes.LongPress => OnLongPress != null,
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
    /// 按名称取「HTML 属性」的值，供 CSS 属性选择器（<c>[type="checkbox"]</c>）匹配。
    /// 名称比较不区分大小写。返回 <c>false</c> 表示该元素没有这个属性。
    ///
    /// <para>Miko 没有属性字典，HTML 属性就是元素的 CLR 属性，所以这里原本用
    /// <c>GetType().GetProperty(name, IgnoreCase)</c> 反射。裁剪器看不见那次查找（IL2075），
    /// AOT 下所有属性选择器会静默失配——规则看似存在却永不命中。改由
    /// <c>Miko.SourceGenerators.ElementAttributeAccessorGenerator</c> 生成的
    /// <see cref="ElementAttributeAccessor"/> 按具体类型分派，反射彻底消失（ISSUE-140）。</para>
    ///
    /// <para><c>virtual</c> 而非直接调用生成表：<c>Miko</c> 之外的程序集（组件库、应用）也能
    /// 定义自己的 <see cref="Element"/> 子类，重写本方法即可让属性选择器认得它们的属性。</para>
    /// </summary>
    public virtual bool TryGetAttributeValue(string name, out object? value)
        => ElementAttributeAccessor.TryGetValue(this, name, out value);

    /// <summary>
    /// 添加子元素
    /// </summary>
    public void AddChild(Element child)
    {
        if (child.Parent != null)
        {
            child.Parent.RemoveChild(child);
        }

        // 用不记账的原始写入，本方法自己完成父引用/归属/记账（否则版本号会被递增两次）。
        Children.AddInternal(child);
        child.Parent = this;
        // 新子树立即继承本元素的引擎归属，使其在挂入后的变更马上被计入，而不必等到
        // 下一帧引擎的 AssignOwner 遍历（ISSUE-129）。本元素尚无归属时不下发，
        // 该子树会在整棵树被挂入引擎时一并赋值。
        if (Owner != null) child.AssignOwner(Owner);
        IsDirty = true;
        BumpMutationVersion();
    }

    /// <summary>
    /// 移除子元素
    /// </summary>
    public bool RemoveChild(Element child)
    {
        // 用不记账的原始写入，本方法自己完成记账（否则版本号会被递增两次）。
        if (Children.RemoveInternal(child))
        {
            child.Parent = null;
            // 移除本身是结构变更，必须先记在**旧引擎**头上——它的布局缓存要失效。
            IsDirty = true;
            BumpMutationVersion();
            // 随后才清除被移除子树的归属：已脱离 DOM 的元素继续被修改时不应再递增旧引擎的
            // 版本号，否则会产生无意义的重排工作，也违背「未挂入引擎的游离元素不记账」的
            // 归属语义（ISSUE-129）。顺序不能颠倒。
            child.ClearOwner();
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
