using Miko.Common;
using Miko.Ionic.Components;
using Miko.Styling;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Stylesheet cascade-override assertions for <see cref="IonIcon"/> (ISSUE-107): a rule
/// from an application stylesheet must override the component host box
/// (<c>.ion-icon.{mode}</c> — a 1em × 1em box), even though the compound host selector
/// has higher specificity. Mirrors how outer-document rules beat shadow-tree
/// <c>:host</c> rules in the browser (CSS Scoping); the Ionic sheet sits in a lower
/// cascade layer (<see cref="IonicStyleSheetFactory.CascadeLayer"/>).
/// </summary>
public class IonIconStyleTests : IonicComponentTestBase
{
    [Fact]
    public void IonIcon_UserClass_OverridesHostBoxSize()
    {
        // The app sheet is added AFTER the Ionic sheet (builder.AddIonic() then
        // builder.AddStyleSheet(app)). Before the layer fix, the compound
        // .ion-icon.{mode} selector (specificity 20) beat the single app class (10)
        // regardless of that order, so Width/Height could not be overridden.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var appSheet = new StyleSheet();
        appSheet.Add(new CssObject
        {
            [".component-icon"] = new()
            {
                Width = Length.Px(50),
                Height = Length.Px(50),
                BackgroundColor = Color.FromHex("0054e9"),
            }
        });
        Context.AddStyleSheet(appSheet);

        var cut = Context.Render<IonIcon>(p => p
            .Add(nameof(IonIcon.Icon), "triangle")
            .Add(nameof(IonIcon.Class), "component-icon"));
        var computed = cut.GetComputedStyle(cut.Root)!;

        computed.Width.Value.ShouldBe(50f);
        computed.Height.Value.ShouldBe(50f);
        computed.BackgroundColor.ShouldBe(Color.FromHex("0054e9"));

        var box = cut.GetBoxModel(cut.Root)!.BorderBox;
        box.Width.ShouldBe(50f, 0.01f);
        box.Height.ShouldBe(50f, 0.01f);
    }
}
