using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Accordion
    {
        [Parameter] public RenderFragment ChildContent { get; set; }

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
