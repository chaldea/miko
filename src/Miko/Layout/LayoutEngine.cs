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
    private readonly GridLayout _gridLayout = new();
    private readonly TableLayout _tableLayout = new();
    private readonly TextLayout _textLayout = new();

    // 当前布局的安全区边距。在样式计算阶段用于折算各元素的 env(safe-area-inset-*) 长度。
    // 不内缩视口本身——视口始终为全屏，仅“声明了 env() 的内容元素”据此添加内边距，
    // 从而全屏浮层（菜单遮罩等）仍覆盖整个屏幕（见 ISSUE-054）。
    private SafeAreaInsets _safeArea;

    // 当前布局的视口尺寸。用于折算各元素（含伪元素）的 vw/vh 视窗单位（见 ISSUE-091）。
    private ViewportInfo _viewport = new(0, 0);

    // ---- 布局结果缓存（ISSUE-096）----
    // 一次完整布局的输入为：根元素、样式表列表、视口尺寸、安全区、以及全局变更版本号
    // （Element.MutationVersion 覆盖结构/文本/class/行内样式/状态/图片尺寸等所有布局输入）。
    // 这些输入全部未变时，重跑布局必然得到相同结果，因此直接复用上次的布局树，
    // 稳态帧（仅视频新帧、滚动等绘制级失效）不再产生任何样式/布局分配。
    private Element? _cachedRoot;
    private List<StyleSheet>? _cachedStyleSheets;
    private int _cachedStyleSheetCount;
    private float _cachedViewportWidth;
    private float _cachedViewportHeight;
    private SafeAreaInsets _cachedSafeArea;
    private long _cachedMutationVersion = -1;
    private LayoutBox? _cachedResult;

    /// <summary>
    /// 使缓存的布局结果失效。一般无需调用——所有常规变更都会递增
    /// <see cref="Element.MutationVersion"/> 而被自动检测。仅在引擎外发生了未被追踪的
    /// 变化时（如运行时注册新字体改变了文本度量、直接改写样式表规则内容）调用。
    /// </summary>
    public void InvalidateCache()
    {
        _cachedRoot = null;
        _cachedStyleSheets = null;
        _cachedResult = null;
        _cachedMutationVersion = -1;
    }

    /// <summary>判断给定输入下缓存的布局结果是否仍然有效（无需重排）。</summary>
    public bool IsLayoutCurrent(Element? root, List<StyleSheet> styleSheets, float viewportWidth, float viewportHeight,
        SafeAreaInsets safeArea = default)
    {
        return _cachedResult != null
            && ReferenceEquals(_cachedRoot, root)
            && ReferenceEquals(_cachedStyleSheets, styleSheets)
            && _cachedStyleSheetCount == styleSheets.Count
            && Math.Abs(_cachedViewportWidth - viewportWidth) < 0.01f
            && Math.Abs(_cachedViewportHeight - viewportHeight) < 0.01f
            && _cachedSafeArea == safeArea
            && _cachedMutationVersion == Element.MutationVersion;
    }

    /// <summary>
    /// 执行布局计算
    /// </summary>
    /// <param name="safeArea">
    /// 安全区边距（逻辑像素）。通过 CSS <c>env(safe-area-inset-*)</c> 暴露给内容元素，
    /// 使其可主动添加内边距避开系统状态栏/导航栏；视口本身不内缩。默认无安全区（桌面）。
    /// </param>
    public LayoutBox Layout(Element root, List<StyleSheet> styleSheets, float viewportWidth, float viewportHeight,
        SafeAreaInsets safeArea = default)
    {
        // 快速路径：布局输入全部未变，直接复用上次的布局树（零样式解析、零布局、零分配）。
        if (IsLayoutCurrent(root, styleSheets, viewportWidth, viewportHeight, safeArea))
            return _cachedResult!;

        _safeArea = safeArea;

        // 视口为全屏：env(safe-area-inset-*) 由各内容元素按需折算成内边距，浮层不受影响。
        // 1. 样式计算：为每个元素计算最终样式（并折算其 env() 安全区分量与 vw/vh 视窗分量）。
        var viewport = new ViewportInfo(viewportWidth, viewportHeight);
        _viewport = viewport;
        ComputeStyles(root, styleSheets, viewport);

        // 2. 构建布局树：根据 display 属性过滤和组织
        var layoutRoot = BuildLayoutTree(root, styleSheets);

        if (layoutRoot == null)
        {
            throw new InvalidOperationException("Failed to build layout tree");
        }

        // 3. 布局计算：从视口原点 (0,0) 开始，覆盖整个视口。
        var constraints = new LayoutConstraints(viewportWidth, viewportHeight);
        CalculateLayout(layoutRoot, constraints, 0f, 0f);

        // 4. 定位调整：处理 relative/absolute 定位的偏移。根包含块为整个视口。
        var viewportBlock = new RectF(0f, 0f, viewportWidth, viewportHeight);
        ApplyPositioning(layoutRoot, viewportBlock);

        // 记录缓存键。变更版本号在布局完成后读取：布局期间用户代码（事件回调等）
        // 造成的任何修改都会使版本号领先于缓存值，下一帧必然重排，不会复用到中间态。
        _cachedRoot = root;
        _cachedStyleSheets = styleSheets;
        _cachedStyleSheetCount = styleSheets.Count;
        _cachedViewportWidth = viewportWidth;
        _cachedViewportHeight = viewportHeight;
        _cachedSafeArea = safeArea;
        _cachedMutationVersion = Element.MutationVersion;
        _cachedResult = layoutRoot;

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

        // 折算该元素声明的视窗单位（vw/vh）为像素。vw/vh 始终相对整个视口，与包含块无关，
        // 故在此（样式计算阶段、已知视口时）一次折算，之后布局对其无感（font-size 中的 vw/vh
        // 已由 StyleResolver 经 FromStyle 单独折算，须先于其 em 解析）。
        computedStyle.ResolveViewport(viewport);

        // 折算该元素声明的 env(safe-area-inset-*) 长度为像素（桌面/零安全区时为空操作）。
        computedStyle.ResolveSafeArea(_safeArea);

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

        // 文本节点：始终使用 Text 布局（匿名行内文本盒），不受 display 影响。
        if (element is TextNode)
        {
            layoutBox.Type = LayoutType.Text;
            return layoutBox;
        }

        // 根据 display 属性确定布局类型
        layoutBox.Type = layoutBox.ComputedStyle.Display switch
        {
            Display.Block => LayoutType.Block,
            Display.Inline => LayoutType.Inline,
            Display.InlineBlock => LayoutType.InlineBlock,
            Display.Flex => LayoutType.Flex,
            Display.InlineFlex => LayoutType.InlineFlex,
            Display.Grid => LayoutType.Grid,
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
            AppendChildLayoutBoxes(layoutBox, child, styleSheets);
        }

        // 注入 ::after 伪元素
        var afterBox = CreatePseudoElementBox(element, styleSheets, PseudoElementType.After);
        if (afterBox != null)
        {
            layoutBox.Children.Add(afterBox);
        }

        return layoutBox;
    }

    /// <summary>
    /// 将 <paramref name="child"/> 的布局盒加入 <paramref name="parentBox"/>。
    /// <see cref="FragmentElement"/> 是透明容器：不为其自身建盒，而是把其子节点的布局盒
    /// 直接摊平进父盒（等价 CSS <c>display: contents</c>）。片段留在 DOM 树中作为多根组件的
    /// 稳定根（供 StateHasChanged 原地重渲染），但在布局上不产生任何包裹盒，从而不影响样式。
    /// 注意：当片段本身是布局根（无父，如无 Layout 的多根页面）时不经此路径，而是按普通块盒
    /// 充当 issue 所允许的"自动创建的根包裹"。
    /// </summary>
    private void AppendChildLayoutBoxes(LayoutBox parentBox, Element child, List<StyleSheet> styleSheets)
    {
        if (child is FragmentElement)
        {
            foreach (var grandChild in child.Children)
            {
                AppendChildLayoutBoxes(parentBox, grandChild, styleSheets);
            }
            return;
        }

        var childLayoutBox = BuildLayoutTree(child, styleSheets);
        if (childLayoutBox != null)
        {
            parentBox.Children.Add(childLayoutBox);
        }
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

        // 伪元素继承其宿主元素的自定义变量作用域，使 content:/color: 等的 Var(...) 可解析。
        // 传入视口以折算伪元素自身 font-size 中的 vw/vh（须先于其 em 解析）。
        var hostVarScope = element.LayoutBox?.ComputedStyle?.Vars;
        var computedStyle = ComputedStyle.FromStyle(matchedStyle, varScope: hostVarScope, viewport: _viewport);
        // content 文本通过 facade setter 变为 pseudoElement 的 TextNode 子节点（见 ISSUE-086）。
        // Content 可能是 Var(...) 引用，用计算样式解析后的具体值。
        computedStyle.TryResolveStyleProperty(matchedStyle.Content ?? default, out string? resolvedContent);
        var pseudoElement = new PseudoElement { TextContent = resolvedContent, Type = type };
        // 折算伪元素声明的 vw/vh 视窗分量与 env() 安全区分量（与普通元素一致）。
        computedStyle.ResolveViewport(_viewport);
        computedStyle.ResolveSafeArea(_safeArea);

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
            Display.InlineFlex => LayoutType.InlineFlex,
            Display.Grid => LayoutType.Grid,
            Display.Table => LayoutType.Table,
            Display.TableRow => LayoutType.TableRow,
            Display.TableCell => LayoutType.TableCell,
            Display.None => LayoutType.Block,
            _ => LayoutType.Inline
        };

        if (computedStyle.Display == Display.None) return null;

        // 为 content 文本节点建盒并挂到伪元素盒下，使其作为普通行内子盒被布局/绘制。
        foreach (var child in pseudoElement.Children)
        {
            if (child is TextNode)
            {
                var textStyle = ComputedStyle.FromStyle(new Style());
                InheritComputedStyle(textStyle, computedStyle);
                child.LayoutBox = new LayoutBox
                {
                    Element = child,
                    ComputedStyle = textStyle,
                    Type = LayoutType.Text
                };
                box.Children.Add(child.LayoutBox);
            }
        }

        return box;
    }

    /// <summary>
    /// 把父计算样式的可继承文本属性复制到子（用于伪元素 content 文本节点等无独立样式解析的场景）。
    /// Miko 无 CSS inherit 关键字，需显式镜像（见 memory: miko-no-inherit-keyword）。
    /// </summary>
    private static void InheritComputedStyle(ComputedStyle target, ComputedStyle parent)
    {
        target.Color = parent.Color;
        target.FontFamily = parent.FontFamily;
        target.FontSize = parent.FontSize;
        target.FontWeight = parent.FontWeight;
        target.TextAlign = parent.TextAlign;
        target.LineHeight = parent.LineHeight;
        target.WhiteSpace = parent.WhiteSpace;
        target.TextDecoration = parent.TextDecoration;
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
            case LayoutType.InlineFlex:
                _flexLayout.Layout(box, constraints, x, y);
                break;

            case LayoutType.Grid:
                _gridLayout.Layout(box, constraints, x, y);
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

            case LayoutType.Text:
                _textLayout.Layout(box, constraints, x, y);
                break;
        }
    }
}
