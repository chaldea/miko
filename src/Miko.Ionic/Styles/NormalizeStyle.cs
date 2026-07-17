// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Styles
{
    /// <summary>
    /// Normalize.css v3.0.2 styles adapted for Miko.
    /// Provides consistent cross-browser styling defaults.
    /// </summary>
    public static class NormalizeStyle
    {
        public static CssObject GenStyle()
        {
            return new CssObject
            {
                // HTML5 display definitions
                ["audio, canvas, progress, video"] = new()
                {
                    VerticalAlign = VerticalAlign.Baseline,
                },
                ["audio:not([controls])"] = new()
                {
                    Display = Display.None,
                    Height = Length.Px(0),
                },

                // Text-level semantics
                ["b, strong"] = new()
                {
                    FontWeight = FontWeight.Bold,
                },

                // Embedded content
                ["img"] = new()
                {
                    MaxWidth = Length.Percent(100),
                },

                // Grouping content
                ["hr"] = new()
                {
                    Height = Length.Px(1),
                    BorderWidth = Length.Px(0),
                    BoxSizing = BoxSizing.ContentBox,
                },
                ["pre"] = new()
                {
                    Overflow = Overflow.Auto,
                },
                ["code, kbd, pre, samp"] = new()
                {
                    FontFamily = "monospace, monospace",
                    FontSize = Length.Em(1),
                },

                // Forms
                ["label, input, select, textarea"] = new()
                {
                    FontFamily = "inherit",
                    LineHeight = new Length(1, LengthUnit.Number),
                },
                ["textarea"] = new()
                {
                    Overflow = Overflow.Auto,
                    Height = Length.Auto,
                    FontFamily = "inherit",
                    // Color inherit not supported directly, using transparent as fallback
                },
                ["form, input, optgroup, select"] = new()
                {
                    Margin = new Margin(Length.Px(0)),
                    FontFamily = "inherit",
                    // Color inherit not supported directly
                },
                ["input[type=\"button\"], input[type=\"reset\"], input[type=\"submit\"]"] = new()
                {
                    Cursor = Cursor.Pointer,
                },

                // Remove 300ms delay - TouchAction not implemented in Miko
                ["button"] = new()
                {
                    Padding = new Padding(Length.Px(0)),
                    Border = new Border(Length.Px(0)),
                    BorderRadius = new BorderRadius(Length.Px(0)),
                    FontFamily = "inherit",
                    LineHeight = new Length(1, LengthUnit.Number),
                    TextTransform = TextTransform.None,
                    Cursor = Cursor.Pointer,
                },
                ["a[disabled], button[disabled], input[disabled]"] = new()
                {
                    Cursor = Cursor.Default,
                },

                // Tables - BorderCollapse not yet implemented in Miko
                // ["table"] = new()
                // {
                //     BorderCollapse = BorderCollapse.Collapse,
                // },
                ["td, th"] = new()
                {
                    Padding = new Padding(Length.Px(0)),
                },
            };
        }
    }
}
