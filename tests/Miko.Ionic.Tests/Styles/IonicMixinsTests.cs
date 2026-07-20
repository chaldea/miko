using Miko.Core.DomElements;
using Miko.Ionic.Styles;
using Miko.Styling;
using Shouldly;

namespace Miko.Ionic.Tests.Styles;

/// <summary>
/// ISSUE-095: <see cref="IonicMixins"/> ports Ionic's shared SCSS mixins as spreadable
/// <see cref="CssObject"/>s. Verifies <c>TextInherit()</c> sets its text properties to the
/// <c>inherit</c> keyword and spreads cleanly through the <c>["..."]</c> merge operator.
/// </summary>
public class IonicMixinsTests
{
    [Fact]
    public void TextInherit_SetsTextPropertiesToInheritKeyword()
    {
        var mixin = IonicMixins.TextInherit();

        mixin.Color!.Value.IsKeyword.ShouldBeTrue();
        mixin.Color!.Value.Keyword.ShouldBe(StyleKeyword.Inherit);
        mixin.FontSize!.Value.IsKeyword.ShouldBeTrue();
        mixin.FontSize!.Value.Keyword.ShouldBe(StyleKeyword.Inherit);
        mixin.FontFamily!.Value.Keyword.ShouldBe(StyleKeyword.Inherit);
        mixin.TextAlign!.Value.Keyword.ShouldBe(StyleKeyword.Inherit);
    }

    [Fact]
    public void TextInherit_SpreadsIntoRule_DirectPropertyWins()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".native"] = new()
            {
                ["..."] = IonicMixins.TextInherit(),
                Color = Common.Color.Red,   // explicit color overrides the mixin's inherit
            }
        });

        var rule = sheet.Rules.Single(r => r.Selector.Matches(new DivElement { Class = "native" }));
        // Explicit direct property wins over the spread mixin.
        rule.Style.Color!.Value.TryGetValue(out var color).ShouldBeTrue();
        color.ShouldBe(Common.Color.Red);
        // A property only in the mixin still comes through as the inherit keyword.
        rule.Style.FontSize!.Value.Keyword.ShouldBe(StyleKeyword.Inherit);
    }
}
