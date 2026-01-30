using Miko.Common;
using Miko.Utils;

namespace Miko.Layout.LayoutAlgorithms;

/// <summary>
/// Flexbox 布局算法
/// </summary>
public class FlexLayout
{
    public void Layout(LayoutBox box, LayoutConstraints constraints, float x, float y)
    {
        var style = box.ComputedStyle;

        // 1. 计算 margin, border, padding
        float containerWidth = constraints.AvailableWidth ?? 0;

        box.BoxModel.Margin = new EdgeSizes(
            style.MarginTop.ToPixels(containerWidth),
            style.MarginRight.ToPixels(containerWidth),
            style.MarginBottom.ToPixels(containerWidth),
            style.MarginLeft.ToPixels(containerWidth)
        );

        box.BoxModel.Border = new EdgeSizes(
            style.BorderWidth.ToPixels(containerWidth)
        );

        box.BoxModel.Padding = new EdgeSizes(
            style.PaddingTop.ToPixels(containerWidth),
            style.PaddingRight.ToPixels(containerWidth),
            style.PaddingBottom.ToPixels(containerWidth),
            style.PaddingLeft.ToPixels(containerWidth)
        );

        // 2. 计算容器宽度
        float contentWidth;
        if (!style.Width.IsAuto)
        {
            contentWidth = style.Width.ToPixels(containerWidth);
        }
        else if (constraints.IsInfiniteWidth || containerWidth <= 0)
        {
            // 当没有可用宽度约束时（如在 flex row 容器中），根据内容计算宽度
            if (box.Children.Count == 0 && !string.IsNullOrEmpty(box.Element.TextContent))
            {
                // 根据文本内容计算宽度
                var (textWidth, _) = TextMeasurer.MeasureText(
                    box.Element.TextContent,
                    style.FontFamily,
                    style.FontSize.Value,
                    style.FontWeight);
                contentWidth = textWidth;
            }
            else
            {
                // 没有子元素时宽度为 0，有子元素时稍后根据子元素计算
                contentWidth = 0;
            }
        }
        else
        {
            float availableWidth = containerWidth - box.BoxModel.Margin.Horizontal;
            contentWidth = availableWidth - box.BoxModel.Border.Horizontal - box.BoxModel.Padding.Horizontal;
        }

        // 计算容器高度
        float containerHeight = constraints.AvailableHeight ?? 0;
        float contentHeight;
        if (!style.Height.IsAuto)
        {
            contentHeight = style.Height.ToPixels(containerHeight);
        }
        else
        {
            contentHeight = 0; // 稍后根据子元素计算
        }

        // 3. 确定主轴和交叉轴方向
        bool isRow = style.FlexDirection == FlexDirection.Row ||
                     style.FlexDirection == FlexDirection.RowReverse;

        // 4. 布局子元素
        float contentX = x + box.BoxModel.Margin.Left + box.BoxModel.Border.Left + box.BoxModel.Padding.Left;
        float contentY = y + box.BoxModel.Margin.Top + box.BoxModel.Border.Top + box.BoxModel.Padding.Top;

        float maxCrossSize = 0;

        if (isRow)
        {
            LayoutRowDirection(box, contentX, contentY, contentWidth, contentHeight, ref maxCrossSize);
        }
        else
        {
            LayoutColumnDirection(box, contentX, contentY, contentWidth, contentHeight, ref maxCrossSize);
        }

        // 5. 计算最终容器高度
        if (style.Height.IsAuto)
        {
            if (isRow)
            {
                contentHeight = maxCrossSize;
            }
            else
            {
                // 列布局：计算所有子元素的总高度
                float totalChildHeight = 0;
                foreach (var child in box.Children)
                {
                    totalChildHeight += child.BoxModel.MarginBox.Height;
                }
                contentHeight = totalChildHeight;
            }
        }

        box.BoxModel.Content = new RectF(contentX, contentY, contentWidth, contentHeight);
    }

