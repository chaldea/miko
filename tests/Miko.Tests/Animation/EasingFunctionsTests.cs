using Miko.Animation;
using Shouldly;

namespace Miko.Tests.Animation;

public class EasingFunctionsTests
{
    [Fact]
    public void Linear_ShouldReturnInputDirectly()
    {
        EasingFunctions.Evaluate(TimingFunction.Linear, 0f).ShouldBe(0f);
        EasingFunctions.Evaluate(TimingFunction.Linear, 0.5f).ShouldBe(0.5f);
        EasingFunctions.Evaluate(TimingFunction.Linear, 1f).ShouldBe(1f);
    }

    [Fact]
    public void Ease_ShouldReturnBoundaryValues()
    {
        EasingFunctions.Evaluate(TimingFunction.Ease, 0f).ShouldBe(0f, 0.01f);
        EasingFunctions.Evaluate(TimingFunction.Ease, 1f).ShouldBe(1f, 0.01f);
    }

    [Fact]
    public void EaseIn_ShouldStartSlow()
    {
        float earlyValue = EasingFunctions.Evaluate(TimingFunction.EaseIn, 0.25f);
        earlyValue.ShouldBeLessThan(0.25f);
    }

    [Fact]
    public void EaseOut_ShouldEndSlow()
    {
        float lateValue = EasingFunctions.Evaluate(TimingFunction.EaseOut, 0.75f);
        lateValue.ShouldBeGreaterThan(0.75f);
    }

    [Fact]
    public void EaseInOut_ShouldBeSymmetric()
    {
        float midValue = EasingFunctions.Evaluate(TimingFunction.EaseInOut, 0.5f);
        midValue.ShouldBe(0.5f, 0.05f);
    }

    [Fact]
    public void CubicBezier_ShouldUseCustomParams()
    {
        var bezier = new CubicBezierParams(0f, 0f, 1f, 1f);
        float result = EasingFunctions.Evaluate(TimingFunction.CubicBezier, 0.5f, bezier);
        result.ShouldBe(0.5f, 0.01f);
    }

    [Fact]
    public void StepStart_ShouldJumpToOne()
    {
        EasingFunctions.Evaluate(TimingFunction.StepStart, 0.01f).ShouldBe(1f);
        EasingFunctions.Evaluate(TimingFunction.StepStart, 0.5f).ShouldBe(1f);
    }

    [Fact]
    public void StepEnd_ShouldStayAtZeroUntilEnd()
    {
        EasingFunctions.Evaluate(TimingFunction.StepEnd, 0.5f).ShouldBe(0f);
        EasingFunctions.Evaluate(TimingFunction.StepEnd, 1f).ShouldBe(1f);
    }
}
