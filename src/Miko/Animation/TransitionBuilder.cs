namespace Miko.Animation;

public class TransitionBuilder
{
    private readonly string _property;
    private float _duration;
    private TimingFunction _timingFunction = TimingFunction.Ease;
    private CubicBezierParams? _cubicBezier;
    private float _delay;

    internal TransitionBuilder(string property) => _property = property;

    public TransitionBuilder Duration(float seconds) { _duration = seconds; return this; }
    public TransitionBuilder Ease() { _timingFunction = TimingFunction.Ease; return this; }
    public TransitionBuilder Linear() { _timingFunction = TimingFunction.Linear; return this; }
    public TransitionBuilder EaseIn() { _timingFunction = TimingFunction.EaseIn; return this; }
    public TransitionBuilder EaseOut() { _timingFunction = TimingFunction.EaseOut; return this; }
    public TransitionBuilder EaseInOut() { _timingFunction = TimingFunction.EaseInOut; return this; }

    public TransitionBuilder CubicBezier(float x1, float y1, float x2, float y2)
    {
        _timingFunction = TimingFunction.CubicBezier;
        _cubicBezier = new CubicBezierParams(x1, y1, x2, y2);
        return this;
    }

    public TransitionBuilder Delay(float seconds) { _delay = seconds; return this; }

    public Transition Build() => new(_property, _duration, _timingFunction, _delay) { CubicBezier = _cubicBezier };

    public static implicit operator Transition(TransitionBuilder builder) => builder.Build();
}
