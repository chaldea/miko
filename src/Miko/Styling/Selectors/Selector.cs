using Miko.Core;

namespace Miko.Styling.Selectors;

/// <summary>
/// 选择器基类
/// </summary>
public abstract class Selector
{
    public abstract bool Matches(Element element);
    public abstract int Specificity { get; }
}
