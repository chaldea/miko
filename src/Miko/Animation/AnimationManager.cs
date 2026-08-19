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
    public Action<Element, Transform>? ApplyTransform { get; set; }
}

internal class ActiveAnimation
{
    public Element Element { get; set; } = null!;
    public KeyframeAnimation Definition { get; set; } = null!;
    public float ElapsedTime { get; set; }
    public int CurrentIteration { get; set; }
    public bool IsComplete { get; set; }

    /// <summary>
    /// 该动画是否由元素的 <c>Style.Animations</c> 声明而来（对应 CSS 的 <c>animation</c> 属性）。
    /// <para>声明式动画的存续<b>取决于样式里是否仍然声明它</b>：组件把动画从样式中撤下即应停止
    /// （ISSUE-127）。而经 <c>MikoEngine.StartAnimation</c> 命令式启动的动画从不出现在任何样式里，
    /// 其存续只由 <c>StopAnimation</c> 或自然播完决定——若一并按「是否仍被声明」剪枝，
    /// 会在下一帧就把它误杀。</para>
    /// </summary>
    public bool IsDeclarative { get; set; }

    /// <summary>
    /// 本帧该条目的元素引用是否被 <c>MigrateSupersededTargets</c> 前推过（即元素是重渲染换上来的
    /// 新实例）。用于区分「样式被原地改动」与「元素被整体替换」两种停止动画的场景：前者需要抹掉
    /// 遗留在行内样式上的动画值，后者的新实例行内样式本就干净，抹除反而会清掉它自己声明的值
    /// （ISSUE-127）。
    /// </summary>
    public bool WasMigrated { get; set; }
}

public class AnimationManager
{
    private readonly List<ActiveTransition> _transitions = new();
    private readonly List<ActiveAnimation> _animations = new();
    private readonly Dictionary<string, KeyframeAnimation> _registeredAnimations = new();
    private readonly Dictionary<Element, Dictionary<string, float>> _previousValues = new();
    private readonly Dictionary<Element, Dictionary<string, Color>> _previousColors = new();
    private readonly HashSet<(Element, string)> _recentlyCompleted = new();
    private ILogger _logger = NullLogger.Instance;

    public AnimationManager() { }

    public AnimationManager(ILogger<AnimationManager> logger) => _logger = logger;

    public void SetLogger(ILogger logger) => _logger = logger;

    public bool HasActiveAnimations => _transitions.Count > 0 || _animations.Count > 0;

    public int ActiveTransitionCount => _transitions.Count;

    /// <summary>
    /// Raised when a property transition finishes, with the animated element and the
    /// CSS property name (e.g. <c>nameof(Style.Opacity)</c>). Fired from
    /// <see cref="Update"/> on the render thread. Lets callers defer work until an
    /// animate-out completes (e.g. drop <c>display:none</c> after a fade-out).
    /// </summary>
    public event Action<Element, string>? TransitionCompleted;

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
        => StartAnimation(element, definition, isDeclarative: false);

    private void StartAnimation(Element element, KeyframeAnimation definition, bool isDeclarative)
    {
        _animations.RemoveAll(a => a.Element == element && a.Definition.Name == definition.Name);
        _animations.Add(new ActiveAnimation
        {
            Element = element,
            Definition = definition,
            ElapsedTime = 0,
            CurrentIteration = 0,
            IsDeclarative = isDeclarative
        });
        _logger.LogDebug("Animation started: \"{Name}\" on <{Tag} id=\"{Id}\">, duration={Duration}s, infinite={Infinite}, direction={Direction}",
            definition.Name, element.TagName, element.Id ?? "", definition.Duration, definition.Infinite, definition.Direction);
    }

    /// <summary>
    /// Starts an animation only if it's not already running. Used during re-renders to start
    /// animations on newly created elements without restarting animations that are already playing.
    ///
    /// <para>「已在运行」按 <c>(元素, 动画名)</c> 判定。命中时<b>不重启</b>（保住
    /// <c>ElapsedTime</c>），但会把条目的 <c>Definition</c> 换成本次重渲染声明的那一份——
    /// 同名动画的时长/播放态/关键帧都可能随参数变化（如 <c>animation-play-state</c> 切到
    /// paused），沿用旧定义等于让这些声明失效（ISSUE-127）。</para>
    /// </summary>
    public void StartAnimationIfNotRunning(Element element, KeyframeAnimation definition)
    {
        // 元素引用在本帧稍早的 MigrateSupersededTargets 中已前推到在场实例，
        // 故这里比引用即可命中重渲染前后的「同一个逻辑元素」。
        var running = _animations.FirstOrDefault(a => a.Element == element
                                                      && a.Definition.Name == definition.Name);
        if (running != null)
        {
            _logger.LogTrace("Animation \"{Name}\" already running on <{Tag} id=\"{Id}\">, refreshing definition",
                definition.Name, element.TagName, element.Id ?? "");
            // 只换定义，保留进度。ReferenceEquals 时是同一份对象，无需改写。
            if (!ReferenceEquals(running.Definition, definition))
                running.Definition = definition;
            // 命令式启动的动画若随后又被样式声明，转由声明接管其存续。
            running.IsDeclarative = true;
            return;
        }

        // Not running, start it
        StartAnimation(element, definition, isDeclarative: true);
    }