    private void LayoutRowDirection(LayoutBox box, float contentX, float contentY,
        float contentWidth, float contentHeight, ref float maxCrossSize)
    {
        // 第一遍：使用 flex-basis 或自然尺寸布局子元素
        var childInfos = new List<FlexChildInfo>();
        float totalFlexBasisSize = 0;
        float totalFlexGrow = 0;
        float totalFlexShrinkWeighted = 0;

        foreach (var child in box.Children)
        {
            var childStyle = child.ComputedStyle;
            float flexBasis;
            bool usedAutoSize = false;

            // 确定 flex-basis
            if (!childStyle.FlexBasis.IsAuto)
            {
                flexBasis = childStyle.FlexBasis.ToPixels(contentWidth);
            }
            else if (!childStyle.Width.IsAuto)
            {
                flexBasis = childStyle.Width.ToPixels(contentWidth);
            }
            else
            {
                // 使用内容自然尺寸
                var childConstraints = new LayoutConstraints(null, null);
                LayoutDispatcher.Dispatch(child, childConstraints, 0, 0);
                flexBasis = child.BoxModel.MarginBox.Width;
                usedAutoSize = true;
            }

            var info = new FlexChildInfo
            {
                Child = child,
                FlexBasis = flexBasis,
                FlexGrow = childStyle.FlexGrow,
                FlexShrink = childStyle.FlexShrink,
                FinalSize = flexBasis,
                UsedAutoSize = usedAutoSize
            };

            childInfos.Add(info);
            totalFlexBasisSize += flexBasis;
            totalFlexGrow += childStyle.FlexGrow;
            totalFlexShrinkWeighted += childStyle.FlexShrink * flexBasis;
        }

        // 计算剩余空间
        float freeSpace = contentWidth - totalFlexBasisSize;

        // 分配空间（grow 或 shrink）
        bool anyAdjustment = false;
        if (freeSpace > 0 && totalFlexGrow > 0)
        {
            // 有剩余空间且有元素可以增长
            anyAdjustment = true;
            foreach (var info in childInfos)
            {
                float growRatio = info.FlexGrow / totalFlexGrow;
                info.FinalSize = info.FlexBasis + freeSpace * growRatio;
            }
        }
        else if (freeSpace < 0 && totalFlexShrinkWeighted > 0)
        {
            // 空间不足且有元素可以收缩
            anyAdjustment = true;
            float shrinkAmount = -freeSpace;
            foreach (var info in childInfos)
            {
                float shrinkRatio = (info.FlexShrink * info.FlexBasis) / totalFlexShrinkWeighted;
                info.FinalSize = info.FlexBasis - shrinkAmount * shrinkRatio;
                // 确保不会收缩到负值
                info.FinalSize = Math.Max(0, info.FinalSize);
            }
        }

        // 第二遍：使用最终尺寸重新布局子元素
        float currentX = contentX;
        foreach (var info in childInfos)
        {
            var child = info.Child;
            var childStyle = child.ComputedStyle;
            bool needsResize = anyAdjustment && Math.Abs(info.FinalSize - info.FlexBasis) > 0.01f;

            if (needsResize || !info.UsedAutoSize)
            {
                // 需要调整大小，或者使用了显式的 flex-basis/width
                float childMarginLeft = childStyle.MarginLeft.ToPixels(contentWidth);
                float childMarginRight = childStyle.MarginRight.ToPixels(contentWidth);
                float childBorderWidth = childStyle.BorderWidth.ToPixels(contentWidth);
                float childPaddingLeft = childStyle.PaddingLeft.ToPixels(contentWidth);
                float childPaddingRight = childStyle.PaddingRight.ToPixels(contentWidth);

                // 计算内容宽度
                float childContentWidth;
                if (info.UsedAutoSize)
                {
                    // 从 margin box 计算 content width
                    childContentWidth = info.FinalSize - childMarginLeft - childMarginRight
                        - childBorderWidth * 2 - childPaddingLeft - childPaddingRight;
                }
                else
                {
                    // flex-basis/width 就是 content width
                    childContentWidth = info.FinalSize;
                }
                childContentWidth = Math.Max(0, childContentWidth);

                // 使用计算出的宽度约束重新布局
                var childConstraints = new LayoutConstraints(childContentWidth, null);
                LayoutDispatcher.Dispatch(child, childConstraints, currentX, contentY);

                // 强制设置宽度（覆盖子元素自己计算的宽度）
                child.BoxModel.Content = new RectF(
                    child.BoxModel.Content.X,
                    child.BoxModel.Content.Y,
                    childContentWidth,
                    child.BoxModel.Content.Height
                );
            }
            else
            {
                // 使用自动尺寸，不需要强制调整
                var childConstraints = new LayoutConstraints(null, null);
                LayoutDispatcher.Dispatch(child, childConstraints, currentX, contentY);
            }

            currentX = child.BoxModel.MarginBox.Right;
            maxCrossSize = Math.Max(maxCrossSize, child.BoxModel.MarginBox.Height);
        }

        float totalWidth = currentX - contentX;

        // 主轴对齐 (justify-content) - 只有当没有 flex-grow 时才应用
        if (totalFlexGrow == 0)
        {
            ApplyJustifyContent(box, contentX, contentWidth, totalWidth, true);
        }

        // 交叉轴对齐 (align-items)
        ApplyAlignItems(box, contentY, maxCrossSize, true);
    }

