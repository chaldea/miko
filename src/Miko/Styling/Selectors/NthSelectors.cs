using Miko.Core;
using Miko.Core.DomElements;

namespace Miko.Styling.Selectors;

/// <summary>
/// <c>:nth-child()</c> 家族的基类（ISSUE-158）：元素在参与计数的兄弟中的 1 起序号 p 满足
/// p = An + B（n 取非负整数）时匹配。<c>odd</c> 即 2n+1，<c>even</c> 即 2n，纯整数 B 即 0n+B。
///
/// <para>与 <see cref="FirstChildSelector"/> 一致，序号只计元素子节点，文本节点不计入（ISSUE-086）。
/// 每次匹配扫描一遍兄弟列表（与兄弟组合器的 <c>IndexOf</c> 同量级），索引循环、零分配。</para>
///
/// <para>无需额外的失效记账：兄弟的增删、类名与状态变化都按结构变更触发整树级联，
/// 读后续兄弟的 <c>:nth-last-*</c> 也随之重新匹配。</para>
/// </summary>
public abstract class NthSelector : PseudoClassSelector
{
    private readonly bool _fromEnd;

    protected NthSelector(int a, int b, bool fromEnd, Selector? of)
    {
        A = a;
        B = b;
        _fromEnd = fromEnd;
        Of = of;
    }

    /// <summary>步长 A（<c>2n+1</c> 中的 2）。</summary>
    public int A { get; }

    /// <summary>偏移 B（<c>2n+1</c> 中的 1）。</summary>
    public int B { get; }

    /// <summary>
    /// <c>of S</c> 的选择器列表（仅 <c>:nth-child()</c> / <c>:nth-last-child()</c>）：只在匹配 S 的兄弟中
    /// 计数，且元素自身须匹配 S。无则为 null。
    /// </summary>
    public Selector? Of { get; }

    public override bool Matches(Element element)
    {
        if (element is TextNode) return false;
        var siblings = element.Parent?.Children;
        if (siblings == null) return false;
        if (Of != null && !Of.Matches(element)) return false;

        int position = 0;
        int count = siblings.Count;
        for (int k = 0; k < count; k++)
        {
            var sibling = siblings[_fromEnd ? count - 1 - k : k];
            if (sibling is TextNode) continue;
            if (ReferenceEquals(sibling, element)) return IsMatch(A, B, position + 1);
            if (IsCounted(sibling, element)) position++;
        }
        return false;
    }

    /// <summary>兄弟 <paramref name="sibling"/> 是否与 <paramref name="element"/> 一同计数。</summary>
    protected abstract bool IsCounted(Element sibling, Element element);

    /// <summary>
    /// 序号 <paramref name="position"/>（1 起）是否等于某个 n ≥ 0 下的 <paramref name="a"/>n + <paramref name="b"/>。
    /// </summary>
    public static bool IsMatch(int a, int b, int position)
    {
        // long：B 可以是任意整数，position - b 在 int 上可能溢出。
        long diff = (long)position - b;
        if (a == 0) return diff == 0;
        return diff % a == 0 && diff / a >= 0;
    }

    // 带 of S 时为伪类本身加上 S 中最具体的一支（Selectors Level 4）。
    public override int Specificity => base.Specificity + (Of?.Specificity ?? 0);

    public override bool MayReadContentOrInlineStyle => Of?.MayReadContentOrInlineStyle ?? false;
}

/// <summary>
/// <c>:nth-child(An+B [of S])</c> 伪类选择器。<c>:nth-child(2 of .item)</c> 是第二个 <c>.item</c>，
/// 而非恰好排在第二位的 <c>.item</c>。
/// </summary>
public class NthChildSelector : NthSelector
{
    public NthChildSelector(int a, int b, Selector? of = null) : base(a, b, fromEnd: false, of) { }

    protected override bool IsCounted(Element sibling, Element element) => Of == null || Of.Matches(sibling);
}

/// <summary>
/// <c>:nth-last-child(An+B [of S])</c> 伪类选择器：与 <see cref="NthChildSelector"/> 相同，但从最后一个子元素倒数。
/// </summary>
public class NthLastChildSelector : NthSelector
{
    public NthLastChildSelector(int a, int b, Selector? of = null) : base(a, b, fromEnd: true, of) { }

    protected override bool IsCounted(Element sibling, Element element) => Of == null || Of.Matches(sibling);
}

/// <summary>
/// <c>:nth-of-type(An+B)</c> 伪类选择器：只在同标签的兄弟中计数（与 <see cref="FirstOfTypeSelector"/> 同样按标签名比较）。
/// </summary>
public class NthOfTypeSelector : NthSelector
{
    public NthOfTypeSelector(int a, int b) : base(a, b, fromEnd: false, of: null) { }

    protected override bool IsCounted(Element sibling, Element element) => sibling.TagName == element.TagName;
}

/// <summary>
/// <c>:nth-last-of-type(An+B)</c> 伪类选择器：与 <see cref="NthOfTypeSelector"/> 相同，但从最后一个同类兄弟倒数。
/// </summary>
public class NthLastOfTypeSelector : NthSelector
{
    public NthLastOfTypeSelector(int a, int b) : base(a, b, fromEnd: true, of: null) { }

    protected override bool IsCounted(Element sibling, Element element) => sibling.TagName == element.TagName;
}
