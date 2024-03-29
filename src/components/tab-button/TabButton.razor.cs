﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Miko
{
    public partial class TabButton
    {
        [Parameter] public bool Selected { get; set; }

        [Parameter] public string Tab { get; set; }

        [Parameter] public bool Disabled { get; set; }

        [Parameter] public bool HasLabel { get; set; }

        [Parameter] public bool HasIcon { get; set; }

        [Parameter] public string Layout { get; set; } = "icon-top";

        [Parameter] public string Href { get; set; }

        [Parameter] public RenderFragment ChildContent { get; set; }

        [CascadingParameter]
        public Tabs Tabs { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Tabs.AddTabButton(this);
            SetClassMap();
        }

        protected void SetClassMap()
        {
            ClassMapper
                .Clear()
                .Add(Mode)
                .If("tab-selected", () => Selected)
                .If("tab-disabled", () => Disabled)
                .If("tab-has-label", () => true)
                .If("tab-has-icon", () => true)
                .If("tab-has-label-only", () => HasLabel && !HasIcon)
                .If("tab-has-icon-only", () => HasIcon && !HasLabel)
                .Add($"tab-layout-{Layout}")
                .Add("ion-activatable")
                .Add("ion-selectable")
                .Add("ion-focusable");
        }

        public void SetSelected(bool selected)
        {
            Selected = selected;
        }

        private void HandleOnClick(MouseEventArgs args)
        {
            Tabs.SelectTab(Tab);
        }
    }
}
