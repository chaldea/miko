using Miko.Common;
using Miko.Core;

namespace Miko.Rendering;

/// <summary>
/// 脏区域管理器
/// </summary>
public class DirtyRegionManager
{
    private readonly List<RectF> _dirtyRegions = new();

    /// <summary>
    /// 标记元素为脏
    /// </summary>
    public void MarkDirty(Element element)
    {
        if (element.LayoutBox != null)
        {
            var rect = element.LayoutBox.BoxModel.BorderBox;
            AddDirtyRegion(rect);
            element.IsDirty = true;
        }
    }

    /// <summary>
    /// 添加脏区域
    /// </summary>
    private void AddDirtyRegion(RectF rect)
    {
        // 合并相邻或重叠的脏区域以优化性能
        bool merged = false;

        for (int i = 0; i < _dirtyRegions.Count; i++)
        {
            if (_dirtyRegions[i].IntersectsWith(rect) || IsAdjacent(_dirtyRegions[i], rect))
            {
                _dirtyRegions[i] = RectF.Union(_dirtyRegions[i], rect);
                merged = true;
                break;
            }
        }

        if (!merged)
        {
            _dirtyRegions.Add(rect);
        }
    }

    /// <summary>
    /// 检查两个矩形是否相邻
    /// </summary>
    private bool IsAdjacent(RectF a, RectF b)
    {
        const float threshold = 1.0f;

        // 水平相邻
        bool horizontalAdjacent = Math.Abs(a.Right - b.Left) < threshold ||
                                  Math.Abs(b.Right - a.Left) < threshold;

        // 垂直相邻
        bool verticalAdjacent = Math.Abs(a.Bottom - b.Top) < threshold ||
                                Math.Abs(b.Bottom - a.Top) < threshold;

        // 检查是否在同一行或同一列
        bool sameRow = !(a.Bottom < b.Top || b.Bottom < a.Top);
        bool sameColumn = !(a.Right < b.Left || b.Right < a.Left);

        return (horizontalAdjacent && sameRow) || (verticalAdjacent && sameColumn);
    }

    /// <summary>
    /// 获取所有脏区域并清空
    /// </summary>
    public List<RectF> GetDirtyRegions()
    {
        var regions = new List<RectF>(_dirtyRegions);
        _dirtyRegions.Clear();
        return regions;
    }

    /// <summary>
    /// 检查是否有脏区域
    /// </summary>
    public bool HasDirtyRegions() => _dirtyRegions.Count > 0;

    /// <summary>
    /// 清空所有脏区域
    /// </summary>
    public void Clear() => _dirtyRegions.Clear();
}
