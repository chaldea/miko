using Miko.Core;
using Miko.Styling.Selectors;

namespace Miko.Styling;

/// <summary>
/// 规则索引（ISSUE-113）：按选择器最右侧复合选择器的「关键」简单选择器把规则分桶，
/// 使样式解析时每个元素只测试**有可能命中**的规则，而非整表逐条测试。
///
/// <para>这是浏览器样式引擎的标准做法。以 Ionic 样式表为例：1868 条规则 × 287 个元素
/// = 53 万次选择器求值，其中绝大多数在第一个类名比较就失败。按关键类名分桶后，
/// 每个元素通常只需测试个位数条候选规则。</para>
///
/// <para><b>正确性</b>：分桶只做「快速否定」——被分到 <c>.foo</c> 桶的规则，其最右侧复合
/// 选择器必然要求元素含类名 <c>foo</c>，故不含该类名的元素绝无可能匹配，跳过它们不会
/// 丢失任何匹配。无法提取关键选择器的规则（如纯伪类、通配、分组等）一律进入
/// <see cref="_universal"/> 桶，对每个元素都会测试，因此判定保守、只多不漏。
/// 最终是否匹配仍由完整选择器的 <see cref="Selector.Matches"/> 决定，级联结果与未索引时完全一致。</para>
/// </summary>
internal sealed class RuleIndex
{
    /// <summary>索引中的一条规则：规则本身 + 它在样式表中的全局定义序号（级联排序键）。</summary>
    internal readonly struct Entry(StyleRule rule, int order)
    {
        public readonly StyleRule Rule = rule;
        public readonly int Order = order;
    }

    private readonly Dictionary<string, List<Entry>> _byClass = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Entry>> _byId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Entry>> _byTag = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Entry> _universal = [];

    /// <summary>把一条规则加入索引。<paramref name="order"/> 为其全局定义序号。</summary>
    public void Add(StyleRule rule, int order)
    {
        var entry = new Entry(rule, order);

        // 分组选择器（A, B）的各分支可能有不同的关键选择器，需逐分支入桶；
        // 任一分支无法提取关键选择器时，整条规则退回通用桶（保守）。
        //
        // 必须先整体判定、再统一入桶：若边判定边入桶，一条形如 ".a, :hover" 的规则会先被
        // 写入 .a 桶，随后 :hover 分支判定失败又被写入通用桶——于是含 .a 的元素会拿到同一条
        // 规则两次。级联结果本身不受影响（Merge 是空值合并且排序键相同），但会平白多出一次
        // 选择器求值与一个排序项，与本次优化的目的相悖。
        if (rule.Selector is GroupSelector group)
        {
            var branches = group.Selectors;
            for (int i = 0; i < branches.Count; i++)
            {
                if (!CanBucket(branches[i]))
                {
                    _universal.Add(entry);
                    return;
                }
            }
            for (int i = 0; i < branches.Count; i++)
                TryBucket(branches[i], entry);
            return;
        }

        if (!TryBucket(rule.Selector, entry))
            _universal.Add(entry);
    }

    /// <summary>该选择器能否提取到关键简单选择器（即能否入桶），不产生任何写入。</summary>
    private static bool CanBucket(Selector selector)
    {
        var key = RightmostCompound(selector);
        return TryGetKey(key, out _, KeyKind.Id)
            || TryGetKey(key, out _, KeyKind.Class)
            || TryGetKey(key, out _, KeyKind.Tag);
    }

    /// <summary>
    /// 把 <paramref name="selector"/> 按其关键简单选择器放入对应桶。
    /// 返回 false 表示无法提取关键选择器，调用方应放入通用桶。
    /// </summary>
    private bool TryBucket(Selector selector, in Entry entry)
    {
        // 组合器选择器的匹配由最右侧（目标）一段决定，祖先/兄弟段只是附加约束。
        var key = RightmostCompound(selector);

        // 关键选择器优先级：ID > Class > Tag（选择性由强到弱）。
        if (TryGetKey(key, out string? id, KeyKind.Id))
        {
            Bucket(_byId, id!, entry);
            return true;
        }
        if (TryGetKey(key, out string? cls, KeyKind.Class))
        {
            Bucket(_byClass, cls!, entry);
            return true;
        }
        if (TryGetKey(key, out string? tag, KeyKind.Tag))
        {
            Bucket(_byTag, tag!, entry);
            return true;
        }
        return false;
    }

    /// <summary>取选择器最右侧的复合/简单选择器（即实际作用于候选元素的那一段）。</summary>
    private static Selector RightmostCompound(Selector selector) => selector switch
    {
        DescendantSelector d => RightmostCompound(d.Descendant),
        ChildSelector c => RightmostCompound(c.Child),
        AdjacentSiblingSelector a => RightmostCompound(a.Target),
        GeneralSiblingSelector g => RightmostCompound(g.Target),
        _ => selector,
    };

