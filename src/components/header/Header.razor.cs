using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Header
    {
        [Parameter] public string Collapse { get; set; } = "condense";

        [Parameter] public bool Translucent { get; set; }

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
                .Add($"header-{Mode}")
                .If("header-translucent", () => Translucent)
                .If($"header-collapse-{Collapse}", () => true)
                .If($"header-translucent-{Mode}", () => Translucent);

        }
    }
}