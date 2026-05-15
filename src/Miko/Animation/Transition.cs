namespace Miko.Animation;

public record struct CubicBezierParams(float X1, float Y1, float X2, float Y2);

public class Transition
{
    public string Property { get; set; } = "all";
    public float Duration { get; set; }
    public TimingFunction TimingFunction { get; set; } = TimingFunction.Ease;
    public CubicBezierParams? CubicBezier { get; set; }
    public float Delay { get; set; }

    public Transition() { }

    public Transition(string property, float duration, TimingFunction timingFunction = TimingFunction.Ease, float delay = 0)
    {
        Property = property;
        Duration = duration;
        TimingFunction = timingFunction;
        Delay = delay;
    }

    public static Transition All(float duration, TimingFunction timingFunction = TimingFunction.Ease, float delay = 0)
        => new("all", duration, timingFunction, delay);

    public static Transition For(string property, float duration, TimingFunction timingFunction = TimingFunction.Ease, float delay = 0)
        => new(property, duration, timingFunction, delay);
}
