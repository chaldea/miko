using Miko.Core;

namespace Miko.Styling.Selectors;

/// <summary>
/// 复合选择器，组合多个简单选择器（如 button:hover, .class:active）
/// 所有选择器都必须匹配才能使复合选择器匹配
/// </summary>
public class CompoundSelector : Selector
{
    private readonly List<Selector> _selectors = new();

    public IReadOnlyList<Selector> Selectors => _selectors;

    public CompoundSelector(params Selector[] selectors)
    {
        _selectors.AddRange(selectors);
    }

    public CompoundSelector(IEnumerable<Selector> selectors)
    {
        _selectors.AddRange(selectors);
    }

    public void Add(Selector selector)
    {
        _selectors.Add(selector);
    }

    public override bool Matches(Element element)
    {
        if (_selectors.Count == 0) return false;

        // 所有选择器都必须匹配
        return _selectors.All(s => s.Matches(element));
    }

    /// <summary>
    /// 特异性是所有组成选择器特异性的总和
    /// </summary>
    public override int Specificity => _selectors.Sum(s => s.Specificity);
}
