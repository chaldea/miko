using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the <c>ion-page</c> shell. Component-specific rules live alongside their
/// respective components in <see cref="HeaderStyles"/>, <see cref="ToolbarStyles"/>, and
/// <see cref="TitleStyles"/>.
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>) that each component
/// stamps onto its root element, so a single stylesheet carries both modes and switching the
/// root class alone re-styles the tree. <paramref name="mode"/> is woven into each selector;
/// the mode-specific values come from <paramref name="t"/> (a per-mode <see cref="IonicTheme"/>).
/// </para>
/// </summary>
internal static class PageStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        return new CssObject
        {
            // ion-page — flex column shell: header on top, content fills the rest.
            // Grows to fill its flex parent (the tabs-inner content area) so percentage
            // heights are not resolved against a zero-height basis.
            [$".ion-page.{mode}"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            },

        };
    }
}
