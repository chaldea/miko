using Miko.Common;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

public class AttributeSelectorTests
{
    [Fact]
    public void AttributeSelector_Exists_ShouldMatchWhenAttributePresent()
    {
        var input = new InputElement { Type = InputType.Checkbox };
        var selector = new AttributeSelector("Type", AttributeMatchOperator.Exists);

        selector.Matches(input).ShouldBeTrue();
    }

    [Fact]
    public void AttributeSelector_Exists_ShouldNotMatchWhenAttributeAbsent()
    {
        var div = new DivElement();
        var selector = new AttributeSelector("Type", AttributeMatchOperator.Exists);

        selector.Matches(div).ShouldBeFalse();
    }

    [Fact]
    public void AttributeSelector_Equals_ShouldMatchExactValue()
    {
        var input = new InputElement { Type = InputType.Checkbox };
        var selector = new AttributeSelector("Type", AttributeMatchOperator.Equals, "Checkbox");

        selector.Matches(input).ShouldBeTrue();
    }

    [Fact]
    public void AttributeSelector_Equals_ShouldBeCaseInsensitive()
    {
        var input = new InputElement { Type = InputType.Text };
        var selector = new AttributeSelector("Type", AttributeMatchOperator.Equals, "text");

        selector.Matches(input).ShouldBeTrue();
    }

    [Fact]
    public void AttributeSelector_Equals_ShouldNotMatchDifferentValue()
    {
        var input = new InputElement { Type = InputType.Text };
        var selector = new AttributeSelector("Type", AttributeMatchOperator.Equals, "Checkbox");

        selector.Matches(input).ShouldBeFalse();
    }

    [Fact]
    public void AttributeSelector_Prefix_ShouldMatchStartsWith()
    {
        var input = new InputElement { Type = InputType.Checkbox };
        var selector = new AttributeSelector("Type", AttributeMatchOperator.Prefix, "Check");

        selector.Matches(input).ShouldBeTrue();
    }

    [Fact]
    public void AttributeSelector_Suffix_ShouldMatchEndsWith()
    {
        var input = new InputElement { Type = InputType.Checkbox };
        var selector = new AttributeSelector("Type", AttributeMatchOperator.Suffix, "box");

        selector.Matches(input).ShouldBeTrue();
    }

    [Fact]
    public void AttributeSelector_Substring_ShouldMatchContains()
    {
        var input = new InputElement { Type = InputType.Checkbox };
        var selector = new AttributeSelector("Type", AttributeMatchOperator.Substring, "eckb");

        selector.Matches(input).ShouldBeTrue();
    }

    [Fact]
    public void AttributeSelector_Includes_ShouldMatchWordInList()
    {
        var div = new DivElement { Class = "form-control form-control-sm" };
        var selector = new AttributeSelector("Class", AttributeMatchOperator.Includes, "form-control-sm");

        selector.Matches(div).ShouldBeTrue();
    }

    [Fact]
    public void AttributeSelector_Specificity_ShouldBe10()
    {
        var selector = new AttributeSelector("Type", AttributeMatchOperator.Equals, "text");

        selector.Specificity.ShouldBe(10);
    }

    [Fact]
    public void ParseAttributeSelector_Exists_ShouldParse()
    {
        var selector = CssSelectorParser.Parse("[type]");

        selector.ShouldBeOfType<AttributeSelector>();
        var attrSel = (AttributeSelector)selector;
        attrSel.AttributeName.ShouldBe("type");
        attrSel.Operator.ShouldBe(AttributeMatchOperator.Exists);
    }

    [Fact]
    public void ParseAttributeSelector_Equals_DoubleQuoted_ShouldParse()
    {
        var selector = CssSelectorParser.Parse("[type=\"checkbox\"]");

        selector.ShouldBeOfType<AttributeSelector>();
        var attrSel = (AttributeSelector)selector;
        attrSel.AttributeName.ShouldBe("type");
        attrSel.Operator.ShouldBe(AttributeMatchOperator.Equals);
        attrSel.Value.ShouldBe("checkbox");
    }

    [Fact]
    public void ParseAttributeSelector_Equals_SingleQuoted_ShouldParse()
    {
        var selector = CssSelectorParser.Parse("[type='text']");

        selector.ShouldBeOfType<AttributeSelector>();
        var attrSel = (AttributeSelector)selector;
        attrSel.AttributeName.ShouldBe("type");
        attrSel.Operator.ShouldBe(AttributeMatchOperator.Equals);
        attrSel.Value.ShouldBe("text");
    }