    /// <summary>
    /// 把动画/过渡条目持有的元素引用沿 <c>SupersededBy</c> 链前推到当前在场的实例（ISSUE-127）。
    ///
    /// <para>进度（<c>ElapsedTime</c>）活在条目上并以元素引用为键，而组件每次
    /// <c>StateHasChanged</c> 都产出<b>全新</b>元素实例。不迁移则同一个逻辑元素的动画每帧都被
    /// 判为「未运行」而重启——现场表现是任意子组件回调都把页面上所有动画打回起点。</para>
    ///
    /// <para>必须在<b>过渡检测之前</b>调用：检测按元素引用去重（见 <see cref="TrackPropertyChange"/>
    /// 的 <c>RemoveAll</c>），若此时条目还挂在旧实例上，替换元素上新检出的同一属性过渡会另起一条，
    /// 同一个属性上就并存两条相互覆写的过渡。</para>
    /// </summary>
    internal void MigrateSupersededTargets()
    {
        foreach (var anim in _animations)
        {
            var resolved = anim.Element.ResolveSuperseded();
            // 记下「元素被换过」：本帧若要停掉该动画，据此决定是否抹除行内遗留值。
            if (!ReferenceEquals(resolved, anim.Element))
            {
                anim.Element = resolved;
                anim.WasMigrated = true;
            }
        }

        foreach (var transition in _transitions)
            transition.Element = transition.Element.ResolveSuperseded();
    }

    /// <summary>
    /// 回收已经不该继续播放的条目（ISSUE-127）：元素彻底脱离 DOM，或元素仍在树上但本次重渲染
    /// 已不再声明该动画。
    ///
    /// <para><b>为什么必须回收</b>：此前没有任何清理，被替换掉的旧实例对应的条目永久留在列表里，
    /// 继续每帧写入一棵孤儿树的行内样式并 <see cref="Element.BumpMutationVersion"/>。既泄漏
    /// （旧子树被条目持有而无法回收），又让 <c>HasActiveAnimations</c> 恒为真，击穿 ISSUE-096
    /// 的稳态空闲。</para>
    ///
    /// <para><b>声明消失也要停</b>：<c>Style.Animations</c> 是声明式的，组件把某个动画从样式里
    /// 撤下（如骨架屏加载完成后去掉 shimmer）就等同于 CSS 里删掉 <c>animation</c> 属性，动画应当
    /// 停止。只按「是否脱离 DOM」剪枝会让它在元素原地不动的情况下永远播下去。</para>
    ///
    /// <para>顺序要紧：<b>先迁移再回收</b>。否则刚被重渲染替换掉的元素会被当成「已脱离」而连同
    /// 进度一起丢弃，等价于没修。过渡不参与「声明消失」这一支——过渡是一次性的属性插值，
    /// 跑完自然结束，其存续不取决于样式里是否仍声明 <c>transition</c>。</para>
    /// </summary>
    /// <param name="declaredAnimations">
    /// 本帧扫描到的、仍在 DOM 树中的元素 → 其 <c>Style.Animations</c> 声明的动画名集合。
    /// 不在字典中的元素即已脱离 DOM。
    /// </param>
    internal void PruneDetachedTargets(Dictionary<Element, HashSet<string>> declaredAnimations)
    {
        for (int i = _animations.Count - 1; i >= 0; i--)
        {
            var anim = _animations[i];
            bool attached = declaredAnimations.TryGetValue(anim.Element, out var names);

            // 命令式动画（MikoEngine.StartAnimation）从不出现在样式里，只在元素脱离 DOM 时回收；
            // 声明式动画还要求样式中仍然声明着它。
            if (attached && (!anim.IsDeclarative || names!.Contains(anim.Definition.Name)))
                continue;

            _animations.RemoveAt(i);
            // 声明被撤下、但元素<b>不是</b>重渲染换上来的新实例（原地改了 Style.Animations）：
            // 动画最后一帧的值仍留在它的行内样式里，会盖过样式级联给出的值（骨架屏撤掉
            // shimmer 后停在半路）。抹掉这些属性把控制权交还级联（ISSUE-127）。
            //
            // 反之，若元素是重渲染的替换实例，它的行内样式<b>本就是刚声明出来的干净状态</b>，
            // 陈旧值留在已被丢弃的旧实例上。此时抹除只会把替换实例自己声明的值一起清掉。
            if (attached && !anim.WasMigrated)
                ClearAnimatedProperties(anim);

            _logger.LogDebug("Animation \"{Name}\" removed: <{Tag} id=\"{Id}\"> left the DOM or no longer declares it",
                anim.Definition.Name, anim.Element.TagName, anim.Element.Id ?? "");
        }

        for (int i = _transitions.Count - 1; i >= 0; i--)
        {
            var transition = _transitions[i];
            if (declaredAnimations.ContainsKey(transition.Element)) continue;

            _transitions.RemoveAt(i);
            _logger.LogDebug("Transition \"{Property}\" removed: <{Tag} id=\"{Id}\"> left the DOM",
                transition.Property.PropertyName, transition.Element.TagName, transition.Element.Id ?? "");
        }
    }

