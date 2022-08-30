using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Tabs
    {
        private readonly List<Tab> _tabs = new();
        private readonly List<TabButton> _tabButtons = new();

        [Parameter] public string Slot { get; set; }

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public RenderFragment TabBarRender { get; set; }

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
            if (_tabButtons.Count == 1)
            {
                _tabButtons[0].SetSelected(true);
            }
        }

        public void SelectTab(string name)
        {
            foreach (var tab in _tabs)
            {
                tab.SetActive(tab.Name == name);
            }

            foreach (var button in _tabButtons)
            {
                button.SetSelected(button.Tab == name);
            }
            StateHasChanged();
        }
    }
}
