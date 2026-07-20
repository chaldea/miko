using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

public class CssObjectTests
{
    [Fact]
    public void Register_SimpleClassSelector()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".btn"] = new() { Display = Display.Block }
        });

        sheet.Rules.Count.ShouldBe(1);
        var element = new DivElement { Class = "btn" };
        new StyleResolver().Resolve(element, [sheet]).Display.ShouldBe(Display.Block);
    }

    [Fact]
    public void Register_BoxSizingOnlyRule()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["*"] = new() { BoxSizing = BoxSizing.BorderBox }
        });

        sheet.Rules.Count.ShouldBe(1);
        var element = new DivElement();
        new StyleResolver().Resolve(element, [sheet]).BoxSizing.ShouldBe(BoxSizing.BorderBox);
    }

    [Fact]
    public void Register_TagSelector()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { Color = Color.Red }
        });

        var element = new DivElement();
        new StyleResolver().Resolve(element, [sheet]).Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void Register_IdSelector()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["#main"] = new() { Width = Length.Px(100) }
        });

        var element = new DivElement { Id = "main" };
        new StyleResolver().Resolve(element, [sheet]).Width.ShouldBe(Length.Px(100));
    }

    [Fact]
    public void Register_NestedDescendant()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".parent"] = new()
            {
                [".child"] = new() { Color = Color.Blue }
            }
        });

        var parent = new DivElement { Class = "parent" };
        var child = new DivElement { Class = "child" };
        parent.AddChild(child);

        new StyleResolver().Resolve(child, [sheet]).Color.ShouldBe(Color.Blue);
    }

    [Fact]
    public void Register_ParentReference_PseudoClass()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".btn"] = new()
            {
                Color = Color.Blue,
                ["&:hover"] = new() { Color = Color.Red }
            }
        });

        // & expands to .btn:hover
        sheet.Rules.Count.ShouldBe(2);
        var element = new DivElement { Class = "btn" };
        element.SetState(ElementState.Hover);
        new StyleResolver().Resolve(element, [sheet]).Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void Register_ParentReference_CompoundClass()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".nav-pills"] = new()
            {
                ["&.active"] = new() { BackgroundColor = Color.Blue }
            }
        });

        var element = new DivElement { Class = "nav-pills active" };
        new StyleResolver().Resolve(element, [sheet]).BackgroundColor.ShouldBe(Color.Blue);
    }

    [Fact]
    public void Register_ChildCombinator()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".parent > .child"] = new() { Color = Color.Green }
        });

        var parent = new DivElement { Class = "parent" };
        var child = new DivElement { Class = "child" };
        parent.AddChild(child);

        new StyleResolver().Resolve(child, [sheet]).Color.ShouldBe(Color.Green);
    }

    [Fact]
    public void Register_GroupSelector()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["h1, h2, h3"] = new() { FontWeight = FontWeight.Bold }
        });

        var h1 = new H1Element();
        var h2 = new H2Element();
        new StyleResolver().Resolve(h1, [sheet]).FontWeight.ShouldBe(FontWeight.Bold);
        new StyleResolver().Resolve(h2, [sheet]).FontWeight.ShouldBe(FontWeight.Bold);
    }

    [Fact]
    public void Register_PseudoClass_FirstOfType()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".item:first-of-type"] = new() { Color = Color.Red }
        });

        var parent = new DivElement();
        var first = new DivElement { Class = "item" };
        var second = new DivElement { Class = "item" };
        parent.AddChild(first);
        parent.AddChild(second);

        new StyleResolver().Resolve(first, [sheet]).Color.ShouldBe(Color.Red);
        new StyleResolver().Resolve(second, [sheet]).Color.ShouldBe(Color.Black);
    }

    [Fact]
    public void Register_NotSelector()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".item:not(:first-of-type)"] = new() { Color = Color.Blue }
        });

        var parent = new DivElement();
        var first = new DivElement { Class = "item" };
        var second = new DivElement { Class = "item" };
        parent.AddChild(first);
        parent.AddChild(second);

        new StyleResolver().Resolve(first, [sheet]).Color.ShouldBe(Color.Black);
        new StyleResolver().Resolve(second, [sheet]).Color.ShouldBe(Color.Blue);
    }

    [Fact]
    public void Register_DeepNesting()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".a"] = new()
            {
                [".b"] = new()
                {
                    [".c"] = new() { Color = Color.Red }
                }
            }
        });

        // Expands to: .a .b .c
        var a = new DivElement { Class = "a" };
        var b = new DivElement { Class = "b" };
        var c = new DivElement { Class = "c" };
        a.AddChild(b);
        b.AddChild(c);

        new StyleResolver().Resolve(c, [sheet]).Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void Register_MultipleProperties()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                Width = Length.Px(100),
                Height = Length.Px(200),
                BackgroundColor = Color.Red,
                Padding = new Padding(16)
            }
        });

        var element = new DivElement { Class = "box" };
        var resolved = new StyleResolver().Resolve(element, [sheet]);
        resolved.Width.ShouldBe(Length.Px(100));
        resolved.Height.ShouldBe(Length.Px(200));
        resolved.BackgroundColor.ShouldBe(Color.Red);
        resolved.PaddingTop.ShouldBe(Length.Px(16));
    }

    [Fact]
    public void Register_NoStyleProperties_NoRule()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".parent"] = new()
            {
                [".child"] = new() { Color = Color.Red }
            }
        });

        // .parent has no style properties, so only 1 rule for .parent .child
        sheet.Rules.Count.ShouldBe(1);
    }

    [Fact]
    public void Register_ParentWithStyleAndChildren()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".parent"] = new()
            {
                Color = Color.Blue,
                [".child"] = new() { Color = Color.Red }
            }
        });

        // Both .parent and .parent .child should have rules
        sheet.Rules.Count.ShouldBe(2);
    }

    // --- Spread / merge operator ["..."] (ISSUE-095) --------------------------------------------

    [Fact]
    public void Spread_MergesMixinIntoRule()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                ["..."] = new() { Color = Color.Red },
                Display = Display.Flex,
            }
        });

        var element = new DivElement { Class = "box" };
        var resolved = new StyleResolver().Resolve(element, [sheet]);
        resolved.Color.ShouldBe(Color.Red);       // filled by the mixin
        resolved.Display.ShouldBe(Display.Flex);  // the rule's own property
    }

    [Fact]
    public void Spread_DirectPropertyWinsOverMixin_MixinFirst()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                ["..."] = new() { Color = Color.Red },
                Color = Color.Blue,   // direct property wins
            }
        });

        var element = new DivElement { Class = "box" };
        new StyleResolver().Resolve(element, [sheet]).Color.ShouldBe(Color.Blue);
    }

    [Fact]
    public void Spread_DirectPropertyWinsOverMixin_DirectFirst()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                Color = Color.Blue,   // direct property wins regardless of source order
                ["..."] = new() { Color = Color.Red },
            }
        });

        var element = new DivElement { Class = "box" };
        new StyleResolver().Resolve(element, [sheet]).Color.ShouldBe(Color.Blue);
    }

    [Fact]
    public void Spread_MultipleMixins_FirstWins_AndNonOverlappingApply()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                ["..."] = new() { Color = Color.Red, Width = Length.Px(10) },
                ["..."] = new() { Color = Color.Green, Height = Length.Px(20) },
            }
        });

        var element = new DivElement { Class = "box" };
        var resolved = new StyleResolver().Resolve(element, [sheet]);
        resolved.Color.ShouldBe(Color.Red);       // first mixin wins the shared property
        resolved.Width.ShouldBe(Length.Px(10));   // only in first mixin
        resolved.Height.ShouldBe(Length.Px(20));  // only in second mixin
    }

    [Fact]
    public void Spread_MixinOnlyRule_IsEmitted()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                ["..."] = new() { Display = Display.Block },
            }
        });

        sheet.Rules.Count.ShouldBe(1);
        var element = new DivElement { Class = "box" };
        new StyleResolver().Resolve(element, [sheet]).Display.ShouldBe(Display.Block);
    }

    [Fact]
    public void Spread_CoexistsWithNestedChild()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".parent"] = new()
            {
                ["..."] = new() { Color = Color.Red },
                [".child"] = new() { Color = Color.Blue },
            }
        });

        // One rule for .parent (with the merged mixin) and one for .parent .child.
        sheet.Rules.Count.ShouldBe(2);

        var parent = new DivElement { Class = "parent" };
        var child = new DivElement { Class = "child" };
        parent.AddChild(child);

        new StyleResolver().Resolve(parent, [sheet]).Color.ShouldBe(Color.Red);
        new StyleResolver().Resolve(child, [sheet]).Color.ShouldBe(Color.Blue);
    }

    [Fact]
    public void Spread_ReadingSpreadKey_Throws()
    {
        var css = new CssObject { ["..."] = new() { Color = Color.Red } };
        Should.Throw<InvalidOperationException>(() => _ = css["..."]);
    }
}
