using System.Linq.Expressions;

namespace Miko.Styling;

public class MediaCondition
{
    private readonly Func<ViewportInfo, bool> _compiled;

    public Expression<Func<ViewportInfo, bool>> Expression { get; }

    public MediaCondition(Expression<Func<ViewportInfo, bool>> expression)
    {
        Expression = expression;
        _compiled = expression.Compile();
    }

    public bool Matches(ViewportInfo viewport) => _compiled(viewport);

    public static MediaCondition MinWidth(float value) =>
        new(v => v.Width >= value);

    public static MediaCondition MaxWidth(float value) =>
        new(v => v.Width <= value);

    public static MediaCondition MinHeight(float value) =>
        new(v => v.Height >= value);

    public static MediaCondition MaxHeight(float value) =>
        new(v => v.Height <= value);
}
