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
        private IAnimation _openAnimation;

        [Parameter] public string ContentId { get; set; }
        [Parameter] public string Type { get; set; } = "overlay";
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public string Side { get; set; } = "start";
        [Parameter] public string MenuId { get; set; } = Guid.NewGuid().ToString("N");
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Inject] public AnimationService AnimationService { get; set; }
        [Inject] public MenuService MenuService { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            MenuService.Add(MenuId, this);
            SetClassMap();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                _openAnimation = await AnimationService
                    .Create()
                    .AddElement(_containerRef)
                    .Fill("both")
                    .Direction("normal")
                    .Easing("ease-in")
                    .Duration(300)
                    .FromTo("transform", "translateX(-100%)", "translateX(-0px)");
            }
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

        public async Task Show()
        {
            _isOpen = true;
            await _openAnimation.Play();
        }

        public async Task Close()
        {
            _isOpen = false;
            await _openAnimation.Stop();
        }

        public async Task HandleBackdropTap(MouseEventArgs args)
        {
            await Close();
        }
    }
}
