using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Tabs
    {
        private readonly List<Tab> _tabs = new();
        private readonly List<TabButton> _tabButtons = new();
        private bool _isRoutingTab = false;

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public RenderFragment TopSlot { get; set; }

        [Parameter] public RenderFragment BottomSlot { get; set; }

        [Inject] public NavigationManager NavigationManager { get; set; }

        [Inject] public RouteService RouteService { get; set; }

        public void AddTab(Tab tab)
        {
            _tabs.Add(tab);
            if (_tabs.Count == 1)
            {
                _tabs[0].SetActive(true);
            }
        }

        public void AddTabButton(TabButton tabButton)
        {
            _tabButtons.Add(tabButton);
            if (tabButton.Href != null)
            {
                RouteService.AddTabRoute(tabButton.Href);
                _isRoutingTab = true;
            }

            if (_isRoutingTab)
            {
                var route = $"/{NavigationManager.ToBaseRelativePath(NavigationManager.Uri)}";
                tabButton.SetSelected(route == tabButton.Href);
            }
            else if (_tabButtons.Count == 1)
            {
                _tabButtons[0].SetSelected(true);
            }
        }

        public void SelectTab(string name)
        {
            if (!_isRoutingTab)
            {
                // set selected tab
                foreach (var tab in _tabs)
                {
                    tab.SetActive(tab.Name == name);
                }
            }
            
            // set selected button
            TabButton selectedButton = null;
            foreach (var button in _tabButtons)
            {
                button.SetSelected(button.Tab == name);
                if (button.Tab == name)
                {
                    selectedButton = button;
                }
            }

            if (_isRoutingTab && selectedButton != null)
            {
                NavigationManager.NavigateTo(selectedButton.Href);
            }
        }
    }
}
