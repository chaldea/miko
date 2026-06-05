using Miko.Animation;
using Miko.Common;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Animation;

public class TransitionTests
{
    [Fact]
    public void All_ShouldCreateTransitionForAllProperties()
    {
        var transition = Transition.All(0.3f);

        transition.Property.ShouldBe("all");
        transition.Duration.ShouldBe(0.3f);
        transition.TimingFunction.ShouldBe(TimingFunction.Ease);
        transition.Delay.ShouldBe(0f);
    }

    [Fact]
    public void For_ShouldCreateTransitionForSpecificProperty()
    {
        var transition = Transition.For(nameof(Style.Opacity), 0.5f, TimingFunction.Linear, 0.1f);

        transition.Property.ShouldBe(nameof(Style.Opacity));
        transition.Duration.ShouldBe(0.5f);
        transition.TimingFunction.ShouldBe(TimingFunction.Linear);
        transition.Delay.ShouldBe(0.1f);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var transition = new Transition();

        transition.Property.ShouldBe("all");
        transition.Duration.ShouldBe(0f);
        transition.TimingFunction.ShouldBe(TimingFunction.Ease);
        transition.Delay.ShouldBe(0f);
    }

    [Fact]
    public void CubicBezier_ShouldBeSettable()
    {
        var transition = new Transition(nameof(Style.Opacity), 1f, TimingFunction.CubicBezier)
        {
            CubicBezier = new CubicBezierParams(0.25f, 0.1f, 0.25f, 1f)
        };

        transition.CubicBezier.ShouldNotBeNull();
        transition.CubicBezier.Value.X1.ShouldBe(0.25f);
    }

    [Fact]
    public void FluentApi_ShouldCreateTransitionWithExpression()
    {
        Transition transition = Transition.For<float?>(x => x.Opacity).Duration(0.5f).Linear().Delay(0.1f);

        transition.Property.ShouldBe(nameof(Style.Opacity));
        transition.Duration.ShouldBe(0.5f);
        transition.TimingFunction.ShouldBe(TimingFunction.Linear);
        transition.Delay.ShouldBe(0.1f);
    }

    [Fact]
    public void FluentApi_ShouldSupportColorProperty()
    {
        Transition transition = Transition.For<Color?>(x => x.BackgroundColor).Duration(1f).EaseInOut();

        transition.Property.ShouldBe(nameof(Style.BackgroundColor));
        transition.TimingFunction.ShouldBe(TimingFunction.EaseInOut);
    }

    [Fact]
    public void FluentApi_ShouldSupportCubicBezier()
    {
        Transition transition = Transition.For<float?>(x => x.Opacity).Duration(0.3f).CubicBezier(0.25f, 0.1f, 0.25f, 1f);

        transition.TimingFunction.ShouldBe(TimingFunction.CubicBezier);
        transition.CubicBezier.ShouldNotBeNull();
        transition.CubicBezier.Value.X1.ShouldBe(0.25f);
    }

    [Fact]
    public void FluentApi_ShouldSupportImplicitConversion()
    {
        Transition transition = Transition.For<Length?>(x => x.Width).Duration(0.5f);

        transition.Property.ShouldBe(nameof(Style.Width));
        transition.Duration.ShouldBe(0.5f);
        transition.TimingFunction.ShouldBe(TimingFunction.Ease);
    }
}