    private void LayoutColumnDirection(LayoutBox box, float contentX, float contentY,
        float contentWidth, float contentHeight, ref float maxCrossSize)
    {
        // 第一遍：使用 flex-basis 或自然尺寸布局子元素
        var childInfos = new List<FlexChildInfo>();
        float totalFlexBasisSize = 0;
        float totalFlexGrow = 0;
        float totalFlexShrinkWeighted = 0;

        foreach (var child in box.Children)
        {
            var childStyle = child.ComputedStyle;
            float flexBasis;
            bool usedAutoSize = false;

            // 确定 flex-basis（列方向使用高度）
            if (!childStyle.FlexBasis.IsAuto)
            {
                flexBasis = childStyle.FlexBasis.ToPixels(contentHeight);
            }
            else if (!childStyle.Height.IsAuto)
            {
                flexBasis = childStyle.Height.ToPixels(contentHeight);
            }
            else
            {
                // 使用内容自然尺寸
                var childConstraints = new LayoutConstraints(contentWidth, null);
                LayoutDispatcher.Dispatch(child, childConstraints, 0, 0);
                flexBasis = child.BoxModel.MarginBox.Height;
                usedAutoSize = true;
            }

            var info = new FlexChildInfo
            {
                Child = child,
                FlexBasis = flexBasis,
                FlexGrow = childStyle.FlexGrow,
                FlexShrink = childStyle.FlexShrink,
                FinalSize = flexBasis,
                UsedAutoSize = usedAutoSize
            };

            childInfos.Add(info);
            totalFlexBasisSize += flexBasis;
            totalFlexGrow += childStyle.FlexGrow;
            totalFlexShrinkWeighted += childStyle.FlexShrink * flexBasis;
        }

        // 计算剩余空间（只有当容器高度明确时才分配）
        bool hasDefiniteHeight = contentHeight > 0;
        float freeSpace = hasDefiniteHeight ? contentHeight - totalFlexBasisSize : 0;

        // 分配空间（grow 或 shrink）
        bool anyAdjustment = false;
        if (freeSpace > 0 && totalFlexGrow > 0)
        {
            // 有剩余空间且有元素可以增长
            anyAdjustment = true;
            foreach (var info in childInfos)
            {
                float growRatio = info.FlexGrow / totalFlexGrow;
                info.FinalSize = info.FlexBasis + freeSpace * growRatio;
            }
        }
        else if (freeSpace < 0 && totalFlexShrinkWeighted > 0)
        {
            // 空间不足且有元素可以收缩
            anyAdjustment = true;
            float shrinkAmount = -freeSpace;
            foreach (var info in childInfos)
            {
                float shrinkRatio = (info.FlexShrink * info.FlexBasis) / totalFlexShrinkWeighted;
                info.FinalSize = info.FlexBasis - shrinkAmount * shrinkRatio;
                // 确保不会收缩到负值
                info.FinalSize = Math.Max(0, info.FinalSize);
            }
        }

        // 第二遍：使用最终尺寸重新布局子元素
        float currentY = contentY;
        foreach (var info in childInfos)
        {
            var child = info.Child;
            var childStyle = child.ComputedStyle;
            bool needsResize = anyAdjustment && Math.Abs(info.FinalSize - info.FlexBasis) > 0.01f;

            if (needsResize || !info.UsedAutoSize)
            {
                // 需要调整大小，或者使用了显式的 flex-basis/height
                float childMarginTop = childStyle.MarginTop.ToPixels(contentWidth);
                float childMarginBottom = childStyle.MarginBottom.ToPixels(contentWidth);
                float childBorderWidth = childStyle.BorderWidth.ToPixels(contentWidth);
                float childPaddingTop = childStyle.PaddingTop.ToPixels(contentWidth);
                float childPaddingBottom = childStyle.PaddingBottom.ToPixels(contentWidth);

                // 计算内容高度
                float childContentHeight;
                if (info.UsedAutoSize)
                {
                    // 从 margin box 计算 content height
                    childContentHeight = info.FinalSize - childMarginTop - childMarginBottom
                        - childBorderWidth * 2 - childPaddingTop - childPaddingBottom;
                }
                else
                {
                    // flex-basis/height 就是 content height
                    childContentHeight = info.FinalSize;
                }
                childContentHeight = Math.Max(0, childContentHeight);

                // 使用计算出的高度约束重新布局
                var childConstraints = new LayoutConstraints(contentWidth, childContentHeight);
                LayoutDispatcher.Dispatch(child, childConstraints, contentX, currentY);

                // 强制设置高度（覆盖子元素自己计算的高度）
                child.BoxModel.Content = new RectF(
                    child.BoxModel.Content.X,
                    child.BoxModel.Content.Y,
                    child.BoxModel.Content.Width,
                    childContentHeight
                );
            }
            else
            {
                // 使用自动尺寸，不需要强制调整
                var childConstraints = new LayoutConstraints(contentWidth, null);
                LayoutDispatcher.Dispatch(child, childConstraints, contentX, currentY);
            }

            currentY = child.BoxModel.MarginBox.Bottom;
            maxCrossSize = Math.Max(maxCrossSize, child.BoxModel.MarginBox.Width);
        }

        float totalHeight = currentY - contentY;

        // 主轴对齐 (justify-content) - 只有当没有 flex-grow 且有明确高度时才应用
        if (totalFlexGrow == 0 && hasDefiniteHeight)
        {
            ApplyJustifyContent(box, contentY, contentHeight, totalHeight, false);
        }

        // 交叉轴对齐 (align-items)
        ApplyAlignItems(box, contentX, maxCrossSize, false);
    }

