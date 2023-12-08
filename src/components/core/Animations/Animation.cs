namespace Miko
{
    public class Animation
    {
        private static int _index = 0;
        private readonly int _instanceId = 0;
        private readonly Dictionary<string, string> _style = new();
        private string _keyframeName;
        private int _playCount = 0;
        private int _duration = 0;
        private int _delay = 0;
        private int _iterations = 1;
        private string _easing;
        private string _fill = "both";
        private string _direction = "normal";
        private string _styleSheet;
        private bool _initialized;
        private List<AnimationKeyframe>? _keyframes;

        public string Style => GetStyle();

        public string StyleSheet => _styleSheet;

        public Animation()
        {
            _instanceId = _index++;
            _keyframeName = string.Format($"ion-animation-{_instanceId}_{_playCount}");
        }

        public Animation Duration(int animationDuration)
        {
            _duration = animationDuration;
            Update();
            return this;
        }

        public Animation Iterations(int animationIterations)
        {
            _iterations = animationIterations;
            Update();
            return this;
        }

        public Animation Fill(string animationFill)
        {
            _fill = animationFill;
            Update();
            return this;
        }

        public Animation Direction(string animationDirection)
        {
            _direction = animationDirection;
            Update();
            return this;
        }

        public Animation Easing(string animationEasing)
        {
            _easing = animationEasing;
            Update();
            return this;
        }

        public Animation From(string property, string value)
        {
            if (_keyframes is not { Count: > 0 })
            {
                _keyframes = new List<AnimationKeyframe>
                {
                    new(0, property, value)
                };
            }
            else
            {
                _keyframes[0].Set(property, value);
            }

            return this;
        }

        public Animation To(string property, string value)
        {
            if (_keyframes == null || _keyframes.Count < 1) return this;
            if (_keyframes.Count > 1)
            {
                _keyframes[1].Set(property, value);
            }
            else
            {
                _keyframes.Add(new AnimationKeyframe(1, property, value));
            }

            return this;
        }

        public Animation FromTo(string property, string fromValue, string toValue)
        {
            return From(property, fromValue).To(property, toValue);
        }

        public Animation Keyframes(List<AnimationKeyframe> keyframeValues)
        {
            _keyframes = keyframeValues;
            return this;
        }

        public void Play(Action onFinish = null)
        {
            InitializeAnimation();
            SetStyleProperty("animation-play-state", "running");
            if (onFinish != null)
            {
                /*
                 * todo: blazor not support onanimationend event.
                 */
                var animationTime = _delay + _duration * _iterations;
                SetTimeout(onFinish, animationTime);
            }
        }

        public void Pause()
        {
            if (!_initialized) return;
            SetStyleProperty("animation-play-state", "paused");
        }

        public void Stop()
        {
            if (!_initialized) return;
            _initialized = false;
            _style.Clear();
            _styleSheet = string.Empty;
        }

        private void InitializeAnimation()
        {
            _initialized = true;
            _playCount++;
            _keyframeName = string.Format($"ion-animation-{_instanceId}_{_playCount}");
            CreateKeyframeStylesheet();
            Update();
            SetStyleProperty("animation-play-state", "paused");
        }

        private void Update(bool toggleAnimationName = true, int? step = null)
        {
            SetStyleProperty("animation-name", toggleAnimationName ? $"{_keyframeName}-alt" : _keyframeName);
            SetStyleProperty("animation-duration", $"{_duration}ms");
            SetStyleProperty("animation-timing-function", _easing);
            SetStyleProperty("animation-delay", step != null ? $"{step * _duration}ms" : $"{_delay}ms");
            SetStyleProperty("animation-fill-mode", _fill);
            SetStyleProperty("animation-direction", _direction);
            SetStyleProperty("animation-iteration-count", _iterations == int.MaxValue ? "infinite" : _iterations.ToString());
        }

        private void CreateKeyframeStylesheet()
        {
            if (_keyframes == null) return;
            var rules = string.Join(" ", _keyframes.Select(x => x.GetRule()));
            _styleSheet = $"@keyframes {_keyframeName} {{ {rules} }} @keyframes {_keyframeName}-alt {{ {rules} }}";
        }

        private void SetStyleProperty(string propertyName, string value)
        {
            if (_style.ContainsKey(propertyName))
            {
                _style[propertyName] = value;
            }
            else
            {
                _style.Add(propertyName, value);
            }
        }

        private string GetStyle()
        {
            if (_style.Count <= 0) return string.Empty;
            return string.Join(" ", _style.Select(x => $"{x.Key}: {x.Value};"));
        }

        private void SetTimeout(Action action, int milliseconds)
        {
            Task.Delay(milliseconds).ContinueWith((task) => { action(); });
        }
    }
}
