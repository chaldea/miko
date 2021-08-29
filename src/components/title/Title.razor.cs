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
        private Dictionary<string, string> _styleUrls = new Dictionary<string, string>
        {
            {"ios", "title.ios.scss"},
            {"md", "title.md.scss"},
        };

        [Parameter] public string Color { get; set; }

        [Parameter] public string Size { get; set; } = "default"; // large small

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
