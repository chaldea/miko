using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-label</c>. Within a tab button the label sits below the icon with
/// the active mode's tab-button typography.
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>); see
/// <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class LabelStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        return new CssObject
        {
            [$".ion-label.{mode}"] = new()
            {
                Display = Display.Block,
            },

            [$".ion-tab-button.{mode} .ion-label"] = new()
            {
                FontSize = Length.Px(t.TabButtonFontSize),
                MarginTop = Length.Px(2),
                TextAlign = TextAlign.Center,
            },
        };
    }
}
