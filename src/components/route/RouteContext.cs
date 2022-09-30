using Microsoft.AspNetCore.Components;

namespace Miko
{
    public class RouteContext
    {
        public RouteData RouteData { get; }
        public RouteData SwitchedRouteData { get; }
        public bool IntoView { get; }
        public bool Backwards { get; }
        public bool FirstRender { get; }
        public Animation Effect { get; }

        private RouteContext(
            RouteData routeData, 
            RouteData switchedRouteData,
            Animation effect,
            bool intoView, 
            bool backwards, 
            bool firstRender)
        {
            RouteData = routeData;
            SwitchedRouteData = switchedRouteData;
            Effect = effect;
            IntoView = intoView;
            Backwards = backwards;
            FirstRender = firstRender;
        }

        public static RouteContext Create(
            RouteData routeData, 
            RouteData switchedRouteData,
            Animation effect,
            bool intoView,
            bool backwards, 
            bool firstRender)
            => new(routeData, switchedRouteData, effect, intoView, backwards, firstRender);
    }
}
