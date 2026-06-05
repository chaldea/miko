using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Animation;

public class AnimationManagerTests
{
    private readonly AnimationManager _manager = new();

    [Fact]
    public void HasActiveAnimations_ShouldBeFalseInitially()
    {
        _manager.HasActiveAnimations.ShouldBeFalse();
    }

    [Fact]
    public void TrackPropertyChange_ShouldCreateTransition()
    {
        var element = new DivElement { Style = new Style { Opacity = 1f } };
        var transition = Transition.For(nameof(Style.Opacity), 1f, TimingFunction.Linear);

        _manager.TrackPropertyChange(element, nameof(Style.Opacity), 1f, 0f, transition);

        _manager.HasActiveAnimations.ShouldBeTrue();
    }

    [Fact]
    public void Update_ShouldInterpolatePropertyOverTime()
    {
        var element = new DivElement { Style = new Style { Opacity = 1f } };
        var transition = Transition.For(nameof(Style.Opacity), 1f, TimingFunction.Linear);

        _manager.TrackPropertyChange(element, nameof(Style.Opacity), 1f, 0f, transition);

        _manager.Update(0.5f);
        element.Style!.Opacity!.Value.ShouldBe(0.5f, 0.01f);

        _manager.Update(0.5f);
        element.Style!.Opacity!.Value.ShouldBe(0f, 0.01f);
    }

    [Fact]
    public void Update_ShouldRespectDelay()
    {
        var element = new DivElement { Style = new Style { Opacity = 1f } };
        var transition = Transition.For(nameof(Style.Opacity), 1f, TimingFunction.Linear, delay: 0.5f);

        _manager.TrackPropertyChange(element, nameof(Style.Opacity), 1f, 0f, transition);

        _manager.Update(0.3f);
        element.Style!.Opacity!.Value.ShouldBe(1f);

        _manager.Update(0.7f);
        element.Style!.Opacity!.Value.ShouldBe(0.5f, 0.05f);
    }

    [Fact]
    public void TrackColorChange_ShouldInterpolateColor()
    {
        var element = new DivElement { Style = new Style { BackgroundColor = Color.Black } };
        var transition = Transition.For(nameof(Style.BackgroundColor), 1f, TimingFunction.Linear);

        _manager.TrackColorChange(element, nameof(Style.BackgroundColor), Color.Black, Color.White, transition);

        _manager.Update(0.5f);
        element.Style!.BackgroundColor!.Value.R.ShouldBeInRange((byte)120, (byte)135);
        element.Style!.BackgroundColor!.Value.G.ShouldBeInRange((byte)120, (byte)135);
        element.Style!.BackgroundColor!.Value.B.ShouldBeInRange((byte)120, (byte)135);
    }

    [Fact]
    public void Transition_ShouldCompleteAndRemove()
    {
        var element = new DivElement { Style = new Style { Opacity = 1f } };
        var transition = Transition.For(nameof(Style.Opacity), 0.5f, TimingFunction.Linear);

        _manager.TrackPropertyChange(element, nameof(Style.Opacity), 1f, 0f, transition);
        _manager.Update(1f);

        element.Style!.Opacity!.Value.ShouldBe(0f, 0.01f);
        _manager.HasActiveAnimations.ShouldBeFalse();
    }

    [Fact]
    public void StartAnimation_WithRegisteredName_ShouldActivate()
    {
        var element = new DivElement { Style = new Style() };
        var animation = new KeyframeAnimation("fadeIn", 1f,
            new Keyframe(0f, new Style { Opacity = 0f }),
            new Keyframe(1f, new Style { Opacity = 1f })
        )
        {
            TimingFunction = TimingFunction.Linear
        };

        _manager.RegisterAnimation(animation);
        _manager.StartAnimation(element, "fadeIn");

        _manager.HasActiveAnimations.ShouldBeTrue();
    }

    [Fact]
    public void KeyframeAnimation_ShouldInterpolateOverTime()
    {
        var element = new DivElement { Style = new Style() };
        var animation = new KeyframeAnimation("fadeIn", 1f,
            new Keyframe(0f, new Style { Opacity = 0f }),
            new Keyframe(1f, new Style { Opacity = 1f })
        )
        {
            TimingFunction = TimingFunction.Linear
        };

        _manager.StartAnimation(element, animation);
        _manager.Update(0.5f);

        element.Style!.Opacity!.Value.ShouldBe(0.5f, 0.05f);
    }

