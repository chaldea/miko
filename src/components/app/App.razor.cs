using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class App
    {
        [Parameter] public RenderFragment ChildContent { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            SetClassMap();
        }

        protected void SetClassMap()
        {
            ClassMapper
                .Clear()
                .Add(Mode)
                .Add("ion-page");
        }
    }
}