    private enum KeyKind { Id, Class, Tag }

    /// <summary>
    /// 从（可能是复合的）选择器中提取指定种类的关键名。
    /// <c>:not()</c> 内部的简单选择器<b>不可</b>作为关键选择器——它表达的是否定条件，
    /// 元素不含该名字反而可能匹配。
    /// </summary>
    private static bool TryGetKey(Selector selector, out string? name, KeyKind kind)
    {
        name = null;

        switch (selector)
        {
            case IdSelector id when kind == KeyKind.Id:
                name = id.Id;
                return true;
            case ClassSelector cls when kind == KeyKind.Class:
                name = cls.ClassName;
                return true;
            case TagSelector tag when kind == KeyKind.Tag:
                name = tag.TagName;
                return true;
            case CompoundSelector compound:
            {
                var parts = compound.Selectors;
                for (int i = 0; i < parts.Count; i++)
                {
                    // 伪类（含 :not）不是 Id/Class/Tag 选择器，会在下方 default 分支返回 false，
                    // 因而自然被跳过——:not 内部的类名绝不会被当作关键选择器（那会把候选集
                    // 变成语义的反面）。此处显式排除 NotSelector，使该意图在代码上可见，
                    // 并防止后续为 NotSelector 增加提取分支时意外破坏索引正确性。
                    if (parts[i] is NotSelector) continue;
                    if (TryGetKey(parts[i], out name, kind)) return true;
                }
                return false;
            }
            default:
                return false;
        }
    }

    private static void Bucket(Dictionary<string, List<Entry>> map, string key, in Entry entry)
    {
        if (!map.TryGetValue(key, out var list))
        {
            list = [];
            map[key] = list;
        }
        list.Add(entry);
    }

    /// <summary>
    /// 收集 <paramref name="element"/> 的候选规则到 <paramref name="sink"/>（追加，不清空）。
    /// 候选集是真实匹配集的超集——调用方仍须用完整选择器逐条 <see cref="Selector.Matches"/> 判定。
    /// <para>同一条规则至多出现一次：分组选择器（如 <c>.a, .b</c>）的多个分支可能落入不同的桶，
    /// 而元素可能同时命中其中数个（<c>class="a b"</c>），若不去重该规则会被重复求值与重复排序。</para>
    /// </summary>
    public void CollectCandidates(Element element, List<Entry> sink)
    {
        // 通用桶：无法按名字快速否定的规则，每个元素都要测。
        sink.AddRange(_universal);

        if (_byTag.Count > 0 && _byTag.TryGetValue(element.TagName, out var byTag))
            sink.AddRange(byTag);

        if (_byId.Count > 0 && element.Id is { Length: > 0 } elementId
            && _byId.TryGetValue(elementId, out var byId))
            sink.AddRange(byId);

        // 类名桶是唯一可能让同一条规则重复入选的来源（分组选择器的多个类分支），
        // 故只对这一段做去重，避免为常见情形付出额外开销。
        int beforeClasses = sink.Count;
        if (_byClass.Count > 0)
            CollectClassCandidates(element.Class, sink);
        DeduplicateFrom(sink, beforeClasses);
    }

    /// <summary>
    /// 就地去除 <paramref name="sink"/> 中从 <paramref name="start"/> 起的重复规则条目
    /// （按规则对象身份判重）。类名桶通常只有个位数条目，线性判重比建哈希集更划算，且零分配。
    /// </summary>
    private static void DeduplicateFrom(List<Entry> sink, int start)
    {
        for (int i = start; i < sink.Count; i++)
        {
            bool duplicate = false;
            for (int j = start; j < i; j++)
            {
                if (ReferenceEquals(sink[j].Rule, sink[i].Rule)) { duplicate = true; break; }
            }
            if (duplicate)
            {
                sink.RemoveAt(i);
                i--;
            }
        }
    }

    /// <summary>按元素 class 串中的每个 token 取对应桶（分词零分配，见 Element.HasClass）。</summary>
    private void CollectClassCandidates(string? classList, List<Entry> sink)
    {
        if (string.IsNullOrEmpty(classList)) return;

        ReadOnlySpan<char> remaining = classList.AsSpan();
        while (!remaining.IsEmpty)
        {
            int start = 0;
            while (start < remaining.Length && char.IsWhiteSpace(remaining[start])) start++;
            if (start >= remaining.Length) break;
            remaining = remaining[start..];

            int end = 0;
            while (end < remaining.Length && !char.IsWhiteSpace(remaining[end])) end++;

            // .NET 的 Dictionary 支持以 ReadOnlySpan<char> 作为查找键（无需实体化子串）。
            if (_byClass.GetAlternateLookup<ReadOnlySpan<char>>()
                    .TryGetValue(remaining[..end], out var bucket))
            {
                sink.AddRange(bucket);
            }

            remaining = remaining[end..];
        }
    }
}
