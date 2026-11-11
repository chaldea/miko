using Miko.Platform;
using Shouldly;

namespace Miko.Browser.Tests;

public class BrowserKeyMapTests
{
    [Theory]
    [InlineData("Backspace", MikoKey.Backspace)]
    [InlineData("ArrowLeft", MikoKey.Left)]
    [InlineData("ArrowUp", MikoKey.Up)]
    [InlineData("NumpadEnter", MikoKey.Enter)]
    [InlineData("OSLeft", MikoKey.MetaLeft)]
    [InlineData("Backquote", MikoKey.Grave)]
    [InlineData("NumpadDivide", MikoKey.KeypadDivide)]
    [InlineData("KeyA", MikoKey.A)]
    [InlineData("KeyZ", MikoKey.Z)]
    [InlineData("Digit0", MikoKey.D0)]
    [InlineData("Digit9", MikoKey.D9)]
    [InlineData("F12", MikoKey.F12)]
    [InlineData("Unidentified", MikoKey.Unknown)]
    [InlineData("", MikoKey.Unknown)]
    public void Should_map_browser_physical_key_codes(string code, MikoKey expected)
        => BrowserKeyMap.ToMikoKey(code).ShouldBe(expected);

    [Fact]
    public void Should_prevent_default_only_for_editing_and_navigation_keys()
    {
        BrowserKeyMap.IsEditingOrNavigationKey(MikoKey.Backspace).ShouldBeTrue();
        BrowserKeyMap.IsEditingOrNavigationKey(MikoKey.Tab).ShouldBeTrue();
        BrowserKeyMap.IsEditingOrNavigationKey(MikoKey.A).ShouldBeFalse();
        BrowserKeyMap.IsEditingOrNavigationKey(MikoKey.Space).ShouldBeFalse("text insertion arrives through beforeinput");
        BrowserKeyMap.IsEditingOrNavigationKey(MikoKey.F5).ShouldBeFalse();
    }
}
