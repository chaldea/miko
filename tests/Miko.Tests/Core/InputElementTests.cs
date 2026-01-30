using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Core;

/// <summary>
/// InputElement 默认样式测试
/// 验证不同类型的 Input 元素是否具有与浏览器一致的默认样式
/// </summary>
public class InputElementTests
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly StyleResolver _styleResolver = new();

    #region Text Input Default Styles

    /// <summary>
    /// 文本输入框应该具有与浏览器一致的默认高度
    /// 浏览器默认文本输入框高度约为 20-22px（包含 padding 和 border）
    /// </summary>
    [Fact]
    public void TextInput_DefaultHeight_ShouldMatchBrowserBehavior()
    {
        // Arrange
        var input = new InputElement { Type = InputType.Text };

        // Act
        var computed = _styleResolver.Resolve(input, new List<StyleSheet>());

        // Assert: 浏览器默认文本输入框高度约为 21px (1em + padding + border)
        // Height 应该被设置为一个合理的默认值
        computed.Height.Value.ShouldBeGreaterThan(0,
            "Text input should have a default height like browser behavior");
        computed.Height.Value.ShouldBeInRange(18, 24,
            "Text input default height should be around 20-22px (browser default)");
    }

    /// <summary>
    /// 文本输入框应该具有默认宽度
    /// 浏览器默认宽度约为 150-200px (Chrome ~173px, Firefox ~150px)
    /// </summary>
    [Fact]
    public void TextInput_DefaultWidth_ShouldMatchBrowserBehavior()
    {
        // Arrange
        var input = new InputElement { Type = InputType.Text };

        // Act
        var computed = _styleResolver.Resolve(input, new List<StyleSheet>());

        // Assert: 浏览器默认文本输入框宽度约为 173px (Chrome) 或 150px (Firefox)
        // We use Chrome's default value of 173px
        computed.Width.Value.ShouldBe(173,
            "Text input should have default width of 173px (browser default)");
    }

    /// <summary>
    /// 文本输入框布局后应该具有正确的尺寸
    /// </summary>
    [Fact]
    public void TextInput_Layout_ShouldHaveCorrectDimensions()
    {
        // Arrange
        var input = new InputElement { Type = InputType.Text, Value = "Test" };

        // Act
        var layoutRoot = _layoutEngine.Layout(input, new List<StyleSheet>(), 800, 600);

        // Assert: 布局后内容区域应为 173x21px (浏览器默认尺寸)
        layoutRoot.BoxModel.Content.Width.ShouldBe(173,
            "Text input content width should be 173px (browser default)");
        layoutRoot.BoxModel.Content.Height.ShouldBe(21,
            "Text input content height should be 21px (browser default)");

        // BorderBox includes padding (1px top/bottom, 2px left/right) and border (1px)
        // Width: 173 + 2 + 2 + 1 + 1 = 179
        // Height: 21 + 1 + 1 + 1 + 1 = 25
        layoutRoot.BoxModel.BorderBox.Width.ShouldBe(179,
            "Text input border box width should include padding and border");
        layoutRoot.BoxModel.BorderBox.Height.ShouldBeInRange(23, 27,
            "Text input border box height should be around browser default");
    }

    #endregion

    #region Checkbox Default Styles

    /// <summary>
    /// 复选框应该具有与浏览器一致的默认大小
    /// 浏览器默认复选框大小约为 13x13px
    /// </summary>
    [Fact]
    public void Checkbox_DefaultSize_ShouldMatchBrowserBehavior()
    {
        // Arrange
        var checkbox = new InputElement { Type = InputType.Checkbox };

        // Act
        var computed = _styleResolver.Resolve(checkbox, new List<StyleSheet>());

        // Assert: 浏览器默认复选框大小约为 13x13px
        computed.Width.Value.ShouldBe(13,
            "Checkbox width should be 13px (browser default)");
        computed.Height.Value.ShouldBe(13,
            "Checkbox height should be 13px (browser default)");
    }

    /// <summary>
    /// 复选框布局后应该具有正确的尺寸
    /// </summary>
    [Fact]
    public void Checkbox_Layout_ShouldHaveCorrectDimensions()
    {
        // Arrange
        var checkbox = new InputElement { Type = InputType.Checkbox };

        // Act
        var layoutRoot = _layoutEngine.Layout(checkbox, new List<StyleSheet>(), 800, 600);

        // Assert: 布局后内容区域应为 13x13px
        layoutRoot.BoxModel.Content.Width.ShouldBe(13,
            "Checkbox content width should be 13px after layout");
        layoutRoot.BoxModel.Content.Height.ShouldBe(13,
            "Checkbox content height should be 13px after layout");
    }

    /// <summary>
    /// 复选框不应该有 padding 和 border（与文本输入框不同）
    /// </summary>
    [Fact]
    public void Checkbox_ShouldHaveNoPaddingAndBorder()
    {
        // Arrange
        var checkbox = new InputElement { Type = InputType.Checkbox };

        // Act
        var computed = _styleResolver.Resolve(checkbox, new List<StyleSheet>());

        // Assert: 复选框不应该有 padding 和边框（边框是绘制的一部分，不是布局的一部分）
        computed.PaddingTop.Value.ShouldBe(0);
        computed.PaddingRight.Value.ShouldBe(0);
        computed.PaddingBottom.Value.ShouldBe(0);
        computed.PaddingLeft.Value.ShouldBe(0);
        computed.BorderWidth.Value.ShouldBe(0);
    }

    #endregion

    #region Radio Button Default Styles

    /// <summary>
    /// 单选按钮应该具有与浏览器一致的默认大小
    /// 浏览器默认单选按钮大小约为 13x13px
    /// </summary>
    [Fact]
    public void Radio_DefaultSize_ShouldMatchBrowserBehavior()
    {
        // Arrange
        var radio = new InputElement { Type = InputType.Radio };

        // Act
        var computed = _styleResolver.Resolve(radio, new List<StyleSheet>());

        // Assert: 浏览器默认单选按钮大小约为 13x13px
        computed.Width.Value.ShouldBe(13,
            "Radio button width should be 13px (browser default)");
        computed.Height.Value.ShouldBe(13,
            "Radio button height should be 13px (browser default)");
    }

    /// <summary>
    /// 单选按钮布局后应该具有正确的尺寸
    /// </summary>
    [Fact]
    public void Radio_Layout_ShouldHaveCorrectDimensions()
    {
        // Arrange
        var radio = new InputElement { Type = InputType.Radio };

        // Act
        var layoutRoot = _layoutEngine.Layout(radio, new List<StyleSheet>(), 800, 600);

        // Assert: 布局后内容区域应为 13x13px
        layoutRoot.BoxModel.Content.Width.ShouldBe(13,
            "Radio button content width should be 13px after layout");
        layoutRoot.BoxModel.Content.Height.ShouldBe(13,
            "Radio button content height should be 13px after layout");
    }

    /// <summary>
    /// 单选按钮不应该有 padding 和 border（与文本输入框不同）
    /// </summary>
    [Fact]
    public void Radio_ShouldHaveNoPaddingAndBorder()
    {
        // Arrange
        var radio = new InputElement { Type = InputType.Radio };

        // Act
        var computed = _styleResolver.Resolve(radio, new List<StyleSheet>());

        // Assert: 单选按钮不应该有 padding 和边框（边框是绘制的一部分，不是布局的一部分）
        computed.PaddingTop.Value.ShouldBe(0);
        computed.PaddingRight.Value.ShouldBe(0);
        computed.PaddingBottom.Value.ShouldBe(0);
        computed.PaddingLeft.Value.ShouldBe(0);
        computed.BorderWidth.Value.ShouldBe(0);
    }

    #endregion

    #region Range Input Default Styles

    /// <summary>
    /// Range 滑块应该有默认宽度和高度
    /// </summary>
    [Fact]
    public void Range_DefaultSize_ShouldHaveReasonableDefaults()
    {
        // Arrange
        var range = new InputElement { Type = InputType.Range };

        // Act
        var computed = _styleResolver.Resolve(range, new List<StyleSheet>());

        // Assert: Range 输入通常有固定宽度
        computed.Width.Value.ShouldBeGreaterThan(0,
            "Range input should have a default width");
        computed.Height.Value.ShouldBeGreaterThan(0,
            "Range input should have a default height");
    }

    #endregion

    #region Password Input Default Styles

    /// <summary>
    /// 密码输入框应该与文本输入框具有相同的默认样式
    /// </summary>
    [Fact]
    public void PasswordInput_DefaultSize_ShouldMatchTextInput()
    {
        // Arrange
        var textInput = new InputElement { Type = InputType.Text };
        var passwordInput = new InputElement { Type = InputType.Password };

        // Act
        var textComputed = _styleResolver.Resolve(textInput, new List<StyleSheet>());
        var passwordComputed = _styleResolver.Resolve(passwordInput, new List<StyleSheet>());

        // Assert: 密码输入框应该与文本输入框具有相同的尺寸
        passwordComputed.Width.Value.ShouldBe(textComputed.Width.Value,
            "Password input width should match text input");
        passwordComputed.Height.Value.ShouldBe(textComputed.Height.Value,
            "Password input height should match text input");
    }

    #endregion

    #region Input Type Specific Styles

    /// <summary>
    /// 不同类型的输入框应该有不同的默认样式
    /// </summary>
    [Theory]
    [InlineData(InputType.Text)]
    [InlineData(InputType.Password)]
    [InlineData(InputType.Checkbox)]
    [InlineData(InputType.Radio)]
    [InlineData(InputType.Range)]
    public void AllInputTypes_ShouldHaveValidDefaultStyles(InputType inputType)
    {
        // Arrange
        var input = new InputElement { Type = inputType };

        // Act
        var computed = _styleResolver.Resolve(input, new List<StyleSheet>());

        // Assert: 所有输入类型都应该有有效的默认样式
        computed.Display.ShouldBe(Display.InlineBlock,
            $"{inputType} input should have InlineBlock display");
        computed.Width.Value.ShouldBeGreaterThan(0,
            $"{inputType} input should have positive width");
        computed.Height.Value.ShouldBeGreaterThan(0,
            $"{inputType} input should have positive height");
    }

    #endregion
}
