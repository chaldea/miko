using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Miko
{
    public partial class Button
    {
        private readonly IDictionary<string, string> _styleUrls = new Dictionary<string, string>
        {
            {"ios", "button.ios.scss"},
            {"md", "button.md.scss"},
        };
        private bool inItem = false;
        private bool inListHeader = false;
        private bool inToolbar = false;

        /// <summary>
        /// Default options are: 
        /// `"primary"`, `"secondary"`, `"tertiary"`, `"success"`, `"warning"`, `"danger"`, `"light"`, `"medium"`, and `"dark"`
        /// </summary>
        [Parameter] public string Color { get; set; }

        [Parameter] public string ButtonType { get; set; } = "button";

        [Parameter] public bool Disabled { get; set; } = false;

        [Parameter] public string Expand { get; set; } // full | block

        [Parameter] public string Size { get; set; } // small | default | large

        [Parameter] public bool Strong { get; set; } = false;

        [Parameter] public string Shape { get; set; } // round

        [Parameter] public string Fill { get; set; } // clear | outline | solid | default

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            SetClassMap();
        }

        protected void SetClassMap()
        {
            var finalSize = string.IsNullOrEmpty(Size) && inItem ? "small" : Size;
            var fill = string.IsNullOrEmpty(Fill) ? inToolbar || inListHeader ? "clear" : "solid" : Fill;
            ClassMapper
                .Clear()
                .If("ion-color", () => !string.IsNullOrEmpty(Color))
                .If($"ion-color-{Color}", () => !string.IsNullOrEmpty(Color))
                .Add(Mode)
                .Add(ButtonType)
                .If($"{ButtonType}-{Expand}", () => !string.IsNullOrEmpty(Expand))
                .If($"{ButtonType}-{finalSize}", () => !string.IsNullOrEmpty(finalSize))
                .If($"{ButtonType}-{Shape}", () => !string.IsNullOrEmpty(Shape))
                .Add($"{ButtonType}-{fill}")
                .If($"{ButtonType}-strong", () => Strong)
                .If("in-toolbar", () => false)
                .If("in-toolbar-color", () => false)
                .If("button-has-icon-only", () => false)
                .If("button-disabled", () => Disabled)
                .Add("ion-activatable")
                .Add("ion-focusable")
                .Add("ion-focusable")
                .Add("hydrated");
        }

        private async Task HandleOnClick(MouseEventArgs args)
        {
            if (OnClick.HasDelegate)
            {
                await OnClick.InvokeAsync(args);
            }
        }
    }
}
