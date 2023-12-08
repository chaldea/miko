using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Content
    {
        [Parameter] public string Color { get; set; }

        [Parameter] public bool Fullscreen { get; set; }

        [Parameter] public bool ForceOverscroll { get; set; }

        [Parameter] public bool ScrollX { get; set; }

        [Parameter] public bool ScrollY { get; set; }

        [Parameter] public bool ScrollEvents { get; set; }

        [Parameter] public RenderFragment ChildContent { get; set; }

    }
}
