using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Common;
using Miko.Core;
using Miko.Styling;

namespace Miko.Animation;

public class AnimatedProperty
{
    public string PropertyName { get; set; } = string.Empty;
    public float StartValue { get; set; }
    public float EndValue { get; set; }
    public Color? StartColor { get; set; }
    public Color? EndColor { get; set; }
    public Transform? StartTransform { get; set; }
    public Transform? EndTransform { get; set; }
}

internal class ActiveTransition
{
    public Element Element { get; set; } = null!;
    public AnimatedProperty Property { get; set; } = null!;
    public float Duration { get; set; }
    public float Delay { get; set; }
    public float ElapsedTime { get; set; }
    public TimingFunction TimingFunction { get; set; }
    public CubicBezierParams? CubicBezier { get; set; }
    public bool IsComplete => ElapsedTime >= Duration + Delay;
    public Action<Element, float>? ApplyFloat { get; set; }
    public Action<Element, Color>? ApplyColor { get; set; }
}

internal class ActiveAnimation
{
    public Element Element { get; set; } = null!;
    public KeyframeAnimation Definition { get; set; } = null!;
    public float ElapsedTime { get; set; }
    public int CurrentIteration { get; set; }
    public bool IsComplete { get; set; }
}

public class AnimationManager
{
    private readonly List<ActiveTransition> _transitions = new();
    private readonly List<ActiveAnimation> _animations = new();
    private readonly Dictionary<string, KeyframeAnimation> _registeredAnimations = new();
    private readonly Dictionary<Element, Dictionary<string, float>> _previousValues = new();
    private readonly Dictionary<Element, Dictionary<string, Color>> _previousColors = new();
    private ILogger _logger = NullLogger.Instance;

    public AnimationManager() { }

    public AnimationManager(ILogger<AnimationManager> logger) => _logger = logger;

    public void SetLogger(ILogger logger) => _logger = logger;

    public bool HasActiveAnimations => _transitions.Count > 0 || _animations.Count > 0;

    public void Clear()
    {
        _logger.LogDebug("AnimationManager.Clear: removing {Transitions} transitions, {Animations} animations",
            _transitions.Count, _animations.Count);
        _transitions.Clear();
        _animations.Clear();
    }

    public void RegisterAnimation(KeyframeAnimation animation)
    {
        _registeredAnimations[animation.Name] = animation;
        _logger.LogDebug("Animation registered: \"{Name}\", duration={Duration}s, keyframes={Count}",
            animation.Name, animation.Duration, animation.Keyframes.Count);
    }

    public void StartAnimation(Element element, string animationName)
    {
        if (!_registeredAnimations.TryGetValue(animationName, out var definition))
        {
            _logger.LogWarning("StartAnimation: animation \"{Name}\" not registered, element=<{Tag} id=\"{Id}\">",
                animationName, element.TagName, element.Id ?? "");
            return;
        }
        StartAnimation(element, definition);
    }

    public void StartAnimation(Element element, KeyframeAnimation definition)
    {
        _animations.RemoveAll(a => a.Element == element && a.Definition.Name == definition.Name);
        _animations.Add(new ActiveAnimation
        {
            Element = element,
            Definition = definition,
            ElapsedTime = 0,
            CurrentIteration = 0
        });
        _logger.LogDebug("Animation started: \"{Name}\" on <{Tag} id=\"{Id}\">, duration={Duration}s, infinite={Infinite}, direction={Direction}",
            definition.Name, element.TagName, element.Id ?? "", definition.Duration, definition.Infinite, definition.Direction);
    }

    public void StopAnimation(Element element, string? animationName = null)
    {
        if (animationName == null)
        {
            int count = _animations.RemoveAll(a => a.Element == element);
            _logger.LogDebug("StopAnimation: removed all ({Count}) animations from <{Tag} id=\"{Id}\">",
                count, element.TagName, element.Id ?? "");
        }
        else
        {
            int count = _animations.RemoveAll(a => a.Element == element && a.Definition.Name == animationName);
            _logger.LogDebug("StopAnimation: removed \"{Name}\" ({Count}) from <{Tag} id=\"{Id}\">",
                animationName, count, element.TagName, element.Id ?? "");
        }
    }

