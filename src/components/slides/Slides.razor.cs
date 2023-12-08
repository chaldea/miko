using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Slides
    {
        [Parameter]
        public bool Pager { get; set; }

        [Parameter]
        public bool Scrollbar { get; set; }

        [Parameter] 
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment Pagination { get; set; }

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
                .Add($"slides-{Mode}")
                .Add("swiper-container");
        }
    }
}
