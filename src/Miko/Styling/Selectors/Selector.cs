using Miko.Core;

namespace Miko.Styling.Selectors;

/// <summary>
/// 选择器基类
/// </summary>
public abstract class Selector
{
    public abstract bool Matches(Element element);
    public abstract int Specificity { get; }

    /// <summary>
    /// 匹配结果是否可能取决于元素的<b>文字内容</b>或<b>行内样式</b>（ISSUE-146）。
    ///
    /// <para>这两类变更不按「可能改变任意元素样式」记账：改文字只重排、替换行内样式只重算该元素
    /// 的子树（见 <see cref="MutationTracker"/>）。前提是没有选择器读取它们——样式表中出现
    /// 返回 true 的选择器时，引擎对这两类变更一律退回整树重算。</para>
    ///
    /// <para>默认 true：未知的自定义选择器可能读取任何东西，保守处理。只读类名、ID、标签、元素
    /// 状态或树结构的选择器应重写为 false；组合型选择器返回其各部分之或。<c>:empty</c> 只关心
    /// 空与非空，文本节点在两者之间切换时按样式变更记账，因此它也返回 false。</para>
    /// </summary>
    public virtual bool MayReadContentOrInlineStyle => true;
}
