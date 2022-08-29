using Microsoft.AspNetCore.Components;
using System.ComponentModel;
using System.Drawing;

namespace Miko
{
    public partial class Tab
    {
        [Parameter] public bool Active { get; set; }

        [Parameter] public string Name { get; set; }

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
                .If("ion-page", () => ChildContent == null)
                .If("tab-hidden", () => !Active);
        }
    }
}
