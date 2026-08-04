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

        // 所有选择器都必须匹配。样式解析热路径（ISSUE-113）：用索引循环而非
        // LINQ All(lambda)，避免每次调用分配闭包与枚举器。
        for (int i = 0; i < _selectors.Count; i++)
        {
            if (!_selectors[i].Matches(element)) return false;
        }
        return true;
    }

    /// <summary>
    /// 特异性是所有组成选择器特异性的总和。
    /// 每条匹配规则的排序键都会读取它，故同样用索引循环避免 LINQ 分配（ISSUE-113）。
    /// </summary>
    public override int Specificity
    {
        get
        {
            int sum = 0;
            for (int i = 0; i < _selectors.Count; i++) sum += _selectors[i].Specificity;
            return sum;
        }
    }
}