    [Fact]
    public void KeyframeAnimation_WithMultipleKeyframes_ShouldInterpolateBetweenPairs()
    {
        var element = new DivElement { Style = new Style() };
        var animation = new KeyframeAnimation("pulse", 1f,
            new Keyframe(0f, new Style { Opacity = 0f }),
            new Keyframe(0.5f, new Style { Opacity = 1f }),
            new Keyframe(1f, new Style { Opacity = 0f })
        )
        {
            TimingFunction = TimingFunction.Linear
        };

        _manager.StartAnimation(element, animation);

        _manager.Update(0.25f);
        element.Style!.Opacity!.Value.ShouldBe(0.5f, 0.05f);

        _manager.Update(0.25f);
        element.Style!.Opacity!.Value.ShouldBe(1f, 0.05f);

        _manager.Update(0.25f);
        element.Style!.Opacity!.Value.ShouldBe(0.5f, 0.05f);
    }

    [Fact]
    public void KeyframeAnimation_Infinite_ShouldLoop()
    {
        var element = new DivElement { Style = new Style() };
        var animation = new KeyframeAnimation("blink", 1f,
            new Keyframe(0f, new Style { Opacity = 0f }),
            new Keyframe(1f, new Style { Opacity = 1f })
        )
        {
            Infinite = true,
            TimingFunction = TimingFunction.Linear
        };

        _manager.StartAnimation(element, animation);

        _manager.Update(1f);
        _manager.HasActiveAnimations.ShouldBeTrue();

        _manager.Update(0.5f);
        element.Style!.Opacity!.Value.ShouldBe(0.5f, 0.05f);
    }

    [Fact]
    public void KeyframeAnimation_Alternate_ShouldReverseOnEvenIterations()
    {
        var element = new DivElement { Style = new Style() };
        var animation = new KeyframeAnimation("bounce", 1f,
            new Keyframe(0f, new Style { Opacity = 0f }),
            new Keyframe(1f, new Style { Opacity = 1f })
        )
        {
            IterationCount = 2,
            Direction = AnimationDirection.Alternate,
            TimingFunction = TimingFunction.Linear
        };

        _manager.StartAnimation(element, animation);

        _manager.Update(1f);
        element.Style!.Opacity!.Value.ShouldBe(1f, 0.05f);

        _manager.Update(0.5f);
        element.Style!.Opacity!.Value.ShouldBe(0.5f, 0.05f);
    }

    [Fact]
    public void StopAnimation_ShouldRemoveActiveAnimation()
    {
        var element = new DivElement { Style = new Style() };
        var animation = new KeyframeAnimation("fadeIn", 1f,
            new Keyframe(0f, new Style { Opacity = 0f }),
            new Keyframe(1f, new Style { Opacity = 1f })
        )
        {
            Infinite = true
        };

        _manager.StartAnimation(element, animation);
        _manager.HasActiveAnimations.ShouldBeTrue();

        _manager.StopAnimation(element, "fadeIn");
        _manager.HasActiveAnimations.ShouldBeFalse();
    }

    [Fact]
    public void TrackPropertyChange_ShouldReplaceExistingTransition()
    {
        var element = new DivElement { Style = new Style { Opacity = 1f } };
        var transition = Transition.For(nameof(Style.Opacity), 1f, TimingFunction.Linear);

        _manager.TrackPropertyChange(element, nameof(Style.Opacity), 1f, 0.5f, transition);
        _manager.TrackPropertyChange(element, nameof(Style.Opacity), 0.5f, 0f, transition);

        _manager.Update(1f);
        element.Style!.Opacity!.Value.ShouldBe(0f, 0.01f);
    }

    [Fact]
    public void KeyframeAnimation_FillModeForwards_ShouldRetainFinalState()
    {
        var element = new DivElement { Style = new Style() };
        var animation = new KeyframeAnimation("fadeIn", 0.5f,
            new Keyframe(0f, new Style { Opacity = 0f }),
            new Keyframe(1f, new Style { Opacity = 1f })
        )
        {
            FillMode = AnimationFillMode.Forwards,
            TimingFunction = TimingFunction.Linear
        };

        _manager.StartAnimation(element, animation);
        _manager.Update(1f);

        element.Style!.Opacity!.Value.ShouldBe(1f, 0.01f);
    }

