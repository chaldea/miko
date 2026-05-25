using Miko.Styling;

namespace Miko.Core.DomElements;

public class PseudoElement : Element
{
    public override string TagName => "::pseudo";
    public PseudoElementType Type { get; set; }
}
