using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout.LayoutAlgorithms;
using Miko.Styling;

namespace Miko.Layout;

/// <summary>
/// 布局引擎
/// </summary>
public class LayoutEngine
{
    private readonly StyleResolver _styleResolver = new();
    private readonly BlockLayout _blockLayout = new();
    private readonly InlineLayout _inlineLayout = new();
    private readonly FlexLayout _flexLayout = new();
    private readonly TableLayout _tableLayout = new();

    /// <summary>
    /// 执行布局计算
    /// </summary>
    public LayoutBox Layout(Element root, List<StyleSheet> styleSheets, float viewportWidth, float viewportHeight)
    {
        // 1. 样式计算：为每个元素计算最终样式
        var viewport = new ViewportInfo(viewportWidth, viewportHeight);
        ComputeStyles(root, styleSheets, viewport);

        // 2. 构建布局树：根据 display 属性过滤和组织
        var layoutRoot = BuildLayoutTree(root, styleSheets);

        if (layoutRoot == null)
        {
            throw new InvalidOperationException("Failed to build layout tree");
        }

        // 3. 布局计算：计算每个盒子的位置和尺寸
        var constraints = new LayoutConstraints(viewportWidth, viewportHeight);
        CalculateLayout(layoutRoot, constraints, 0, 0);

        // 4. 定位调整：处理 relative/absolute 定位的偏移
        // 根元素的初始包含块为视口
        var viewportBlock = new RectF(0, 0, viewportWidth, viewportHeight);
        ApplyPositioning(layoutRoot, viewportBlock);

        return layoutRoot;
    }

    /// <summary>
    /// 应用定位偏移（relative / absolute）。
    /// 在常规流布局完成后，根据 position 和 top/right/bottom/left 调整盒子位置。
    /// </summary>
    /// <param name="box">当前盒子</param>
    /// <param name="containingBlock">最近的定位包含块（绝对定位参照的 padding box）</param>
    private void ApplyPositioning(LayoutBox box, RectF containingBlock)
    {
        var style = box.ComputedStyle;
        var position = style.Position;
        // 元素自身字体大小（px），用于解析 top/right/bottom/left 中的 em 分量。
        float fs = style.FontSize.Value;

        // relative/absolute 元素本身成为后代绝对定位元素的包含块。
        // 包含块使用元素的 padding box（CSS 规范：绝对定位相对于包含块的 padding 边缘）。
        RectF childContainingBlock = containingBlock;

        if (position == Position.Relative)
        {
            // relative：相对于自身在常规流中的位置偏移
            float dx = 0f;
            float dy = 0f;

            if (!style.Left.IsAuto)
                dx = style.Left.ToPixels(containingBlock.Width, fs);
            else if (!style.Right.IsAuto)
                dx = -style.Right.ToPixels(containingBlock.Width, fs);

            if (!style.Top.IsAuto)
                dy = style.Top.ToPixels(containingBlock.Height, fs);
            else if (!style.Bottom.IsAuto)
                dy = -style.Bottom.ToPixels(containingBlock.Height, fs);

            if (dx != 0f || dy != 0f)
            {
                OffsetSubtree(box, dx, dy);
            }

            childContainingBlock = box.BoxModel.PaddingBox;
        }
        else if (position == Position.Absolute || position == Position.Fixed)
        {
            // absolute/fixed：相对于包含块定位
            var marginBox = box.BoxModel.MarginBox;

            // 水平方向
            float targetX = marginBox.Left;
            if (!style.Left.IsAuto)
            {
                targetX = containingBlock.Left + style.Left.ToPixels(containingBlock.Width, fs);
            }
            else if (!style.Right.IsAuto)
            {
                targetX = containingBlock.Right - style.Right.ToPixels(containingBlock.Width, fs) - marginBox.Width;
            }

            // 垂直方向
            float targetY = marginBox.Top;
            if (!style.Top.IsAuto)
            {
                targetY = containingBlock.Top + style.Top.ToPixels(containingBlock.Height, fs);
            }
            else if (!style.Bottom.IsAuto)
            {
                targetY = containingBlock.Bottom - style.Bottom.ToPixels(containingBlock.Height, fs) - marginBox.Height;
            }

            float dx = targetX - marginBox.Left;
            float dy = targetY - marginBox.Top;

            if (dx != 0f || dy != 0f)
            {
                OffsetSubtree(box, dx, dy);
            }

            childContainingBlock = box.BoxModel.PaddingBox;
        }
        else if (position == Position.Static)
        {
            // static 元素不建立包含块，沿用祖先的包含块
            childContainingBlock = containingBlock;
        }

        // 递归处理子元素
        foreach (var child in box.Children)
        {
            ApplyPositioning(child, childContainingBlock);
        }
    }

    /// <summary>
    /// 将盒子及其所有后代的位置整体平移 (dx, dy)。
    /// 渲染与命中测试均从 BoxModel.Content 派生，因此只需平移每个盒子的 Content 矩形。
    /// </summary>
    private static void OffsetSubtree(LayoutBox box, float dx, float dy)
    {
        var content = box.BoxModel.Content;
        box.BoxModel.Content = new RectF(content.X + dx, content.Y + dy, content.Width, content.Height);

        foreach (var child in box.Children)
        {
            OffsetSubtree(child, dx, dy);
        }
    }

