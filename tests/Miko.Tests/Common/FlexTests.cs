using Miko.Common;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Common;

public class FlexTests
{
    [Fact]
    public void Constructor_ShouldSetAllComponents()
    {
        var flex = new Flex(1, 1, Length.Px(150));

        flex.Grow.ShouldBe(1);
        flex.Shrink.ShouldBe(1);
        flex.Basis.ShouldBe(Length.Px(150));
    }

    [Fact]
    public void None_ShouldBeZeroZeroAuto()
    {
        var flex = Flex.None;

        flex.Grow.ShouldBe(0);
        flex.Shrink.ShouldBe(0);
        flex.Basis.IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void Auto_ShouldBeOneOneAuto()
    {
        var flex = Flex.Auto;

        flex.Grow.ShouldBe(1);
        flex.Shrink.ShouldBe(1);
        flex.Basis.IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void ImplicitFromInt_ShouldExpandToNOneZeroPercent()
    {
        Flex flex = 2;

        flex.Grow.ShouldBe(2);
        flex.Shrink.ShouldBe(1);
        flex.Basis.ShouldBe(Length.Percent(0));
    }

    [Fact]
    public void ImplicitFromFloat_ShouldExpandToNOneZeroPercent()
    {
        Flex flex = 1.5f;

        flex.Grow.ShouldBe(1.5f);
        flex.Shrink.ShouldBe(1);
        flex.Basis.ShouldBe(Length.Percent(0));
    }

    [Fact]
    public void StyleFlex_WithFlexValue_ShouldSetSubProperties()
    {
        var style = new Style { Flex = new Flex(1, 1, Length.Px(150)) };

        style.FlexGrow.ShouldBe(1);
        style.FlexShrink.ShouldBe(1);
        style.FlexBasis.ShouldBe(Length.Px(150));
    }

    [Fact]
    public void StyleFlex_WithSingleValue_ShouldRemainCompatible()
    {
        var style = new Style { Flex = 1 };

        style.FlexGrow.ShouldBe(1);
        style.FlexShrink.ShouldBe(1);
        style.FlexBasis.ShouldBe(Length.Percent(0));
    }

    [Fact]
    public void StyleFlex_None_ShouldSetZeroZeroAuto()
    {
        var style = new Style { Flex = Flex.None };

        style.FlexGrow.ShouldBe(0);
        style.FlexShrink.ShouldBe(0);
        style.FlexBasis!.Value.IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void StyleFlex_Auto_ShouldSetOneOneAuto()
    {
        var style = new Style { Flex = Flex.Auto };

        style.FlexGrow.ShouldBe(1);
        style.FlexShrink.ShouldBe(1);
        style.FlexBasis!.Value.IsAuto.ShouldBeTrue();
    }
}
