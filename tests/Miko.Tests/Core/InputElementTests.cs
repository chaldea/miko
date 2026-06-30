using Miko.Common;
using Miko.Components;
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
    /// 文本输入框默认不设固定高度（height: auto）。
    /// 其高度由行高 / 字体度量加上 padding、border 在布局阶段撑开，
    /// 而非在样式解析阶段写死像素值（参见 ISSUE-040）。
    /// </summary>
    [Fact]
    public void TextInput_DefaultHeight_ShouldBeAuto()
    {
        // Arrange
        var input = new InputElement { Type = InputType.Text };

        // Act
        var computed = _styleResolver.Resolve(input, new List<StyleSheet>());

        // Assert: 高度未被写死，保持 auto，留给布局按内容计算
        computed.Height.IsAuto.ShouldBeTrue(
            "Text input height should remain auto and be derived from content during layout");
    }

    /// <summary>
    /// 文本输入框布局后的高度应由字体度量（一行文本）加 padding、border 撑开，
    /// 而不是塌缩为内容 0、整体仅剩 padding+border。
    /// </summary>
    [Fact]
    public void TextInput_Layout_DefaultHeight_ShouldBeDerivedFromFont()
    {
        // Arrange
        var input = new InputElement { Type = InputType.Text };

        // Act
        var layoutRoot = _layoutEngine.Layout(input, new List<StyleSheet>(), 800, 600);

        // Assert: 内容高度应为一行文本的字体度量高度（>0），而非 0
        layoutRoot.BoxModel.Content.Height.ShouldBeGreaterThan(0,
            "Text input content height should be derived from font metrics, not collapse to 0");

        // border-box = 内容高度 + padding(1+1) + border(1+1) = 字体高度 + 4
        var expectedBorderBox = layoutRoot.BoxModel.Content.Height + 4;
        layoutRoot.BoxModel.BorderBox.Height.ShouldBe(expectedBorderBox);
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
    /// 文本输入框布局后应该具有正确的尺寸：
    /// 宽度沿用 UA 默认的 border-box 173px；高度由内容（字体度量）撑开。
    /// </summary>
    [Fact]
    public void TextInput_Layout_ShouldHaveCorrectDimensions()
    {
        // Arrange
        var input = new InputElement { Type = InputType.Text, Value = "Test" };

        // Act
        var layoutRoot = _layoutEngine.Layout(input, new List<StyleSheet>(), 800, 600);

        // Assert: input 使用 border-box，UA 设置 Width=173 为 border-box 宽度
        layoutRoot.BoxModel.BorderBox.Width.ShouldBe(173,
            "Text input border-box width should be 173px (browser default)");

        // 高度不再写死，应大于仅 padding+border（4px），即内容高度 > 0
        layoutRoot.BoxModel.BorderBox.Height.ShouldBeGreaterThan(4,
            "Text input height should be derived from content, not a fixed pixel value");
    }

    /// <summary>
    /// ISSUE-040 回归测试：
    /// 一个应用了 Bootstrap form-control 风格样式的 input（block 显示、font-size 16px、
    /// line-height 24px、padding 6px、border 1px、border-box），其内容高度应为 24px、
    /// 整体（border-box）高度应为 38px，而不是被固定为 21px / 内容塌缩为 7px。
    /// </summary>
    [Fact]
    public void TextInput_WithFormControlStyle_ShouldComputeHeightFromLineHeight()
    {
        // Arrange：复现 DebugDemo 中 .root > input.form-control 的结构与样式
        var root = new Miko.Core.DomElements.DivElement { Class = "root" };
        var input = new InputElement { Type = InputType.Text };
        input.Class = "form-control";
        root.AddChild(input);

        var sheet = new StyleSheet();
        sheet.AddRule(new Miko.Styling.Selectors.ClassSelector("root"), new Style
        {
            Width = Length.Px(500),
            Height = Length.Px(500),
        });
        sheet.AddRule(new Miko.Styling.Selectors.ClassSelector("form-control"), new Style
        {
            Display = Display.Block,
            Width = Length.Percent(100),
            BoxSizing = BoxSizing.BorderBox,
            PaddingTop = Length.Px(6),
            PaddingRight = Length.Px(6),
            PaddingBottom = Length.Px(6),
            PaddingLeft = Length.Px(6),
            FontSize = Length.Px(16),
            LineHeight = Length.Px(24),
            BorderWidth = Length.Px(1),
            BorderStyle = Miko.Common.BorderStyle.Solid,
            BorderColor = Color.Gray,
        });

        // Act
        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var inputBox = layoutRoot.Children[0];

        // Assert: 内容高度 = line-height = 24px
        inputBox.BoxModel.Content.Height.ShouldBe(24,
            "Input content height should equal the 24px line-height, not collapse to 7px");

        // border-box = 内容 24 + padding(6+6) + border(1+1) = 38px
        inputBox.BoxModel.BorderBox.Height.ShouldBe(38,
            "Input overall (border-box) height should be 38px, not fixed at 21px");
    }

    /// <summary>
    /// ISSUE-040 后续（DebugDemo 三输入框）回归测试：
    /// 复现 lg / normal / sm 三种 form-control 尺寸，验证两点：
    /// (1) em 相对元素自身字体大小解析（lg=1.25rem=20px → 1.5em=30；sm=0.875rem=14px → 1.5em=21）；
    /// (2) box-sizing:border-box（经 * 选择器设置）对 min-height 生效。
    /// 期望 border-box 高度：lg=48、normal=38、sm=31（与浏览器一致）。
    /// </summary>
    [Fact]
    public void FormControlSizes_ResolveEmAgainstOwnFontSizeUnderBorderBox()
    {
        var root = new Miko.Core.DomElements.DivElement { Class = "root" };
        var lg = new InputElement { Type = InputType.Text }; lg.Class = "form-control form-control-lg";
        var normal = new InputElement { Type = InputType.Text }; normal.Class = "form-control";
        var sm = new InputElement { Type = InputType.Text }; sm.Class = "form-control form-control-sm";
        root.AddChild(lg);
        root.AddChild(normal);
        root.AddChild(sm);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["*"] = new() { BoxSizing = BoxSizing.BorderBox },
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
        });
        sheet.Add(new CssObject
        {
            [".form-control"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                Padding = new Padding(Length.Rem(0.375f), Length.Rem(0.375f)),
                FontSize = Length.Rem(1f),                  // 16px
                LineHeight = Length.Number(1.5f),           // 无单位：1.5 × 字体大小
                BorderWidth = Length.Px(1),
                BorderStyle = Miko.Common.BorderStyle.Solid,
                BorderColor = Color.Gray,
            },
            [".form-control-lg"] = new()
            {
                MinHeight = Length.Em(1.5f) + Length.Rem(1f) + Length.Px(1) * 2,
                Padding = new Padding(Length.Rem(0.5f), Length.Rem(1f)),
                FontSize = Length.Rem(1.25f),               // 20px
            },
            [".form-control-sm"] = new()
            {
                MinHeight = Length.Em(1.5f) + Length.Rem(0.5f) + Length.Px(1) * 2,
                Padding = new Padding(Length.Rem(0.25f), Length.Rem(0.5f)),
                FontSize = Length.Rem(0.875f),              // 14px
            },
        });

        // Act
        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var lgBox = layoutRoot.Children[0];
        var normalBox = layoutRoot.Children[1];
        var smBox = layoutRoot.Children[2];

        // border-box 高度（浏览器期望）
        lgBox.BoxModel.BorderBox.Height.ShouldBe(48,
            "lg: min-height 1.5em(=30 at 20px)+1rem(16)+2 = 48px");
        normalBox.BoxModel.BorderBox.Height.ShouldBe(38,
            "normal: line-height 1.5*16=24 内容 + padding(6+6) + border(1+1) = 38px");
        smBox.BoxModel.BorderBox.Height.ShouldBe(31,
            "sm: min-height 1.5em(=21 at 14px)+0.5rem(8)+2 = 31px");

        // 同一 1.5em 项随各自字体大小变化，证明 em 未用 root(16) 提前折算
        lgBox.BoxModel.BorderBox.Height.ShouldNotBe(smBox.BoxModel.BorderBox.Height);
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
        computed.BorderTopWidth.Value.ShouldBe(0);
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
        computed.BorderTopWidth.Value.ShouldBe(0);
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

    /// <summary>
    /// Range 滑块应支持百分比宽度（相对父容器）
    /// </summary>
    [Fact]
    public void Range_PercentWidth_ShouldResolveAgainstParent()
    {
        // Arrange: 父容器 500px，range 设置 100% 宽度
        var root = new DivElement();
        var range = new InputElement { Type = InputType.Range };
        range.Class = "full-width";
        root.AddChild(range);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new()
            {
                Width = Length.Px(500),
                Height = Length.Px(200),
            },
            [".full-width"] = new()
            {
                Width = Length.Percent(100),
            }
        });

        // Act
        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var rangeBox = layoutRoot.Children[0];

        // Assert: range 的 border-box 宽度应为父内容区宽度的 100%（即 500px）
        rangeBox.BoxModel.BorderBox.Width.ShouldBe(500,
            "Range with width: 100% should fill parent content width");
    }

    /// <summary>
    /// Range 滑块应支持百分比宽度，即使设置了 box-sizing: border-box
    /// </summary>
    [Fact]
    public void Range_PercentWidth_WithBorderBox_ShouldWork()
    {
        // Arrange
        var root = new DivElement();
        var range = new InputElement { Type = InputType.Range };
        range.Class = "full-width";
        root.AddChild(range);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["*"] = new()
            {
                BoxSizing = BoxSizing.BorderBox,
            },
            ["div"] = new()
            {
                Width = Length.Px(500),
                Height = Length.Px(200),
            },
            [".full-width"] = new()
            {
                Width = Length.Percent(100),
            }
        });

        // Act
        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var rangeBox = layoutRoot.Children[0];

        // Assert: border-box 宽度 = 父容器宽度 100% = 500px
        rangeBox.BoxModel.BorderBox.Width.ShouldBe(500,
            "Range with width: 100% and border-box should fill parent content width");
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
    [InlineData(InputType.Search)]
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

        // 高度规则因类型而异：
        // - Checkbox/Radio/Range 为本征尺寸控件，保留固定默认高度；
        // - Text/Password/Search 高度为 auto，由内容（行高/字体）在布局阶段撑开（ISSUE-040）。
        if (inputType is InputType.Text or InputType.Password or InputType.Search)
        {
            computed.Height.IsAuto.ShouldBeTrue(
                $"{inputType} input height should be auto and derived from content");
        }
        else
        {
            computed.Height.Value.ShouldBeGreaterThan(0,
                $"{inputType} input should have positive height");
        }
    }

    /// <summary>
    /// 通过 RenderTreeBuilder 渲染 <c>type="search"</c> 的 input 时，应解析为
    /// <see cref="InputType.Search"/>（Ionic searchbar 依赖此映射）。
    /// </summary>
    [Fact]
    public void SearchType_Attribute_ResolvesToInputTypeSearch()
    {
        var component = new SearchInputComponent();
        var root = component.Build();

        var input = root.ShouldBeOfType<InputElement>();
        input.Type.ShouldBe(InputType.Search);
    }

    private sealed class SearchInputComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "type", "search");
            builder.CloseElement();
        }
    }

    #endregion

    #region Select Default Styles

    /// <summary>
    /// select 默认不写死高度（height: auto），由行高/字体度量加 padding、border 撑开
    /// （与 input 同类问题，参见 ISSUE-040），不应被固定为 22px。
    /// </summary>
    [Fact]
    public void Select_DefaultHeight_ShouldBeAuto()
    {
        var select = new SelectElement();

        var computed = _styleResolver.Resolve(select, new List<StyleSheet>());

        computed.Height.IsAuto.ShouldBeTrue(
            "Select height should remain auto and be derived from content during layout");
    }

    /// <summary>
    /// select 布局后的高度应由一行文本（字体度量）加 padding、border 撑开，
    /// 且其 option 子元素由下拉层渲染、不计入闭合态高度。
    /// </summary>
    [Fact]
    public void Select_Layout_DefaultHeight_ShouldBeDerivedFromFont()
    {
        var select = new SelectElement();
        select.AddChild(new OptionElement { TextContent = "Option 1" });
        select.AddChild(new OptionElement { TextContent = "Option 2" });

        var layoutRoot = _layoutEngine.Layout(select, new List<StyleSheet>(), 800, 600);

        // 内容高度为一行文本高度（>0），而非塌缩为 0，也不会被 option 撑高
        layoutRoot.BoxModel.Content.Height.ShouldBeGreaterThan(0,
            "Select content height should be one line of text derived from font metrics");

        // border-box = 内容高度 + padding(1+1) + border(1+1) = 内容 + 4
        layoutRoot.BoxModel.BorderBox.Height.ShouldBe(layoutRoot.BoxModel.Content.Height + 4);

        // 不应是旧的固定 22px 写死值
        layoutRoot.BoxModel.BorderBox.Height.ShouldNotBe(22);
    }

    /// <summary>
    /// 当显式设置高度时，select 仍应遵循设置值（行为不变）。
    /// </summary>
    [Fact]
    public void Select_ExplicitHeight_ShouldBeRespected()
    {
        var select = new SelectElement();
        select.Class = "sized";
        select.AddChild(new OptionElement { TextContent = "Option 1" });

        var sheet = new StyleSheet();
        sheet.AddRule(new Miko.Styling.Selectors.ClassSelector("sized"), new Style
        {
            Height = Length.Px(40),
            BoxSizing = BoxSizing.BorderBox,
        });

        var layoutRoot = _layoutEngine.Layout(select, new List<StyleSheet> { sheet }, 800, 600);

        layoutRoot.BoxModel.BorderBox.Height.ShouldBe(40,
            "Explicit select height should be honored");
    }

    #endregion
}
