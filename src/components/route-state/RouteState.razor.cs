using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Miko
{
    public partial class RouteState
    {
        private bool _invokesStateChanged = true;
        private bool _isActive = true;
        private RouteData _lastRouteData;
        private RouteContext RouteContext { get; set; }

        [Parameter]
        public string Name { get; set; }

        [Parameter]
        public RouteData RouteData { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public bool IsActive { get; set; }

        [Parameter]
        public bool ForgetStateOnTransition { get; set; }

        [Parameter]
        public Animation ActiveEffect { get; set; } = new();

        [Parameter]
        public Animation DeactiveEffect { get; set; } = new();

        [Inject]
        public RouteService RouteService { get; set; }

        [JSInvokable]
        public void Navigate(bool backwards)
        {
            Navigate(backwards, firstRender: false);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _isActive = IsActive;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await HandleFirstRender();
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        internal async Task HandleFirstRender()
        {
            await JsInvokeAsync("Miko.interop.onRouteStateChange", DotNetObjectReference.Create(this), _isActive);
            Navigate(backwards: false, firstRender: true);
        }

        internal void Navigate(bool backwards, bool firstRender)
        {
            var routeDataToUse = _isActive ? RouteData : _lastRouteData;
            var switchedRouteData = (_isActive ? _lastRouteData : RouteData) ?? RouteData;
            if (_isActive)
            {
                RouteService.AddRoute(switchedRouteData.PageType, routeDataToUse.PageType);
            }
            var canResetStateOnTransitionOut = ForgetStateOnTransition && !_isActive;

            var from = _isActive ? switchedRouteData?.PageType : routeDataToUse?.PageType;
            var to = _isActive ? routeDataToUse?.PageType : switchedRouteData?.PageType;
            var effect = RouteService.GetEffect(from, to, _isActive);
            if (!firstRender)
            {
                effect.Play(() =>
                {
                    // remove view
                    if (canResetStateOnTransitionOut)
                    {
                        Console.WriteLine($"{Name} remove");
                        RouteContext = RouteContext.Create(routeData: null, switchedRouteData: null, effect: null, RouteContext.IntoView, RouteContext.Backwards, RouteContext.FirstRender);
                        if (_invokesStateChanged)
                        {
                            StateHasChanged();
                        }
                    }
                });
            }
            RouteContext = RouteContext.Create(routeDataToUse, switchedRouteData, effect, _isActive, backwards, firstRender);
            if (_invokesStateChanged)
            {
                StateHasChanged();
            }
            _isActive = !_isActive;
            _lastRouteData = RouteData;
        }
    }
}
