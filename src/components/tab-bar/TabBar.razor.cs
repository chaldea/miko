using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class TabBar
    {
        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public string Slot { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            SetClassMap();
        }

        protected void SetClassMap()
        {
            ClassMapper
                .Clear()
                .Add(Mode);
        }
    }
}