    [Fact]
    public void KeyframeAnimation_ColorInterpolation_ShouldWork()
    {
        var element = new DivElement { Style = new Style() };
        var animation = new KeyframeAnimation("colorShift", 1f,
            new Keyframe(0f, new Style { BackgroundColor = Color.Red }),
            new Keyframe(1f, new Style { BackgroundColor = Color.Blue })
        )
        {
            TimingFunction = TimingFunction.Linear
        };

        _manager.StartAnimation(element, animation);
        _manager.Update(0.5f);

        element.Style!.BackgroundColor!.Value.R.ShouldBeInRange((byte)120, (byte)135);
        element.Style!.BackgroundColor!.Value.B.ShouldBeInRange((byte)120, (byte)135);
    }

    [Fact]
    public void TrackTransformChange_ShouldCreateTransition()
    {
        var element = new DivElement { Style = new Style() };
        var transition = Transition.For(nameof(Style.Transform), 1f, TimingFunction.Linear);
        var from = Transform.None;
        var to = Transform.FromRotate(180f);

        _manager.TrackTransformChange(element, from, to, transition);

        _manager.HasActiveAnimations.ShouldBeTrue();
    }

    [Fact]
    public void TrackTransformChange_ShouldInterpolateRotation()
    {
        var element = new DivElement { Style = new Style() };
        var transition = Transition.For(nameof(Style.Transform), 1f, TimingFunction.Linear);
        var from = Transform.FromRotate(0f);
        var to = Transform.FromRotate(180f);

        _manager.TrackTransformChange(element, from, to, transition);

        _manager.Update(0.5f);

        var transform = element.Style!.Transform!;
        transform.Functions.Count.ShouldBe(1);
        var rotate = transform.Functions[0].ShouldBeOfType<TransformFunction.Rotate>();
        rotate.Degrees.ShouldBe(90f, 1f);
    }

    [Fact]
    public void TrackTransformChange_ShouldCompleteAtEndValue()
    {
        var element = new DivElement { Style = new Style() };
        var transition = Transition.For(nameof(Style.Transform), 1f, TimingFunction.Linear);
        var from = Transform.FromRotate(0f);
        var to = Transform.FromRotate(-180f);

        _manager.TrackTransformChange(element, from, to, transition);

        _manager.Update(1f);

        var transform = element.Style!.Transform!;
        var rotate = transform.Functions[0].ShouldBeOfType<TransformFunction.Rotate>();
        rotate.Degrees.ShouldBe(-180f, 0.1f);
        _manager.HasActiveAnimations.ShouldBeFalse();
    }

    [Fact]
    public void LerpTransform_ShouldInterpolateScale()
    {
        var from = Transform.FromScale(1f, 1f);
        var to = Transform.FromScale(2f, 3f);

        var result = AnimationManager.LerpTransform(from, to, 0.5f);

        result.Functions.Count.ShouldBe(1);
        var scale = result.Functions[0].ShouldBeOfType<TransformFunction.Scale>();
        scale.X.ShouldBe(1.5f, 0.01f);
        scale.Y.ShouldBe(2f, 0.01f);
    }

    [Fact]
    public void LerpTransform_FromNone_ShouldInterpolateFromIdentity()
    {
        var from = Transform.None;
        var to = Transform.FromRotate(-180f);

        var result = AnimationManager.LerpTransform(from, to, 0.5f);

        result.Functions.Count.ShouldBe(1);
        var rotate = result.Functions[0].ShouldBeOfType<TransformFunction.Rotate>();
        rotate.Degrees.ShouldBe(-90f, 0.01f);
    }

    [Fact]
    public void TrackTransformChangeWithApplier_ShouldInterpolate()
    {
        var element = new DivElement { Style = new Style() };
        var transition = Transition.For("Transform", 1f, TimingFunction.Linear);
        var from = Transform.None;
        var to = Transform.FromRotate(-180f);

        Transform? applied = null;
        _manager.TrackTransformChangeWithApplier(element, "test.Transform", from, to, transition,
            (e, t) => { applied = t; });

        // Initial apply should be the start value
        applied.ShouldNotBeNull();
        applied!.Functions.Count.ShouldBe(0);

        _manager.Update(0.5f);

        applied.ShouldNotBeNull();
        applied!.Functions.Count.ShouldBe(1);
        var rotate = applied.Functions[0].ShouldBeOfType<TransformFunction.Rotate>();
        rotate.Degrees.ShouldBe(-90f, 1f);
    }
}
