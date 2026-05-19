using System.Linq.Expressions;
using Miko.Styling;

namespace Miko.Animation;

internal static class TransitionPropertyMapper
{
    public static string GetPropertyName<T>(Expression<Func<Style, T>> expression)
        => ((MemberExpression)expression.Body).Member.Name;
}