    private void ApplyJustifyContent(LayoutBox box, float start, float containerSize, float contentSize, bool isRow)
    {
        if (box.Children.Count == 0) return;

        float spacing = 0;
        float offset = 0;

        switch (box.ComputedStyle.JustifyContent)
        {
            case JustifyContent.FlexStart:
                // 默认，无需调整
                break;

            case JustifyContent.FlexEnd:
                offset = containerSize - contentSize;
                break;

            case JustifyContent.Center:
                offset = (containerSize - contentSize) / 2;
                break;

            case JustifyContent.SpaceBetween:
                if (box.Children.Count > 1)
                {
                    spacing = (containerSize - contentSize) / (box.Children.Count - 1);
                }
                break;

            case JustifyContent.SpaceAround:
                spacing = (containerSize - contentSize) / box.Children.Count;
                offset = spacing / 2;
                break;

            case JustifyContent.SpaceEvenly:
                spacing = (containerSize - contentSize) / (box.Children.Count + 1);
                offset = spacing;
                break;
        }

        // 应用偏移
        for (int i = 0; i < box.Children.Count; i++)
        {
            var child = box.Children[i];
            float itemOffset = offset + spacing * i;

            if (isRow)
            {
                child.BoxModel.Content = new RectF(
                    child.BoxModel.Content.X + itemOffset,
                    child.BoxModel.Content.Y,
                    child.BoxModel.Content.Width,
                    child.BoxModel.Content.Height
                );
            }
            else
            {
                child.BoxModel.Content = new RectF(
                    child.BoxModel.Content.X,
                    child.BoxModel.Content.Y + itemOffset,
                    child.BoxModel.Content.Width,
                    child.BoxModel.Content.Height
                );
            }
        }
    }

