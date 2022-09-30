using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class RouteLayout
    {
        protected ClassMapper ClassMapper { get; } = new();

        [CascadingParameter]
        public RouteContext RouteContext { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            SetClassMap();
        }

        protected void SetClassMap()
        {
            ClassMapper
                .If("transitioned", () => RouteContext.IntoView)
                .If("transitioned_behind", () => !RouteContext.IntoView);
        }
    }
}
