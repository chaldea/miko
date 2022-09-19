namespace Miko
{
    public class AnimationKeyframe
    {
        private readonly Dictionary<string, string> _props = new();

        public AnimationKeyframe()
        {
        }

        public AnimationKeyframe(float offset, string property, string value)
        {
            Offset = offset;
            _props[property] = value;
        }

        public float Offset { get; set; }

        public string Background
        {
            get => Get("background");
            set => Set("background", value);
        }

        public string Transform
        {
            get => Get("transform");
            set => Set("transform", value);
        }

        public string Opacity
        {
            get => Get("opacity");
            set => Set("opacity", value);
        }

        public string Get(string key)
        {
            return _props[key];
        }

        public void Set(string key, string value)
        {
            _props[key] = value;
        }

        public string GetRule()
        {
            var rule = string.Join(" ", _props.Select(x => $"{x.Key}: {x.Value};"));
            return $"{Offset * 100}% {{ {rule} }}";
        }
    }
}
