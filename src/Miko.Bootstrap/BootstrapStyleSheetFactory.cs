using Miko.Bootstrap.Components;
using Miko.Bootstrap.Styles;
using Miko.Styling;

namespace Miko.Bootstrap;

public static class BootstrapStyleSheetFactory
{
    public static StyleSheet Create(Theme theme)
    {
        var sheet = new StyleSheet();
        sheet.Add(RebootStyles.GenStyle(theme));
        sheet.Add(UtilitiesStyle.GenStyle(theme));
        sheet.Add(BtnCloseStyle.GenStyle(theme));
        sheet.Add(GridStyles.GenStyle(new GridToken(theme)));
        sheet.Add(AccordionStyles.GenStyle(new AccordionToken(theme)));
        sheet.Add(AlertStyles.GenStyle(new AlertToken(theme)));
        sheet.Add(BadgeStyles.GenStyle(new BadgeToken(theme)));
        sheet.Add(BreadcrumbStyles.GenStyle(new BreadcrumbToken(theme)));
        sheet.Add(ButtonStyles.GenStyle(new ButtonToken(theme)));
        sheet.Add(CardStyles.GenStyle(new CardToken(theme)));
        sheet.Add(CarouselStyles.GenStyle(new CarouselToken(theme)));
        sheet.Add(DropdownStyles.GenStyle(new DropdownToken(theme)));
        sheet.Add(FormStyles.GenStyle(new FormToken(theme)));
        sheet.Add(IconStyles.GenStyle(theme));
        sheet.Add(ListStyles.GenStyle(new ListToken(theme)));
        sheet.Add(ModalStyles.GenStyle(new ModalToken(theme)));
        sheet.Add(NavbarStyles.GenStyle(new NavbarToken(theme)));
        sheet.Add(PaginationStyles.GenStyle(new PaginationToken(theme)));
        sheet.Add(PopoverStyles.GenStyle(new PopoverToken(theme)));
        sheet.Add(ProgressStyles.GenStyle(new ProgressToken(theme)));
        sheet.Add(SpinnerStyles.GenStyle(new SpinnerToken(theme)));
        sheet.Add(TabStyles.GenStyle(new TabToken(theme)));
        sheet.Add(TableStyles.GenStyle(new TableToken(theme)));
        sheet.Add(ToastStyles.GenStyle(new ToastToken(theme)));
        sheet.Add(TooltipStyles.GenStyle(new TooltipToken(theme)));
        return sheet;
    }
}
