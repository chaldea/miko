using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Menu
    {
        private bool isPaneVisible = false;
        private bool isEndSide = false;

        [Parameter] public string ContentId { get; set; }
        [Parameter] public string Type { get; set; } = "overlay";
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public string Side { get; set; } = "start";
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
                .Add($"menu-type-{Type}")
                .If("menu-enabled", () => !Disabled)
                .If("menu-side-end", () => isEndSide)
                .If("menu-side-start", () => !isEndSide)
                .If("menu-pane-visible", () => !isPaneVisible);
        }
    }
}
