namespace Miko.Animation;

public enum TimingFunction
{
    Linear,
    Ease,
    EaseIn,
    EaseOut,
    EaseInOut,
    StepStart,
    StepEnd,
    CubicBezier
}

public enum AnimationDirection
{
    Normal,
    Reverse,
    Alternate,
    AlternateReverse
}

public enum AnimationFillMode
{
    None,
    Forwards,
    Backwards,
    Both
}

public enum AnimationPlayState
{
    Running,
    Paused
}
