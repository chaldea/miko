using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using Xunit;

namespace Miko.Tests.Styling;

/// <summary>
/// 测试 input[type="range"] 伪元素支持 (::range-thumb, ::range-track, ::range-progress)
/// </summary>
public class RangePseudoElementTests
{
    #region Parser Tests

    [Fact]
    public void ParseRangeThumbPseudoElement_ShouldSucceed()
    {
        // Act
        var selector = CssSelectorParser.Parse("input::range-thumb");

        // Assert
        selector.ShouldBeOfType<CompoundSelector>();
        var compound = (CompoundSelector)selector;
        compound.Selectors.ShouldContain(s => s is RangeThumbPseudoElement);
    }

    [Fact]
    public void ParseRangeTrackPseudoElement_ShouldSucceed()
    {
        // Act
        var selector = CssSelectorParser.Parse("input::range-track");

        // Assert
        selector.ShouldBeOfType<CompoundSelector>();
        var compound = (CompoundSelector)selector;
        compound.Selectors.ShouldContain(s => s is RangeTrackPseudoElement);
    }

    [Fact]
    public void ParseRangeProgressPseudoElement_ShouldSucceed()
    {
        // Act
        var selector = CssSelectorParser.Parse("input::range-progress");

        // Assert
        selector.ShouldBeOfType<CompoundSelector>();
        var compound = (CompoundSelector)selector;
        compound.Selectors.ShouldContain(s => s is RangeProgressPseudoElement);
    }

    [Fact]
    public void ParseRangeThumbWithClass_ShouldSucceed()
    {
        // Act
        var selector = CssSelectorParser.Parse(".form-range::range-thumb");

        // Assert
        selector.ShouldBeOfType<CompoundSelector>();
        var compound = (CompoundSelector)selector;
        compound.Selectors.ShouldContain(s => s is ClassSelector);
        compound.Selectors.ShouldContain(s => s is RangeThumbPseudoElement);
    }

    [Fact]
    public void ParseRangeTrackWithAttributeSelector_ShouldSucceed()
    {
        // Act
        var selector = CssSelectorParser.Parse("[type=\"range\"]::range-track");

        // Assert
        selector.ShouldBeOfType<CompoundSelector>();
        var compound = (CompoundSelector)selector;
        compound.Selectors.ShouldContain(s => s is AttributeSelector);
        compound.Selectors.ShouldContain(s => s is RangeTrackPseudoElement);
    }

    #endregion

    #region CssObject Integration Tests

    [Fact]
    public void CssObject_RangeThumb_ShouldCreatePseudoElementRule()
    {
        // Arrange
        var css = new CssObject
        {
            [".form-range::range-thumb"] = new()
            {
                Width = Length.Rem(1),
                Height = Length.Rem(1),
                BackgroundColor = Color.FromHex("#0d6efd"),
                BorderRadius = Length.Rem(1),
            }
        };

        var sheet = new StyleSheet();

        // Act
        sheet.Add(css);

        // Assert
        sheet.PseudoElementRules.Count.ShouldBe(1);
        var rule = sheet.PseudoElementRules[0];
        rule.Type.ShouldBe(PseudoElementType.RangeThumb);
        rule.Style.Width.ShouldBe(Length.Rem(1));
        rule.Style.Height.ShouldBe(Length.Rem(1));
        rule.Style.BackgroundColor.ShouldBe(Color.FromHex("#0d6efd"));
        rule.Style.BorderRadius.ShouldBe(Length.Rem(1));
    }

    [Fact]
    public void CssObject_RangeTrack_ShouldCreatePseudoElementRule()
    {
        // Arrange
        var css = new CssObject
        {
            [".form-range::range-track"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Rem(0.5f),
                BackgroundColor = Color.FromHex("#e9ecef"),
                BorderRadius = Length.Rem(1),
            }
        };

        var sheet = new StyleSheet();

        // Act
        sheet.Add(css);

        // Assert
        sheet.PseudoElementRules.Count.ShouldBe(1);
        var rule = sheet.PseudoElementRules[0];
        rule.Type.ShouldBe(PseudoElementType.RangeTrack);
        rule.Style.Width.ShouldBe(Length.Percent(100));
        rule.Style.Height.ShouldBe(Length.Rem(0.5f));
        rule.Style.BackgroundColor.ShouldBe(Color.FromHex("#e9ecef"));
    }

    [Fact]
    public void CssObject_RangeProgress_ShouldCreatePseudoElementRule()
    {
        // Arrange
        var css = new CssObject
        {
            [".form-range::range-progress"] = new()
            {
                BackgroundColor = Color.FromHex("#0d6efd"),
            }
        };

        var sheet = new StyleSheet();

        // Act
        sheet.Add(css);

        // Assert
        sheet.PseudoElementRules.Count.ShouldBe(1);
        var rule = sheet.PseudoElementRules[0];
        rule.Type.ShouldBe(PseudoElementType.RangeProgress);
        rule.Style.BackgroundColor.ShouldBe(Color.FromHex("#0d6efd"));
    }

