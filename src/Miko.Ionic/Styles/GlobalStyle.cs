// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Styles
{
    public class GlobalStyle
    {
        public static CssObject GenStyle()
        {
            return new CssObject
            {
                ["*"] = new()
                {
                    BoxSizing = BoxSizing.BorderBox,
                },
            };
        }
    }
}
