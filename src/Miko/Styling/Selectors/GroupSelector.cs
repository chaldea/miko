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

    /// <summary>各分支选择器。返回内部列表视图，读取不分配（ISSUE-113）。</summary>
    public IReadOnlyList<Selector> Selectors => _selectors;

    public override bool Matches(Element element)
    {
        // 索引循环而非 LINQ Any(lambda)：样式解析热路径，避免每次调用分配枚举器（ISSUE-113）。
        for (int i = 0; i < _selectors.Count; i++)
        {
            if (_selectors[i].Matches(element)) return true;
        }
        return false;
    }

    public override int Specificity
    {
        get
        {
            int max = 0;
            for (int i = 0; i < _selectors.Count; i++)
            {
                int s = _selectors[i].Specificity;
                if (s > max) max = s;
            }
            return max;
        }
    }
}
