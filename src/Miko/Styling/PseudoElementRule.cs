using Miko.Styling.Selectors;

namespace Miko.Styling;

public enum PseudoElementType
{
    Before,
    After
}

public class PseudoElementRule
{
    public Selector Selector { get; set; } = null!;
    public PseudoElementType Type { get; set; }
    public Style Style { get; set; } = null!;
}