    public void TrackPropertyChange(Element element, string property, float oldValue, float newValue, Transition transition)
    {
        if (MathF.Abs(oldValue - newValue) < 1e-6f) return;

        _transitions.RemoveAll(t => t.Element == element && t.Property.PropertyName == property);

        var activeTransition = new ActiveTransition
        {
            Element = element,
            Property = new AnimatedProperty
            {
                PropertyName = property,
                StartValue = oldValue,
                EndValue = newValue
            },
            Duration = transition.Duration,
            Delay = transition.Delay,
            TimingFunction = transition.TimingFunction,
            CubicBezier = transition.CubicBezier,
            ElapsedTime = 0,
            ApplyFloat = GetFloatApplier(property)
        };

        _transitions.Add(activeTransition);
        _logger.LogDebug("Transition started: \"{Property}\" {OldValue} -> {NewValue} on <{Tag} id=\"{Id}\">, duration={Duration}s, delay={Delay}s",
            property, oldValue, newValue, element.TagName, element.Id ?? "", transition.Duration, transition.Delay);
    }

    public void TrackColorChange(Element element, string property, Color oldColor, Color newColor, Transition transition)
    {
        if (oldColor.R == newColor.R && oldColor.G == newColor.G &&
            oldColor.B == newColor.B && oldColor.A == newColor.A) return;

        _transitions.RemoveAll(t => t.Element == element && t.Property.PropertyName == property);

        var activeTransition = new ActiveTransition
        {
            Element = element,
            Property = new AnimatedProperty
            {
                PropertyName = property,
                StartColor = oldColor,
                EndColor = newColor
            },
            Duration = transition.Duration,
            Delay = transition.Delay,
            TimingFunction = transition.TimingFunction,
            CubicBezier = transition.CubicBezier,
            ElapsedTime = 0,
            ApplyColor = GetColorApplier(property)
        };

        _transitions.Add(activeTransition);
        _logger.LogDebug("Color transition started: \"{Property}\" {OldColor} -> {NewColor} on <{Tag} id=\"{Id}\">, duration={Duration}s",
            property, oldColor, newColor, element.TagName, element.Id ?? "", transition.Duration);
    }

    public void Update(float deltaTime)
    {
        // _logger.LogTrace("AnimationManager.Update: deltaTime={DeltaTime}s, transitions={TransitionCount}, animations={AnimationCount}",
        //     deltaTime, _transitions.Count, _animations.Count);
        UpdateTransitions(deltaTime);
        UpdateAnimations(deltaTime);
    }

    private void UpdateTransitions(float deltaTime)
    {
        for (int i = _transitions.Count - 1; i >= 0; i--)
        {
            var transition = _transitions[i];
            transition.ElapsedTime += deltaTime;

            if (transition.ElapsedTime < transition.Delay) continue;

            float activeTime = transition.ElapsedTime - transition.Delay;
            float progress = Math.Clamp(activeTime / transition.Duration, 0f, 1f);
            float easedProgress = EasingFunctions.Evaluate(transition.TimingFunction, progress, transition.CubicBezier);

            if (transition.ApplyFloat != null)
            {
                float value = Lerp(transition.Property.StartValue, transition.Property.EndValue, easedProgress);
                transition.ApplyFloat(transition.Element, value);
            }
            else if (transition.ApplyColor != null && transition.Property.StartColor.HasValue && transition.Property.EndColor.HasValue)
            {
                var color = LerpColor(transition.Property.StartColor.Value, transition.Property.EndColor.Value, easedProgress);
                transition.ApplyColor(transition.Element, color);
            }

            transition.Element.IsDirty = true;

            if (transition.IsComplete)
            {
                _transitions.RemoveAt(i);
                _logger.LogDebug("Transition completed: \"{Property}\" on <{Tag} id=\"{Id}\">",
                    transition.Property.PropertyName, transition.Element.TagName, transition.Element.Id ?? "");
            }
        }
    }

