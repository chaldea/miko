using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Tab
    {
        [Parameter] public bool Active { get; set; }

        [Parameter] public string Name { get; set; }

        [Parameter] public RenderFragment ChildContent { get; set; }

        [CascadingParameter]
        public Tabs Tabs { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Tabs.AddTab(this);
            SetClassMap();
        }

        protected void SetClassMap()
        {
            ClassMapper
                .Clear()
                .If("ion-page", () => ChildContent == null)
                .If("tab-hidden", () => !Active);
        }

        public void SetActive(bool active)
        {
            Active = active;
        }
    }
}
