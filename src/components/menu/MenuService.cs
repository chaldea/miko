using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miko
{
    public class MenuService
    {
        private readonly IDictionary<string, Menu> _menus = new Dictionary<string, Menu>();

        public void Add(string menuId, Menu menu)
        {
            _menus.TryAdd(menuId, menu);
        }

        public async Task OpenAsync(string menuId = null)
        {
            Menu menu;
            if (menuId == null)
            {
                menu = _menus.First().Value;
            }
            else
            {
                _menus.TryGetValue(menuId, out menu);
            }

            await menu.Show();
        }
    }
}
