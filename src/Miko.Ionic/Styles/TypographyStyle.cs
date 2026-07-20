// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Styles
{
    /// <summary>
    /// Typography styles for Ionic components.
    /// Provides heading styles, font weights, and text formatting.
    /// </summary>
    public static class TypographyStyle
    {
        public static CssObject GenStyle()
        {
            return new CssObject
            {
                ["a"] = new()
                {
                    BackgroundColor = Color.Transparent,
                    // TODO: Support CSS variable var(--ion-color-primary)
                    Color = Color.FromHex("#3880ff"), // Ionic default primary color
                },

                ["h1, h2, h3, h4, h5, h6"] = new()
                {
                    Margin = new Margin(Length.Px(16), Length.Px(0), Length.Px(10), Length.Px(0)),
                    FontWeight = FontWeight.Medium,
                    LineHeight = new Length(1.2f, LengthUnit.Number),
                },

                ["h1"] = new()
                {
                    Margin = new Margin(Length.Px(20), Length.Px(0), Length.Px(10), Length.Px(0)),
                    FontSize = Length.Px(26),
                },

                ["h2"] = new()
                {
                    Margin = new Margin(Length.Px(18), Length.Px(0), Length.Px(10), Length.Px(0)),
                    FontSize = Length.Px(24),
                },

                ["h3"] = new()
                {
                    FontSize = Length.Px(22),
                },

                ["h4"] = new()
                {
                    FontSize = Length.Px(20),
                },

                ["h5"] = new()
                {
                    FontSize = Length.Px(18),
                },

                ["h6"] = new()
                {
                    FontSize = Length.Px(16),
                },

                ["small"] = new()
                {
                    FontSize = Length.Percent(75),
                },

                ["sub, sup"] = new()
                {
                    Position = Position.Relative,
                    FontSize = Length.Percent(75),
                    LineHeight = Length.Px(0),
                    VerticalAlign = VerticalAlign.Baseline,
                },

                ["sup"] = new()
                {
                    Top = Length.Em(-0.5f),
                },

                ["sub"] = new()
                {
                    Bottom = Length.Em(-0.25f),
                },
            };
        }
    }
}
