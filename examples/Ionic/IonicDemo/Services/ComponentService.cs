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
                new("Input OTP", icon, "/input-otp"),
                new("Item", icon, "/item"),
                new("Item Group", icon, "/item-group"),
                new("List", icon, "/list"),
                new("Loading", icon, "/loading"),
                new("Menu", icon, "/menu"),
                new("Modal", icon, "/modal"),
                new("Navigation", icon, "/nav"),
                new("Note", icon, "/note"),
                new("Picker", icon, "/picker"),
                new("Popover", icon, "/popover"),
                new("Progress", icon, "/progress"),
                new("Radio", icon, "/radio"),
                new("Range", icon, "/range"),
                new("Refresher", icon, "/refresher"),
                new("Reorder", icon, "/reorder"),
                new("Searchbar", icon, "/searchbar"),
                new("Segment", icon, "/segment"),
                new("Select", icon, "/select"),
                new("Skeleton Text", icon, "/skeleton-text"),
                new("Spinner", icon, "/spinner"),
                new("Tabs", icon, "/tabs"),
                new("Text", icon, "/text"),
                new("Thumbnail", icon, "/thumbnail"),
                new("Toast", icon, "/toast"),
                new("Toggle", icon, "/toggle"),
                new("Toolbar", icon, "/toolbar"),
            };
        }
    }
}
