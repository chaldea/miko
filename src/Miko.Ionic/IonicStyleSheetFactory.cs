using Miko.Ionic.Components;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>
/// Builds the Ionic component stylesheet from a theme by aggregating each component's
/// generated styles. Mirrors <c>BootstrapStyleSheetFactory</c>.
/// </summary>
public static class IonicStyleSheetFactory
{
    public static StyleSheet Create(IonicTheme theme)
    {
        var sheet = new StyleSheet();
        sheet.Add(PageStyles.GenStyle(theme));
        sheet.Add(TabStyles.GenStyle(theme));
        sheet.Add(IconStyles.GenStyle(theme));
        sheet.Add(LabelStyles.GenStyle(theme));
        sheet.Add(MenuStyles.GenStyle(theme));
        sheet.Add(ListStyles.GenStyle(theme));
        return sheet;
    }
}