    private void UpdateAnimations(float deltaTime)
    {
        for (int i = _animations.Count - 1; i >= 0; i--)
        {
            var anim = _animations[i];
            if (anim.Definition.PlayState == AnimationPlayState.Paused)
            {
                _logger.LogTrace("Animation \"{Name}\" paused, skipping", anim.Definition.Name);
                continue;
            }

            anim.ElapsedTime += deltaTime;

            float activeTime = anim.ElapsedTime - anim.Definition.Delay;
            if (activeTime < 0)
            {
                _logger.LogTrace("Animation \"{Name}\" in delay phase, remaining={Remaining}s",
                    anim.Definition.Name, -activeTime);
                if (anim.Definition.FillMode == AnimationFillMode.Backwards || anim.Definition.FillMode == AnimationFillMode.Both)
                {
                    ApplyKeyframeAtProgress(anim, 0f);
                }
                continue;
            }

            float iterationDuration = anim.Definition.Duration;
            if (iterationDuration <= 0)
            {
                _logger.LogWarning("Animation \"{Name}\" has zero duration, removing", anim.Definition.Name);
                anim.IsComplete = true;
                _animations.RemoveAt(i);
                continue;
            }

            float rawProgress = activeTime / iterationDuration;
            int iteration = (int)MathF.Floor(rawProgress);

            if (!anim.Definition.Infinite && iteration >= anim.Definition.IterationCount)
            {
                if (anim.Definition.FillMode == AnimationFillMode.Forwards || anim.Definition.FillMode == AnimationFillMode.Both)
                {
                    float finalProgress = GetDirectionalProgress(1f, anim.Definition.IterationCount - 1, anim.Definition.Direction);
                    ApplyKeyframeAtProgress(anim, finalProgress);
                }
                anim.IsComplete = true;
                _animations.RemoveAt(i);
                _logger.LogDebug("Animation completed: \"{Name}\" on <{Tag} id=\"{Id}\">, iterations={Iterations}",
                    anim.Definition.Name, anim.Element.TagName, anim.Element.Id ?? "", iteration);
                continue;
            }

            anim.CurrentIteration = iteration;
            float localProgress = rawProgress - iteration;
            float directionalProgress = GetDirectionalProgress(localProgress, iteration, anim.Definition.Direction);
            float easedProgress = EasingFunctions.Evaluate(anim.Definition.TimingFunction, directionalProgress, anim.Definition.CubicBezier);

            _logger.LogTrace("Animation \"{Name}\": iteration={Iteration}, progress={Progress:F3}, eased={Eased:F3}",
                anim.Definition.Name, iteration, localProgress, easedProgress);

            ApplyKeyframeAtProgress(anim, easedProgress);
            anim.Element.IsDirty = true;
        }
    }

    private static float GetDirectionalProgress(float progress, int iteration, AnimationDirection direction)
    {
        return direction switch
        {
            AnimationDirection.Normal => progress,
            AnimationDirection.Reverse => 1f - progress,
            AnimationDirection.Alternate => iteration % 2 == 0 ? progress : 1f - progress,
            AnimationDirection.AlternateReverse => iteration % 2 == 0 ? 1f - progress : progress,
            _ => progress
        };
    }

    private void ApplyKeyframeAtProgress(ActiveAnimation anim, float progress)
    {
        var keyframes = anim.Definition.Keyframes;
        if (keyframes.Count == 0) return;

        Keyframe? from = null;
        Keyframe? to = null;

        for (int k = 0; k < keyframes.Count - 1; k++)
        {
            if (progress >= keyframes[k].Offset && progress <= keyframes[k + 1].Offset)
            {
                from = keyframes[k];
                to = keyframes[k + 1];
                break;
            }
        }

        from ??= keyframes[0];
        to ??= keyframes[^1];

        float segmentLength = to.Offset - from.Offset;
        float segmentProgress = segmentLength > 0 ? (progress - from.Offset) / segmentLength : 1f;

        ApplyInterpolatedStyle(anim.Element, from.Style, to.Style, segmentProgress);
    }

