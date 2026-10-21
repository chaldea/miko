using System.Collections;

namespace Miko.Core;

/// <summary>
/// 元素的子节点集合（ISSUE-129）。取代裸 <see cref="List{T}"/>，使**所有**结构写入都可观察：
/// 每次增删改都会为新子树设置父引用、下发引擎归属，并递增所属引擎的变更版本号。
///
/// <para>为什么必须是这样一个类型而不是 <c>List&lt;Element&gt;</c>：<c>Element.Children</c> 是公开
/// 成员，调用方可以绕开 <see cref="Element.AddChild"/> 直接写它——</para>
/// <code>
/// root.Children.Add(child);
/// root.Children.Clear();
/// root.Children[i] = replacement;
/// </code>
/// <para>这些写入若不记账，稳态帧之后的结构变化就不会使布局缓存失效，随后的
/// <c>Render</c> 会命中旧布局：新增/删除/替换的节点根本不呈现。归属下发（<c>AssignOwner</c>）
/// 只能为新节点补归属，识别不了「结构本身变了」，因此必须由集合自身来记账。</para>
///
/// <para>仍支持集合初始化器（<c>Children = { a, b }</c>，仓库里 100+ 处这样构造 DOM），
/// 因为本类型提供了公开的 <see cref="Add"/> 且实现了 <see cref="IEnumerable{T}"/>。</para>
///
/// <para><b>单父节点约束（ISSUE-154 BUG-03）。</b>一个元素任何时刻只在一个集合里出现一次，
/// 所有写入入口（<see cref="Add"/>、<see cref="Insert"/>、索引器、<see cref="AddRange"/>，以及
/// <see cref="Element.AddChild"/>）遵循同一套 DOM 规则：</para>
/// <list type="bullet">
/// <item>已有父节点的元素被<b>移动</b>过来：先从旧父节点摘下（旧父节点、旧引擎记账），再接入这里——
/// 对应 DOM 的 <c>appendChild</c>/<c>insertBefore</c>/<c>replaceChild</c>。此前这些入口只改写
/// <see cref="Element.Parent"/>，旧父节点的集合里仍留着它：同一元素同时出现在两棵树里，旧引擎
/// 的布局缓存也不知道自己的树变了。</item>
/// <item>已在本集合中的元素同样是移动（不会出现两份）。</item>
/// <item>拒绝 <c>null</c>、宿主自身及其祖先（会成环）。校验全部先于任何改动完成，
/// 失败的操作不改变任何集合、父引用或归属。</item>
/// </list>
/// </summary>
public sealed class ElementCollection : IList<Element>, IReadOnlyList<Element>
{
    private readonly List<Element> _items = new();
    private readonly Element _owner;

    internal ElementCollection(Element owner) => _owner = owner;

    /// <summary>把 <paramref name="child"/> 接入宿主元素：设置父引用并继承（或清除）引擎归属。</summary>
    private void Attach(Element child)
    {
        child.SetParent(_owner);
        // 宿主还没有归属时清掉子树原有的归属：它若来自另一个引擎的树，留着旧归属会让它此后的
        // 变更继续记到旧引擎头上。
        if (_owner.Owner != null) child.AssignOwner(_owner.Owner);
        else child.ClearOwner();
    }

    /// <summary>
    /// 把 <paramref name="child"/> 从宿主元素上摘下：清除父引用与引擎归属。
    ///
    /// <para>清除归属与 <see cref="Element.RemoveChild"/> 保持一致：已脱离 DOM 的元素继续被
    /// 修改时不该再递增旧引擎的版本号，否则会产生无意义的重排工作，也违背「未挂入引擎的
    /// 游离元素不记账」的归属语义。缺了这一步，经集合移除（而非 <c>RemoveChild</c>）的元素
    /// 就会绕过该语义——同一个缺陷的另一扇门。</para>
    /// </summary>
    private static void Detach(Element child)
    {
        child.ClearParent();
        child.ClearOwner();
    }

    /// <summary>
    /// 记账一次结构变更。子节点集合是布局输入，任何增删改都必须使布局缓存失效。
    /// 必须在 <see cref="Detach"/> <b>之前</b>调用：记账要落在<b>旧</b>引擎上，它的缓存才会失效。
    /// </summary>
    private void OnStructureChanged()
    {
        _owner.MarkDirty();
        _owner.BumpMutationVersion();
    }

