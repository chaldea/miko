using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Tabs
    {
        [Parameter] public RenderFragment ChildContent { get; set; }
    }
}
