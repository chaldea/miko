using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Miko.Common;
using Miko.Core;
using Miko.Styling.Selectors;

namespace Miko.Styling;

internal class LambdaSelector(Func<Element, bool> predicate, int specificity) : Selector
{
    public override bool Matches(Element element) => predicate(element);
    public override int Specificity => specificity;
}

public class TypedStyleBuilder<T> where T : Element
{
    private readonly List<Selector> _selectors = new();
    private readonly Style _style = new();

    internal TypedStyleBuilder(Selector initial) => _selectors.Add(initial);

    public TypedStyleBuilder<T> Where(Expression<Func<T, bool>> predicate, int specificity = 10)
    {
        var fn = predicate.Compile();
        _selectors.Add(new LambdaSelector(e => e is T t && fn(t), specificity));
        return this;
    }

    public TypedStyleBuilder<T> Set<TValue>(Expression<Func<Style, TValue?>> prop, TValue value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public TypedStyleBuilder<T> Set(Expression<Func<Style, Padding>> prop, Padding value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public TypedStyleBuilder<T> Set(Expression<Func<Style, Margin>> prop, Margin value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public TypedStyleBuilder<T> Set(Expression<Func<Style, Border>> prop, Border value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public TypedStyleBuilder<T> Set(Expression<Func<Style, BorderSide>> prop, BorderSide value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public TypedStyleBuilder<T> Set(Expression<Func<Style, BorderRadius>> prop, BorderRadius value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public TypedStyleBuilder<T> Hover()    { _selectors.Add(new HoverSelector());    return this; }
    public TypedStyleBuilder<T> Active()   { _selectors.Add(new ActiveSelector());   return this; }
    public TypedStyleBuilder<T> Focus()    { _selectors.Add(new FocusSelector());    return this; }
    public TypedStyleBuilder<T> Disabled() { _selectors.Add(new DisabledSelector()); return this; }
    public TypedStyleBuilder<T> Enabled()  { _selectors.Add(new EnabledSelector());  return this; }
    public TypedStyleBuilder<T> FirstChild()  { _selectors.Add(new FirstChildSelector());  return this; }
    public TypedStyleBuilder<T> LastChild()   { _selectors.Add(new LastChildSelector());   return this; }
    public TypedStyleBuilder<T> FirstOfType() { _selectors.Add(new FirstOfTypeSelector()); return this; }
    public TypedStyleBuilder<T> LastOfType()  { _selectors.Add(new LastOfTypeSelector());  return this; }
    public TypedStyleBuilder<T> Not(Selector inner) { _selectors.Add(new NotSelector(inner)); return this; }

    /// <summary>
    /// 后代选择器 (A B) - 当前选择器作为祖先，匹配后代元素
    /// </summary>
    public CombinatorStyleBuilder<TTarget> Descendant<TTarget>(Selector targetSelector) where TTarget : Element
        => new(new DescendantSelector(BuildSelector(), targetSelector));

    /// <summary>
    /// 后代选择器 - 使用标签选择器作为后代目标
    /// </summary>
    public CombinatorStyleBuilder<TTarget> Descendant<TTarget>() where TTarget : Element, new()
        => Descendant<TTarget>(new TagSelector(new TTarget().TagName));

    /// <summary>
    /// 子选择器 (A > B) - 当前选择器作为父元素，匹配直接子元素
    /// </summary>
    public CombinatorStyleBuilder<TTarget> Child<TTarget>(Selector targetSelector) where TTarget : Element
        => new(new ChildSelector(BuildSelector(), targetSelector));

    /// <summary>
    /// 子选择器 - 使用标签选择器作为子元素目标
    /// </summary>
    public CombinatorStyleBuilder<TTarget> Child<TTarget>() where TTarget : Element, new()
        => Child<TTarget>(new TagSelector(new TTarget().TagName));

    /// <summary>
    /// 相邻兄弟选择器 (A + B) - 当前选择器作为前一个兄弟，匹配紧邻的下一个兄弟
    /// </summary>
    public CombinatorStyleBuilder<TTarget> Adjacent<TTarget>(Selector targetSelector) where TTarget : Element
        => new(new AdjacentSiblingSelector(BuildSelector(), targetSelector));

    /// <summary>
    /// 相邻兄弟选择器 - 使用标签选择器作为目标
    /// </summary>
    public CombinatorStyleBuilder<TTarget> Adjacent<TTarget>() where TTarget : Element, new()
        => Adjacent<TTarget>(new TagSelector(new TTarget().TagName));

    /// <summary>
    /// 通用兄弟选择器 (A ~ B) - 当前选择器作为前一个兄弟，匹配之后的所有兄弟
    /// </summary>
    public CombinatorStyleBuilder<TTarget> Sibling<TTarget>(Selector targetSelector) where TTarget : Element
        => new(new GeneralSiblingSelector(BuildSelector(), targetSelector));

    /// <summary>
    /// 通用兄弟选择器 - 使用标签选择器作为目标
    /// </summary>
    public CombinatorStyleBuilder<TTarget> Sibling<TTarget>() where TTarget : Element, new()
        => Sibling<TTarget>(new TagSelector(new TTarget().TagName));

    /// <summary>
    /// ::before 伪元素
    /// </summary>
    public PseudoElementStyleBuilder<T> Before()
        => new(BuildSelector(), PseudoElementType.Before);

    /// <summary>
    /// ::after 伪元素
    /// </summary>
    public PseudoElementStyleBuilder<T> After()
        => new(BuildSelector(), PseudoElementType.After);

    private Selector BuildSelector()
        => _selectors.Count == 1 ? _selectors[0] : new CompoundSelector(_selectors);

    internal (Selector selector, Style style) Build()
    {
        var selector = BuildSelector();
        return (selector, _style);
    }
}

/// <summary>
/// 组合器样式构建器，用于在组合器选择器上设置样式
/// </summary>
public class CombinatorStyleBuilder<T> where T : Element
{
    private Selector _selector;
    private readonly Style _style = new();

    internal CombinatorStyleBuilder(Selector selector) => _selector = selector;

    public CombinatorStyleBuilder<Element> Child(Selector child)
        => new(new ChildSelector(_selector, child));

    public CombinatorStyleBuilder<Element> Descendant(Selector descendant)
        => new(new DescendantSelector(_selector, descendant));

    public CombinatorStyleBuilder<T> Set<TValue>(Expression<Func<Style, TValue?>> prop, TValue value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public CombinatorStyleBuilder<T> Set(Expression<Func<Style, Padding>> prop, Padding value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public CombinatorStyleBuilder<T> Set(Expression<Func<Style, Margin>> prop, Margin value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public CombinatorStyleBuilder<T> Set(Expression<Func<Style, Border>> prop, Border value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public CombinatorStyleBuilder<T> Set(Expression<Func<Style, BorderSide>> prop, BorderSide value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public CombinatorStyleBuilder<T> Set(Expression<Func<Style, BorderRadius>> prop, BorderRadius value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    internal (Selector selector, Style style) Build() => (_selector, _style);
}

/// <summary>
/// 伪元素样式构建器
/// </summary>
public class PseudoElementStyleBuilder<T> where T : Element
{
    private readonly Selector _selector;
    private readonly PseudoElementType _type;
    private readonly Style _style = new();

    internal PseudoElementStyleBuilder(Selector selector, PseudoElementType type)
    {
        _selector = selector;
        _type = type;
    }

    public PseudoElementStyleBuilder<T> Set<TValue>(Expression<Func<Style, TValue?>> prop, TValue value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public PseudoElementStyleBuilder<T> Set(Expression<Func<Style, Padding>> prop, Padding value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public PseudoElementStyleBuilder<T> Set(Expression<Func<Style, Margin>> prop, Margin value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public PseudoElementStyleBuilder<T> Set(Expression<Func<Style, Border>> prop, Border value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public PseudoElementStyleBuilder<T> Set(Expression<Func<Style, BorderSide>> prop, BorderSide value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    public PseudoElementStyleBuilder<T> Set(Expression<Func<Style, BorderRadius>> prop, BorderRadius value)
    {
        ((PropertyInfo)((MemberExpression)prop.Body).Member).SetValue(_style, value);
        return this;
    }

    internal (Selector selector, PseudoElementType type, Style style) Build() => (_selector, _type, _style);
}
