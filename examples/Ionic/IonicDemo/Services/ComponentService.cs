// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace IonicDemo.Services
{
    public record ComponentItem(string Name, string Icon, string Router);
    public class ComponentService
    {
        public List<ComponentItem> GetComponents()
        {
            var icon = "res://IonicDemo.Assets.component-icon.svg";

            return new List<ComponentItem>()
            {
                new("Accordion", icon, "/accordion"),
                new("Action Sheet", icon, "/action-sheet"),
                new("Alert", icon, "/alert"),
                new("Avatar", icon, "/avatar"),
                new("Badge", icon, "/badge"),
                new("Breadcrumbs", icon, "/breadcrumbs"),
                new("Button", icon, "/button"),
                new("Card", icon, "/card"),
                new("Checkbox", icon, "/checkbox"),
                new("Chip", icon, "/chip"),
                new("Content", icon, "/content"),
                new("Date & Time Picker", icon, "/datetime"),
                new("Floating Action Button", icon, "/fab"),
                new("Grid", icon, "/grid"),
                new("Icons", icon, "/icons"),
                new("Infinite Scroll", icon, "/infinite-scroll"),
                new("Input", icon, "/input"),
            };
        }
    }
}
