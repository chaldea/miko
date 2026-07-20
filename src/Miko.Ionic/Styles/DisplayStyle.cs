// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Styles
{
    /// <summary>
    /// Display utility classes for controlling CSS display property.
    /// Provides utilities to show/hide elements and control display types.
    /// </summary>
    public static class DisplayStyle
    {
        public static CssObject GenStyle()
        {
            var styles = new CssObject
            {
                [".ion-hide"] = new()
                {
                    Display = Display.None,
                },

                // Display type utilities
                [".ion-display-none"] = new()
                {
                    Display = Display.None,
                },
                [".ion-display-inline"] = new()
                {
                    Display = Display.Inline,
                },
                [".ion-display-inline-block"] = new()
                {
                    Display = Display.InlineBlock,
                },
                [".ion-display-block"] = new()
                {
                    Display = Display.Block,
                },
                [".ion-display-flex"] = new()
                {
                    Display = Display.Flex,
                },
                // InlineFlex and Grid not yet implemented in Miko
                // [".ion-display-inline-flex"] = new()
                // {
                //     Display = Display.InlineFlex,
                // },
                // [".ion-display-grid"] = new()
                // {
                //     Display = Display.Grid,
                // },
            };

            return styles;
        }
    }
}
