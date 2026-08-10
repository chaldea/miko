using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// ISSUE-121：<c>caret-color</c> —— 文本光标的颜色可由样式指定。
///
/// <para>光标此前恒为黑色硬编码。现按 CSS 语义解析：显式 <c>caret-color</c> 优先，
/// 未设置时回落到该元素的 <c>color</c>（CSS 初始值 <c>auto</c> 的行为），且该属性可继承。</para>
/// </summary>
public class CaretColorTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;

    public CaretColorTests()
    {
        _bitmap = new SKBitmap(200, 100);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    /// <summary>
    /// 聚焦的空输入框：光标画在内容盒左边缘。返回该竖线上出现过的所有非白色像素。
    /// 空值 + 无占位符，故画面上唯一的绘制物就是光标本身。
    /// </summary>
    private List<SKColor> RenderFocusedCaretPixels(Style inputStyle, Style? rootStyle = null)
    {
        var input = new InputElement { Type = InputType.Text, Style = inputStyle };
        input.SetState(ElementState.Focus);

        var root = new DivElement
        {
            Style = rootStyle ?? new Style { Width = Length.Px(200), Height = Length.Px(100) },
        };
        root.AddChild(input);

        var engine = new MikoEngine();
        engine.Initialize(root, [], _canvas, 200, 100);
        engine.Render(_canvas);

        var found = new List<SKColor>();
        for (int y = 0; y < _bitmap.Height; y++)
        {
            for (int x = 0; x < _bitmap.Width; x++)
            {
                var c = _bitmap.GetPixel(x, y);
                if (c.Red != 255 || c.Green != 255 || c.Blue != 255)
                    found.Add(c);
            }
        }
        return found;
    }

    private static void ShouldContainHue(List<SKColor> pixels, byte r, byte g, byte b)
    {
        pixels.ShouldNotBeEmpty("光标未被绘制");
        // 光标为 1px 抗锯齿描边，落在整数列上时颜色可能被混入白色而整体变浅；
        // 因此按“通道间的相对关系”断言色相，而非精确 RGB。
        pixels.ShouldContain(c => IsHue(c, r, g, b));
    }

    // 该像素是否呈现目标色相：目标为 0 的通道明显弱于目标为 255 的通道。
    private static bool IsHue(SKColor c, byte r, byte g, byte b)
    {
        bool Check(byte actual, byte expected, byte reference) =>
            expected == 255 ? actual >= reference : actual < reference;

        byte max = Math.Max(c.Red, Math.Max(c.Green, c.Blue));
        byte min = Math.Min(c.Red, Math.Min(c.Green, c.Blue));
        if (max == min) return r == g && g == b;   // 灰阶：仅与灰阶目标匹配

        return Check(c.Red, r, max) && Check(c.Green, g, max) && Check(c.Blue, b, max);
    }

    [Fact]
    public void CaretColor_ExplicitValue_PaintsCaretInThatColor()
    {
        var pixels = RenderFocusedCaretPixels(new Style
        {
            Width = Length.Px(150),
            Height = Length.Px(30),
            Color = Color.Black,
            CaretColor = Color.FromRgb(255, 0, 0),
        });

        ShouldContainHue(pixels, 255, 0, 0);
    }

    [Fact]
    public void CaretColor_NotSet_FallsBackToColor()
    {
        // CSS 初始值 auto：光标取该元素的 color。
        var pixels = RenderFocusedCaretPixels(new Style
        {
            Width = Length.Px(150),
            Height = Length.Px(30),
            Color = Color.FromRgb(0, 0, 255),
        });

        ShouldContainHue(pixels, 0, 0, 255);
    }

    [Fact]
    public void CaretColor_InheritsFromAncestor()
    {
        // caret-color 在 CSS 中可继承：设在容器上即为其内部输入框的光标着色，
        // 且不受输入框自身 color 的影响。
        var pixels = RenderFocusedCaretPixels(
            new Style { Width = Length.Px(150), Height = Length.Px(30), Color = Color.Black },
            new Style
            {
                Width = Length.Px(200),
                Height = Length.Px(100),
                CaretColor = Color.FromRgb(0, 255, 0),
            });

        ShouldContainHue(pixels, 0, 255, 0);
    }

    [Fact]
    public void CaretColor_OnElement_BeatsInheritedValue()
    {
        var pixels = RenderFocusedCaretPixels(
            new Style
            {
                Width = Length.Px(150),
                Height = Length.Px(30),
                CaretColor = Color.FromRgb(255, 0, 0),
            },
            new Style
            {
                Width = Length.Px(200),
                Height = Length.Px(100),
                CaretColor = Color.FromRgb(0, 255, 0),
            });

        ShouldContainHue(pixels, 255, 0, 0);
        pixels.ShouldNotContain(c => IsHue(c, 0, 255, 0));
    }
}
