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

    internal (Selector selector, Style style) Build()
    {
        var selector = _selectors.Count == 1 ? _selectors[0] : new CompoundSelector(_selectors);
        return (selector, _style);
    }
}