    [Fact]
    public void CssObject_MultipleRangePseudoElements_ShouldCreateMultipleRules()
    {
        // Arrange
        var css = new CssObject
        {
            [".form-range::range-thumb"] = new()
            {
                Width = Length.Rem(1),
                BackgroundColor = Color.FromHex("#0d6efd"),
            },
            [".form-range::range-track"] = new()
            {
                Height = Length.Rem(0.5f),
                BackgroundColor = Color.FromHex("#e9ecef"),
            },
            [".form-range::range-progress"] = new()
            {
                BackgroundColor = Color.FromHex("#0d6efd"),
            }
        };

        var sheet = new StyleSheet();

        // Act
        sheet.Add(css);

        // Assert
        sheet.PseudoElementRules.Count.ShouldBe(3);
        sheet.PseudoElementRules.ShouldContain(r => r.Type == PseudoElementType.RangeThumb);
        sheet.PseudoElementRules.ShouldContain(r => r.Type == PseudoElementType.RangeTrack);
        sheet.PseudoElementRules.ShouldContain(r => r.Type == PseudoElementType.RangeProgress);
    }

    [Fact]
    public void CssObject_RangePseudoElementWithNestedSelector_ShouldWork()
    {
        // Arrange: 使用嵌套选择器语法
        var css = new CssObject
        {
            [".form-range"] = new()
            {
                Width = Length.Percent(100),
                ["::range-thumb"] = new()
                {
                    BackgroundColor = Color.FromHex("#0d6efd"),
                }
            }
        };

        var sheet = new StyleSheet();

        // Act
        sheet.Add(css);

        // Assert: 应该生成一个普通规则和一个伪元素规则
        sheet.Rules.Count.ShouldBe(1);
        sheet.Rules[0].Style.Width.ShouldBe(Length.Percent(100));

        sheet.PseudoElementRules.Count.ShouldBe(1);
        sheet.PseudoElementRules[0].Type.ShouldBe(PseudoElementType.RangeThumb);
        sheet.PseudoElementRules[0].Style.BackgroundColor.ShouldBe(Color.FromHex("#0d6efd"));
    }

    #endregion

    #region Selector Matching Tests

    [Fact]
    public void RangeThumbSelector_ShouldMatchRangeInput()
    {
        // Arrange
        var range = new InputElement { Type = InputType.Range };
        range.Class = "form-range";

        var selector = CssSelectorParser.Parse(".form-range");

        // Act & Assert
        selector.Matches(range).ShouldBeTrue();
    }

    [Fact]
    public void RangeThumbSelector_WithAttributeSelector_ShouldMatch()
    {
        // Arrange
        var range = new InputElement { Type = InputType.Range };

        var selector = CssSelectorParser.Parse("[type=\"range\"]");

        // Act & Assert
        selector.Matches(range).ShouldBeTrue();
    }

    #endregion

    #region Real-world Example Test

    [Fact]
    public void BootstrapStyleRangeInput_ShouldParsePseudoElements()
    {
        // Arrange: Bootstrap 5 风格的 range 样式
        var css = new CssObject
        {
            [".form-range"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Rem(1.5f),
                Padding = new Padding(0),
                BackgroundColor = Color.Transparent,
                ["::range-thumb"] = new()
                {
                    Width = Length.Rem(1),
                    Height = Length.Rem(1),
                    BackgroundColor = Color.FromHex("#0d6efd"),
                    BorderWidth = Length.Px(0),
                    BorderRadius = Length.Rem(1),
                },
                ["::range-track"] = new()
                {
                    Width = Length.Percent(100),
                    Height = Length.Rem(0.5f),
                    BackgroundColor = Color.FromHex("#e9ecef"),
                    BorderRadius = Length.Rem(1),
                }
            }
        };

        var sheet = new StyleSheet();

        // Act
        sheet.Add(css);

        // Assert
        sheet.Rules.Count.ShouldBe(1, "Should have one main style rule for .form-range");
        sheet.PseudoElementRules.Count.ShouldBe(2, "Should have two pseudo-element rules");

        var mainRule = sheet.Rules[0];
        mainRule.Style.Width.ShouldBe(Length.Percent(100));
        mainRule.Style.Height.ShouldBe(Length.Rem(1.5f));

        var thumbRule = sheet.PseudoElementRules.First(r => r.Type == PseudoElementType.RangeThumb);
        thumbRule.Style.Width.ShouldBe(Length.Rem(1));
        thumbRule.Style.BackgroundColor.ShouldBe(Color.FromHex("#0d6efd"));

        var trackRule = sheet.PseudoElementRules.First(r => r.Type == PseudoElementType.RangeTrack);
        trackRule.Style.Height.ShouldBe(Length.Rem(0.5f));
        trackRule.Style.BackgroundColor.ShouldBe(Color.FromHex("#e9ecef"));
    }

    #endregion
}
