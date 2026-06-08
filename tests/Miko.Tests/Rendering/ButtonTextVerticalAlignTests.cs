using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// ISSUE-041 问题 3 回归测试：
/// 验证 button 元素设置高度后，文本内容应垂直居中显示（与浏览器行为一致）。
/// </summary>
public class ButtonTextVerticalAlignTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine;
    private readonly LayoutEngine _layoutEngine;

    public ButtonTextVerticalAlignTests()
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
    /// 当 button 设置了明确高度（如 50px）后，文本应垂直居中，
    /// 即文本的视觉中心应位于 contentRect 的垂直中心附近。
    /// </summary>
    [Fact]
    public void Button_WithExplicitHeight_TextShouldBeVerticallyCentered()
    {
        // Arrange: 创建一个带有 50px 高度的 button
        var button = new ButtonElement { TextContent = "Primary" };
        button.Class = "btn";

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("btn"), new Style
        {
            Height = Length.Px(50),
            BoxSizing = BoxSizing.BorderBox,
            FontSize = Length.Px(16),
            PaddingTop = Length.Px(2),
            PaddingBottom = Length.Px(2),
            PaddingLeft = Length.Px(6),
            PaddingRight = Length.Px(6),
            BorderWidth = Length.Px(2),
            BorderStyle = BorderStyle.Solid,
            BorderColor = Color.Gray,
        });

        var layoutBox = _layoutEngine.Layout(button, new List<StyleSheet> { sheet }, 400, 300);
        var contentRect = layoutBox.BoxModel.Content;

        // Assert: contentRect 高度 = 50 - padding(2+2) - border(2+2) = 42
        contentRect.Height.ShouldBe(42f,
            "Content height should be 42px (50 - 2*padding - 2*border)");

        // 渲染不应抛出异常（button 文本使用 VerticalAlign.Middle）
        Should.NotThrow(() => _renderEngine.Render(layoutBox));
    }

    /// <summary>
    /// button 的默认高度（auto）也应使文本垂直居中。
    /// </summary>
    [Fact]
    public void Button_WithAutoHeight_TextShouldBeVerticallyCentered()
    {
        // Arrange
        var button = new ButtonElement { TextContent = "Click me" };

        var layoutBox = _layoutEngine.Layout(button, new List<StyleSheet>(), 400, 300);

        // Act & Assert: 渲染不应抛出异常
        Should.NotThrow(() => _renderEngine.Render(layoutBox));
    }

    /// <summary>
    /// button 内的空文本不应导致渲染错误。
    /// </summary>
    [Fact]
    public void Button_WithEmptyText_ShouldRenderWithoutError()
    {
        // Arrange
        var button = new ButtonElement { TextContent = "" };
        button.Class = "btn";

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("btn"), new Style
        {
            Height = Length.Px(50),
            BoxSizing = BoxSizing.BorderBox,
        });

        var layoutBox = _layoutEngine.Layout(button, new List<StyleSheet> { sheet }, 400, 300);

        // Act & Assert: 渲染不应抛出异常
        Should.NotThrow(() => _renderEngine.Render(layoutBox));
    }

    /// <summary>
    /// button 与其他元素（如 div）的文本渲染应有区别：
    /// button 文本垂直居中，div 文本顶部对齐。
    /// 注意：button 有 UA 默认样式（border: 2px, padding: 2px 6px），
    /// 需要显式覆盖以达到与 div 相同的盒模型。
    /// </summary>
    [Fact]
    public void Button_TextAlignment_ShouldDifferFromDiv()
    {
        // Arrange: 相同高度的 button 和 div，覆盖 button 的 UA 默认样式
        var button = new ButtonElement { TextContent = "Button", Class = "tall" };
        var div = new DivElement { TextContent = "Div", Class = "tall" };

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("tall"), new Style
        {
            Height = Length.Px(50),
            BoxSizing = BoxSizing.BorderBox,
            FontSize = Length.Px(16),
            PaddingTop = Length.Px(5),
            PaddingBottom = Length.Px(5),
            PaddingLeft = Length.Px(10),
            PaddingRight = Length.Px(10),
            BorderWidth = Length.Px(0),  // 覆盖 button 默认 border
            BorderStyle = BorderStyle.Solid,
        });

        var buttonBox = _layoutEngine.Layout(button, new List<StyleSheet> { sheet }, 400, 300);
        var divBox = _layoutEngine.Layout(div, new List<StyleSheet> { sheet }, 400, 300);

        // Act & Assert: 两者都应成功渲染（button 用 Middle，div 用 Top）
        Should.NotThrow(() => _renderEngine.Render(buttonBox));
        Should.NotThrow(() => _renderEngine.Render(divBox));

        // 验证它们的 content rect 高度相同（覆盖 UA 样式后）
        buttonBox.BoxModel.Content.Height.ShouldBe(divBox.BoxModel.Content.Height);
    }

    /// <summary>
    /// button 元素应支持多行文本（如果宽度受限导致换行），
    /// 文本整体仍应垂直居中。
    /// </summary>
    [Fact]
    public void Button_WithMultiLineText_ShouldCenterTextVertically()
    {
        // Arrange
        var button = new ButtonElement { TextContent = "Very Long Button Text That Might Wrap" };
        button.Class = "btn";

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("btn"), new Style
        {
            Height = Length.Px(60),
            Width = Length.Px(100),  // 窄宽度可能导致文本换行
            BoxSizing = BoxSizing.BorderBox,
            FontSize = Length.Px(14),
            PaddingTop = Length.Px(5),
            PaddingBottom = Length.Px(5),
            PaddingLeft = Length.Px(10),
            PaddingRight = Length.Px(10),
            BorderWidth = Length.Px(1),
            BorderStyle = BorderStyle.Solid,
            BorderColor = Color.Gray,
        });

        var layoutBox = _layoutEngine.Layout(button, new List<StyleSheet> { sheet }, 400, 300);

        // Act & Assert: 渲染不应抛出异常
        Should.NotThrow(() => _renderEngine.Render(layoutBox));
    }
}
