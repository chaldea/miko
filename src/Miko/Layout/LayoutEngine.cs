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
        // 根元素的 AvailableHeight 是填充指令：height:auto + overflow 的根盒子撑满视口高度
        // （Miko 的根滚动模型——文档无独立视口滚动，根元素 overflow 即页面滚动容器）。
        var constraints = new LayoutConstraints(viewportWidth, viewportHeight) { FillAvailableHeight = true };
        CalculateLayout(layoutRoot, constraints, 0f, 0f);

        // 4. 定位调整：处理 relative/absolute/fixed 定位的偏移。根包含块为整个视口；
        // fixed 的包含块恒为视口（不随定位祖先改变），故单独传递。
        var viewportBlock = new RectF(0f, 0f, viewportWidth, viewportHeight);
        ApplyPositioning(layoutRoot, viewportBlock, viewportBlock);

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
    /// 应用定位偏移（relative / absolute / fixed）。
    /// 在常规流布局完成后，根据 position 和 top/right/bottom/left 调整盒子位置；
    /// 对脱离文档流的盒子，还会在此按真实包含块补齐由对边偏移决定的尺寸（见 ResolveOutOfFlowSize）。
    /// </summary>
    /// <param name="box">当前盒子</param>
    /// <param name="containingBlock">最近的定位包含块（绝对定位参照的 padding box）</param>
    /// <param name="viewportBlock">视口矩形，fixed 定位的包含块（不随定位祖先改变）</param>
    private void ApplyPositioning(LayoutBox box, RectF containingBlock, RectF viewportBlock)
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
            // fixed 的包含块恒为视口，与是否存在定位祖先无关（CSS：固定定位相对视窗）。
            // absolute 使用最近定位祖先的 padding box。
            if (position == Position.Fixed)
                containingBlock = viewportBlock;

            // 常规流阶段父级只能以自己的内容盒近似约束该盒子，此处才知道真实包含块：
            // 若某轴由对边偏移（left+right / top+bottom）定型，需按包含块重算尺寸并重排子树。
            ResolveOutOfFlowSize(box, containingBlock);

            // 尺寸定型后，把该轴的剩余空间分配给 auto 外边距（绝对居中等，见 CSS 10.3.7/10.6.4）。
            // 必须在偏移平移之前完成：下面的 targetX/targetY 以 margin box 为基准，
            // auto margin 撑开后 margin box 才是最终尺寸。
            ResolveOutOfFlowAutoMargins(box, containingBlock);

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
            ApplyPositioning(child, childContainingBlock, viewportBlock);
        }
    }

    /// <summary>
    /// 按真实包含块补齐脱离文档流盒子（absolute / fixed）由对边偏移决定的尺寸。
    /// </summary>
    /// <remarks>
    /// CSS 绝对定位的尺寸方程为 <c>left + margin + border + padding + width + padding + border +
    /// margin + right = 包含块宽度</c>（高度同理）。当 <c>width: auto</c> 且 left/right 均非 auto 时，
    /// width 由该方程求解，而非收缩到内容——例如全屏浮层惯用的
    /// <c>position: fixed; top/right/bottom/left: 0</c>，应铺满包含块而不是塌缩为 0×0（ISSUE-112）。
    ///
    /// 常规流布局阶段（BlockLayout / FlexLayout / GridLayout 等）只知道父盒的内容盒，无法得知真实
    /// 包含块（最近的定位祖先，fixed 则是视口），因此那里一律按 shrink-to-fit 预布局；真实包含块直到
    /// 定位阶段才确定，故在此重算并以 ResolvedContentWidth/Height 强制定型重排子树——该通道会让盒子
    /// 跳过自身 width/height 解析与该轴 min/max 夹取，与 flex 主轴定型走同一语义（见 ISSUE-106）。
    /// 只有被对边定型的轴才强制，另一轴保持原有解析结果。
    /// </remarks>
    private void ResolveOutOfFlowSize(LayoutBox box, RectF containingBlock)
    {
        var style = box.ComputedStyle;
        float fs = style.FontSize.Value;

        // 仅当该轴的尺寸为 auto 且两侧偏移都已指定时，尺寸才由偏移方程决定。
        // 百分比宽/高针对确定的包含块可正常解析，不属于此路径。
        // fit-content 明确要求收缩到内容，是「确定尺寸」，不被偏移方程接管（剩余空间归 auto 外边距）。
        bool widthFromInsets = style.Width.IsAuto && !style.Width.IsFitContent
            && !style.Left.IsAuto && !style.Right.IsAuto;
        bool heightFromInsets = style.Height.IsAuto && !style.Height.IsFitContent
            && !style.Top.IsAuto && !style.Bottom.IsAuto;

        if (!widthFromInsets && !heightFromInsets)
            return;

        // 未被偏移定型的轴必须保持常规流阶段的语义：width:auto 的脱离流盒子按 CSS 收缩到内容，
        // 所以此处传 null 宽度触发 shrink-to-fit 分支，而不是把包含块宽度当作「可用宽度」让它撑满
        // （否则只有 top/bottom 被定型时，宽度会平白从内容宽变成包含块宽——ion-fab
        // `vertical="center" horizontal="end"` 正是这样撑满整屏、盖住其它 fab 的）。
        float? availableWidth = widthFromInsets || !style.Width.IsAuto ? containingBlock.Width : null;
        var constraints = new LayoutConstraints(availableWidth, containingBlock.Height);

        if (widthFromInsets)
        {
            float left = style.Left.ToPixels(containingBlock.Width, fs);
            float right = style.Right.ToPixels(containingBlock.Width, fs);
            // 方程求解的是 margin box 宽度；扣掉 margin/border/padding 得到内容宽。
            // auto margin 在此按 0 参与（盒子已被两侧偏移定型，无剩余空间可分配）。
            float contentWidth = containingBlock.Width - left - right
                - box.BoxModel.Margin.Horizontal
                - box.BoxModel.Border.Horizontal
                - box.BoxModel.Padding.Horizontal;
            constraints.ResolvedContentWidth = Math.Max(0, contentWidth);
        }

        if (heightFromInsets)
        {
            float top = style.Top.ToPixels(containingBlock.Height, fs);
            float bottom = style.Bottom.ToPixels(containingBlock.Height, fs);
            float contentHeight = containingBlock.Height - top - bottom
                - box.BoxModel.Margin.Vertical
                - box.BoxModel.Border.Vertical
                - box.BoxModel.Padding.Vertical;
            constraints.ResolvedContentHeight = Math.Max(0, contentHeight);
        }

        // 就地重排：起点沿用常规流阶段的 margin box 原点，随后由调用方按偏移整体平移子树。
        var marginBox = box.BoxModel.MarginBox;
        CalculateLayout(box, constraints, marginBox.Left, marginBox.Top);
    }

    /// <summary>
    /// 解析脱离文档流盒子（absolute / fixed）的 auto 外边距：把该轴的剩余空间分配给它们。
    /// </summary>
    /// <remarks>
    /// CSS 10.3.7 / 10.6.4：当某轴的两侧偏移与尺寸都非 auto 时（如
    /// <c>left:0; right:0; width:200px; margin:auto</c>），偏移方程被过度约束，剩余空间由该轴的
    /// auto 外边距吸收——两侧皆 auto 则均分（绝对居中的惯用写法），仅一侧 auto 则由该侧吃满。
    /// 若无 auto 外边距则方程按 LTR 忽略 <c>right</c>（<c>bottom</c> 同理），即下方定位逻辑的默认行为。
    ///
    /// 两侧都要重算而非只补 auto 侧：BlockLayout 的块流 auto margin 分支会以「父内容宽度」为基准
    /// 预先写入居中值，而脱离流盒子的正确基准是包含块，两者不同（父盒未必是包含块）；此处按包含块
    /// 重新求解并覆盖，非 auto 侧则恢复其声明值。
    ///
    /// 仅在剩余空间为正时分配：负剩余（盒子比可用空间宽）在 CSS 中 auto 外边距按 0 处理。
    /// </remarks>
    private static void ResolveOutOfFlowAutoMargins(LayoutBox box, RectF containingBlock)
    {
        var style = box.ComputedStyle;
        float fs = style.FontSize.Value;

        ResolveAutoMarginAxis(
            startAuto: style.MarginLeft.IsAuto, endAuto: style.MarginRight.IsAuto,
            startOffsetAuto: style.Left.IsAuto, endOffsetAuto: style.Right.IsAuto,
            // fit-content 已收缩到内容，属于确定尺寸：方程过度约束，剩余空间可分配给 auto 外边距。
            sizeIsAuto: style.Width.IsAuto && !style.Width.IsFitContent,
            startOffset: style.Left.ToPixels(containingBlock.Width, fs),
            endOffset: style.Right.ToPixels(containingBlock.Width, fs),
            declaredStart: style.MarginLeft.ToPixels(containingBlock.Width, fs),
            declaredEnd: style.MarginRight.ToPixels(containingBlock.Width, fs),
            containingSize: containingBlock.Width,
            // 外边距之外的已占用尺寸（border box）。
            occupied: box.BoxModel.Content.Width + box.BoxModel.Border.Horizontal + box.BoxModel.Padding.Horizontal,
            out float marginLeft, out float marginRight);

        ResolveAutoMarginAxis(
            startAuto: style.MarginTop.IsAuto, endAuto: style.MarginBottom.IsAuto,
            startOffsetAuto: style.Top.IsAuto, endOffsetAuto: style.Bottom.IsAuto,
            sizeIsAuto: style.Height.IsAuto && !style.Height.IsFitContent,
            startOffset: style.Top.ToPixels(containingBlock.Height, fs),
            endOffset: style.Bottom.ToPixels(containingBlock.Height, fs),
            // 垂直外边距的百分比同样相对包含块「宽度」解析（CSS 规范），与 BlockLayout 一致。
            declaredStart: style.MarginTop.ToPixels(containingBlock.Width, fs),
            declaredEnd: style.MarginBottom.ToPixels(containingBlock.Width, fs),
            containingSize: containingBlock.Height,
            occupied: box.BoxModel.Content.Height + box.BoxModel.Border.Vertical + box.BoxModel.Padding.Vertical,
            out float marginTop, out float marginBottom);

        box.BoxModel.Margin = new EdgeSizes(marginTop, marginRight, marginBottom, marginLeft);
    }

    /// <summary>
    /// 求解单轴的两侧外边距。见 <see cref="ResolveOutOfFlowAutoMargins"/> 的规则说明。
    /// </summary>
    private static void ResolveAutoMarginAxis(
        bool startAuto, bool endAuto,
        bool startOffsetAuto, bool endOffsetAuto,
        bool sizeIsAuto,
        float startOffset, float endOffset,
        float declaredStart, float declaredEnd,
        float containingSize, float occupied,
        out float marginStart, out float marginEnd)
    {
        // 非 auto 的外边距恒为其声明值；auto 默认 0，仅在下面过度约束时才吸收剩余空间。
        marginStart = startAuto ? 0f : declaredStart;
        marginEnd = endAuto ? 0f : declaredEnd;

        if (!startAuto && !endAuto)
            return;

        // 只有「两侧偏移 + 尺寸」都确定时方程才过度约束，才有剩余空间可分配。
        // 尺寸为 auto 时该轴已由偏移方程定型（见 ResolveOutOfFlowSize），剩余空间为 0；
        // 任一侧偏移为 auto 时盒子按常规流位置摆放，同样无剩余空间可言。
        if (sizeIsAuto || startOffsetAuto || endOffsetAuto)
            return;

        float remaining = containingSize - startOffset - endOffset - occupied
            - (startAuto ? 0f : marginStart)
            - (endAuto ? 0f : marginEnd);

        if (remaining <= 0f)
            return;

        if (startAuto && endAuto)
        {
            marginStart = remaining / 2f;
            marginEnd = remaining / 2f;
        }
        else if (startAuto)
        {
            marginStart = remaining;
        }
        else
        {
            marginEnd = remaining;
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
