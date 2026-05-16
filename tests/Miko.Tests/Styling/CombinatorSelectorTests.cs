using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

public class CombinatorSelectorTests
{
    // ========================================
    // 后代选择器 (A B) Tests
    // ========================================

    [Fact]
    public void DescendantSelector_ShouldMatchDirectChild()
    {
        var parent = new DivElement { Class = "container" };
        var child = new SpanElement();
        parent.AddChild(child);

        var selector = new DescendantSelector(new ClassSelector("container"), new TagSelector("span"));
        selector.Matches(child).ShouldBeTrue();
    }

    [Fact]
    public void DescendantSelector_ShouldMatchDeepDescendant()
    {
        var grandparent = new DivElement { Class = "container" };
        var parent = new DivElement();
        var child = new SpanElement();
        grandparent.AddChild(parent);
        parent.AddChild(child);

        var selector = new DescendantSelector(new ClassSelector("container"), new TagSelector("span"));
        selector.Matches(child).ShouldBeTrue();
    }

    [Fact]
    public void DescendantSelector_ShouldNotMatchWhenNoMatchingAncestor()
    {
        var parent = new DivElement { Class = "other" };
        var child = new SpanElement();
        parent.AddChild(child);

        var selector = new DescendantSelector(new ClassSelector("container"), new TagSelector("span"));
        selector.Matches(child).ShouldBeFalse();
    }

    [Fact]
    public void DescendantSelector_ShouldNotMatchWhenTargetDoesNotMatch()
    {
        var parent = new DivElement { Class = "container" };
        var child = new DivElement();
        parent.AddChild(child);

        var selector = new DescendantSelector(new ClassSelector("container"), new TagSelector("span"));
        selector.Matches(child).ShouldBeFalse();
    }

    [Fact]
    public void DescendantSelector_ShouldNotMatchRootElement()
    {
        var root = new DivElement { Class = "container" };

        var selector = new DescendantSelector(new ClassSelector("container"), new TagSelector("div"));
        selector.Matches(root).ShouldBeFalse();
    }

    [Fact]
    public void DescendantSelector_Specificity_ShouldBeSumOfBothSelectors()
    {
        var selector = new DescendantSelector(new ClassSelector("container"), new TagSelector("span"));
        selector.Specificity.ShouldBe(11); // class(10) + tag(1)
    }

    [Fact]
    public void DescendantSelector_FluentApi_ShouldWork()
    {
        var parent = new DivElement { Class = "form-check" };
        var child = new InputElement { Class = "form-check-input" };
        parent.AddChild(child);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Class("form-check")
            .Descendant<InputElement>(new ClassSelector("form-check-input"))
            .Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(child, [sheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void DescendantSelector_StaticFactory_ShouldWork()
    {
        var parent = new DivElement { Class = "container" };
        var child = new SpanElement();
        parent.AddChild(child);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Descendant(new ClassSelector("container"), new TagSelector("span"))
            .Set(x => x.BackgroundColor, Color.Blue));

        new StyleResolver().Resolve(child, [sheet]).BackgroundColor.ShouldBe(Color.Blue);
    }

    // ========================================
    // 子选择器 (A > B) Tests
    // ========================================

    [Fact]
    public void ChildSelector_ShouldMatchDirectChild()
    {
        var parent = new DivElement { Class = "row" };
        var child = new SpanElement();
        parent.AddChild(child);

        var selector = new ChildSelector(new ClassSelector("row"), new TagSelector("span"));
        selector.Matches(child).ShouldBeTrue();
    }

    [Fact]
    public void ChildSelector_ShouldNotMatchGrandchild()
    {
        var grandparent = new DivElement { Class = "row" };
        var parent = new DivElement();
        var child = new SpanElement();
        grandparent.AddChild(parent);
        parent.AddChild(child);

        var selector = new ChildSelector(new ClassSelector("row"), new TagSelector("span"));
        selector.Matches(child).ShouldBeFalse();
    }

    [Fact]
    public void ChildSelector_ShouldNotMatchWhenParentDoesNotMatch()
    {
        var parent = new DivElement { Class = "other" };
        var child = new SpanElement();
        parent.AddChild(child);

        var selector = new ChildSelector(new ClassSelector("row"), new TagSelector("span"));
        selector.Matches(child).ShouldBeFalse();
    }

    [Fact]
    public void ChildSelector_ShouldNotMatchRootElement()
    {
        var root = new DivElement { Class = "row" };

        var selector = new ChildSelector(new ClassSelector("row"), new TagSelector("div"));
        selector.Matches(root).ShouldBeFalse();
    }

    [Fact]
    public void ChildSelector_Specificity_ShouldBeSumOfBothSelectors()
    {
        var selector = new ChildSelector(new IdSelector("main"), new ClassSelector("btn"));
        selector.Specificity.ShouldBe(110); // id(100) + class(10)
    }

    [Fact]
    public void ChildSelector_FluentApi_ShouldWork()
    {
        var parent = new DivElement { Class = "btn-group" };
        var child = new ButtonElement { Class = "btn" };
        parent.AddChild(child);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Class("btn-group")
            .Child<ButtonElement>(new ClassSelector("btn"))
            .Set(x => x.BackgroundColor, Color.Green));

        new StyleResolver().Resolve(child, [sheet]).BackgroundColor.ShouldBe(Color.Green);
    }

    [Fact]
    public void ChildSelector_StaticFactory_ShouldWork()
    {
        var parent = new DivElement { Class = "row" };
        var child = new DivElement();
        parent.AddChild(child);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Child(new ClassSelector("row"), new UniversalSelector())
            .Set(x => x.BackgroundColor, Color.Green));

        new StyleResolver().Resolve(child, [sheet]).BackgroundColor.ShouldBe(Color.Green);
    }

