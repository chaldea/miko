using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class TabButton
    {
        [Parameter] public bool Selected { get; set; }

        [Parameter] public string Tab { get; set; }

        [Parameter] public bool Disabled { get; set; }

        [Parameter] public bool HasLabel { get; set; }

        [Parameter] public bool HasIcon { get; set; }

        [Parameter] public string Layout { get; set; }

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
                .If("tab-selected", () => Selected)
                .If("tab-disabled", () => Disabled)
                .If("tab-has-label", () => HasLabel)
                .If("tab-has-icon", () => HasIcon)
                .If("tab-has-label-only", () => HasLabel && !HasIcon)
                .If("tab-has-icon-only", () => HasIcon && !HasLabel)
                .Add($"tab-layout-{Layout}")
                .Add("ion-activatable")
                .Add("ion-selectable")
                .Add("ion-focusable");
        }
    }
}
