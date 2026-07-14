using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;

namespace Miko.Ionic.Tests;

/// <summary>
/// ISSUE-068: the Ionic stylesheet must carry BOTH the <c>md</c> and <c>ios</c> rule sets, each
/// scoped by the matching mode class, so switching a component's mode class re-styles it without
/// a stylesheet rebuild. A minimal element-matching probe verifies the scoped rules apply to the
/// right mode and the per-mode values differ.
/// </summary>
public class IonicStyleSheetTests
{
    // A bare <div> carrying the given classes — enough for selector matching.
    private static DivElement Div(string cls) => new() { Class = cls };

    private static StyleRule? FirstMatch(StyleSheet sheet, Element element) =>
        sheet.Rules
            .Where(r => r.Selector.Matches(element))
            .OrderByDescending(r => r.Selector.Specificity)
            .FirstOrDefault();

    [Fact]
    public void CreateAllModes_EmitsBothModeRuleSets()
    {
        var sheet = IonicStyleSheetFactory.CreateAllModes();

        // A md header element matches md-scoped rules; an ios header matches ios-scoped rules.
        FirstMatch(sheet, Div("md ion-header")).ShouldNotBeNull();
        FirstMatch(sheet, Div("ios ion-header")).ShouldNotBeNull();
    }

    [Fact]
    public void HeaderRules_DifferByMode()
    {
        var sheet = IonicStyleSheetFactory.CreateAllModes();

        // MD header uses an elevation box-shadow (no bottom border); iOS uses a hairline border
        // and no shadow. The matched rule for each mode reflects its theme values.
        var md = FirstMatch(sheet, Div("md ion-header"))!.Style;
        var ios = FirstMatch(sheet, Div("ios ion-header"))!.Style;

        md.BoxShadow.ShouldNotBeNull();
        md.BoxShadow!.Value.Value.Count.ShouldBeGreaterThan(0);

        // iOS: hairline bottom border with a visible width; MD: zero-width border.
        ios.BorderBottomWidth.ShouldNotBeNull();
        ios.BorderBottomWidth!.Value.Value.Value.ShouldBeGreaterThan(0f);
    }

    [Fact]
    public void ModeScopedRule_DoesNotMatchOtherMode()
    {
        var sheet = IonicStyleSheetFactory.CreateAllModes();

        // The ios-scoped header rule must NOT match an md header element.
        var iosHeaderRule = sheet.Rules.First(r =>
            r.Selector.Matches(Div("ios ion-header")) && r.Selector.Specificity >= 20);

        iosHeaderRule.Selector.Matches(Div("md ion-header")).ShouldBeFalse();
    }
}
