using Miko.Common;
using Miko.Utils;
using Shouldly;
using Xunit;

namespace Miko.Tests.Utils;

/// <summary>
/// TextWrapper 单元测试
/// </summary>
public class TextWrapperTests
{
    [Theory]
    [InlineData(WhiteSpace.Normal, "  hello   world  ", "hello world")]
    [InlineData(WhiteSpace.Nowrap, "  hello   world  ", "hello world")]
    [InlineData(WhiteSpace.Pre, "  hello   world  ", "  hello   world  ")]
    [InlineData(WhiteSpace.PreWrap, "  hello   world  ", "  hello   world  ")]
    [InlineData(WhiteSpace.PreLine, "  hello   world  ", "hello world")]
    public void ProcessText_Should_Handle_Space_Collapsing(WhiteSpace whiteSpace, string input, string expected)
    {
        // Act
        var result = TextWrapper.ProcessText(input, whiteSpace);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(WhiteSpace.Normal, "hello\nworld", "hello world")]
    [InlineData(WhiteSpace.Nowrap, "hello\nworld", "hello world")]
    [InlineData(WhiteSpace.Pre, "hello\nworld", "hello\nworld")]
    [InlineData(WhiteSpace.PreWrap, "hello\nworld", "hello\nworld")]
    [InlineData(WhiteSpace.PreLine, "hello\nworld", "hello\nworld")]
    public void ProcessText_Should_Handle_Newlines(WhiteSpace whiteSpace, string input, string expected)
    {
        // Act
        var result = TextWrapper.ProcessText(input, whiteSpace);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(WhiteSpace.Normal, true)]
    [InlineData(WhiteSpace.Nowrap, false)]
    [InlineData(WhiteSpace.Pre, false)]
    [InlineData(WhiteSpace.PreWrap, true)]
    [InlineData(WhiteSpace.PreLine, true)]
    public void ShouldWrap_Should_Return_Correct_Value(WhiteSpace whiteSpace, bool expected)
    {
        // Act
        var result = TextWrapper.ShouldWrap(whiteSpace);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void WrapText_Should_Not_Wrap_When_Width_Is_Sufficient()
    {
        // Arrange
        string text = "Hello World";
        var lines = TextWrapper.WrapText(text, "Arial", 16, FontWeight.Normal, 1000, WhiteSpace.Normal);

        // Assert
        lines.Count.ShouldBe(1);
        lines[0].ShouldBe("Hello World");
    }

    [Fact]
    public void WrapText_Should_Not_Wrap_When_WhiteSpace_Is_Nowrap()
    {
        // Arrange
        string text = "This is a very long text that would normally wrap";
        var lines = TextWrapper.WrapText(text, "Arial", 16, FontWeight.Normal, 50, WhiteSpace.Nowrap);

        // Assert
        lines.Count.ShouldBe(1);
        lines[0].ShouldBe(text);
    }

    [Fact]
    public void WrapText_Should_Split_On_Spaces()
    {
        // Arrange
        // "Buttons provide" is wider than 100px, should wrap
        string text = "Buttons provide a clickable element";
        var lines = TextWrapper.WrapText(text, "Arial", 16, FontWeight.Normal, 100, WhiteSpace.Normal);

        // Assert
        lines.Count.ShouldBeGreaterThan(1);
        // 验证每行都不为空
        foreach (var line in lines)
        {
            line.ShouldNotBeNullOrEmpty();
        }
    }

    [Fact]
    public void WrapText_Should_Preserve_Explicit_Newlines_In_PreWrap()
    {
        // Arrange
        string text = "Line 1\nLine 2\nLine 3";
        var lines = TextWrapper.WrapText(text, "Arial", 16, FontWeight.Normal, 1000, WhiteSpace.PreWrap);

        // Assert
        lines.Count.ShouldBe(3);
        lines[0].ShouldBe("Line 1");
        lines[1].ShouldBe("Line 2");
        lines[2].ShouldBe("Line 3");
    }

    [Fact]
    public void WrapText_Should_Preserve_Explicit_Newlines_In_PreLine()
    {
        // Arrange
        string text = "Line 1\nLine 2\nLine 3";
        var lines = TextWrapper.WrapText(text, "Arial", 16, FontWeight.Normal, 1000, WhiteSpace.PreLine);

        // Assert
        lines.Count.ShouldBe(3);
    }
}
