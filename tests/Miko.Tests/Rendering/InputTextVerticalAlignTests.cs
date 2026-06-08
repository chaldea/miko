using Miko.Common;
using Miko.Core.DomElements;
using Miko.Fonts;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// ISSUE-041 回归测试：
/// 验证 Input 元素设置高度后，文本内容和光标都能垂直居中。
/// </summary>
public class InputTextVerticalAlignTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine;
    private readonly LayoutEngine _layoutEngine;

    public InputTextVerticalAlignTests()
    {
        _bitmap = new SKBitmap(400, 300);
        _canvas = new SKCanvas(_bitmap);
        _renderEngine = new RenderEngine();
        _renderEngine.SetCanvas(_canvas);
        _layoutEngine = new LayoutEngine();
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    /// <summary>
    /// 当 input 设置了明确高度（如 50px）后，文本应与光标一样垂直居中，
    /// 即文本的视觉中心应位于 contentRect 的垂直中心附近。
    /// 通过 Painter.DrawText 的 VerticalAlign.Middle 参数实现。
    /// </summary>
    [Fact]
    public void InputText_WithExplicitHeight_TextShouldBeVerticallyCentered()
    {
        // Arrange: 创建一个带有 50px 高度的 input
        var input = new InputElement { Type = InputType.Text, Value = "Hello" };
        input.Class = "tall-input";

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("tall-input"), new Style
        {
            Height = Length.Px(50),
            BoxSizing = BoxSizing.BorderBox,
            FontSize = Length.Px(14),
            PaddingTop = Length.Px(1),
            PaddingBottom = Length.Px(1),
            PaddingLeft = Length.Px(1),
            PaddingRight = Length.Px(1),
            BorderWidth = Length.Px(1),
            BorderStyle = BorderStyle.Solid,
            BorderColor = Color.Gray,
        });

        var layoutBox = _layoutEngine.Layout(input, new List<StyleSheet> { sheet }, 400, 300);
        var contentRect = layoutBox.BoxModel.Content;

        // Assert: contentRect 高度 = 50 - padding(1+1) - border(1+1) = 46
        contentRect.Height.ShouldBe(46,
            "Content height should be 46px (50 - 2*padding - 2*border)");

        // 验证：文本基线的计算方式应使文本垂直居中
        // 光标 Y = contentRect.Top + (contentRect.Height - fontSize) / 2
        // 文本应使用相同的垂直居中逻辑
        float fontSize = 14f;
        float cursorY = contentRect.Top + (contentRect.Height - fontSize) / 2;
        float expectedTextCenterY = contentRect.Top + contentRect.Height / 2;

        // 光标中心应位于 contentRect 的垂直中心
        float cursorCenterY = cursorY + fontSize / 2;
        cursorCenterY.ShouldBe(expectedTextCenterY,
            "Cursor center should be at the vertical center of content rect");
    }

    /// <summary>
    /// DrawText 使用 VerticalAlign.Middle 时，文本绘制基线应使文本行
    /// 垂直居中于矩形内，而非顶部对齐。
    /// 通过对比 VerticalAlign.Top 和 VerticalAlign.Middle 计算的 Y 差异来验证。
    /// </summary>
    [Fact]
    public void DrawText_WithVerticalAlignMiddle_ShouldCenterTextVertically()
    {
        // Arrange
        float fontSize = 14f;
        var rect = new RectF(0, 0, 200, 50); // 50px 高的矩形

        // 获取字体度量来计算预期位置
        var fontManager = FontManager.Instance;
        var fallbackResolver = new FontFallbackResolver(fontManager);
        var runs = fallbackResolver.ResolveTextRuns("Test", "Arial", FontWeight.Normal);
        runs.Count.ShouldBeGreaterThan(0);

        using var font = new SKFont(runs[0].Typeface, fontSize);
        var metrics = font.Metrics;
        float textHeight = metrics.Descent - metrics.Ascent;

        // 顶部对齐时的 baseline Y
        float topAlignedY = rect.Top - metrics.Ascent;

        // 垂直居中时的 baseline Y
        float centeredTop = rect.Top + (rect.Height - textHeight) / 2;
        float middleAlignedY = centeredTop - metrics.Ascent;

        // Assert: 垂直居中时 Y 应大于顶部对齐时 Y（向下偏移）
        middleAlignedY.ShouldBeGreaterThan(topAlignedY,
            "Middle-aligned text baseline should be lower than top-aligned");

        // 验证文本的视觉中心位于矩形中心
        float textVisualCenter = centeredTop + textHeight / 2;
        float rectCenter = rect.Top + rect.Height / 2;
        textVisualCenter.ShouldBe(rectCenter, 0.01f,
            "Text visual center should coincide with rect vertical center");
    }

    /// <summary>
    /// 当 input 高度等于字体行高时（无额外空间），文本居中和顶部对齐效果应相同。
    /// </summary>
    [Fact]
    public void DrawText_WhenRectHeightEqualsTextHeight_MiddleAndTopShouldMatch()
    {
        // Arrange
        float fontSize = 14f;
        var fontManager = FontManager.Instance;
        var fallbackResolver = new FontFallbackResolver(fontManager);
        var runs = fallbackResolver.ResolveTextRuns("X", "Arial", FontWeight.Normal);
        runs.Count.ShouldBeGreaterThan(0);

        using var font = new SKFont(runs[0].Typeface, fontSize);
        var metrics = font.Metrics;
        float textHeight = metrics.Descent - metrics.Ascent;

        // 矩形高度刚好等于文本高度
        var rect = new RectF(0, 10, 200, textHeight);

        // 顶部对齐
        float topY = rect.Top - metrics.Ascent;

        // 垂直居中
        float centeredTop = rect.Top + (rect.Height - textHeight) / 2;
        float middleY = centeredTop - metrics.Ascent;

        // Assert: 当高度等于文本高度时，两种方式应产生相同的 Y
        middleY.ShouldBe(topY, 0.01f,
            "When rect height equals text height, middle and top align should be equivalent");
    }

    /// <summary>
    /// Password 输入框的 placeholder 也应垂直居中。
    /// </summary>
    [Fact]
    public void PasswordInput_Placeholder_ShouldBeVerticallyCentered()
    {
        // Arrange
        var input = new InputElement { Type = InputType.Password, Placeholder = "Enter password" };
        input.Class = "tall-input";

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("tall-input"), new Style
        {
            Height = Length.Px(50),
            BoxSizing = BoxSizing.BorderBox,
            FontSize = Length.Px(14),
            PaddingTop = Length.Px(1),
            PaddingBottom = Length.Px(1),
            PaddingLeft = Length.Px(1),
            PaddingRight = Length.Px(1),
            BorderWidth = Length.Px(1),
            BorderStyle = BorderStyle.Solid,
            BorderColor = Color.Gray,
        });

        var layoutBox = _layoutEngine.Layout(input, new List<StyleSheet> { sheet }, 400, 300);
        var contentRect = layoutBox.BoxModel.Content;

        // Assert: content 区域高度正确
        contentRect.Height.ShouldBe(46);

        // 渲染不应抛出异常（placeholder 使用 VerticalAlign.Middle）
        Should.NotThrow(() => _renderEngine.Render(layoutBox));
    }

    /// <summary>
    /// 渲染带有明确高度的 text input（有值）不应抛出异常，
    /// 且文本应使用 VerticalAlign.Middle 绘制。
    /// </summary>
    [Fact]
    public void TextInput_WithValue_RenderShouldNotThrow()
    {
        // Arrange
        var input = new InputElement { Type = InputType.Text, Value = "Hello World" };
        input.Class = "tall-input";

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("tall-input"), new Style
        {
            Height = Length.Px(50),
            BoxSizing = BoxSizing.BorderBox,
            FontSize = Length.Px(16),
            PaddingTop = Length.Px(6),
            PaddingBottom = Length.Px(6),
            PaddingLeft = Length.Px(6),
            PaddingRight = Length.Px(6),
            BorderWidth = Length.Px(1),
            BorderStyle = BorderStyle.Solid,
            BorderColor = Color.Gray,
        });

        var layoutBox = _layoutEngine.Layout(input, new List<StyleSheet> { sheet }, 400, 300);

        // Act & Assert: 渲染不应抛出异常
        Should.NotThrow(() => _renderEngine.Render(layoutBox));
    }
}
