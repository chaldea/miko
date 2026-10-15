using Miko.Common;

namespace Miko.Layout.LayoutAlgorithms;

/// <summary>
/// 由 <c>aspect-ratio</c> 属性声明的首选纵横比（ISSUE-150），连同其尺寸换算所作用的盒。
/// <see cref="Ratio"/> 为 0 表示没有首选纵横比。
/// </summary>
internal readonly struct PreferredAspectRatio
{
    /// <summary>宽 / 高；0 = 无首选纵横比。</summary>
    public float Ratio { get; }

    /// <summary>换算是否作用于 border-box（仅 <c>&lt;ratio&gt;</c> + <c>box-sizing: border-box</c>）；否则作用于内容盒。</summary>
    public bool BorderBox { get; }

    public PreferredAspectRatio(float ratio, bool borderBox)
    {
        Ratio = ratio;
        BorderBox = borderBox;
    }

    public bool HasValue => Ratio > 0;

    /// <summary>由内容宽度推导内容高度（ratio-dependent 轴为高）。</summary>
    /// <param name="horizontalExtra">水平 padding + border。</param>
    /// <param name="verticalExtra">垂直 padding + border。</param>
    public float HeightFromWidth(float contentWidth, float horizontalExtra, float verticalExtra)
        => BorderBox
            ? Math.Max(0, (contentWidth + horizontalExtra) / Ratio - verticalExtra)
            : contentWidth / Ratio;

    /// <summary>由内容高度推导内容宽度（ratio-dependent 轴为宽）。</summary>
    public float WidthFromHeight(float contentHeight, float horizontalExtra, float verticalExtra)
        => BorderBox
            ? Math.Max(0, (contentHeight + verticalExtra) * Ratio - horizontalExtra)
            : contentHeight * Ratio;
}

/// <summary>
/// <c>aspect-ratio</c>（CSS Box Sizing 4 §4，ISSUE-150）的布局侧规则，供各布局算法共用：
/// <list type="bullet">
/// <item>首选纵横比只在 width / height 至少一边为 auto 时起作用：由已确定的一边（ratio-determining 轴）
///   按比值推导 auto 的一边（ratio-dependent 轴）。两边皆 auto 时，块级盒宽度照常填满/收缩，高度由宽度推导。</item>
/// <item>推导出的尺寸在输入确定时也是确定的——子孙的百分比尺寸可以据此解析。</item>
/// <item>换算基于 determining 轴夹取 min/max 之后的尺寸，dependent 轴再按自身 min/max 夹取。
///   这与规范「先换算首选尺寸、再把 min/max 换算过去」的结果一致（定尺寸不受换算约束影响）。</item>
/// <item>非替换、且块轴不是滚动容器（overflow-y: visible）的盒子，块轴自动最小尺寸为其内容高度：
///   内容放不下时盒子长高而不是溢出（§4.3）。Miko 的 min-height 初始值为 0 而非 auto，
///   无法用 <c>min-height: 0</c> 关闭这条规则——要保持比值不变，请把盒子设为 overflow 非 visible。</item>
/// </list>
/// 替换元素（img / video）的<b>自然</b>纵横比不经此处：各算法的 replaced 分支原样处理，
/// 此处只解析属性声明的比值（<c>&lt;ratio&gt;</c> 会覆盖自然纵横比）。
/// </summary>
internal static class AspectRatioSizing
{
    /// <summary>
    /// 解析 <paramref name="box"/> 由 <c>aspect-ratio</c> 声明的首选纵横比；不适用或为 auto 时返回无值。
    /// </summary>
    public static PreferredAspectRatio Resolve(LayoutBox box)
    {
        var style = box.ComputedStyle;
        var aspectRatio = style.AspectRatio;
        if (!aspectRatio.HasRatio) return default;

        // 规范：不适用于非替换 inline 盒、表格内部盒；文本节点不是元素盒。
        switch (box.Type)
        {
            case LayoutType.Text:
            case LayoutType.TableRow:
            case LayoutType.TableCell:
                return default;
            case LayoutType.Inline when !IsReplacedElement(box.Element):
                return default;
        }

        if (aspectRatio.HasAutoKeyword)
        {
            // auto && <ratio>：替换元素已有自然纵横比时用自然值（交给 replaced 分支），
            // 否则用给定比值；两种情况换算都作用于内容盒。
            var (naturalWidth, naturalHeight) = BlockLayout.GetReplacedIntrinsicSize(box.Element);
            if (naturalWidth > 0 && naturalHeight > 0) return default;
            return new PreferredAspectRatio(aspectRatio.Value, borderBox: false);
        }

        return new PreferredAspectRatio(aspectRatio.Value, borderBox: style.BoxSizing == BoxSizing.BorderBox);
    }

    /// <summary>
    /// 块轴推导出的高度是否以内容高度为下限（CSS Box Sizing 4 §4.3 自动最小尺寸）：
    /// 非替换元素，且块轴不是滚动容器。
    /// </summary>
    public static bool HasContentBasedMinimumHeight(LayoutBox box)
        => !IsReplacedElement(box.Element) && box.ComputedStyle.OverflowY == Overflow.Visible;

    /// <summary>
    /// 是否为替换元素（img / video）。与 <see cref="BlockLayout.GetReplacedIntrinsicSize"/> 不同，
    /// 不要求媒体已加载——未加载的 &lt;img&gt; 没有自然尺寸，但仍是替换元素，aspect-ratio 照样适用
    /// （这正是用它在图片加载前预留版面的用法）。
    /// </summary>
    public static bool IsReplacedElement(Core.Element element)
        => element is Core.DomElements.ImageElement or Core.DomElements.VideoElement;
}