    /// <summary>
    /// 校验 <paramref name="item"/> 可以成为宿主的子节点：非 null，且不是宿主自身或宿主的祖先。
    /// 只读不写，任何改动之前调用。
    /// </summary>
    private void Validate(Element? item, string paramName)
    {
        ArgumentNullException.ThrowIfNull(item, paramName);
        for (var ancestor = _owner; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ReferenceEquals(ancestor, item))
            {
                throw new InvalidOperationException(ReferenceEquals(item, _owner)
                    ? $"Cannot add the <{item.TagName}> element as a child of itself."
                    : $"Cannot add the <{item.TagName}> element under its own descendant <{_owner.TagName}>: the tree would contain a cycle.");
            }
        }
    }

    /// <summary>
    /// 把已有父节点的 <paramref name="item"/> 从别的父节点上摘下（移动语义的前半步）。
    /// 旧父节点的结构变更记在旧父节点（及其引擎）头上，随后清除子树归属。
    /// 父节点就是本宿主时不处理——同集合内的移动由调用方就地完成，只记账一次。
    /// </summary>
    private void DetachFromOtherParent(Element item)
    {
        if (item.Parent is { } parent && !ReferenceEquals(parent, _owner))
            parent.Children.Remove(item);
    }

    public Element this[int index]
    {
        get => _items[index];
        set
        {
            Validate(value, nameof(value));
            var replaced = _items[index];
            if (ReferenceEquals(replaced, value)) return;

            // replaceChild：新节点若已在本集合的别处，先把它从那里移走，被替换项的位置随之前移。
            var existing = IndexOf(value);
            if (existing >= 0)
            {
                _items.RemoveAt(existing);
                if (existing < index) index--;
            }
            else
            {
                DetachFromOtherParent(value);
            }

            _items[index] = value;
            Attach(value);
            OnStructureChanged();
            // 被替换掉的旧子树同样脱离了 DOM，按移除处理。
            Detach(replaced);
        }
    }

    public int Count => _items.Count;

    public bool IsReadOnly => false;

    /// <summary>追加到末尾（<c>appendChild</c>）。已在本集合中的元素移到末尾，已有别的父节点的元素被移过来。</summary>
    public void Add(Element item)
    {
        Validate(item, nameof(item));
        AddCore(item);
    }

    private void AddCore(Element item)
    {
        if (ReferenceEquals(item.Parent, _owner))
            _items.Remove(item);
        else
            DetachFromOtherParent(item);

        _items.Add(item);
        Attach(item);
        OnStructureChanged();
    }

    /// <summary>
    /// 依次追加。先对全部元素完成校验再开始改动：任何一项非法（null、会成环）时整个操作不生效。
    /// <paramref name="items"/> 先被快照，因此可以是另一个元素的 <see cref="Element.Children"/>
    /// （其中的元素被逐个移走）乃至本集合自身。
    /// </summary>
    public void AddRange(IEnumerable<Element> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var snapshot = items.ToArray();
        foreach (var item in snapshot) Validate(item, nameof(items));
        foreach (var item in snapshot) AddCore(item);
    }

    /// <summary>
    /// 插入到 <paramref name="index"/> 处现有子节点之前（<c>insertBefore</c>）；
    /// <paramref name="index"/> 等于 <see cref="Count"/> 时追加到末尾。已在本集合中的元素被移到该位置。
    /// </summary>
    public void Insert(int index, Element item)
    {
        Validate(item, nameof(item));
        if ((uint)index > (uint)_items.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (ReferenceEquals(item.Parent, _owner))
        {
            var existing = IndexOf(item);
            if (existing == index || existing == index - 1) return; // 已在目标位置
            _items.RemoveAt(existing);
            if (existing < index) index--;
        }
        else
        {
            DetachFromOtherParent(item);
        }

        _items.Insert(index, item);
        Attach(item);
        OnStructureChanged();
    }

    public bool Remove(Element item)
    {
        var index = IndexOf(item);
        if (index < 0) return false;
        _items.RemoveAt(index);
        OnStructureChanged();
        Detach(item);
        return true;
    }

    public void RemoveAt(int index)
    {
        var removed = _items[index];
        _items.RemoveAt(index);
        OnStructureChanged();
        Detach(removed);
    }

    public int RemoveAll(Predicate<Element> match)
    {
        ArgumentNullException.ThrowIfNull(match);

        // 谓词只求值一次：先收集，再按引用移除。谓词中途抛出时集合原封不动；第二次求值
        // 结果不同（有状态的谓词）也不会让「移除的」与「被摘下归属的」对不上。
        HashSet<Element>? detached = null;
        foreach (var item in _items)
        {
            if (match(item)) (detached ??= new HashSet<Element>(ReferenceEqualityComparer.Instance)).Add(item);
        }
        if (detached is null) return 0;

        int removed = _items.RemoveAll(detached.Contains);
        OnStructureChanged();
        foreach (var item in detached) Detach(item);
        return removed;
    }

    public void Clear()
    {
        if (_items.Count == 0) return;
        var detached = _items.ToArray();
        _items.Clear();
        OnStructureChanged();
        foreach (var item in detached) Detach(item);
    }

    /// <summary>
    /// 按引用查找。单父节点约束下，一个元素属于本集合当且仅当它的父节点是宿主，因此不在本集合中
    /// 的元素无需扫描。
    /// </summary>
    public bool Contains(Element item) => item is not null && ReferenceEquals(item.Parent, _owner) && IndexOf(item) >= 0;

    public int IndexOf(Element item)
    {
        if (item is null) return -1;
        for (int i = 0; i < _items.Count; i++)
        {
            if (ReferenceEquals(_items[i], item)) return i;
        }
        return -1;
    }

    public void CopyTo(Element[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    /// <summary>按索引枚举的结构体枚举器，避免每帧整树遍历产生装箱分配（ISSUE-096 的热路径）。</summary>
    public List<Element>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<Element> IEnumerable<Element>.GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// 不记账的原始写入，仅供 <see cref="Element"/> 内部在<b>已自行完成</b>父引用维护、
    /// 归属下发与记账的路径上使用（<see cref="Element.TextContent"/> 的文本节点重建），
    /// 避免重复递增版本号。调用方保证 <paramref name="item"/> 是新建的游离节点。
    /// </summary>
    internal void InsertInternal(int index, Element item) => _items.Insert(index, item);

    /// <summary>见 <see cref="InsertInternal"/>。</summary>
    internal int RemoveAllInternal(Predicate<Element> match) => _items.RemoveAll(match);
}
