// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Styles
{
    /// <summary>
    /// Padding and margin utility classes.
    /// Provides utilities for controlling element spacing.
    /// Default spacing: 16px
    /// TODO: Support CSS variables var(--ion-padding) and var(--ion-margin)
    /// </summary>
    public static class PaddingMarginStyle
    {
        private const float DefaultSpacing = 16f;

        public static CssObject GenStyle()
        {
            var styles = new CssObject();

            // Padding utilities
            styles[".ion-no-padding"] = new()
            {
                Padding = new Padding(Length.Px(0)),
            };
            styles[".ion-padding"] = new()
            {
                Padding = new Padding(Length.Px(DefaultSpacing)),
            };
            styles[".ion-padding-top"] = new()
            {
                PaddingTop = Length.Px(DefaultSpacing),
            };
            styles[".ion-padding-bottom"] = new()
            {
                PaddingBottom = Length.Px(DefaultSpacing),
            };
            styles[".ion-padding-start"] = new()
            {
                PaddingLeft = Length.Px(DefaultSpacing),
            };
            styles[".ion-padding-end"] = new()
            {
                PaddingRight = Length.Px(DefaultSpacing),
            };
            styles[".ion-padding-vertical"] = new()
            {
                PaddingTop = Length.Px(DefaultSpacing),
                PaddingBottom = Length.Px(DefaultSpacing),
            };
            styles[".ion-padding-horizontal"] = new()
            {
                PaddingLeft = Length.Px(DefaultSpacing),
                PaddingRight = Length.Px(DefaultSpacing),
            };

            // Margin utilities
            styles[".ion-no-margin"] = new()
            {
                Margin = new Margin(Length.Px(0)),
            };
            styles[".ion-margin"] = new()
            {
                Margin = new Margin(Length.Px(DefaultSpacing)),
            };
            styles[".ion-margin-top"] = new()
            {
                MarginTop = Length.Px(DefaultSpacing),
            };
            styles[".ion-margin-bottom"] = new()
            {
                MarginBottom = Length.Px(DefaultSpacing),
            };
            styles[".ion-margin-start"] = new()
            {
                MarginLeft = Length.Px(DefaultSpacing),
            };
            styles[".ion-margin-end"] = new()
            {
                MarginRight = Length.Px(DefaultSpacing),
            };
            styles[".ion-margin-vertical"] = new()
            {
                MarginTop = Length.Px(DefaultSpacing),
                MarginBottom = Length.Px(DefaultSpacing),
            };
            styles[".ion-margin-horizontal"] = new()
            {
                MarginLeft = Length.Px(DefaultSpacing),
                MarginRight = Length.Px(DefaultSpacing),
            };

            return styles;
        }
    }
}
