using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Route
    {
        [Parameter]
        public RouteData RouteData { get; set; }
    }
}
