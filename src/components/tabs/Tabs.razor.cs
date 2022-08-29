using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Tabs
    {
        [Parameter] public string Slot { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public RenderFragment TabBarRender { get; set; }
    }
}
