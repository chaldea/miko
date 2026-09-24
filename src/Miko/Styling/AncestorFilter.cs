using Miko.Core;
using Miko.Styling.Selectors;

namespace Miko.Styling;

/// <summary>
/// 祖先过滤器（ISSUE-146）：样式解析时，用当前元素<b>全部祖先</b>的类名/ID/标签构成一个
/// 计数布隆过滤器，在逐条测试候选规则之前，先否决那些「要求某个祖先带有某类名，而祖先链上
/// 根本没有这个类名」的后代/子代组合器规则。这是浏览器样式引擎的标准做法。
///
/// <para><b>为什么需要</b>：组件库样式表里绝大多数组合器规则长这样
/// <c>.ion-button.md.button-clear.ion-color-primary .button-native</c>。
/// <see cref="RuleIndex"/> 按最右侧的 <c>.button-native</c> 分桶，于是每个
/// <c>.button-native</c> 元素都要测试整个桶的上百条规则，而每条失败的规则都要把祖先链
/// 一直走到根。在 Anime 的详情页上它们占了匹配耗时的 89%，命中率却不到 5%。</para>
///
/// <para><b>正确性</b>：只做快速否决，且只否决必然失配的规则。<see cref="RequiredAncestorKeys"/>
/// 收集的每个键都满足「选择器一旦匹配元素 e，e 的某个真祖先必然带有该键」，因此键不在过滤器
/// 中即证明不匹配。布隆过滤器只会误报（「可能在」），不会漏报，误报的代价只是照常完整测试。
/// 否定（<c>:not</c>）、兄弟组合器的左侧、分组、自定义选择器一律不贡献键——保守。</para>
///
/// <para><b>契约</b>：<see cref="Push"/>/<see cref="Pop"/> 必须严格随 DOM 树递归成对调用，
/// 且顺序与 <see cref="Element.Parent"/> 链一致。使用方在否决前以 <see cref="Top"/> 与
/// <c>element.Parent</c> 做身份核对，过滤器状态与被测元素不对应时不做任何否决。</para>
/// </summary>
internal sealed class AncestorFilter
{
    private const int SlotBits = 12;
    private const int SlotMask = (1 << SlotBits) - 1;

    // 计数而非位图：出栈时只需把入栈时加上的计数减回去，无需整表重建。
    private readonly ushort[] _counts = new ushort[1 << SlotBits];
    private readonly List<Element> _chain = new();
    private readonly List<int> _hashes = new();
    private readonly List<int> _frameStarts = new();

    // 不同种类的键用不同的初值，避免 class="x" 与 id="x" 互相冒充。
    private const uint ClassSeed = 2166136261u;
    private const uint IdSeed = 2166136261u ^ 0x9E3779B9u;
    private const uint TagSeed = 2166136261u ^ 0x7F4A7C15u;

    /// <summary>最近入栈的元素（即当前被解析元素应有的父元素）；空栈为 null。</summary>
    public Element? Top => _chain.Count > 0 ? _chain[^1] : null;

    /// <summary>清空（每次样式阶段开始时调用，防止上一次异常中断留下的残留）。</summary>
    public void Clear()
    {
        Array.Clear(_counts);
        _chain.Clear();
        _hashes.Clear();
        _frameStarts.Clear();
    }

    /// <summary>把 <paramref name="element"/> 作为后续元素的祖先入栈。</summary>
    public void Push(Element element)
    {
        _frameStarts.Add(_hashes.Count);
        _chain.Add(element);

        Add(HashTag(element.TagName));
        if (!string.IsNullOrEmpty(element.Id)) Add(HashId(element.Id));

        var classList = element.Class;
        if (!string.IsNullOrEmpty(classList))
        {
            ReadOnlySpan<char> remaining = classList;
            while (!remaining.IsEmpty)
            {
                int start = 0;
                while (start < remaining.Length && char.IsWhiteSpace(remaining[start])) start++;
                if (start >= remaining.Length) break;
                remaining = remaining[start..];

                int end = 0;
                while (end < remaining.Length && !char.IsWhiteSpace(remaining[end])) end++;
                Add(HashClass(remaining[..end]));
                remaining = remaining[end..];
            }
        }
    }

    /// <summary>弹出最近一次 <see cref="Push"/> 的元素。</summary>
    public void Pop()
    {
        int start = _frameStarts[^1];
        _frameStarts.RemoveAt(_frameStarts.Count - 1);
        _chain.RemoveAt(_chain.Count - 1);
        for (int i = _hashes.Count - 1; i >= start; i--)
        {
            int hash = _hashes[i];
            _counts[hash & SlotMask]--;
            _counts[(hash >> 16) & SlotMask]--;
        }
        _hashes.RemoveRange(start, _hashes.Count - start);
    }

    /// <summary>
    /// <paramref name="keys"/> 中的每个键是否都<b>可能</b>出现在祖先链上。
    /// 返回 false 即证明有键缺席——需要它的规则必然不匹配。
    /// </summary>
    public bool MayContainAll(int[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            int hash = keys[i];
            if (_counts[hash & SlotMask] == 0 || _counts[(hash >> 16) & SlotMask] == 0) return false;
        }
        return true;
    }

