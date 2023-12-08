using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Item
    {
        private string _tagType = "div";
        private bool _showDetail = false;
        private bool _inList = true;
        private bool _multipleInputs = false;
        private bool _focusable = false;

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public string Lines { get; set; }

        [Parameter]
        public string Fill { get; set; } = "none";

        [Parameter]
        public string Shape { get; set; } = "round";

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment StartSlot { get; set; }

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
                .If("item-has-start-slot", () => StartSlot != null)
                .Add("item")
                .Add(Mode)
                .If($"item-lines-{Lines}", () => Lines != null)
                .Add($"item-fill-{Fill}")
                .If($"item-shape-{Shape}", () => Shape != null)
                .If("item-disabled", () => Disabled)
                .If("in-list", () => _inList)
                .If("item-multiple-inputs", () => _multipleInputs)
                .If("ion-activatable", () => false)
                .If("ion-focusable", () => _focusable);
        }
    }
}
