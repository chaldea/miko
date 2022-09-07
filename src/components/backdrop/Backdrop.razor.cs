using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualBasic;

namespace Miko
{
    public partial class Backdrop
    {
        [Parameter] public bool Visible { get; set; }
        [Parameter] public bool Tappable { get; set; }
        [Parameter] public bool StopPropagation { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnTap { get; set; }

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
                .If("backdrop-hide", () => !Visible)
                .If("backdrop-no-tappable", () => !Tappable)
                .If("show-backdrop", () => Visible);
        }
    }
}
