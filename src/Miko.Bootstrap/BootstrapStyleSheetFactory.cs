using Miko.Bootstrap.Styles;
using Miko.Styling;

namespace Miko.Bootstrap;

public static class BootstrapStyleSheetFactory
{
    public static StyleSheet Create(Theme theme)
    {
        var sheet = new StyleSheet();

        RebootStyles.Apply(sheet, theme);
        GridStyles.Apply(sheet, theme);
        ButtonStyles.Apply(sheet, theme);
        FormStyles.Apply(sheet, theme);
        TableStyles.Apply(sheet, theme);
        CardStyles.Apply(sheet, theme);
        AlertStyles.Apply(sheet, theme);
        BadgeStyles.Apply(sheet, theme);
        NavStyles.Apply(sheet, theme);
        ListGroupStyles.Apply(sheet, theme);
        AccordionStyles.Apply(sheet, theme);
        UtilityStyles.Apply(sheet, theme);
        IconStyles.Apply(sheet, theme);

        return sheet;
    }
}
