using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Miko
{
    public partial class Menu
    {
        private bool _isPaneVisible = false;
        private bool _isEndSide = false;
        private bool _isOpen = false;
        private ElementReference _containerRef;
        private Animation _ani;

        [Parameter] public string ContentId { get; set; }
        [Parameter] public string Type { get; set; } = "overlay";
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public string Side { get; set; } = "start";
        [Parameter] public string MenuId { get; set; } = Guid.NewGuid().ToString("N");
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Inject] public MenuService MenuService { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            MenuService.Add(MenuId, this);
            SetClassMap();
            _ani = new Animation()
                .Fill("both")
                .Direction("normal")
                .Duration(300)
                .FromTo("transform", "translateX(-100%)", "translateX(-0px)");
        }

        protected void SetClassMap()
        {
            ClassMapper
                .Clear()
                .Add(Mode)
                .Add($"menu-type-{Type}")
                .If("menu-enabled", () => !Disabled)
                .If("menu-side-end", () => _isEndSide)
                .If("menu-side-start", () => !_isEndSide)
                // .If("menu-pane-visible", () => !isPaneVisible)
                .If("show-menu", () => _isOpen);
        }

        public void Show()
        {
            _isOpen = true;
            StateHasChanged();
            _ani
                .Easing("cubic-bezier(0.0,0.0,0.2,1)")
                .Direction("normal")
                .Play();
        }

        public void Close()
        {
            _ani
                .Easing("cubic-bezier(0.4, 0, 0.6, 1)")
                .Direction("reverse")
                .Play(() =>
                {
                    _isOpen = false;
                    StateHasChanged();
                });
        }

        public void HandleBackdropTap(MouseEventArgs args)
        {
            Close();
        }
    }
}
