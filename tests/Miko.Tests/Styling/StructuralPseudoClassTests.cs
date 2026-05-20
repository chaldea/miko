using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

public class StructuralPseudoClassTests
{
    [Fact]
    public void FirstChild_ShouldMatchFirstChild()
    {
        var parent = new DivElement();
        var first = new DivElement();
        var second = new DivElement();
        parent.AddChild(first);
        parent.AddChild(second);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.For<DivElement>().FirstChild().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(first, [sheet]).BackgroundColor.ShouldBe(Color.Red);
        new StyleResolver().Resolve(second, [sheet]).BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void LastChild_ShouldMatchLastChild()
    {
        var parent = new DivElement();
        var first = new DivElement();
        var last = new DivElement();
        parent.AddChild(first);
        parent.AddChild(last);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.For<DivElement>().LastChild().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(first, [sheet]).BackgroundColor.ShouldBe(Color.Transparent);
        new StyleResolver().Resolve(last, [sheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void FirstOfType_ShouldMatchFirstElementOfSameType()
    {
        var parent = new DivElement();
        var span1 = new SpanElement();
        var div1 = new DivElement();
        var span2 = new SpanElement();
        parent.AddChild(span1);
        parent.AddChild(div1);
        parent.AddChild(span2);

        var selector = new CompoundSelector(new TagSelector("span"), new FirstOfTypeSelector());
        selector.Matches(span1).ShouldBeTrue();
        selector.Matches(span2).ShouldBeFalse();
    }

    [Fact]
    public void LastOfType_ShouldMatchLastElementOfSameType()
    {
        var parent = new DivElement();
        var span1 = new SpanElement();
        var div1 = new DivElement();
        var span2 = new SpanElement();
        parent.AddChild(span1);
        parent.AddChild(div1);
        parent.AddChild(span2);

        var selector = new CompoundSelector(new TagSelector("span"), new LastOfTypeSelector());
        selector.Matches(span1).ShouldBeFalse();
        selector.Matches(span2).ShouldBeTrue();
    }

    [Fact]
    public void Not_ShouldNegateInnerSelector()
    {
        var parent = new DivElement();
        var first = new DivElement();
        var second = new DivElement();
        parent.AddChild(first);
        parent.AddChild(second);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.For<DivElement>().Not(new FirstOfTypeSelector()).Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(first, [sheet]).BackgroundColor.ShouldBe(Color.Transparent);
        new StyleResolver().Resolve(second, [sheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void FluentApi_FirstOfType_ShouldWork()
    {
        var parent = new DivElement();
        var first = new DivElement();
        var second = new DivElement();
        parent.AddChild(first);
        parent.AddChild(second);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.For<DivElement>().FirstOfType().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(first, [sheet]).BackgroundColor.ShouldBe(Color.Red);
        new StyleResolver().Resolve(second, [sheet]).BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void FluentApi_LastOfType_ShouldWork()
    {
        var parent = new DivElement();
        var first = new DivElement();
        var second = new DivElement();
        parent.AddChild(first);
        parent.AddChild(second);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.For<DivElement>().LastOfType().Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(first, [sheet]).BackgroundColor.ShouldBe(Color.Transparent);
        new StyleResolver().Resolve(second, [sheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void GroupSelector_ShouldMatchAnyOfTheSelectors()
    {
        var div = new DivElement();
        var span = new SpanElement();
        var button = new ButtonElement();

        var selector = new GroupSelector(new TagSelector("div"), new TagSelector("span"));
        selector.Matches(div).ShouldBeTrue();
        selector.Matches(span).ShouldBeTrue();
        selector.Matches(button).ShouldBeFalse();
    }

    [Fact]
    public void StyleGroup_ShouldApplyToMultipleSelectors()
    {
        var div = new DivElement();
        var span = new SpanElement();
        var button = new ButtonElement();

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Group(new TagSelector("div"), new TagSelector("span")).Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(div, [sheet]).BackgroundColor.ShouldBe(Color.Red);
        new StyleResolver().Resolve(span, [sheet]).BackgroundColor.ShouldBe(Color.Red);
        new StyleResolver().Resolve(button, [sheet]).BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void CombinatorBuilder_Child_ShouldChain()
    {
        // .accordion-item:first-of-type > .accordion-header
        var parent = new DivElement();
        var item = new DivElement { Class = "accordion-item" };
        var header = new DivElement { Class = "accordion-header" };
        parent.AddChild(item);
        item.AddChild(header);

        var sheet = new StyleSheet();
        sheet.AddRule(
            Style.Class("accordion-item").FirstOfType()
                .Child<Element>(new ClassSelector("accordion-header"))
                .Set(x => x.BackgroundColor, Color.Red)
        );

        new StyleResolver().Resolve(header, [sheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void CombinatorBuilder_Child_Descendant_ShouldChain()
    {
        // .accordion-item:first-of-type > .accordion-header .accordion-button
        var parent = new DivElement();
        var item = new DivElement { Class = "accordion-item" };
        var header = new DivElement { Class = "accordion-header" };
        var button = new ButtonElement { Class = "accordion-button" };
        parent.AddChild(item);
        item.AddChild(header);
        header.AddChild(button);

        var sheet = new StyleSheet();
        sheet.AddRule(
            Style.Class("accordion-item").FirstOfType()
                .Child<Element>(new ClassSelector("accordion-header"))
                .Descendant(new ClassSelector("accordion-button"))
                .Set(x => x.BackgroundColor, Color.Red)
        );

        new StyleResolver().Resolve(button, [sheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void Not_FirstOfType_ShouldMatchNonFirstElements()
    {
        // .accordion-item:not(:first-of-type)
        var parent = new DivElement();
        var first = new DivElement { Class = "accordion-item" };
        var second = new DivElement { Class = "accordion-item" };
        parent.AddChild(first);
        parent.AddChild(second);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Class("accordion-item").Not(new FirstOfTypeSelector()).Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(first, [sheet]).BackgroundColor.ShouldBe(Color.Transparent);
        new StyleResolver().Resolve(second, [sheet]).BackgroundColor.ShouldBe(Color.Red);
    }
}
