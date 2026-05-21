using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

public class CssSelectorParserTests
{
    [Fact]
    public void Parse_TagSelector()
    {
        var selector = CssSelectorParser.Parse("div");
        selector.ShouldBeOfType<TagSelector>();
    }

    [Fact]
    public void Parse_ClassSelector()
    {
        var selector = CssSelectorParser.Parse(".btn");
        selector.ShouldBeOfType<ClassSelector>();
    }

    [Fact]
    public void Parse_IdSelector()
    {
        var selector = CssSelectorParser.Parse("#main");
        selector.ShouldBeOfType<IdSelector>();
    }

    [Fact]
    public void Parse_UniversalSelector()
    {
        var selector = CssSelectorParser.Parse("*");
        selector.ShouldBeOfType<UniversalSelector>();
    }

    [Fact]
    public void Parse_CompoundSelector_TagAndClass()
    {
        var selector = CssSelectorParser.Parse("div.active");
        var compound = selector.ShouldBeOfType<CompoundSelector>();
        compound.Selectors.Count.ShouldBe(2);
        compound.Selectors[0].ShouldBeOfType<TagSelector>();
        compound.Selectors[1].ShouldBeOfType<ClassSelector>();
    }

    [Fact]
    public void Parse_CompoundSelector_ClassAndPseudoClass()
    {
        var selector = CssSelectorParser.Parse(".btn:hover");
        var compound = selector.ShouldBeOfType<CompoundSelector>();
        compound.Selectors[0].ShouldBeOfType<ClassSelector>();
        compound.Selectors[1].ShouldBeOfType<HoverSelector>();
    }

    [Fact]
    public void Parse_PseudoClasses()
    {
        CssSelectorParser.Parse(".a:hover").ShouldBeOfType<CompoundSelector>();
        CssSelectorParser.Parse(".a:active").ShouldBeOfType<CompoundSelector>();
        CssSelectorParser.Parse(".a:focus").ShouldBeOfType<CompoundSelector>();
        CssSelectorParser.Parse(".a:disabled").ShouldBeOfType<CompoundSelector>();
        CssSelectorParser.Parse(".a:enabled").ShouldBeOfType<CompoundSelector>();
        CssSelectorParser.Parse(".a:first-child").ShouldBeOfType<CompoundSelector>();
        CssSelectorParser.Parse(".a:last-child").ShouldBeOfType<CompoundSelector>();
        CssSelectorParser.Parse(".a:first-of-type").ShouldBeOfType<CompoundSelector>();
        CssSelectorParser.Parse(".a:last-of-type").ShouldBeOfType<CompoundSelector>();
    }

    [Fact]
    public void Parse_NotSelector()
    {
        var selector = CssSelectorParser.Parse(".item:not(:first-of-type)");
        var compound = selector.ShouldBeOfType<CompoundSelector>();
        compound.Selectors[0].ShouldBeOfType<ClassSelector>();
        var not = compound.Selectors[1].ShouldBeOfType<NotSelector>();
        not.Inner.ShouldBeOfType<FirstOfTypeSelector>();
    }

    [Fact]
    public void Parse_NotSelector_WithClass()
    {
        var selector = CssSelectorParser.Parse(".btn:not(.collapsed)");
        var compound = selector.ShouldBeOfType<CompoundSelector>();
        compound.Selectors[0].ShouldBeOfType<ClassSelector>();
        var not = compound.Selectors[1].ShouldBeOfType<NotSelector>();
        not.Inner.ShouldBeOfType<ClassSelector>();
    }

    [Fact]
    public void Parse_DescendantCombinator()
    {
        var selector = CssSelectorParser.Parse(".parent .child");
        var desc = selector.ShouldBeOfType<DescendantSelector>();
        desc.Ancestor.ShouldBeOfType<ClassSelector>();
        desc.Descendant.ShouldBeOfType<ClassSelector>();
    }

    [Fact]
    public void Parse_ChildCombinator()
    {
        var selector = CssSelectorParser.Parse(".parent > .child");
        var child = selector.ShouldBeOfType<ChildSelector>();
        child.Parent.ShouldBeOfType<ClassSelector>();
        child.Child.ShouldBeOfType<ClassSelector>();
    }

    [Fact]
    public void Parse_AdjacentSiblingCombinator()
    {
        var selector = CssSelectorParser.Parse(".a + .b");
        var adj = selector.ShouldBeOfType<AdjacentSiblingSelector>();
        adj.Previous.ShouldBeOfType<ClassSelector>();
        adj.Target.ShouldBeOfType<ClassSelector>();
    }

    [Fact]
    public void Parse_GeneralSiblingCombinator()
    {
        var selector = CssSelectorParser.Parse(".a ~ .b");
        var gen = selector.ShouldBeOfType<GeneralSiblingSelector>();
        gen.Previous.ShouldBeOfType<ClassSelector>();
        gen.Target.ShouldBeOfType<ClassSelector>();
    }

