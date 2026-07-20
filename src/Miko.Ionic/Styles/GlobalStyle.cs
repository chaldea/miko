// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Common;
using Miko.Styling;
using System.Reflection;

namespace Miko.Ionic.Styles
{
    /// <summary>
    /// Global styles for Ionic components.
    /// Combines all utility styles (normalize, structure, typography, flex, display, text, padding/margin).
    /// </summary>
    public static class GlobalStyle
    {
        public static CssObject GenStyle()
        {
            var styles = new CssObject();

            // Merge all style modules
            MergeStyles(styles, NormalizeStyle.GenStyle());
            MergeStyles(styles, StructureStyle.GenStyle());
            MergeStyles(styles, TypographyStyle.GenStyle());
            MergeStyles(styles, DisplayStyle.GenStyle());
            MergeStyles(styles, FlexUtilsStyle.GenStyle());
            MergeStyles(styles, TextStyle.GenStyle());
            MergeStyles(styles, PaddingMarginStyle.GenStyle());
            MergeStyles(styles, FloatStyle.GenStyle());

            return styles;
        }

        private static void MergeStyles(CssObject target, CssObject source)
        {
            // Access the internal Children property via reflection
            var childrenProperty = typeof(CssObject).GetProperty("Children",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (childrenProperty != null)
            {
                var sourceChildren = childrenProperty.GetValue(source) as IReadOnlyDictionary<string, CssObject>;
                if (sourceChildren != null)
                {
                    foreach (var kvp in sourceChildren)
                    {
                        target[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
    }
}
