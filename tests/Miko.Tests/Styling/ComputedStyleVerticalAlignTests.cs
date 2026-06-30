using Miko.Common;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Styling;

public class ComputedStyleVerticalAlignTests
{
    [Fact]
    public void FromStyle_WithVerticalAlignSet_ResolvesVerticalAlign()
    {
        var style = new Style { VerticalAlign = VerticalAlign.Middle };

        var computed = ComputedStyle.FromStyle(style);

        computed.VerticalAlign.ShouldBe(VerticalAlign.Middle);
    }

    [Fact]
    public void FromStyle_WithoutVerticalAlign_DefaultsToBaseline()
    {
        // CSS default for vertical-align is baseline.
        var computed = ComputedStyle.FromStyle(new Style());

        computed.VerticalAlign.ShouldBe(VerticalAlign.Baseline);
    }

    [Fact]
    public void FromStyle_NullStyle_DefaultsToBaseline()
    {
        var computed = ComputedStyle.FromStyle(null);

        computed.VerticalAlign.ShouldBe(VerticalAlign.Baseline);
    }
}
