using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Toolbar
    {
        /// <summary>
        /// Default options are: `"primary"`, `"secondary"`, `"tertiary"`, `"success"`, `"warning"`, `"danger"`, `"light"`, `"medium"`, and `"dark"`.
        /// </summary>
        [Parameter]
        public string Color { get; set; }

        [Parameter] 
        public RenderFragment ChildContent { get; set; }

        [Parameter] 
        public RenderFragment StartSlot { get; set; }

        [Parameter] 
        public RenderFragment SecondarySlot { get; set; }

        [Parameter]
        public RenderFragment PrimarySlot { get; set; }

        [Parameter]
        public RenderFragment EndSlot { get; set; }

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
                .If("ion-color", () => !string.IsNullOrEmpty(Color))
                .If($"ion-color-{Color}", () => !string.IsNullOrEmpty(Color))
                .If("toolbar-title-default", () => true)
                .If("in-toolbar", () => true);
        }
    }
}