using Miko.Common;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Styling;

public class FontSizeTests
{
    [Fact]
    public void FontSize_Px_ShouldResolveToSameValue()
    {
        var element = new DivElement
        {
            Style = new Style { FontSize = Length.Px(20) }
        };

        var computed = new StyleResolver().Resolve(element, []);
        computed.FontSize.Value.ShouldBe(20f);
        computed.FontSize.Unit.ShouldBe(LengthUnit.Px);
    }

    [Fact]
    public void FontSize_Rem_ShouldConvertToPixels()
    {
        var element = new DivElement
        {
            Style = new Style { FontSize = Length.Rem(1) }
        };

        var computed = new StyleResolver().Resolve(element, []);
        computed.FontSize.Value.ShouldBe(16f);
        computed.FontSize.Unit.ShouldBe(LengthUnit.Px);
    }

    [Fact]
    public void FontSize_Rem_2_ShouldConvertTo32Pixels()
    {
        var element = new DivElement
        {
            Style = new Style { FontSize = Length.Rem(2) }
        };

        var computed = new StyleResolver().Resolve(element, []);
        computed.FontSize.Value.ShouldBe(32f);
        computed.FontSize.Unit.ShouldBe(LengthUnit.Px);
    }
}