    [Fact]
    public void Parse_GroupSelector()
    {
        var selector = CssSelectorParser.Parse("h1, h2, h3");
        var group = selector.ShouldBeOfType<GroupSelector>();
        group.Selectors.Length.ShouldBe(3);
    }

    [Fact]
    public void Parse_ComplexSelector_ChildWithPseudoClass()
    {
        // .accordion-item:first-of-type > .accordion-header
        var selector = CssSelectorParser.Parse(".accordion-item:first-of-type > .accordion-header");
        var child = selector.ShouldBeOfType<ChildSelector>();
        child.Parent.ShouldBeOfType<CompoundSelector>();
        child.Child.ShouldBeOfType<ClassSelector>();
    }

    [Fact]
    public void Parse_MultiLevelDescendant()
    {
        var selector = CssSelectorParser.Parse(".a .b .c");
        var outer = selector.ShouldBeOfType<DescendantSelector>();
        outer.Ancestor.ShouldBeOfType<DescendantSelector>();
        outer.Descendant.ShouldBeOfType<ClassSelector>();
    }

    [Fact]
    public void Parse_MultipleClasses()
    {
        var selector = CssSelectorParser.Parse(".btn.active.large");
        var compound = selector.ShouldBeOfType<CompoundSelector>();
        compound.Selectors.Count.ShouldBe(3);
        compound.Selectors.ShouldAllBe(s => s is ClassSelector);
    }

    [Fact]
    public void Parse_PseudoElement_After()
    {
        var selector = CssSelectorParser.Parse(".btn::after");
        var compound = selector.ShouldBeOfType<CompoundSelector>();
        compound.Selectors.Count.ShouldBe(2);
        compound.Selectors[0].ShouldBeOfType<ClassSelector>();
        compound.Selectors[1].ShouldBeOfType<AfterPseudoElement>();
    }

    [Fact]
    public void Parse_PseudoElement_Before()
    {
        var selector = CssSelectorParser.Parse(".btn::before");
        var compound = selector.ShouldBeOfType<CompoundSelector>();
        compound.Selectors[0].ShouldBeOfType<ClassSelector>();
        compound.Selectors[1].ShouldBeOfType<BeforePseudoElement>();
    }

    [Fact]
    public void Parse_ComplexSelector_WithNotAndPseudoElement()
    {
        // .accordion-button:not(.collapsed)::after
        var selector = CssSelectorParser.Parse(".accordion-button:not(.collapsed)::after");
        var compound = selector.ShouldBeOfType<CompoundSelector>();
        compound.Selectors.Count.ShouldBe(3);
        compound.Selectors[0].ShouldBeOfType<ClassSelector>();
        compound.Selectors[1].ShouldBeOfType<NotSelector>();
        compound.Selectors[2].ShouldBeOfType<AfterPseudoElement>();
    }

    [Fact]
    public void Parse_PseudoElement_WithDescendantCombinator()
    {
        // .parent .child::before
        var selector = CssSelectorParser.Parse(".parent .child::before");
        var desc = selector.ShouldBeOfType<DescendantSelector>();
        desc.Ancestor.ShouldBeOfType<ClassSelector>();
        var compound = desc.Descendant.ShouldBeOfType<CompoundSelector>();
        compound.Selectors[0].ShouldBeOfType<ClassSelector>();
        compound.Selectors[1].ShouldBeOfType<BeforePseudoElement>();
    }

    [Fact]
    public void Parse_PseudoElement_WithChildCombinator()
    {
        // .parent > .child::after
        var selector = CssSelectorParser.Parse(".parent > .child::after");
        var child = selector.ShouldBeOfType<ChildSelector>();
        var compound = child.Child.ShouldBeOfType<CompoundSelector>();
        compound.Selectors[1].ShouldBeOfType<AfterPseudoElement>();
    }

    [Fact]
    public void Parse_TagWithPseudoElement()
    {
        // p::first-line would be parsed, but we only support ::before and ::after
        var selector = CssSelectorParser.Parse("p::before");
        var compound = selector.ShouldBeOfType<CompoundSelector>();
        compound.Selectors[0].ShouldBeOfType<TagSelector>();
        compound.Selectors[1].ShouldBeOfType<BeforePseudoElement>();
    }

    [Theory]
    [InlineData(".accordion-button:not(.collapsed)::after")]
    [InlineData(".btn::before")]
    [InlineData(".card::after")]
    [InlineData("div.container:first-child::before")]
    public void Parse_BootstrapStyleSelectors_ShouldNotThrow(string selectorString)
    {
        // These are real selectors from Bootstrap CSS that previously caused exceptions
        var exception = Record.Exception(() => CssSelectorParser.Parse(selectorString));
        exception.ShouldBeNull($"Selector '{selectorString}' should parse without throwing");
    }
}
