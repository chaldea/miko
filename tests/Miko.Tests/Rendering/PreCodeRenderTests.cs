using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Highlight;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// ISSUE-098 渲染验证：pre 的多行预格式化文本按行绘制，code 的语法高亮按 token 着色。
/// 通过离屏画布的像素采样验证绘制位置与颜色。
/// </summary>
public class PreCodeRenderTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine;
    private readonly LayoutEngine _layoutEngine;

    public PreCodeRenderTests()
    {
        _bitmap = new SKBitmap(600, 300);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
        _renderEngine = new RenderEngine();
        _renderEngine.SetCanvas(_canvas);
        _layoutEngine = new LayoutEngine();
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Fact]
    public void Pre_MultilineText_DrawsEachLineAtDistinctRows()
    {
        var pre = new PreElement();
        pre.AddChild(new TextNode("AAAA\nBBBB"));

        var layout = _layoutEngine.Layout(pre, [], 600, 300);
        _renderEngine.Render(layout);

        var textContent = layout.Children[0].BoxModel.Content;
        float lineHeight = Miko.Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(
            layout.Children[0].ComputedStyle);

        // 两行都应采到文本像素：第一行区域与第二行区域（行高内避开行间空白）。
        HasDarkPixelInRegion(new RectF(0, textContent.Top, 600, textContent.Top + lineHeight))
            .ShouldBeTrue("第一行应绘制文本");
        HasDarkPixelInRegion(new RectF(0, textContent.Top + lineHeight, 600, textContent.Top + lineHeight * 2))
            .ShouldBeTrue("第二行应绘制文本");
    }

    [Fact]
    public void Code_WithLanguage_DrawsKeywordInThemeColor()
    {
        var pre = new PreElement();
        var code = new CodeElement { Language = "csharp" };
        code.AddChild(new TextNode("class"));
        pre.AddChild(code);

        var layout = _layoutEngine.Layout(pre, [], 600, 300);
        _renderEngine.Render(layout);

        // 关键字 "class" 应以主题关键字色（VS Light+ 蓝 #0000FF）绘制。
        var keyword = SyntaxTheme.Default.Keyword;
        HasColoredPixelInRegion(
            new RectF(0, 0, 600, 300),
            c => CloseTo(c, keyword)).ShouldBeTrue("关键字应以高亮色绘制");
    }

    [Fact]
    public void Code_WithLanguage_DrawsCommentInThemeColor()
    {
        var pre = new PreElement();
        var code = new CodeElement { Language = "csharp" };
        code.AddChild(new TextNode("// note"));
        pre.AddChild(code);

        var layout = _layoutEngine.Layout(pre, [], 600, 300);
        _renderEngine.Render(layout);

        var comment = SyntaxTheme.Default.Comment;
        HasColoredPixelInRegion(
            new RectF(0, 0, 600, 300),
            c => CloseTo(c, comment)).ShouldBeTrue("注释应以高亮色绘制");
    }

    [Fact]
    public void Code_WithHighlightFalse_DrawsNoThemeColors()
    {
        var pre = new PreElement();
        var code = new CodeElement { Language = "csharp", Highlight = false };
        code.AddChild(new TextNode("class"));
        pre.AddChild(code);

        var layout = _layoutEngine.Layout(pre, [], 600, 300);
        _renderEngine.Render(layout);

        // 关闭高亮：文本按元素自身颜色（默认黑）绘制，不出现任何主题色。
        HasColoredPixelInRegion(
            new RectF(0, 0, 600, 300),
            c => CloseTo(c, SyntaxTheme.Default.Keyword)).ShouldBeFalse("关闭高亮后不应出现关键字色");
        HasDarkPixelInRegion(new RectF(0, 0, 600, 300)).ShouldBeTrue("文本仍应正常绘制");
    }

    [Fact]
    public void Code_MultilineHighlight_KeepsColorsOnLaterLines()
    {
        // 跨行 token 偏移：第二行的关键字也应着色（验证行偏移与 token 裁剪）。
        var pre = new PreElement();
        var code = new CodeElement { Language = "csharp" };
        code.AddChild(new TextNode("aaaa\nclass B { }"));
        pre.AddChild(code);

        var layout = _layoutEngine.Layout(pre, [], 600, 300);
        _renderEngine.Render(layout);

        var textContent = layout.Children[0].Children[0].BoxModel.Content;
        float lineHeight = Miko.Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(
            layout.Children[0].Children[0].ComputedStyle);

        // 只在第二行区域内扫描：第一行没有关键字，命中即证明第二行的 class 着了色。
        var keyword = SyntaxTheme.Default.Keyword;
        HasColoredPixelInRegion(
            new RectF(0, textContent.Top + lineHeight, 600, textContent.Top + lineHeight * 2),
            c => CloseTo(c, keyword)).ShouldBeTrue("第二行的 class 关键字应着色");
    }

    // -----------------------------------------------------------------
    // 自定义高亮器（ISSUE-098 内容重构：ISyntaxHighlighter 可替换）
    // -----------------------------------------------------------------

    /// <summary>把全部文本整体着成品红色的自定义高亮器。</summary>
    private sealed class MagentaHighlighter : ISyntaxHighlighter
    {
        public SyntaxTheme Theme { get; } = new()
        {
            Keyword = Color.FromRgb(255, 0, 255),
            String = Color.FromRgb(255, 0, 255),
            Comment = Color.FromRgb(255, 0, 255),
            Number = Color.FromRgb(255, 0, 255),
            Type = Color.FromRgb(255, 0, 255),
            Function = Color.FromRgb(255, 0, 255),
        };

        public IReadOnlyList<CodeToken>? Tokenize(string text, string? language)
            => language == "magenta" && text.Length > 0
                ? [new CodeToken(CodeTokenType.Keyword, 0, text.Length)]
                : null;
    }

    [Fact]
    public void CustomHighlighter_ReplacesDefaultColors()
    {
        _renderEngine.SyntaxHighlighter = new MagentaHighlighter();

        var pre = new PreElement();
        var code = new CodeElement { Language = "magenta" };
        code.AddChild(new TextNode("hello"));
        pre.AddChild(code);

        var layout = _layoutEngine.Layout(pre, [], 600, 300);
        _renderEngine.Render(layout);

        // 自定义高亮器生效：文本以品红绘制。
        HasColoredPixelInRegion(
            new RectF(0, 0, 600, 300),
            c => c.Red > 200 && c.Blue > 200 && c.Green < 80)
            .ShouldBeTrue("自定义高亮器的颜色应被使用");
        // 默认主题的关键字蓝不出现。
        HasColoredPixelInRegion(
            new RectF(0, 0, 600, 300),
            c => CloseTo(c, SyntaxTheme.Default.Keyword))
            .ShouldBeFalse("不应再使用默认高亮颜色");
    }

    // -----------------------------------------------------------------
    // 像素采样辅助
    // -----------------------------------------------------------------

    private bool HasDarkPixelInRegion(RectF region)
        => HasColoredPixelInRegion(region, c => c.Red < 100 && c.Green < 100 && c.Blue < 100);

    private bool HasColoredPixelInRegion(RectF region, Func<SKColor, bool> predicate)
    {
        int left = Math.Clamp((int)region.Left, 0, _bitmap.Width - 1);
        int right = Math.Clamp((int)region.Right, 0, _bitmap.Width);
        int top = Math.Clamp((int)region.Top, 0, _bitmap.Height - 1);
        int bottom = Math.Clamp((int)region.Bottom, 0, _bitmap.Height);
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                if (predicate(_bitmap.GetPixel(x, y))) return true;
            }
        }
        return false;
    }

    // 抗锯齿会让边缘像素偏离纯色，字形内部应有接近纯色的像素（容差 40）。
    private static bool CloseTo(SKColor pixel, Color target)
        => Math.Abs(pixel.Red - target.R) < 40
        && Math.Abs(pixel.Green - target.G) < 40
        && Math.Abs(pixel.Blue - target.B) < 40
        && pixel.Alpha > 200;
}