    // ========================================
    // 相邻兄弟选择器 (A + B) Tests
    // ========================================

    [Fact]
    public void AdjacentSiblingSelector_ShouldMatchImmediateNextSibling()
    {
        var parent = new DivElement();
        var first = new DivElement { Class = "breadcrumb-item" };
        var second = new DivElement { Class = "breadcrumb-item" };
        parent.AddChild(first);
        parent.AddChild(second);

        var selector = new AdjacentSiblingSelector(
            new ClassSelector("breadcrumb-item"),
            new ClassSelector("breadcrumb-item"));
        selector.Matches(second).ShouldBeTrue();
    }

    [Fact]
    public void AdjacentSiblingSelector_ShouldNotMatchFirstChild()
    {
        var parent = new DivElement();
        var first = new DivElement { Class = "breadcrumb-item" };
        parent.AddChild(first);

        var selector = new AdjacentSiblingSelector(
            new ClassSelector("breadcrumb-item"),
            new ClassSelector("breadcrumb-item"));
        selector.Matches(first).ShouldBeFalse();
    }

    [Fact]
    public void AdjacentSiblingSelector_ShouldNotMatchNonAdjacentSibling()
    {
        var parent = new DivElement();
        var first = new DivElement { Class = "item" };
        var middle = new SpanElement();
        var third = new DivElement { Class = "item" };
        parent.AddChild(first);
        parent.AddChild(middle);
        parent.AddChild(third);

        var selector = new AdjacentSiblingSelector(
            new ClassSelector("item"),
            new ClassSelector("item"));
        selector.Matches(third).ShouldBeFalse();
    }

    [Fact]
    public void AdjacentSiblingSelector_ShouldNotMatchWhenPreviousDoesNotMatch()
    {
        var parent = new DivElement();
        var first = new DivElement { Class = "other" };
        var second = new DivElement { Class = "item" };
        parent.AddChild(first);
        parent.AddChild(second);

        var selector = new AdjacentSiblingSelector(
            new ClassSelector("item"),
            new ClassSelector("item"));
        selector.Matches(second).ShouldBeFalse();
    }

    [Fact]
    public void AdjacentSiblingSelector_ShouldNotMatchWithoutParent()
    {
        var element = new DivElement { Class = "item" };

        var selector = new AdjacentSiblingSelector(
            new ClassSelector("item"),
            new ClassSelector("item"));
        selector.Matches(element).ShouldBeFalse();
    }

    [Fact]
    public void AdjacentSiblingSelector_Specificity_ShouldBeSumOfBothSelectors()
    {
        var selector = new AdjacentSiblingSelector(
            new ClassSelector("item"),
            new ClassSelector("item"));
        selector.Specificity.ShouldBe(20); // class(10) + class(10)
    }