    private void ApplyAlignItems(LayoutBox box, float start, float crossSize, bool isRow)
    {
        foreach (var child in box.Children)
        {
            float childCrossSize = isRow ? child.BoxModel.MarginBox.Height : child.BoxModel.MarginBox.Width;
            float offset = 0;

            switch (box.ComputedStyle.AlignItems)
            {
                case AlignItems.FlexStart:
                    // 默认，无需调整
                    break;

                case AlignItems.FlexEnd:
                    offset = crossSize - childCrossSize;
                    break;

                case AlignItems.Center:
                    offset = (crossSize - childCrossSize) / 2;
                    break;

                case AlignItems.Stretch:
                    // 拉伸子元素到交叉轴尺寸
                    if (isRow)
                    {
                        // Row 布局：拉伸高度
                        // 计算目标 content height = crossSize - margin - border - padding
                        float targetContentHeight = crossSize
                            - child.BoxModel.Margin.Vertical
                            - child.BoxModel.Border.Vertical
                            - child.BoxModel.Padding.Vertical;
                        targetContentHeight = Math.Max(0, targetContentHeight);

                        child.BoxModel.Content = new RectF(
                            child.BoxModel.Content.X,
                            child.BoxModel.Content.Y,
                            child.BoxModel.Content.Width,
                            targetContentHeight
                        );
                    }
                    else
                    {
                        // Column 布局：拉伸宽度
                        float targetContentWidth = crossSize
                            - child.BoxModel.Margin.Horizontal
                            - child.BoxModel.Border.Horizontal
                            - child.BoxModel.Padding.Horizontal;
                        targetContentWidth = Math.Max(0, targetContentWidth);

                        child.BoxModel.Content = new RectF(
                            child.BoxModel.Content.X,
                            child.BoxModel.Content.Y,
                            targetContentWidth,
                            child.BoxModel.Content.Height
                        );
                    }
                    break;

                case AlignItems.Baseline:
                    // TODO: 基线对齐
                    break;
            }

            // 应用偏移
            if (isRow)
            {
                child.BoxModel.Content = new RectF(
                    child.BoxModel.Content.X,
                    child.BoxModel.Content.Y + offset,
                    child.BoxModel.Content.Width,
                    child.BoxModel.Content.Height
                );
            }
            else
            {
                child.BoxModel.Content = new RectF(
                    child.BoxModel.Content.X + offset,
                    child.BoxModel.Content.Y,
                    child.BoxModel.Content.Width,
                    child.BoxModel.Content.Height
                );
            }
        }
    }

    /// <summary>
    /// Flex 子元素信息
    /// </summary>
    private class FlexChildInfo
    {
        public required LayoutBox Child { get; set; }
        public float FlexBasis { get; set; }
        public float FlexGrow { get; set; }
        public float FlexShrink { get; set; }
        public float FinalSize { get; set; }
        public bool UsedAutoSize { get; set; }
    }
}
