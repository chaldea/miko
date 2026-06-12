using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-label</c>. Within a tab button the label sits below the icon with
/// the Material Design typography (12px, small top margin) from
/// <c>tab-button.md.vars.scss</c>.
/// </summary>
internal static class LabelStyles
{
    internal static CssObject GenStyle(IonicTheme t)
    {
        return new CssObject
        {
            [".ion-label"] = new()
            {
                Display = Display.Block,
            },

            [".ion-tab-button .ion-label"] = new()
            {
                FontSize = Length.Px(t.TabButtonFontSize),
                MarginTop = Length.Px(2),
                TextAlign = TextAlign.Center,
            },
        };
    }
}
