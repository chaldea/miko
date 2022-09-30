using System.Collections.Concurrent;

namespace Miko
{
    public class RouteService
    {
        private readonly Animation _fadeInLeft;
        private readonly Animation _fadeInRight;
        private readonly Animation _fadeOutLeft;
        private readonly Animation _fadeOutRight;
        private readonly ConcurrentDictionary<(Type From,Type To), (Animation EffectOut, Animation EffectIn)> _animations = new();
        public RouteService()
        {
            _fadeInLeft = new Animation()
                .Duration(500)
                .FromTo("opacity", "0", "1")
                .FromTo("transform", "translate3d(-100%, 0, 0)", "translate3d(0, 0, 0)");

            _fadeInRight = new Animation()
                .Duration(500)
                .FromTo("opacity", "0", "1")
                .FromTo("transform", "translate3d(100%, 0, 0)", "translate3d(0, 0, 0)");

            _fadeOutLeft = new Animation()
                .Duration(500)
                .FromTo("opacity", "1", "0")
                .FromTo("transform", "translate3d(0, 0, 0)", "translate3d(-100%, 0, 0)");

            _fadeOutRight = new Animation()
                .Duration(500)
                .FromTo("opacity", "1", "0")
                .FromTo("transform", "translate3d(0, 0, 0)", "translate3d(100%, 0, 0)");
        }

        public void AddRoute(Type from, Type to)
        {
            if (from == to) return;
            if (from == null || to == null) return;
            _animations.TryAdd((from, to), (_fadeOutLeft, _fadeInRight));
            _animations.TryAdd((to, from), (_fadeOutRight, _fadeInLeft));
        }

        public Animation GetEffect(Type from, Type to, bool isActive)
        {
            if (_animations.TryGetValue((from, to), out var value))
            {
                return isActive? value.EffectIn: value.EffectOut;
            }

            return isActive ? _fadeInRight : _fadeOutLeft;
        }
    }
}
