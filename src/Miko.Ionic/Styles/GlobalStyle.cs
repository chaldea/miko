// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Common;
using Miko.Styling;

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

            // Merge all style modules. MergeChildrenFrom replaces a reflective read of
            // CssObject's internal Children dictionary — cross-assembly non-public reflection
            // that silently yields nothing once trimmed, dropping every global style (ISSUE-140).
            styles.MergeChildrenFrom(NormalizeStyle.GenStyle());
            styles.MergeChildrenFrom(StructureStyle.GenStyle());
            styles.MergeChildrenFrom(TypographyStyle.GenStyle());
            styles.MergeChildrenFrom(DisplayStyle.GenStyle());
            styles.MergeChildrenFrom(FlexUtilsStyle.GenStyle());
            styles.MergeChildrenFrom(TextStyle.GenStyle());
            styles.MergeChildrenFrom(PaddingMarginStyle.GenStyle());
            styles.MergeChildrenFrom(FloatStyle.GenStyle());

            return styles;
        }
    }
}
