using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Title
    {
        private string _documentDir = "";

        [Parameter] public string Color { get; set; }

        [Parameter] public string Size { get; set; } // large small

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
                .Add(Mode)
                .Add($"title-{Size}")
                .If("title-rtl", () => _documentDir == "rtl");
        }
    }
}
