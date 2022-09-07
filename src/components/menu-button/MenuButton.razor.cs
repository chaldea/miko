using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class MenuButton
    {
        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool Hidden { get; set; }

        [Parameter] 
        public RenderFragment ChildContent { get; set; }

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
                .Add("button")
                .Add("ion-activatable")
                .Add("ion-focusable")
                .If("menu-button-hidden", () => Hidden)
                .If("menu-button-disabled", () => Disabled)
                .If("in-toolbar", () => true)
                .If("in-toolbar-color", () => true)
                .If("in-toolbar-color", () => true);
        }
    }
}
