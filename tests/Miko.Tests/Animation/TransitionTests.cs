using Miko.Animation;
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
        var transition = Transition.For("opacity", 0.5f, TimingFunction.Linear, 0.1f);

        transition.Property.ShouldBe("opacity");
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
        var transition = new Transition("opacity", 1f, TimingFunction.CubicBezier)
        {
            CubicBezier = new CubicBezierParams(0.25f, 0.1f, 0.25f, 1f)
        };

        transition.CubicBezier.ShouldNotBeNull();
        transition.CubicBezier.Value.X1.ShouldBe(0.25f);
    }
}