    /// <summary>
    /// 把每条动画与过渡在当前进度上的值重新写入其元素的行内样式（ISSUE-127）。
    ///
    /// <para>重渲染后元素是全新实例，行内样式是<b>刚声明出来的原始状态</b>，而上一帧的插值写在
    /// 已被丢弃的旧实例上。若不补写：动画会在替换后的首帧画出起始态，下一次 <see cref="Update"/>
    /// 才跳回正确位置（每次组件回调都闪一下）；过渡则更糟——它的目标值本就来自新样式，
    /// 首帧会直接跳到终点，整段过渡被吃掉。</para>
    ///
    /// <para>由引擎在布局<b>之前</b>调用，使补写的值参与本帧的样式解析。</para>
    /// </summary>
    internal void ReapplyCurrentValues()
    {
        foreach (var anim in _animations)
        {
            if (anim.Definition.Duration <= 0) continue;

            float activeTime = anim.ElapsedTime - anim.Definition.Delay;
            if (activeTime < 0)
            {
                if (anim.Definition.FillMode is AnimationFillMode.Backwards or AnimationFillMode.Both)
                    ApplyKeyframeAtProgress(anim, 0f);
                continue;
            }

            float rawProgress = activeTime / anim.Definition.Duration;
            int iteration = (int)MathF.Floor(rawProgress);
            float localProgress = rawProgress - iteration;
            float directionalProgress = GetDirectionalProgress(localProgress, iteration, anim.Definition.Direction);
            float easedProgress = EasingFunctions.Evaluate(
                anim.Definition.TimingFunction, directionalProgress, anim.Definition.CubicBezier);
            ApplyKeyframeAtProgress(anim, easedProgress);
        }

        foreach (var transition in _transitions)
        {
            // 尚在 delay 阶段：起始值已在 TrackXxx 中写入，无需补写。
            if (transition.ElapsedTime < transition.Delay) continue;

            float activeTime = transition.ElapsedTime - transition.Delay;
            float progress = transition.Duration <= 0
                ? 1f
                : Math.Clamp(activeTime / transition.Duration, 0f, 1f);
            ApplyTransitionAtProgress(transition, progress);
        }
    }

    /// <summary>
    /// 把一条过渡在给定线性进度上的插值写入其元素。缓动与三种载荷（float/Color/Transform）的
    /// 分派与 <see cref="UpdateTransitions"/> 完全一致。
    /// </summary>
    private static void ApplyTransitionAtProgress(ActiveTransition transition, float progress)
    {
        float eased = EasingFunctions.Evaluate(transition.TimingFunction, progress, transition.CubicBezier);

        if (transition.ApplyFloat != null)
        {
            transition.ApplyFloat(transition.Element,
                Lerp(transition.Property.StartValue, transition.Property.EndValue, eased));
        }
        else if (transition.ApplyColor != null
                 && transition.Property.StartColor.HasValue && transition.Property.EndColor.HasValue)
        {
            transition.ApplyColor(transition.Element,
                LerpColor(transition.Property.StartColor.Value, transition.Property.EndColor.Value, eased));
        }
        else if (transition.ApplyTransform != null
                 && transition.Property.StartTransform != null && transition.Property.EndTransform != null)
        {
            transition.ApplyTransform(transition.Element,
                LerpTransform(transition.Property.StartTransform, transition.Property.EndTransform, eased));
        }
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

    public bool HasActiveTransition(Element element, string property)
    {
        if (_recentlyCompleted.Contains((element, property))) return true;
        return _transitions.Any(t => t.Element == element && t.Property.PropertyName == property);
    }

    public void TrackPropertyChange(Element element, string property, float oldValue, float newValue, Transition transition)
    {
        if (MathF.Abs(oldValue - newValue) < 1e-6f) return;

        _transitions.RemoveAll(t => t.Element == element && t.Property.PropertyName == property);

        var applier = GetFloatApplier(property);
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
            ApplyFloat = applier
        };

        _transitions.Add(activeTransition);

        // 立即应用起始值，避免首帧渲染目标值导致闪烁
        applier?.Invoke(element, oldValue);

        _logger.LogDebug("Transition started: \"{Property}\" {OldValue} -> {NewValue} on <{Tag} id=\"{Id}\">, duration={Duration}s, delay={Delay}s",
            property, oldValue, newValue, element.TagName, element.Id ?? "", transition.Duration, transition.Delay);
    }

