using Miko.Common;

namespace Miko.Styling;

/// <summary>物理边（top/right/bottom/left）。</summary>
internal enum PhysicalEdge { Top, Right, Bottom, Left }

/// <summary>逻辑边（inline/block 轴 × start/end）。</summary>
internal enum LogicalEdge { InlineStart, InlineEnd, BlockStart, BlockEnd }

/// <summary>
/// 逻辑边与物理边的映射（CSS Writing Modes，见 ISSUE-103）。
/// inline 轴的映射同时受 <see cref="WritingMode"/> 与 <see cref="Direction"/> 影响；
/// block 轴仅受 <see cref="WritingMode"/> 影响（块方向不受 direction 翻转）。
/// <list type="bullet">
/// <item>horizontal-tb：inline 轴水平（ltr 时 start=left），block 轴垂直向下（start=top）。</item>
/// <item>vertical-rl：inline 轴垂直（ltr 时 start=top），block 轴水平自右而左（start=right）。</item>
/// <item>vertical-lr：inline 轴垂直（ltr 时 start=top），block 轴水平自左而右（start=left）。</item>
/// </list>
/// </summary>
internal static class LogicalEdgeMap
{
    /// <summary>将逻辑边映射为当前书写模式/方向下的物理边。</summary>
    public static PhysicalEdge ToPhysical(LogicalEdge edge, WritingMode writingMode, Direction direction) => edge switch
    {
        LogicalEdge.InlineStart => writingMode == WritingMode.HorizontalTb
            ? (direction == Direction.Ltr ? PhysicalEdge.Left : PhysicalEdge.Right)
            : (direction == Direction.Ltr ? PhysicalEdge.Top : PhysicalEdge.Bottom),
        LogicalEdge.InlineEnd => writingMode == WritingMode.HorizontalTb
            ? (direction == Direction.Ltr ? PhysicalEdge.Right : PhysicalEdge.Left)
            : (direction == Direction.Ltr ? PhysicalEdge.Bottom : PhysicalEdge.Top),
        LogicalEdge.BlockStart => writingMode switch
        {
            WritingMode.HorizontalTb => PhysicalEdge.Top,
            WritingMode.VerticalRl => PhysicalEdge.Right,
            _ => PhysicalEdge.Left,
        },
        _ => writingMode switch
        {
            WritingMode.HorizontalTb => PhysicalEdge.Bottom,
            WritingMode.VerticalRl => PhysicalEdge.Left,
            _ => PhysicalEdge.Right,
        },
    };

    /// <summary>将物理边映射为当前书写模式/方向下的逻辑边（<see cref="ToPhysical"/> 的逆映射）。</summary>
    public static LogicalEdge ToLogical(PhysicalEdge edge, WritingMode writingMode, Direction direction) => edge switch
    {
        PhysicalEdge.Top => writingMode == WritingMode.HorizontalTb
            ? LogicalEdge.BlockStart
            : (direction == Direction.Ltr ? LogicalEdge.InlineStart : LogicalEdge.InlineEnd),
        PhysicalEdge.Bottom => writingMode == WritingMode.HorizontalTb
            ? LogicalEdge.BlockEnd
            : (direction == Direction.Ltr ? LogicalEdge.InlineEnd : LogicalEdge.InlineStart),
        PhysicalEdge.Left => writingMode switch
        {
            WritingMode.HorizontalTb => direction == Direction.Ltr ? LogicalEdge.InlineStart : LogicalEdge.InlineEnd,
            WritingMode.VerticalRl => LogicalEdge.BlockEnd,
            _ => LogicalEdge.BlockStart,
        },
        _ => writingMode switch
        {
            WritingMode.HorizontalTb => direction == Direction.Ltr ? LogicalEdge.InlineEnd : LogicalEdge.InlineStart,
            WritingMode.VerticalRl => LogicalEdge.BlockStart,
            _ => LogicalEdge.BlockEnd,
        },
    };
}
