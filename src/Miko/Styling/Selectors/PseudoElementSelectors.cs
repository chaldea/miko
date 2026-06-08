using Miko.Core;

namespace Miko.Styling.Selectors;

/// <summary>
/// 伪元素选择器基类
/// </summary>
public abstract class PseudoElementSelector : Selector
{
    public override bool Matches(Element element)
    {
        // 伪元素不直接匹配元素，它们在渲染时生成虚拟内容
        // 在选择器匹配阶段，我们只需要返回 true 表示选择器本身有效
        return true;
    }

    public override int Specificity => 1; // 伪元素的特异性为 0,0,0,1
}

/// <summary>
/// ::before 伪元素选择器
/// </summary>
public class BeforePseudoElement : PseudoElementSelector
{
}

/// <summary>
/// ::after 伪元素选择器
/// </summary>
public class AfterPseudoElement : PseudoElementSelector
{
}

/// <summary>
/// ::range-thumb 伪元素选择器（input[type="range"] 滑块）
/// </summary>
public class RangeThumbPseudoElement : PseudoElementSelector
{
}

/// <summary>
/// ::range-track 伪元素选择器（input[type="range"] 滑轨）
/// </summary>
public class RangeTrackPseudoElement : PseudoElementSelector
{
}

/// <summary>
/// ::range-progress 伪元素选择器（input[type="range"] 已填充进度）
/// </summary>
public class RangeProgressPseudoElement : PseudoElementSelector
{
}
