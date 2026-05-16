using Miko.Core;

namespace Miko.Styling.Selectors;

/// <summary>
/// 后代选择器 (A B) - 匹配作为A后代的B元素
/// </summary>
public class DescendantSelector : Selector
{
    public Selector Ancestor { get; }
    public Selector Descendant { get; }

    public DescendantSelector(Selector ancestor, Selector descendant)
    {
        Ancestor = ancestor;
        Descendant = descendant;
    }

    public override bool Matches(Element element)
    {
        if (!Descendant.Matches(element)) return false;

        var current = element.Parent;
        while (current != null)
        {
            if (Ancestor.Matches(current)) return true;
            current = current.Parent;
        }
        return false;
    }

    public override int Specificity => Ancestor.Specificity + Descendant.Specificity;
}

/// <summary>
/// 子选择器 (A > B) - 匹配作为A直接子元素的B元素
/// </summary>
public class ChildSelector : Selector
{
    public Selector Parent { get; }
    public Selector Child { get; }

    public ChildSelector(Selector parent, Selector child)
    {
        Parent = parent;
        Child = child;
    }

    public override bool Matches(Element element)
    {
        if (!Child.Matches(element)) return false;
        return element.Parent != null && Parent.Matches(element.Parent);
    }

    public override int Specificity => Parent.Specificity + Child.Specificity;
}

/// <summary>
/// 相邻兄弟选择器 (A + B) - 匹配紧跟在A后面的B元素
/// </summary>
public class AdjacentSiblingSelector : Selector
{
    public Selector Previous { get; }
    public Selector Target { get; }

    public AdjacentSiblingSelector(Selector previous, Selector target)
    {
        Previous = previous;
        Target = target;
    }

    public override bool Matches(Element element)
    {
        if (!Target.Matches(element)) return false;
        if (element.Parent == null) return false;

        var siblings = element.Parent.Children;
        var index = siblings.IndexOf(element);
        return index > 0 && Previous.Matches(siblings[index - 1]);
    }

    public override int Specificity => Previous.Specificity + Target.Specificity;
}

/// <summary>
/// 通用兄弟选择器 (A ~ B) - 匹配A之后的所有兄弟B元素
/// </summary>
public class GeneralSiblingSelector : Selector
{
    public Selector Previous { get; }
    public Selector Target { get; }

    public GeneralSiblingSelector(Selector previous, Selector target)
    {
        Previous = previous;
        Target = target;
    }

    public override bool Matches(Element element)
    {
        if (!Target.Matches(element)) return false;
        if (element.Parent == null) return false;

        var siblings = element.Parent.Children;
        var index = siblings.IndexOf(element);

        for (var i = 0; i < index; i++)
        {
            if (Previous.Matches(siblings[i])) return true;
        }
        return false;
    }

    public override int Specificity => Previous.Specificity + Target.Specificity;
}
