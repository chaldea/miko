using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class TabBar
    {
        protected bool keyboardVisible = false;

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public bool Translucent { get; set; }

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
                .Add(Mode)
                .If("tab-bar-translucent", () => Translucent)
                .If("tab-bar-hidden", () => keyboardVisible);
        }
    }
}
