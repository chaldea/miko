using Miko.Common;

namespace Miko.Layout.LayoutAlgorithms;

/// <summary>
/// 布局调度器 - 根据 LayoutBox 的类型选择正确的布局算法
/// </summary>
public static class LayoutDispatcher
{
    private static readonly BlockLayout _blockLayout = new();
    private static readonly InlineLayout _inlineLayout = new();
    private static readonly FlexLayout _flexLayout = new();

    /// <summary>
    /// 根据盒子类型执行相应的布局算法
    /// </summary>
    public static void Dispatch(LayoutBox box, LayoutConstraints constraints, float x, float y)
    {
        switch (box.Type)
        {
            case LayoutType.Block:
                _blockLayout.Layout(box, constraints, x, y);
                break;

            case LayoutType.Inline:
            case LayoutType.InlineBlock:
                _inlineLayout.Layout(box, constraints, x, y);
                break;

            case LayoutType.Flex:
                _flexLayout.Layout(box, constraints, x, y);
                break;
        }
    }
}
