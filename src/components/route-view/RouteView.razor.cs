using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class RouteView
    {
        [CascadingParameter]
        public RouteContext RouteContext { get; set; }

        [Parameter]
        public Type DefaultLayout { get; set; }
    }
}
