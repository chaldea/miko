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

        return layoutRoot;
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

        // 递归处理子元素
        foreach (var child in element.Children)
        {
            ComputeStyles(child, styleSheets, viewport);
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

        var pseudoElement = new PseudoElement { TextContent = matchedStyle.Content };
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
        }
    }
}