    private void Add(int hash)
    {
        _hashes.Add(hash);
        _counts[hash & SlotMask]++;
        _counts[(hash >> 16) & SlotMask]++;
    }

    /// <summary>
    /// 收集 <paramref name="selector"/> 匹配某元素时，该元素的真祖先上<b>必然</b>存在的键。
    /// 无可收集的键时返回 null（规则不参与过滤）。
    /// </summary>
    public static int[]? RequiredAncestorKeys(Selector selector)
    {
        List<int>? keys = null;
        CollectFromTarget(selector, ref keys);
        return keys?.Distinct().ToArray();
    }

    // selector 作用于被测元素本身：只有其组合器左侧（祖先一侧）才贡献键。
    private static void CollectFromTarget(Selector selector, ref List<int>? keys)
    {
        switch (selector)
        {
            case DescendantSelector d:
                CollectFromAncestor(d.Ancestor, ref keys);
                CollectFromTarget(d.Descendant, ref keys);
                break;
            case ChildSelector c:
                CollectFromAncestor(c.Parent, ref keys);
                CollectFromTarget(c.Child, ref keys);
                break;
            // 兄弟组合器的左侧匹配的是兄弟而非祖先，跳过；右侧仍作用于被测元素。
            case AdjacentSiblingSelector a:
                CollectFromTarget(a.Target, ref keys);
                break;
            case GeneralSiblingSelector g:
                CollectFromTarget(g.Target, ref keys);
                break;
        }
    }

    // selector 必须匹配被测元素的某个真祖先 p：它自身的键在 p 上，它左侧的祖先键在 p 的祖先上
    // ——都是被测元素的真祖先。
    private static void CollectFromAncestor(Selector selector, ref List<int>? keys)
    {
        switch (selector)
        {
            case DescendantSelector d:
                CollectFromAncestor(d.Ancestor, ref keys);
                CollectFromAncestor(d.Descendant, ref keys);
                break;
            case ChildSelector c:
                CollectFromAncestor(c.Parent, ref keys);
                CollectFromAncestor(c.Child, ref keys);
                break;
            case AdjacentSiblingSelector a:
                CollectFromAncestor(a.Target, ref keys);
                break;
            case GeneralSiblingSelector g:
                CollectFromAncestor(g.Target, ref keys);
                break;
            case CompoundSelector compound:
                var parts = compound.Selectors;
                for (int i = 0; i < parts.Count; i++)
                    CollectSimple(parts[i], ref keys);
                break;
            default:
                CollectSimple(selector, ref keys);
                break;
        }
    }

    // 只认三种能确定「元素必须带有」的简单选择器；:not 等其余一律不贡献（保守）。
    // 嵌套的复合选择器（Ionic 的作用域改写会产生 Compound(Compound(...), .scope)）逐层展开：
    // 复合选择器的每一部分都必须作用于同一个元素。
    private static void CollectSimple(Selector selector, ref List<int>? keys)
    {
        if (selector is CompoundSelector nested)
        {
            var parts = nested.Selectors;
            for (int i = 0; i < parts.Count; i++)
                CollectSimple(parts[i], ref keys);
            return;
        }

        int? hash = selector switch
        {
            ClassSelector cls when cls.ClassName.Length > 0 && !HasWhiteSpace(cls.ClassName) => HashClass(cls.ClassName),
            IdSelector id when id.Id.Length > 0 => HashId(id.Id),
            TagSelector tag when tag.TagName.Length > 0 => HashTag(tag.TagName),
            _ => null,
        };
        if (hash is { } value) (keys ??= new List<int>()).Add(value);
    }

    // 含空白的类名永远匹配不到（见 Element.ContainsClassToken）；不为它产生键，避免哈希分词不一致。
    private static bool HasWhiteSpace(string value)
    {
        foreach (var c in value)
        {
            if (char.IsWhiteSpace(c)) return true;
        }
        return false;
    }

    internal static int HashClass(ReadOnlySpan<char> name) => Finish(Fnv(ClassSeed, name, foldCase: false));

    internal static int HashId(ReadOnlySpan<char> id) => Finish(Fnv(IdSeed, id, foldCase: false));

    // 标签按忽略大小写比较（见 TagSelector 的 OrdinalIgnoreCase），哈希前同样折叠大小写。
    // 用大写折叠：OrdinalIgnoreCase 的定义就是按不变区域的大写映射比较，两者逐字符一致。
    internal static int HashTag(ReadOnlySpan<char> tag) => Finish(Fnv(TagSeed, tag, foldCase: true));

    private static uint Fnv(uint hash, ReadOnlySpan<char> text, bool foldCase)
    {
        foreach (var raw in text)
        {
            var c = foldCase ? char.ToUpperInvariant(raw) : raw;
            hash = (hash ^ c) * 16777619u;
        }
        return hash;
    }

    // murmur3 的收尾混合：让高低两个 16 位都充分依赖全部输入，两个探针位置相互独立。
    private static int Finish(uint hash)
    {
        hash ^= hash >> 16;
        hash *= 0x85EBCA6Bu;
        hash ^= hash >> 13;
        hash *= 0xC2B2AE35u;
        hash ^= hash >> 16;
        return (int)hash;
    }
}