    [Fact]
    public void ParseAttributeSelector_Equals_Unquoted_ShouldParse()
    {
        var selector = CssSelectorParser.Parse("[type=radio]");

        selector.ShouldBeOfType<AttributeSelector>();
        var attrSel = (AttributeSelector)selector;
        attrSel.Value.ShouldBe("radio");
    }

    [Fact]
    public void ParseAttributeSelector_WithWhitespace_ShouldParse()
    {
        var selector = CssSelectorParser.Parse("[ type = \"checkbox\" ]");

        selector.ShouldBeOfType<AttributeSelector>();
        var attrSel = (AttributeSelector)selector;
        attrSel.AttributeName.ShouldBe("type");
        attrSel.Value.ShouldBe("checkbox");
    }

    [Fact]
    public void ParseAttributeSelector_Includes_ShouldParse()
    {
        var selector = CssSelectorParser.Parse("[class~=\"active\"]");

        selector.ShouldBeOfType<AttributeSelector>();
        var attrSel = (AttributeSelector)selector;
        attrSel.Operator.ShouldBe(AttributeMatchOperator.Includes);
    }

    [Fact]
    public void ParseAttributeSelector_DashMatch_ShouldParse()
    {
        var selector = CssSelectorParser.Parse("[lang|=\"en\"]");

        selector.ShouldBeOfType<AttributeSelector>();
        var attrSel = (AttributeSelector)selector;
        attrSel.Operator.ShouldBe(AttributeMatchOperator.DashMatch);
    }

    [Fact]
    public void ParseAttributeSelector_Prefix_ShouldParse()
    {
        var selector = CssSelectorParser.Parse("[href^=\"https\"]");

        selector.ShouldBeOfType<AttributeSelector>();
        var attrSel = (AttributeSelector)selector;
        attrSel.Operator.ShouldBe(AttributeMatchOperator.Prefix);
    }

    [Fact]
    public void ParseAttributeSelector_Suffix_ShouldParse()
    {
        var selector = CssSelectorParser.Parse("[href$=\".pdf\"]");

        selector.ShouldBeOfType<AttributeSelector>();
        var attrSel = (AttributeSelector)selector;
        attrSel.Operator.ShouldBe(AttributeMatchOperator.Suffix);
    }

    [Fact]
    public void ParseAttributeSelector_Substring_ShouldParse()
    {
        var selector = CssSelectorParser.Parse("[href*=\"example\"]");

        selector.ShouldBeOfType<AttributeSelector>();
        var attrSel = (AttributeSelector)selector;
        attrSel.Operator.ShouldBe(AttributeMatchOperator.Substring);
    }

    [Fact]
    public void ParseAttributeSelector_InCompound_ShouldParse()
    {
        var selector = CssSelectorParser.Parse("input[type=\"checkbox\"]");

        selector.ShouldBeOfType<CompoundSelector>();
        var compound = (CompoundSelector)selector;
        compound.Selectors.ShouldContain(s => s is TagSelector);
        compound.Selectors.ShouldContain(s => s is AttributeSelector);
    }

    [Fact]
    public void ParseAttributeSelector_Multiple_ShouldParse()
    {
        var selector = CssSelectorParser.Parse("input[type=\"text\"][disabled]");

        selector.ShouldBeOfType<CompoundSelector>();
        var compound = (CompoundSelector)selector;
        var attrSelectors = compound.Selectors.OfType<AttributeSelector>().ToList();
        attrSelectors.Count.ShouldBe(2);
    }

    [Fact]
    public void AttributeSelector_InStylesheet_ShouldMatch()
    {
        var input = new InputElement { Type = InputType.Checkbox };
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["[type=\"checkbox\"]"] = new()
            {
                Width = Length.Px(20),
                Height = Length.Px(20),
            }
        });

        var resolver = new StyleResolver();
        var computed = resolver.Resolve(input, new List<StyleSheet> { sheet });

        computed.Width.Value.ShouldBe(20);
        computed.Height.Value.ShouldBe(20);
    }

    [Fact]
    public void AttributeSelector_CombinedWithClass_ShouldMatch()
    {
        var input = new InputElement { Type = InputType.Text };
        input.Class = "form-control";

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["input[type=\"text\"].form-control"] = new()
            {
                Padding = new Padding(Length.Px(10), Length.Px(10)),
            }
        });

        var resolver = new StyleResolver();
        var computed = resolver.Resolve(input, new List<StyleSheet> { sheet });

        computed.PaddingTop.Value.ShouldBe(10);
    }
}
