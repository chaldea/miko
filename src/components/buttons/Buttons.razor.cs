using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Buttons
    {
        [Parameter] public bool Collapse { get; set; }

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
                .Add(Mode)
                .If("buttons-collapse", () => Collapse);
        }
    }
}
