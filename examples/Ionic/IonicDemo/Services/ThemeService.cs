using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Ionic;

namespace IonicDemo.Services;

public sealed class ThemeService(IOptions<IonicOptions> options)
{
    private readonly IonicTheme _light = options.Value.Theme;
    private readonly IonicTheme _dark = CreateDark();
    private bool _isDark;

    public IonicTheme Current => options.Value.Theme;

    public bool IsDark
    {
        get => _isDark;
        set
        {
            _isDark = value;
            // Routed pages are built before the layout. Retain the selection in the app theme
            // so newly navigated pages and controller-created overlays receive it as well.
            options.Value.Theme = value ? _dark : _light;
        }
    }

    private static IonicTheme CreateDark()
    {
        Color background = "121212", surface = "202124", text = "f1f3f4";
        Color muted = "adb0b5", border = "414348", accent = "7ca8ff";
        return new IonicTheme
        {
            Palette = new PaletteToken { BackgroundColor = background, TextColor = text, Primary = accent },
            Menu = new MenuToken { AppBackground = background, Background = surface, BorderColor = border },
            Content = new ContentToken { Background = background, Color = text },
            Header = new HeaderToken { BorderColor = border },
            Toolbar = new ToolbarToken { Background = surface, Color = text, BorderColor = border },
            List = new ListToken { Background = background, HeaderColor = muted },
            Item = new ItemToken { Color = text, BorderColor = border, DividerBackground = surface, DividerColor = muted },
            Card = new CardToken { Background = surface, Color = text },
            Accordion = new AccordionToken { Background = surface },
            Button = new ButtonToken { SolidBackground = accent, SolidColor = Color.Black, TextColor = accent },
            Chip = new ChipToken { Background = border, Color = text, BorderColor = muted },
            Note = new NoteToken { Color = muted },
            Input = new InputToken { Background = surface, TextColor = text, LabelColor = muted, PlaceholderColor = muted,
                BorderColor = border, HighlightColor = accent, HelperColor = muted, ClearIconColor = muted },
            Select = new SelectToken { Background = surface, TextColor = text, LabelColor = muted, PlaceholderColor = muted,
                BorderColor = border, HighlightColor = accent, HelperColor = muted },
            Checkbox = new CheckboxToken { BackgroundOff = surface, BorderColorOff = muted, BackgroundChecked = accent },
            Toggle = new ToggleToken { TrackBackgroundOff = border, TrackBackgroundOn = accent, HandleBackground = muted,
                HandleBackgroundChecked = accent },
            Searchbar = new SearchbarToken { InputBackground = surface, InputTextColor = text, SearchIconColor = muted,
                ClearIconColor = muted, CancelButtonColor = accent },
            Segment = new SegmentToken { Background = surface, ButtonColor = muted, ButtonCheckedColor = text, IndicatorColor = border },
            Tab = new TabToken { BarBackground = surface, BarBorderColor = border, BarColor = muted, BarColorSelected = accent },
            Breadcrumb = new BreadcrumbToken { Color = muted, ColorActive = text, IconColor = muted, IconColorActive = text,
                SeparatorColor = muted, IndicatorBackground = surface, IndicatorColor = text },
            Datetime = new DatetimeToken { Background = surface, HeaderBackground = background, HeaderColor = text,
                DayColor = text, DayOfWeekColor = muted, MonthYearColor = text, TodayColor = accent,
                ButtonBackground = surface, ButtonColor = text, ButtonActiveColor = accent },
            Alert = new AlertToken { Background = surface, TitleColor = text, SubTitleColor = muted, MessageColor = text,
                ButtonColor = accent, ListBorderColor = border, ControlBorderColorOff = muted },
            ActionSheet = new ActionSheetToken { Background = surface, TitleColor = muted, ButtonColor = text,
                ButtonBorderColor = border, DestructiveColor = "ff808b" },
            Fab = new FabToken { Background = accent, Color = Color.Black, ListButtonBackground = surface, ListButtonColor = text },
            SkeletonText = new SkeletonTextToken { Background = border, BackgroundAnimated = muted },
            Spinner = new SpinnerToken { Color = accent, TrackColor = border },
            ProgressBar = new ProgressBarToken { Background = border, ProgressBackground = accent },
            Slides = new SlidesToken { BulletBackground = muted, BulletBackgroundActive = accent, NavigationColor = accent,
                ScrollBarBackground = border, ScrollBarBackgroundActive = muted },
        };
    }
}
