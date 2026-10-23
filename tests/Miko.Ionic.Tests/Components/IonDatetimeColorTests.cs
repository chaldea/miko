using Miko.Ionic.Tests.Fixtures;
using Miko.Ionic.Tests.Fixtures.Routed;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// <c>Color</c> on <see cref="Miko.Ionic.Components.IonDatetime"/> and
/// <see cref="Miko.Ionic.Components.IonDatetimeButton"/> (P0-C1): the palette color replaces
/// primary, as Ionic's <c>:host(.ion-color)</c> rules do. Both used to stamp a class no rule read,
/// and their summaries said so ("on its own it changes nothing").
/// </summary>
public sealed class IonDatetimeColorTests : IDisposable
{
    private readonly RoutedApp _app = new(("/", typeof(DatetimePageFixture)));
    private readonly IonicTheme _theme = IonicTheme.CreateMd();

    public void Dispose() => _app.Dispose();

    private Miko.Styling.ComputedStyle Style(string datetime, string part)
        => _app.ComputedStyleOf(_app.Single(datetime).FindByClass(part).First());

    [Fact]
    public void Should_fill_the_selected_day_with_the_palette_color()
    {
        Style("colored", "calendar-day-active").BackgroundColor.ShouldBe(_theme.Danger);
        Style("plain", "calendar-day-active").BackgroundColor.ShouldBe(_theme.Primary);
    }

    [Fact]
    public void Should_tint_the_md_header_and_action_buttons_with_the_palette_color()
    {
        _app.ComputedStyleOf(_app.Single("colored").FindByClass("datetime-header").Single())
            .BackgroundColor.ShouldBe(_theme.Danger);
        var buttons = _app.Single("colored").FindByClass("datetime-action-button");
        buttons.ShouldNotBeEmpty();
        foreach (var button in buttons)
            _app.ComputedStyleOf(button).Color.ShouldBe(_theme.Danger);
    }

    [Fact]
    public void Should_color_the_active_datetime_button_text()
    {
        var date = _app.Single("colored-button").FindById("date-button").ShouldNotBeNull();
        _app.ComputedStyleOf(date).Color.ShouldBe(_theme.Danger);
    }
}
