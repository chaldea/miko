using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

public class DynamicStyleSheetTests
{
    [Fact]
    public void AddingRulesToAnAttachedSheetInvalidatesTheLayoutCache()
    {
        var engine = new LayoutEngine(new MutationTracker());
        var root = new DivElement { Class = "test" };
        var sheet = new StyleSheet();
        var sheets = new List<StyleSheet> { sheet };
        engine.Layout(root, sheets, 300, 200);
        engine.IsLayoutCurrent(root, sheets, 300, 200).ShouldBeTrue();

        sheet.Add(new CssObject { [".test"] = new() { Width = Length.Px(75) } });
        engine.IsLayoutCurrent(root, sheets, 300, 200).ShouldBeFalse();
        engine.Layout(root, sheets, 300, 200).BoxModel.Content.Width.ShouldBe(75);
        engine.IsLayoutCurrent(root, sheets, 300, 200).ShouldBeTrue();
    }
}