    [Fact]
    public void AdjacentSiblingSelector_FluentApi_ShouldWork()
    {
        var parent = new DivElement();
        var first = new DivElement { Class = "breadcrumb-item" };
        var second = new DivElement { Class = "breadcrumb-item" };
        parent.AddChild(first);
        parent.AddChild(second);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Class("breadcrumb-item")
            .Adjacent<Element>(new ClassSelector("breadcrumb-item"))
            .Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(second, [sheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void AdjacentSiblingSelector_StaticFactory_ShouldWork()
    {
        var parent = new DivElement();
        var first = new H1Element();
        var second = new ParagraphElement();
        parent.AddChild(first);
        parent.AddChild(second);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Adjacent(new TagSelector("h1"), new TagSelector("p"))
            .Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(second, [sheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    // ========================================
    // 通用兄弟选择器 (A ~ B) Tests
    // ========================================

    [Fact]
    public void GeneralSiblingSelector_ShouldMatchImmediateNextSibling()
    {
        var parent = new DivElement();
        var first = new DivElement { Class = "trigger" };
        var second = new SpanElement { Class = "target" };
        parent.AddChild(first);
        parent.AddChild(second);

        var selector = new GeneralSiblingSelector(
            new ClassSelector("trigger"),
            new ClassSelector("target"));
        selector.Matches(second).ShouldBeTrue();
    }

    [Fact]
    public void GeneralSiblingSelector_ShouldMatchNonAdjacentSibling()
    {
        var parent = new DivElement();
        var first = new DivElement { Class = "trigger" };
        var middle = new SpanElement();
        var third = new DivElement { Class = "target" };
        parent.AddChild(first);
        parent.AddChild(middle);
        parent.AddChild(third);

        var selector = new GeneralSiblingSelector(
            new ClassSelector("trigger"),
            new ClassSelector("target"));
        selector.Matches(third).ShouldBeTrue();
    }

    [Fact]
    public void GeneralSiblingSelector_ShouldNotMatchElementBeforeTrigger()
    {
        var parent = new DivElement();
        var first = new DivElement { Class = "target" };
        var second = new DivElement { Class = "trigger" };
        parent.AddChild(first);
        parent.AddChild(second);

        var selector = new GeneralSiblingSelector(
            new ClassSelector("trigger"),
            new ClassSelector("target"));
        selector.Matches(first).ShouldBeFalse();
    }

    [Fact]
    public void GeneralSiblingSelector_ShouldNotMatchWithoutParent()
    {
        var element = new DivElement { Class = "target" };

        var selector = new GeneralSiblingSelector(
            new ClassSelector("trigger"),
            new ClassSelector("target"));
        selector.Matches(element).ShouldBeFalse();
    }

    [Fact]
    public void GeneralSiblingSelector_ShouldNotMatchWhenNoMatchingPreviousSibling()
    {
        var parent = new DivElement();
        var first = new DivElement { Class = "other" };
        var second = new DivElement { Class = "target" };
        parent.AddChild(first);
        parent.AddChild(second);

        var selector = new GeneralSiblingSelector(
            new ClassSelector("trigger"),
            new ClassSelector("target"));
        selector.Matches(second).ShouldBeFalse();
    }

    [Fact]
    public void GeneralSiblingSelector_Specificity_ShouldBeSumOfBothSelectors()
    {
        var selector = new GeneralSiblingSelector(
            new TagSelector("input"),
            new ClassSelector("label"));
        selector.Specificity.ShouldBe(11); // tag(1) + class(10)
    }

    [Fact]
    public void GeneralSiblingSelector_FluentApi_ShouldWork()
    {
        var parent = new DivElement();
        var input = new InputElement { Class = "form-check-input" };
        var label = new LabelElement { Class = "form-check-label" };
        parent.AddChild(input);
        parent.AddChild(label);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Class("form-check-input")
            .Sibling<LabelElement>(new ClassSelector("form-check-label"))
            .Set(x => x.Color, Color.Gray));

        new StyleResolver().Resolve(label, [sheet]).Color.ShouldBe(Color.Gray);
    }

    [Fact]
    public void GeneralSiblingSelector_StaticFactory_ShouldWork()
    {
        var parent = new DivElement();
        var trigger = new DivElement { Class = "trigger" };
        var middle = new SpanElement();
        var target = new ParagraphElement();
        parent.AddChild(trigger);
        parent.AddChild(middle);
        parent.AddChild(target);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Sibling(new ClassSelector("trigger"), new TagSelector("p"))
            .Set(x => x.BackgroundColor, Color.Blue));

        new StyleResolver().Resolve(target, [sheet]).BackgroundColor.ShouldBe(Color.Blue);
    }

    // ========================================
    // 组合器与样式级联集成测试
    // ========================================

    [Fact]
    public void CombinatorSelector_ShouldParticipateInCascade()
    {
        var parent = new DivElement { Class = "container" };
        var child = new SpanElement { Class = "text" };
        parent.AddChild(child);

        var sheet = new StyleSheet();
        // descendant selector: .container span => specificity 11
        sheet.AddRule(Style.Descendant(new ClassSelector("container"), new TagSelector("span"))
            .Set(x => x.Color, Color.Red));
        // class selector: .text => specificity 10
        sheet.AddRule(Style.Class("text")
            .Set(x => x.Color, Color.Blue));

        // descendant selector wins (11 > 10)
        new StyleResolver().Resolve(child, [sheet]).Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void ChildSelector_WithUniversalSelector_ShouldMatchAnyDirectChild()
    {
        var parent = new DivElement { Class = "row" };
        var child1 = new DivElement();
        var child2 = new SpanElement();
        parent.AddChild(child1);
        parent.AddChild(child2);

        var selector = new ChildSelector(new ClassSelector("row"), new UniversalSelector());
        selector.Matches(child1).ShouldBeTrue();
        selector.Matches(child2).ShouldBeTrue();
    }

    [Fact]
    public void FluentApi_Descendant_WithTagSelector_ShouldWork()
    {
        var parent = new DivElement { Class = "container" };
        var child = new ButtonElement();
        parent.AddChild(child);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Class("container")
            .Descendant<ButtonElement>()
            .Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(child, [sheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void FluentApi_Child_WithTagSelector_ShouldWork()
    {
        var parent = new DivElement { Class = "row" };
        var child = new SpanElement();
        parent.AddChild(child);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Class("row")
            .Child<SpanElement>()
            .Set(x => x.Color, Color.Blue));

        new StyleResolver().Resolve(child, [sheet]).Color.ShouldBe(Color.Blue);
    }
}
