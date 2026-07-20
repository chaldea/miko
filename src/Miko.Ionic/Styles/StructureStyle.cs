// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Styles
{
    /// <summary>
    /// Structural CSS for native HTML elements.
    /// Provides base layout and positioning for the app container.
    /// </summary>
    public static class StructureStyle
    {
        public static CssObject GenStyle()
        {
            return new CssObject
            {
                ["*"] = new()
                {
                    BoxSizing = BoxSizing.BorderBox,
                },

                ["html"] = new()
                {
                    Width = Length.Percent(100),
                    Height = Length.Percent(100),
                },

                ["body"] = new()
                {
                    Margin = new Margin(Length.Px(0)),
                    Padding = new Padding(Length.Px(0)),
                    Position = Position.Fixed,
                    Width = Length.Percent(100),
                    MaxWidth = Length.Percent(100),
                    Height = Length.Percent(100),
                    MaxHeight = Length.Percent(100),
                    Overflow = Overflow.Hidden,
                    OverflowWrap = OverflowWrap.BreakWord,
                    // TODO: Support CSS variables for background and text colors
                    // BackgroundColor = Color.Parse("var(--ion-background-color)"),
                    // Color = Color.Parse("var(--ion-text-color)"),
                },

                ["body.backdrop-no-scroll"] = new()
                {
                    Overflow = Overflow.Hidden,
                },
            };
        }
    }
}