    private void ApplyInterpolatedStyle(Element element, Style from, Style to, float progress)
    {
        element.Style ??= new Style();

        if (from.Opacity.HasValue || to.Opacity.HasValue)
        {
            float fromVal = from.Opacity ?? 1f;
            float toVal = to.Opacity ?? 1f;
            element.Style.Opacity = Lerp(fromVal, toVal, progress);
        }

        if (from.Width.HasValue || to.Width.HasValue)
        {
            var fromVal = from.Width ?? Length.Px(0);
            var toVal = to.Width ?? Length.Px(0);
            if (fromVal.Unit == toVal.Unit && !fromVal.IsAuto && !toVal.IsAuto)
                element.Style.Width = new Length(Lerp(fromVal.Value, toVal.Value, progress), fromVal.Unit);
        }

        if (from.Height.HasValue || to.Height.HasValue)
        {
            var fromVal = from.Height ?? Length.Px(0);
            var toVal = to.Height ?? Length.Px(0);
            if (fromVal.Unit == toVal.Unit && !fromVal.IsAuto && !toVal.IsAuto)
                element.Style.Height = new Length(Lerp(fromVal.Value, toVal.Value, progress), fromVal.Unit);
        }

        InterpolateLengthProperty(element, from.MarginTop, to.MarginTop, progress, (s, v) => s.MarginTop = v);
        InterpolateLengthProperty(element, from.MarginRight, to.MarginRight, progress, (s, v) => s.MarginRight = v);
        InterpolateLengthProperty(element, from.MarginBottom, to.MarginBottom, progress, (s, v) => s.MarginBottom = v);
        InterpolateLengthProperty(element, from.MarginLeft, to.MarginLeft, progress, (s, v) => s.MarginLeft = v);

        InterpolateLengthProperty(element, from.PaddingTop, to.PaddingTop, progress, (s, v) => s.PaddingTop = v);
        InterpolateLengthProperty(element, from.PaddingRight, to.PaddingRight, progress, (s, v) => s.PaddingRight = v);
        InterpolateLengthProperty(element, from.PaddingBottom, to.PaddingBottom, progress, (s, v) => s.PaddingBottom = v);
        InterpolateLengthProperty(element, from.PaddingLeft, to.PaddingLeft, progress, (s, v) => s.PaddingLeft = v);

        InterpolateLengthProperty(element, from.Top, to.Top, progress, (s, v) => s.Top = v);
        InterpolateLengthProperty(element, from.Right, to.Right, progress, (s, v) => s.Right = v);
        InterpolateLengthProperty(element, from.Bottom, to.Bottom, progress, (s, v) => s.Bottom = v);
        InterpolateLengthProperty(element, from.Left, to.Left, progress, (s, v) => s.Left = v);

        InterpolateLengthProperty(element, from.FontSize, to.FontSize, progress, (s, v) => s.FontSize = v);
        InterpolateLengthProperty(element, from.BorderWidth, to.BorderWidth, progress, (s, v) => s.BorderWidth = v);

        InterpolateLengthProperty(element, from.BorderTopLeftRadius, to.BorderTopLeftRadius, progress, (s, v) => s.BorderTopLeftRadius = v);
        InterpolateLengthProperty(element, from.BorderTopRightRadius, to.BorderTopRightRadius, progress, (s, v) => s.BorderTopRightRadius = v);
        InterpolateLengthProperty(element, from.BorderBottomRightRadius, to.BorderBottomRightRadius, progress, (s, v) => s.BorderBottomRightRadius = v);
        InterpolateLengthProperty(element, from.BorderBottomLeftRadius, to.BorderBottomLeftRadius, progress, (s, v) => s.BorderBottomLeftRadius = v);

        if (from.BackgroundColor.HasValue || to.BackgroundColor.HasValue)
        {
            var fromColor = from.BackgroundColor ?? Color.Transparent;
            var toColor = to.BackgroundColor ?? Color.Transparent;
            element.Style.BackgroundColor = LerpColor(fromColor, toColor, progress);
        }

        if (from.Color.HasValue || to.Color.HasValue)
        {
            var fromColor = from.Color ?? Color.Black;
            var toColor = to.Color ?? Color.Black;
            element.Style.Color = LerpColor(fromColor, toColor, progress);
        }

        if (from.BorderColor.HasValue || to.BorderColor.HasValue)
        {
            var fromColor = from.BorderColor ?? Color.Transparent;
            var toColor = to.BorderColor ?? Color.Transparent;
            element.Style.BorderColor = LerpColor(fromColor, toColor, progress);
        }
    }

