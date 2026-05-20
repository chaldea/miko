using Miko.Core;

namespace Miko.Styling.Selectors;

/// <summary>
/// 分组选择器 (A, B, C) - 任一选择器匹配即可
/// </summary>
public class GroupSelector : Selector
{
    private readonly List<Selector> _selectors;

    public GroupSelector(params Selector[] selectors) => _selectors = new(selectors);
    public GroupSelector(IEnumerable<Selector> selectors) => _selectors = new(selectors);

    public Selector[] Selectors => _selectors.ToArray();

    public override bool Matches(Element element) => _selectors.Any(s => s.Matches(element));
    public override int Specificity => _selectors.Max(s => s.Specificity);
}