    public void TrackColorChange(Element element, string property, Color oldColor, Color newColor, Transition transition)
    {
        if (oldColor.R == newColor.R && oldColor.G == newColor.G &&
            oldColor.B == newColor.B && oldColor.A == newColor.A) return;

        _transitions.RemoveAll(t => t.Element == element && t.Property.PropertyName == property);

        var applier = GetColorApplier(property);
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
            ApplyColor = applier
        };

        _transitions.Add(activeTransition);

        applier?.Invoke(element, oldColor);

        _logger.LogDebug("Color transition started: \"{Property}\" {OldColor} -> {NewColor} on <{Tag} id=\"{Id}\">, duration={Duration}s",
            property, oldColor, newColor, element.TagName, element.Id ?? "", transition.Duration);
    }

    public void TrackTransformChange(Element element, Transform oldTransform, Transform newTransform, Transition transition)
    {
        if (oldTransform == newTransform) return;

        _transitions.RemoveAll(t => t.Element == element && t.Property.PropertyName == nameof(Style.Transform));

        var activeTransition = new ActiveTransition
        {
            Element = element,
            Property = new AnimatedProperty
            {
                PropertyName = nameof(Style.Transform),
                StartTransform = oldTransform,
                EndTransform = newTransform
            },
            Duration = transition.Duration,
            Delay = transition.Delay,
            TimingFunction = transition.TimingFunction,
            CubicBezier = transition.CubicBezier,
            ElapsedTime = 0,
            ApplyTransform = (e, t) => { e.Style ??= new Style(); e.Style.Transform = t; }
        };

        _transitions.Add(activeTransition);
        activeTransition.ApplyTransform?.Invoke(element, oldTransform);

        _logger.LogDebug("Transform transition started on <{Tag} id=\"{Id}\">, duration={Duration}s",
            element.TagName, element.Id ?? "", transition.Duration);
    }

    public void TrackPropertyChangeWithApplier(Element element, string property, float oldValue, float newValue, Transition transition, Action<Element, float> applier)
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
            ApplyFloat = applier
        };

        _transitions.Add(activeTransition);
        applier(element, oldValue);
        // 起始值直接写入行内样式（绕过 Element.Style setter 的版本追踪），显式递增版本号（ISSUE-096）。
        Element.BumpMutationVersion();
    }

    public void TrackColorChangeWithApplier(Element element, string property, Color oldColor, Color newColor, Transition transition, Action<Element, Color> applier)
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
            ApplyColor = applier
        };

        _transitions.Add(activeTransition);
        applier(element, oldColor);
        Element.BumpMutationVersion();
    }

    public void TrackTransformChangeWithApplier(Element element, string property, Transform oldTransform, Transform newTransform, Transition transition, Action<Element, Transform> applier)
    {
        if (oldTransform == newTransform) return;

        _transitions.RemoveAll(t => t.Element == element && t.Property.PropertyName == property);

        var activeTransition = new ActiveTransition
        {
            Element = element,
            Property = new AnimatedProperty
            {
                PropertyName = property,
                StartTransform = oldTransform,
                EndTransform = newTransform
            },
            Duration = transition.Duration,
            Delay = transition.Delay,
            TimingFunction = transition.TimingFunction,
            CubicBezier = transition.CubicBezier,
            ElapsedTime = 0,
            ApplyTransform = applier
        };

        _transitions.Add(activeTransition);
        applier(element, oldTransform);
        Element.BumpMutationVersion();
    }

    public void Update(float deltaTime)
    {
        _recentlyCompleted.Clear();
        UpdateTransitions(deltaTime);
        UpdateAnimations(deltaTime);
        // 动画/过渡的帧值直接改写元素行内样式（绕过 Element.Style setter 的版本追踪），
        // 显式递增全局变更版本号，使下一帧布局重新解析样式（ISSUE-096）。
        if (_transitions.Count > 0 || _animations.Count > 0)
            Element.BumpMutationVersion();
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
            else if (transition.ApplyTransform != null && transition.Property.StartTransform != null && transition.Property.EndTransform != null)
            {
                var transform = LerpTransform(transition.Property.StartTransform, transition.Property.EndTransform, easedProgress);
                transition.ApplyTransform(transition.Element, transform);
            }

            transition.Element.IsDirty = true;

            if (transition.IsComplete)
            {
                _recentlyCompleted.Add((transition.Element, transition.Property.PropertyName));
                _transitions.RemoveAt(i);
                _logger.LogDebug("Transition completed: \"{Property}\" on <{Tag} id=\"{Id}\">",
                    transition.Property.PropertyName, transition.Element.TagName, transition.Element.Id ?? "");
                TransitionCompleted?.Invoke(transition.Element, transition.Property.PropertyName);
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

    /// <summary>
    /// 把某条动画曾经写入元素行内样式的属性统统抹回 <c>null</c>（＝未设置），
    /// 使这些属性重新由样式级联决定（ISSUE-127）。
    ///
    /// <para>动画帧值是直接写在元素行内样式上的，优先级最高。声明被撤下时若不抹掉，
    /// 元素就永远停在动画最后一帧的位置，盖过它自己声明的值。</para>
    ///
    /// <para>「曾经写入哪些属性」由该动画所有关键帧声明过的属性并集决定——与
    /// <see cref="ApplyInterpolatedStyle"/> 写入的集合一一对应。</para>
    /// </summary>
    private static void ClearAnimatedProperties(ActiveAnimation anim)
    {
        var style = anim.Element.Style;
        if (style == null) return;

        foreach (var keyframe in anim.Definition.Keyframes)
        {
            var k = keyframe.Style;

            if (k.Opacity.ValueOrNull() != null) style.Opacity = null;
            if (k.Width.ValueOrNull() != null) style.Width = null;
            if (k.Height.ValueOrNull() != null) style.Height = null;

            if (k.MarginTop.ValueOrNull() != null) style.MarginTop = null;
            if (k.MarginRight.ValueOrNull() != null) style.MarginRight = null;
            if (k.MarginBottom.ValueOrNull() != null) style.MarginBottom = null;
            if (k.MarginLeft.ValueOrNull() != null) style.MarginLeft = null;

            if (k.PaddingTop.ValueOrNull() != null) style.PaddingTop = null;
            if (k.PaddingRight.ValueOrNull() != null) style.PaddingRight = null;
            if (k.PaddingBottom.ValueOrNull() != null) style.PaddingBottom = null;
            if (k.PaddingLeft.ValueOrNull() != null) style.PaddingLeft = null;

            if (k.Top.ValueOrNull() != null) style.Top = null;
            if (k.Right.ValueOrNull() != null) style.Right = null;
            if (k.Bottom.ValueOrNull() != null) style.Bottom = null;
            if (k.Left.ValueOrNull() != null) style.Left = null;

            if (k.FontSize.ValueOrNull() != null) style.FontSize = null;
            if (k.BorderWidth.ValueOrNull() != null) style.BorderWidth = null;

            if (k.BorderTopLeftRadius.ValueOrNull() != null) style.BorderTopLeftRadius = null;
            if (k.BorderTopRightRadius.ValueOrNull() != null) style.BorderTopRightRadius = null;
            if (k.BorderBottomRightRadius.ValueOrNull() != null) style.BorderBottomRightRadius = null;
            if (k.BorderBottomLeftRadius.ValueOrNull() != null) style.BorderBottomLeftRadius = null;

            if (k.BackgroundColor.ValueOrNull() != null) style.BackgroundColor = null;
            if (k.Color.ValueOrNull() != null) style.Color = null;
            if (k.BorderColor.ValueOrNull() != null) style.BorderColor = null;

            if (k.Transform.RefValueOrNull() != null) style.Transform = null;
        }

        // 行内样式是布局输入，且这里绕过了 Element.Style setter 的版本追踪（ISSUE-096）。
        Element.BumpMutationVersion();
    }

    private void ApplyInterpolatedStyle(Element element, Style from, Style to, float progress)
    {
        element.Style ??= new Style();

        // 关键帧样式绕过 StyleResolver，无元素变量作用域上下文，因此持有变量引用的属性
        // 通过 ValueOrNull()/RefValueOrNull() 取回“具体值或 null”，为 null 时跳过插值。
        var fromOpacity = from.Opacity.ValueOrNull();
        var toOpacity = to.Opacity.ValueOrNull();
        if (fromOpacity != null || toOpacity != null)
        {
            float fromVal = fromOpacity ?? 1f;
            float toVal = toOpacity ?? 1f;
            element.Style.Opacity = Lerp(fromVal, toVal, progress);
        }

        InterpolateLengthProperty(element, from.Width.ValueOrNull(), to.Width.ValueOrNull(), progress, (s, v) => s.Width = v);
        InterpolateLengthProperty(element, from.Height.ValueOrNull(), to.Height.ValueOrNull(), progress, (s, v) => s.Height = v);

        InterpolateLengthProperty(element, from.MarginTop.ValueOrNull(), to.MarginTop.ValueOrNull(), progress, (s, v) => s.MarginTop = v);
        InterpolateLengthProperty(element, from.MarginRight.ValueOrNull(), to.MarginRight.ValueOrNull(), progress, (s, v) => s.MarginRight = v);
        InterpolateLengthProperty(element, from.MarginBottom.ValueOrNull(), to.MarginBottom.ValueOrNull(), progress, (s, v) => s.MarginBottom = v);
        InterpolateLengthProperty(element, from.MarginLeft.ValueOrNull(), to.MarginLeft.ValueOrNull(), progress, (s, v) => s.MarginLeft = v);

        InterpolateLengthProperty(element, from.PaddingTop.ValueOrNull(), to.PaddingTop.ValueOrNull(), progress, (s, v) => s.PaddingTop = v);
        InterpolateLengthProperty(element, from.PaddingRight.ValueOrNull(), to.PaddingRight.ValueOrNull(), progress, (s, v) => s.PaddingRight = v);
        InterpolateLengthProperty(element, from.PaddingBottom.ValueOrNull(), to.PaddingBottom.ValueOrNull(), progress, (s, v) => s.PaddingBottom = v);
        InterpolateLengthProperty(element, from.PaddingLeft.ValueOrNull(), to.PaddingLeft.ValueOrNull(), progress, (s, v) => s.PaddingLeft = v);

        InterpolateLengthProperty(element, from.Top.ValueOrNull(), to.Top.ValueOrNull(), progress, (s, v) => s.Top = v);
        InterpolateLengthProperty(element, from.Right.ValueOrNull(), to.Right.ValueOrNull(), progress, (s, v) => s.Right = v);
        InterpolateLengthProperty(element, from.Bottom.ValueOrNull(), to.Bottom.ValueOrNull(), progress, (s, v) => s.Bottom = v);
        InterpolateLengthProperty(element, from.Left.ValueOrNull(), to.Left.ValueOrNull(), progress, (s, v) => s.Left = v);

        InterpolateLengthProperty(element, from.FontSize.ValueOrNull(), to.FontSize.ValueOrNull(), progress, (s, v) => s.FontSize = v);
        InterpolateLengthProperty(element, from.BorderWidth.ValueOrNull(), to.BorderWidth.ValueOrNull(), progress, (s, v) => s.BorderWidth = v);

        InterpolateLengthProperty(element, from.BorderTopLeftRadius.ValueOrNull(), to.BorderTopLeftRadius.ValueOrNull(), progress, (s, v) => s.BorderTopLeftRadius = v);
        InterpolateLengthProperty(element, from.BorderTopRightRadius.ValueOrNull(), to.BorderTopRightRadius.ValueOrNull(), progress, (s, v) => s.BorderTopRightRadius = v);
        InterpolateLengthProperty(element, from.BorderBottomRightRadius.ValueOrNull(), to.BorderBottomRightRadius.ValueOrNull(), progress, (s, v) => s.BorderBottomRightRadius = v);
        InterpolateLengthProperty(element, from.BorderBottomLeftRadius.ValueOrNull(), to.BorderBottomLeftRadius.ValueOrNull(), progress, (s, v) => s.BorderBottomLeftRadius = v);

        var fromBg = from.BackgroundColor.ValueOrNull();
        var toBg = to.BackgroundColor.ValueOrNull();
        if (fromBg != null || toBg != null)
        {
            element.Style.BackgroundColor = LerpColor(fromBg ?? Color.Transparent, toBg ?? Color.Transparent, progress);
        }

        var fromColor = from.Color.ValueOrNull();
        var toColor = to.Color.ValueOrNull();
        if (fromColor != null || toColor != null)
        {
            element.Style.Color = LerpColor(fromColor ?? Color.Black, toColor ?? Color.Black, progress);
        }

        var fromBorderColor = from.BorderColor.ValueOrNull();
        var toBorderColor = to.BorderColor.ValueOrNull();
        if (fromBorderColor != null || toBorderColor != null)
        {
            element.Style.BorderColor = LerpColor(fromBorderColor ?? Color.Transparent, toBorderColor ?? Color.Transparent, progress);
        }

        var fromTransform = from.Transform.RefValueOrNull();
        var toTransform = to.Transform.RefValueOrNull();
        if (fromTransform != null || toTransform != null)
        {
            element.Style.Transform = LerpTransform(fromTransform ?? Transform.None, toTransform ?? Transform.None, progress);
        }
    }

    private static void InterpolateLengthProperty(Element element, Length? from, Length? to, float progress, Action<Style, Length> setter)
    {
        if (from == null && to == null) return;
        var fromVal = from?.Value ?? Length.Px(0);
        var toVal = to?.Value ?? Length.Px(0);
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

    internal static Transform LerpTransform(Transform from, Transform to, float t)
    {
        var result = new Transform();
        int count = Math.Max(from.Functions.Count, to.Functions.Count);

        for (int i = 0; i < count; i++)
        {
            var fromFn = i < from.Functions.Count ? from.Functions[i] : GetIdentity(to.Functions[i]);
            var toFn = i < to.Functions.Count ? to.Functions[i] : GetIdentity(from.Functions[i]);
            result.Functions.Add(LerpFunction(fromFn, toFn, t));
        }

        return result;
    }

    private static TransformFunction GetIdentity(TransformFunction reference)
    {
        return reference switch
        {
            TransformFunction.Translate tr => new TransformFunction.Translate(
                new Length(0, tr.X.Unit), new Length(0, tr.Y.Unit)),
            TransformFunction.TranslateX tx => new TransformFunction.TranslateX(
                new Length(0, tx.X.Unit)),
            TransformFunction.TranslateY ty => new TransformFunction.TranslateY(
                new Length(0, ty.Y.Unit)),
            TransformFunction.Scale => new TransformFunction.Scale(1f, 1f),
            TransformFunction.ScaleX => new TransformFunction.ScaleX(1f),
            TransformFunction.ScaleY => new TransformFunction.ScaleY(1f),
            TransformFunction.Rotate => new TransformFunction.Rotate(0f),
            TransformFunction.SkewX => new TransformFunction.SkewX(0f),
            TransformFunction.SkewY => new TransformFunction.SkewY(0f),
            TransformFunction.Skew => new TransformFunction.Skew(0f, 0f),
            TransformFunction.Matrix => new TransformFunction.Matrix(1, 0, 0, 1, 0, 0),
            _ => new TransformFunction.Rotate(0f)
        };
    }

    private static TransformFunction LerpFunction(TransformFunction from, TransformFunction to, float t)
    {
        return (from, to) switch
        {
            (TransformFunction.Rotate a, TransformFunction.Rotate b) =>
                new TransformFunction.Rotate(Lerp(a.Degrees, b.Degrees, t)),
            (TransformFunction.Scale a, TransformFunction.Scale b) =>
                new TransformFunction.Scale(Lerp(a.X, b.X, t), Lerp(a.Y, b.Y, t)),
            (TransformFunction.ScaleX a, TransformFunction.ScaleX b) =>
                new TransformFunction.ScaleX(Lerp(a.X, b.X, t)),
            (TransformFunction.ScaleY a, TransformFunction.ScaleY b) =>
                new TransformFunction.ScaleY(Lerp(a.Y, b.Y, t)),
            (TransformFunction.Translate a, TransformFunction.Translate b)
                when UnitsCompatible(a.X, b.X) && UnitsCompatible(a.Y, b.Y) =>
                new TransformFunction.Translate(
                    LerpLength(a.X, b.X, t),
                    LerpLength(a.Y, b.Y, t)),
            (TransformFunction.TranslateX a, TransformFunction.TranslateX b)
                when UnitsCompatible(a.X, b.X) =>
                new TransformFunction.TranslateX(LerpLength(a.X, b.X, t)),
            (TransformFunction.TranslateY a, TransformFunction.TranslateY b)
                when UnitsCompatible(a.Y, b.Y) =>
                new TransformFunction.TranslateY(LerpLength(a.Y, b.Y, t)),
            (TransformFunction.SkewX a, TransformFunction.SkewX b) =>
                new TransformFunction.SkewX(Lerp(a.Degrees, b.Degrees, t)),
            (TransformFunction.SkewY a, TransformFunction.SkewY b) =>
                new TransformFunction.SkewY(Lerp(a.Degrees, b.Degrees, t)),
            (TransformFunction.Skew a, TransformFunction.Skew b) =>
                new TransformFunction.Skew(Lerp(a.DegreesX, b.DegreesX, t), Lerp(a.DegreesY, b.DegreesY, t)),
            _ => t < 0.5f ? from : to
        };
    }

    /// <summary>
    /// 两个平移长度能否插值。单位相同即可插值；此外，值为 0 的长度视为与任意单位兼容——
    /// CSS 中零长度无单位（<c>0% == 0px</c>），但 <see cref="Length"/> 按分量存储，
    /// 任何全零长度的 <see cref="Length.Unit"/> 都回落为 Px。若不作此放宽，
    /// <c>translateY(100%) → translateY(0)</c> 这类常见组合会被判为单位不符，
    /// 落入 LerpFunction 的兜底分支而发生跳变（而非平滑过渡）。
    /// </summary>
    private static bool UnitsCompatible(Length a, Length b)
        => a.Unit == b.Unit || a.Value == 0f || b.Value == 0f;

    /// <summary>
    /// 插值两个平移长度，采用非零一侧的单位（零长度无单位，见 <see cref="UnitsCompatible"/>）。
    /// </summary>
    private static Length LerpLength(Length from, Length to, float t)
    {
        // 取非零一侧的单位：起点为 0 时用终点单位，否则用起点单位。
        var unit = from.Value == 0f && to.Value != 0f ? to.Unit : from.Unit;
        return new Length(Lerp(from.Value, to.Value, t), unit);
    }

    private static Action<Element, float>? GetFloatApplier(string property)
    {
        return property switch
        {
            nameof(Style.Opacity) => (e, v) => { e.Style ??= new Style(); e.Style.Opacity = v; },
            nameof(Style.Width) => (e, v) => { e.Style ??= new Style(); e.Style.Width = Length.Px(v); },
            nameof(Style.Height) => (e, v) => { e.Style ??= new Style(); e.Style.Height = Length.Px(v); },
            nameof(Style.MaxWidth) => (e, v) => { e.Style ??= new Style(); e.Style.MaxWidth = Length.Px(v); },
            nameof(Style.MaxHeight) => (e, v) => { e.Style ??= new Style(); e.Style.MaxHeight = Length.Px(v); },
            nameof(Style.MinWidth) => (e, v) => { e.Style ??= new Style(); e.Style.MinWidth = Length.Px(v); },
            nameof(Style.MinHeight) => (e, v) => { e.Style ??= new Style(); e.Style.MinHeight = Length.Px(v); },
            nameof(Style.MarginTop) => (e, v) => { e.Style ??= new Style(); e.Style.MarginTop = Length.Px(v); },
            nameof(Style.MarginRight) => (e, v) => { e.Style ??= new Style(); e.Style.MarginRight = Length.Px(v); },
            nameof(Style.MarginBottom) => (e, v) => { e.Style ??= new Style(); e.Style.MarginBottom = Length.Px(v); },
            nameof(Style.MarginLeft) => (e, v) => { e.Style ??= new Style(); e.Style.MarginLeft = Length.Px(v); },
            nameof(Style.PaddingTop) => (e, v) => { e.Style ??= new Style(); e.Style.PaddingTop = Length.Px(v); },
            nameof(Style.PaddingRight) => (e, v) => { e.Style ??= new Style(); e.Style.PaddingRight = Length.Px(v); },
            nameof(Style.PaddingBottom) => (e, v) => { e.Style ??= new Style(); e.Style.PaddingBottom = Length.Px(v); },
            nameof(Style.PaddingLeft) => (e, v) => { e.Style ??= new Style(); e.Style.PaddingLeft = Length.Px(v); },
            nameof(Style.Top) => (e, v) => { e.Style ??= new Style(); e.Style.Top = Length.Px(v); },
            nameof(Style.Right) => (e, v) => { e.Style ??= new Style(); e.Style.Right = Length.Px(v); },
            nameof(Style.Bottom) => (e, v) => { e.Style ??= new Style(); e.Style.Bottom = Length.Px(v); },
            nameof(Style.Left) => (e, v) => { e.Style ??= new Style(); e.Style.Left = Length.Px(v); },
            nameof(Style.FontSize) => (e, v) => { e.Style ??= new Style(); e.Style.FontSize = Length.Px(v); },
            nameof(Style.BorderWidth) => (e, v) => { e.Style ??= new Style(); e.Style.BorderWidth = Length.Px(v); },
            nameof(Style.FlexGrow) => (e, v) => { e.Style ??= new Style(); e.Style.FlexGrow = v; },
            nameof(Style.FlexShrink) => (e, v) => { e.Style ??= new Style(); e.Style.FlexShrink = v; },
            nameof(Style.BorderTopLeftRadius) => (e, v) => { e.Style ??= new Style(); e.Style.BorderTopLeftRadius = Length.Px(v); },
            nameof(Style.BorderTopRightRadius) => (e, v) => { e.Style ??= new Style(); e.Style.BorderTopRightRadius = Length.Px(v); },
            nameof(Style.BorderBottomRightRadius) => (e, v) => { e.Style ??= new Style(); e.Style.BorderBottomRightRadius = Length.Px(v); },
            nameof(Style.BorderBottomLeftRadius) => (e, v) => { e.Style ??= new Style(); e.Style.BorderBottomLeftRadius = Length.Px(v); },
            _ => null
        };
    }

    private static Action<Element, Color>? GetColorApplier(string property)
    {
        return property switch
        {
            nameof(Style.BackgroundColor) => (e, c) => { e.Style ??= new Style(); e.Style.BackgroundColor = c; },
            nameof(Style.Color) => (e, c) => { e.Style ??= new Style(); e.Style.Color = c; },
            nameof(Style.BorderColor) => (e, c) => { e.Style ??= new Style(); e.Style.BorderColor = c; },
            _ => null
        };
    }
}
