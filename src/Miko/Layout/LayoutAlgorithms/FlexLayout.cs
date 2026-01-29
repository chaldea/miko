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
        else
        {
            float availableWidth = containerWidth - box.BoxModel.Margin.Horizontal;
            contentWidth = availableWidth - box.BoxModel.Border.Horizontal - box.BoxModel.Padding.Horizontal;
        }

        // 3. 确定主轴和交叉轴方向
        bool isRow = style.FlexDirection == FlexDirection.Row ||
                     style.FlexDirection == FlexDirection.RowReverse;

        // 4. 布局子元素
        float contentX = x + box.BoxModel.Margin.Left + box.BoxModel.Border.Left + box.BoxModel.Padding.Left;
        float contentY = y + box.BoxModel.Margin.Top + box.BoxModel.Border.Top + box.BoxModel.Padding.Top;

        float currentX = contentX;
        float currentY = contentY;
        float maxCrossSize = 0;

        if (isRow)
        {
            // 行方向：水平排列
            foreach (var child in box.Children)
            {
                var childConstraints = new LayoutConstraints(null, null);
                LayoutDispatcher.Dispatch(child, childConstraints, currentX, currentY);

                currentX = child.BoxModel.MarginBox.Right;
                maxCrossSize = Math.Max(maxCrossSize, child.BoxModel.MarginBox.Height);
            }

            float totalWidth = currentX - contentX;

            // 主轴对齐 (justify-content)
            ApplyJustifyContent(box, contentX, contentWidth, totalWidth, isRow);

            // 交叉轴对齐 (align-items)
            ApplyAlignItems(box, contentY, maxCrossSize, isRow);
        }
        else
        {
            // 列方向：垂直排列
            foreach (var child in box.Children)
            {
                var childConstraints = new LayoutConstraints(contentWidth, null);
                LayoutDispatcher.Dispatch(child, childConstraints, currentX, currentY);

                currentY = child.BoxModel.MarginBox.Bottom;
                maxCrossSize = Math.Max(maxCrossSize, child.BoxModel.MarginBox.Width);
            }

            float totalHeight = currentY - contentY;

            // 主轴对齐 (justify-content)
            ApplyJustifyContent(box, contentY, constraints.AvailableHeight ?? totalHeight, totalHeight, isRow);

            // 交叉轴对齐 (align-items)
            ApplyAlignItems(box, contentX, maxCrossSize, isRow);
        }

        // 5. 计算容器高度
        float contentHeight;
        if (!style.Height.IsAuto)
        {
            contentHeight = style.Height.ToPixels(constraints.AvailableHeight ?? 0);
        }
        else
        {
            contentHeight = isRow ? maxCrossSize : (currentY - contentY);
        }

        box.BoxModel.Content = new RectF(contentX, contentY, contentWidth, contentHeight);
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
                    // TODO: 拉伸子元素
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
}