    /// <summary>
    /// 计算所有元素的样式
    /// </summary>
    private void ComputeStyles(Element element, List<StyleSheet> styleSheets, ViewportInfo viewport)
    {
        var computedStyle = _styleResolver.Resolve(element, styleSheets, viewport);

        // 创建布局盒子并关联
        element.LayoutBox = new LayoutBox
        {
            Element = element,
            ComputedStyle = computedStyle
        };

        // 应用伪元素样式（::range-thumb, ::range-track, ::range-progress 等）
        ApplyPseudoElementStyles(element, styleSheets);

        // 递归处理子元素，传递当前元素的自定义属性作用域
        foreach (var child in element.Children)
        {
            ComputeStyles(child, styleSheets, viewport);
        }
    }

    /// <summary>
    /// 应用伪元素样式到元素
    /// </summary>
    private void ApplyPseudoElementStyles(Element element, List<StyleSheet> styleSheets)
    {
        foreach (var sheet in styleSheets)
        {
            foreach (var rule in sheet.PseudoElementRules)
            {
                // 检查选择器是否匹配元素
                if (rule.Selector.Matches(element))
                {
                    // 初始化伪元素样式字典（如果需要）
                    element.PseudoElementStyles ??= new Dictionary<PseudoElementType, Style>();

                    // 获取或创建该伪元素类型的样式
                    if (!element.PseudoElementStyles.TryGetValue(rule.Type, out var existingStyle))
                    {
                        existingStyle = new Style();
                        element.PseudoElementStyles[rule.Type] = existingStyle;
                    }

                    // 合并样式规则
                    existingStyle.Merge(rule.Style);
                }
            }
        }
    }

    /// <summary>
    /// 构建布局树
    /// </summary>
    private LayoutBox? BuildLayoutTree(Element element, List<StyleSheet> styleSheets)
    {
        if (element.LayoutBox == null)
        {
            return null;
        }

        var layoutBox = element.LayoutBox;

        // 根据 display 属性确定布局类型
        layoutBox.Type = layoutBox.ComputedStyle.Display switch
        {
            Display.Block => LayoutType.Block,
            Display.Inline => LayoutType.Inline,
            Display.InlineBlock => LayoutType.InlineBlock,
            Display.Flex => LayoutType.Flex,
            Display.Table => LayoutType.Table,
            Display.TableRow => LayoutType.TableRow,
            Display.TableCell => LayoutType.TableCell,
            Display.None => LayoutType.Block, // 不会被添加到树中
            _ => LayoutType.Block
        };

        // 如果是 display: none，不添加到布局树
        if (layoutBox.ComputedStyle.Display == Display.None)
        {
            return null;
        }

        // 注入 ::before 伪元素
        var beforeBox = CreatePseudoElementBox(element, styleSheets, PseudoElementType.Before);
        if (beforeBox != null)
        {
            layoutBox.Children.Add(beforeBox);
        }

        // 递归构建子元素的布局树
        foreach (var child in element.Children)
        {
            var childLayoutBox = BuildLayoutTree(child, styleSheets);
            if (childLayoutBox != null)
            {
                layoutBox.Children.Add(childLayoutBox);
            }
        }

        // 注入 ::after 伪元素
        var afterBox = CreatePseudoElementBox(element, styleSheets, PseudoElementType.After);
        if (afterBox != null)
        {
            layoutBox.Children.Add(afterBox);
        }

        return layoutBox;
    }

    private LayoutBox? CreatePseudoElementBox(Element element, List<StyleSheet> styleSheets, PseudoElementType type)
    {
        Style? matchedStyle = null;

        foreach (var sheet in styleSheets)
        {
            foreach (var rule in sheet.PseudoElementRules)
            {
                if (rule.Type == type && rule.Selector.Matches(element))
                {
                    matchedStyle ??= new Style();
                    matchedStyle.Merge(rule.Style);
                }
            }
        }

        if (matchedStyle == null) return null;

        if (element.PseudoElementStyles != null &&
            element.PseudoElementStyles.TryGetValue(type, out var overrideStyle))
        {
            var merged = overrideStyle.Clone();
            merged.Merge(matchedStyle);
            matchedStyle = merged;
        }

        var pseudoElement = new PseudoElement { TextContent = matchedStyle.Content, Type = type };
        var computedStyle = ComputedStyle.FromStyle(matchedStyle);

        pseudoElement.LayoutBox = new LayoutBox
        {
            Element = pseudoElement,
            ComputedStyle = computedStyle
        };

        var box = pseudoElement.LayoutBox;
        box.Type = computedStyle.Display switch
        {
            Display.Block => LayoutType.Block,
            Display.Inline => LayoutType.Inline,
            Display.InlineBlock => LayoutType.InlineBlock,
            Display.Flex => LayoutType.Flex,
            Display.Table => LayoutType.Table,
            Display.TableRow => LayoutType.TableRow,
            Display.TableCell => LayoutType.TableCell,
            Display.None => LayoutType.Block,
            _ => LayoutType.Inline
        };

        if (computedStyle.Display == Display.None) return null;

        return box;
    }

    /// <summary>
    /// 计算布局
    /// </summary>
    private void CalculateLayout(LayoutBox box, LayoutConstraints constraints, float x, float y)
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

            case LayoutType.Table:
                _tableLayout.Layout(box, constraints, x, y);
                break;

            case LayoutType.TableRow:
            case LayoutType.TableCell:
                // TableRow 和 TableCell 由 TableLayout 直接布局
                // 如果单独调用，使用 Block 布局作为后备
                _blockLayout.Layout(box, constraints, x, y);
                break;
        }
    }
}
