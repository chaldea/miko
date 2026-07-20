// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Styles
{
    /// <summary>
    /// Text utility classes for alignment, transformation, and wrapping.
    /// Provides utilities for text-align, text-transform, and white-space.
    /// </summary>
    public static class TextStyle
    {
        public static CssObject GenStyle()
        {
            var styles = new CssObject();

            // Text Alignment
            styles[".ion-text-center"] = new() { TextAlign = TextAlign.Center };
            styles[".ion-text-justify"] = new() { TextAlign = TextAlign.Justify };
            styles[".ion-text-start"] = new() { TextAlign = TextAlign.Left }; // Start maps to Left in LTR
            styles[".ion-text-end"] = new() { TextAlign = TextAlign.Right }; // End maps to Right in LTR
            styles[".ion-text-left"] = new() { TextAlign = TextAlign.Left };
            styles[".ion-text-right"] = new() { TextAlign = TextAlign.Right };
            styles[".ion-text-nowrap"] = new() { WhiteSpace = WhiteSpace.Nowrap };
            styles[".ion-text-wrap"] = new() { WhiteSpace = WhiteSpace.Normal };

            // Text Transformation
            styles[".ion-text-uppercase"] = new() { TextTransform = TextTransform.Uppercase };
            styles[".ion-text-lowercase"] = new() { TextTransform = TextTransform.Lowercase };
            styles[".ion-text-capitalize"] = new() { TextTransform = TextTransform.Capitalize };

            return styles;
        }
    }
}
