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
/// </summary>
public sealed class ElementCollection : IList<Element>, IReadOnlyList<Element>
{
    private readonly List<Element> _items = new();
    private readonly Element _owner;

    internal ElementCollection(Element owner) => _owner = owner;

    /// <summary>把 <paramref name="child"/> 接入宿主元素：设置父引用并继承引擎归属。</summary>
    private void Attach(Element child)
    {
        child.SetParent(_owner);
        if (_owner.Owner != null) child.AssignOwner(_owner.Owner);
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

    public Element this[int index]
    {
        get => _items[index];
        set
        {
            // 被替换掉的旧子树同样脱离了 DOM，按移除处理。
            var replaced = _items[index];
            _items[index] = value;
            Attach(value);
            OnStructureChanged();
            if (!ReferenceEquals(replaced, value)) Detach(replaced);
        }
    }

    public int Count => _items.Count;

    public bool IsReadOnly => false;

    public void Add(Element item)
    {
        _items.Add(item);
        Attach(item);
        OnStructureChanged();
    }

    public void AddRange(IEnumerable<Element> items)
    {
        foreach (var item in items) Add(item);
    }

    public void Insert(int index, Element item)
    {
        _items.Insert(index, item);
        Attach(item);
        OnStructureChanged();
    }

    public bool Remove(Element item)
    {
        if (!_items.Remove(item)) return false;
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
        // 先收集将被移除的项：RemoveAll 之后就无从得知移除了谁。
        List<Element>? detached = null;
        foreach (var item in _items)
        {
            if (match(item)) (detached ??= new List<Element>()).Add(item);
        }

        int removed = _items.RemoveAll(match);
        if (removed > 0)
        {
            OnStructureChanged();
            if (detached != null)
            {
                foreach (var item in detached) Detach(item);
            }
        }
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

    public bool Contains(Element item) => _items.Contains(item);

    public int IndexOf(Element item) => _items.IndexOf(item);

    public void CopyTo(Element[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    /// <summary>按索引枚举的结构体枚举器，避免每帧整树遍历产生装箱分配（ISSUE-096 的热路径）。</summary>
    public List<Element>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<Element> IEnumerable<Element>.GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// 不记账的原始写入，仅供 <see cref="Element"/> 内部在<b>已自行完成</b>父引用维护、
    /// 归属下发与记账的路径上使用（<see cref="Element.AddChild"/> / <see cref="Element.RemoveChild"/>），
    /// 避免重复递增版本号。
    /// </summary>
    internal void AddInternal(Element item) => _items.Add(item);

    /// <summary>见 <see cref="AddInternal"/>。</summary>
    internal bool RemoveInternal(Element item) => _items.Remove(item);

    /// <summary>见 <see cref="AddInternal"/>。</summary>
    internal void InsertInternal(int index, Element item) => _items.Insert(index, item);

    /// <summary>见 <see cref="AddInternal"/>。</summary>
    internal int RemoveAllInternal(Predicate<Element> match) => _items.RemoveAll(match);
}
