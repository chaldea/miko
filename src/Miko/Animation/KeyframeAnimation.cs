using Miko.Styling;

namespace Miko.Animation;

public class Keyframe
{
    public float Offset { get; set; }
    public Style Style { get; set; } = new();

    public Keyframe() { }

    public Keyframe(float offset, Style style)
    {
        Offset = offset;
        Style = style;
    }
}

public class KeyframeAnimation
{
    public string Name { get; set; } = string.Empty;
    public List<Keyframe> Keyframes { get; set; } = new();
    public float Duration { get; set; }
    public TimingFunction TimingFunction { get; set; } = TimingFunction.Ease;
    public CubicBezierParams? CubicBezier { get; set; }
    public float Delay { get; set; }
    public int IterationCount { get; set; } = 1;
    public bool Infinite { get; set; }
    public AnimationDirection Direction { get; set; } = AnimationDirection.Normal;
    public AnimationFillMode FillMode { get; set; } = AnimationFillMode.None;
    public AnimationPlayState PlayState { get; set; } = AnimationPlayState.Running;

    public KeyframeAnimation() { }

    public KeyframeAnimation(string name, float duration, params Keyframe[] keyframes)
    {
        Name = name;
        Duration = duration;
        Keyframes.AddRange(keyframes);
    }
}
