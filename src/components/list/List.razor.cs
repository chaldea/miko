using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class List
    {
        [Parameter]
        public bool Inset { get; set; }

        [Parameter]
        public string Lines { get; set; }

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
                .Add($"list-{Mode}")
                .If("list-inset", () => Inset)
                .If($"list-lines-{Lines}", () => Lines != null)
                .If($"list-{Mode}-lines-{Lines}", () => Lines != null);
        }
    }
}