    private static void InterpolateLengthProperty(Element element, Length? from, Length? to, float progress, Action<Style, Length> setter)
    {
        if (!from.HasValue && !to.HasValue) return;
        var fromVal = from ?? Length.Px(0);
        var toVal = to ?? Length.Px(0);
        if (fromVal.Unit != toVal.Unit || fromVal.IsAuto || toVal.IsAuto) return;
        setter(element.Style!, new Length(Lerp(fromVal.Value, toVal.Value, progress), fromVal.Unit));
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static Color LerpColor(Color a, Color b, float t)
    {
        return new Color(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t)
        );
    }

    private static Action<Element, float>? GetFloatApplier(string property)
    {
        return property switch
        {
            "opacity" => (e, v) => { e.Style ??= new Style(); e.Style.Opacity = v; },
            "width" => (e, v) => { e.Style ??= new Style(); e.Style.Width = Length.Px(v); },
            "height" => (e, v) => { e.Style ??= new Style(); e.Style.Height = Length.Px(v); },
            "margin-top" => (e, v) => { e.Style ??= new Style(); e.Style.MarginTop = Length.Px(v); },
            "margin-right" => (e, v) => { e.Style ??= new Style(); e.Style.MarginRight = Length.Px(v); },
            "margin-bottom" => (e, v) => { e.Style ??= new Style(); e.Style.MarginBottom = Length.Px(v); },
            "margin-left" => (e, v) => { e.Style ??= new Style(); e.Style.MarginLeft = Length.Px(v); },
            "padding-top" => (e, v) => { e.Style ??= new Style(); e.Style.PaddingTop = Length.Px(v); },
            "padding-right" => (e, v) => { e.Style ??= new Style(); e.Style.PaddingRight = Length.Px(v); },
            "padding-bottom" => (e, v) => { e.Style ??= new Style(); e.Style.PaddingBottom = Length.Px(v); },
            "padding-left" => (e, v) => { e.Style ??= new Style(); e.Style.PaddingLeft = Length.Px(v); },
            "top" => (e, v) => { e.Style ??= new Style(); e.Style.Top = Length.Px(v); },
            "right" => (e, v) => { e.Style ??= new Style(); e.Style.Right = Length.Px(v); },
            "bottom" => (e, v) => { e.Style ??= new Style(); e.Style.Bottom = Length.Px(v); },
            "left" => (e, v) => { e.Style ??= new Style(); e.Style.Left = Length.Px(v); },
            "font-size" => (e, v) => { e.Style ??= new Style(); e.Style.FontSize = Length.Px(v); },
            "border-width" => (e, v) => { e.Style ??= new Style(); e.Style.BorderWidth = Length.Px(v); },
            "flex-grow" => (e, v) => { e.Style ??= new Style(); e.Style.FlexGrow = v; },
            "flex-shrink" => (e, v) => { e.Style ??= new Style(); e.Style.FlexShrink = v; },
            _ => null
        };
    }

    private static Action<Element, Color>? GetColorApplier(string property)
    {
        return property switch
        {
            "background-color" => (e, c) => { e.Style ??= new Style(); e.Style.BackgroundColor = c; },
            "color" => (e, c) => { e.Style ??= new Style(); e.Style.Color = c; },
            "border-color" => (e, c) => { e.Style ??= new Style(); e.Style.BorderColor = c; },
            _ => null
        };
    }
}
