using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Label
    {
        [Parameter] public string Position { get; set; }

        [Parameter] public bool NoAnimate { get; set; }

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
                .If($"label-{Position}", () => !string.IsNullOrEmpty(Position))
                .If("abel-no-animate", () => NoAnimate)
                .If("label-rtl", () => false);
        }
    }
}
