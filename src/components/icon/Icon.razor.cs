using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Icon
    {
        private string _svgContent;

        [Parameter] public string Color { get; set; }

        [Parameter] public string Name { get; set; }

        [Parameter] public string Size { get; set; }

        [Parameter] public bool FlipRtl { get; set; }

        [Parameter]
        [CascadingParameter(Name = "Slot")]
        public string Slot { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _svgContent = IconResource.Get(Name);
            SetClassMap();
        }

        protected void SetClassMap()
        {
            ClassMapper
                .Clear()
                .Add(Mode)
                .If($"icon-{Size}", () => !string.IsNullOrEmpty(Size))
                .If("flip-rtl", () => FlipRtl);
        }
    }
}
