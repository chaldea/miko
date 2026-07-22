using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// ISSUE-099 问题4 回归测试：flex/grid 容器直接文本的绘制位置必须遵循容器对齐
/// （justify-content 等）定位后的文本节点盒，不能在绘制期被 text-align 重新锚定
/// 到父内容盒边缘。
///
/// 根因：RenderTextNode 对"父元素唯一在流行内子节点"的文本，会以父内容盒作为
/// text-align 的对齐容器——该覆盖是常规流（block/inline-block）中 text-align:
/// center/right 的实现手段，但对 flex/grid 容器同样生效，把 justify-content:center
/// 定位好的文本又画回父盒左缘（用户可见"按钮文本居左"）。
/// CSS 中 text-align 只作用于块容器的行内内容，不适用于 flex/grid 项目。
/// </summary>
public class FlexTextPaintAlignmentTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine;
    private readonly LayoutEngine _layoutEngine;

    public FlexTextPaintAlignmentTests()
    {
        _bitmap = new SKBitmap(500, 500);
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

    /// <summary>与 issues/ISSUE-099.md 问题4 一致的样式（100×30 蓝底白字按钮）。</summary>
    private static List<StyleSheet> BuildSheets(JustifyContent? justify = null, bool blockWithTextAlignCenter = false)
    {
        var sheet = new StyleSheet();
        var btnStyle = new CssObject
        {
            Width = Length.Px(100),
            Height = Length.Px(30),
            BackgroundColor = Color.Blue,
            Color = Color.White,
            Cursor = Cursor.Pointer,
        };
        if (blockWithTextAlignCenter)
        {
            btnStyle.Display = Display.Block;
            btnStyle.TextAlign = TextAlign.Center;
        }
        else
        {
            btnStyle.Display = Display.Flex;
            btnStyle.AlignItems = AlignItems.Center;
            btnStyle.JustifyContent = justify ?? JustifyContent.Center;
        }

        sheet.Add(new CssObject
        {
            ["*"] = new() { BoxSizing = BoxSizing.BorderBox },
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".btn"] = btnStyle,
        });
        return new List<StyleSheet> { sheet };
    }

    private void RenderButton(List<StyleSheet> sheets)
    {
        var btn = new DivElement { Class = "btn", TextContent = "Click" };
        var root = new DivElement { Class = "root", Children = { btn } };

        _canvas.Clear(SKColors.Black);
        var layout = _layoutEngine.Layout(root, sheets, 500, 500);
        _renderEngine.Render(layout);
    }

    /// <summary>在按钮区域（0..100 × 0..30）内扫描白色（文本）像素的水平范围。</summary>
    private (int minX, int maxX) ScanTextHorizontalExtent()
    {
        int minX = int.MaxValue, maxX = int.MinValue;
        for (int y = 1; y < 29; y++)
        for (int x = 1; x < 99; x++)
        {
            var p = _bitmap.GetPixel(x, y);
            if (p.Red > 180 && p.Green > 180 && p.Blue > 180)
            {
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }
        }
        return (minX, maxX);
    }

    [Fact]
    public void FlexButton_JustifyCenter_TextPaintedCentered()
    {
        RenderButton(BuildSheets());

        var (minX, maxX) = ScanTextHorizontalExtent();
        minX.ShouldBeLessThan(int.MaxValue); // 文本确实被绘制

        // 居中：文本左右边距应大致相等（修复前文本被画在左缘，左边距≈0）。
        int leftMargin = minX;
        int rightMargin = 100 - 1 - maxX;
        leftMargin.ShouldBeGreaterThan(10);
        Math.Abs(leftMargin - rightMargin).ShouldBeLessThanOrEqualTo(6);
    }

    [Fact]
    public void FlexButton_JustifyFlexEnd_TextPaintedAtRightEdge()
    {
        RenderButton(BuildSheets(justify: JustifyContent.FlexEnd));

        var (minX, maxX) = ScanTextHorizontalExtent();
        minX.ShouldBeLessThan(int.MaxValue);

        // 文本应贴近按钮右缘（修复前任何 justify 值都被画在左缘）。
        int rightMargin = 100 - 1 - maxX;
        rightMargin.ShouldBeLessThanOrEqualTo(8);
        minX.ShouldBeGreaterThan(30);
    }

    [Fact]
    public void FlexButton_JustifyFlexStart_TextPaintedAtLeftEdge()
    {
        RenderButton(BuildSheets(justify: JustifyContent.FlexStart));

        var (minX, _) = ScanTextHorizontalExtent();
        minX.ShouldBeLessThan(int.MaxValue);

        // flex-start：文本贴左缘（这是唯一与旧行为一致的场景，防止过矫正）。
        minX.ShouldBeLessThanOrEqualTo(5);
    }

    [Fact]
    public void BlockButton_TextAlignCenter_StillPaintedCentered()
    {
        // 常规流回归保护：block + text-align:center 的绘制期居中必须保持
        // （纯文本元素与 button UA 居中的既有行为，见 ISSUE-041/070）。
        RenderButton(BuildSheets(blockWithTextAlignCenter: true));

        var (minX, maxX) = ScanTextHorizontalExtent();
        minX.ShouldBeLessThan(int.MaxValue);

        int leftMargin = minX;
        int rightMargin = 100 - 1 - maxX;
        leftMargin.ShouldBeGreaterThan(10);
        Math.Abs(leftMargin - rightMargin).ShouldBeLessThanOrEqualTo(6);
    }
}
