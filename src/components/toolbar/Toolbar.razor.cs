using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Toolbar
    {
        private IDictionary<string, string> _styleUrls = new Dictionary<string, string>
        {
            { "ios", "toolbar.ios.scss" },
            { "md", "toolbar.md.scss" },
        };

        /// <summary>
        /// Default options are: `"primary"`, `"secondary"`, `"tertiary"`, `"success"`, `"warning"`, `"danger"`, `"light"`, `"medium"`, and `"dark"`.
        /// </summary>
        [Parameter]
        public string Color { get; set; }

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
                .If("ion-color", () => !string.IsNullOrEmpty(Color))
                .If($"ion-color-{Color}", () => !string.IsNullOrEmpty(Color))
                .If("toolbar-title-default", () => true)
                .Add(Mode)
                .If("in-toolbar", () => true);
        }
    }
}