using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// <c>opacity &lt; 1</c> 的合成层现在带 bounds（否则 Skia 按整个裁剪区分配离屏缓冲，
/// 800×5000 上 50 个半透明元素每帧约 226 ms）。bounds 会裁掉层内容，因此这些用例覆盖
/// 「内容画到自身边框盒之外」的各条路径，确认包围盒是上界而非下界。
/// </summary>
public class OpacityLayerBoundsTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine;
    private readonly LayoutEngine _layoutEngine = new();

    public OpacityLayerBoundsTests()
    {
        _bitmap = new SKBitmap(300, 300);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
        _renderEngine = new RenderEngine();
        _renderEngine.SetCanvas(_canvas);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private void Render(Element root)
    {
        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 300, 300);
        _renderEngine.Render(layout);
    }

    private SKColor Pixel(int x, int y) => _bitmap.GetPixel(x, y);

    /// <summary>子元素超出半透明父元素的边框盒时，不能被合成层裁掉。</summary>
    [Fact]
    public void OverflowingChild_IsNotClippedByOpacityLayer()
    {
        var root = new DivElement { Style = new Style { Width = Length.Px(300), Height = Length.Px(300) } };
        var faded = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(20),
                Height = Length.Px(20),
                Opacity = 0.5f,
                // overflow 默认 visible，子元素允许画到父盒之外
            }
        };
        // 绝对定位到父盒右下方之外
        var child = new DivElement
        {
            Style = new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(60),
                Top = Length.Px(60),
                Width = Length.Px(60),
                Height = Length.Px(60),
                BackgroundColor = Color.Red
            }
        };
        faded.AddChild(child);
        root.AddChild(faded);

        Render(root);

        // 子元素中心在 (90,90)，远在半透明父盒 (0,0,20,20) 之外。
        // 半透明红色合成到白底上应为浅红，而不是未绘制的纯白。
        var pixel = Pixel(90, 90);
        pixel.Red.ShouldBeGreaterThan((byte)200);
        pixel.Green.ShouldBeLessThan((byte)200);
        pixel.Blue.ShouldBeLessThan((byte)200);
    }

    /// <summary>半透明元素自身的 box-shadow 画在边框盒之外，不能被裁掉。</summary>
    [Fact]
    public void BoxShadow_IsNotClippedByOpacityLayer()
    {
        var root = new DivElement { Style = new Style { Width = Length.Px(300), Height = Length.Px(300) } };
        var faded = new DivElement
        {
            Style = new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(100),
                Top = Length.Px(100),
                Width = Length.Px(40),
                Height = Length.Px(40),
                BackgroundColor = Color.Blue,
                Opacity = 0.8f,
                BoxShadow = new List<BoxShadow>
                {
                    // 无模糊、无扩散，纯偏移，落点可精确断言
                    new BoxShadow(Length.Px(40), Length.Px(40), Color.Black)
                }
            }
        };
        root.AddChild(faded);

        Render(root);

        // 阴影盒为 (140,140)-(180,180)，其中心 (160,160) 应被绘制（暗色），而非纯白。
        Pixel(160, 160).Red.ShouldBeLessThan((byte)200);
    }

    /// <summary>变换在合成层内部应用，包围盒必须覆盖变换后的几何。</summary>
    [Fact]
    public void TransformedContent_IsNotClippedByOpacityLayer()
    {
        var root = new DivElement { Style = new Style { Width = Length.Px(300), Height = Length.Px(300) } };
        var faded = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(40),
                Height = Length.Px(40),
                BackgroundColor = Color.Red,
                Opacity = 0.9f,
                // 向右下平移 100px：变换后几何完全在原边框盒之外
                Transform = Transform.FromTranslate(Length.Px(100), Length.Px(100)),
                TransformOrigin = TransformOrigin.TopLeft
            }
        };
        root.AddChild(faded);

        Render(root);

        // 变换后盒为 (100,100)-(140,140)，中心 (120,120) 应为红色系。
        var pixel = Pixel(120, 120);
        pixel.Red.ShouldBeGreaterThan((byte)200);
        pixel.Green.ShouldBeLessThan((byte)150);
    }

    /// <summary>半透明本身仍要生效：不能因为传了 bounds 就变成全不透明。</summary>
    [Fact]
    public void Opacity_StillCompositesAlpha()
    {
        var root = new DivElement { Style = new Style { Width = Length.Px(300), Height = Length.Px(300) } };
        var faded = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(100),
                Height = Length.Px(100),
                BackgroundColor = Color.Black,
                Opacity = 0.5f
            }
        };
        root.AddChild(faded);

        Render(root);

        // 50% 黑合成到白底应为中灰，而不是纯黑（层被忽略）或纯白（层被裁空）。
        var pixel = Pixel(50, 50);
        pixel.Red.ShouldBeInRange((byte)100, (byte)160);
        pixel.Green.ShouldBeInRange((byte)100, (byte)160);
    }

    /// <summary>裁剪盒内的溢出后代由 overflow 负责裁掉，合成层不应改变这一行为。</summary>
    [Fact]
    public void ClippedSubtree_StillRespectsOverflowHidden()
    {
        var root = new DivElement { Style = new Style { Width = Length.Px(300), Height = Length.Px(300) } };
        var faded = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(40),
                Height = Length.Px(40),
                Opacity = 0.5f,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden
            }
        };
        var child = new DivElement
        {
            Style = new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(60),
                Top = Length.Px(60),
                Width = Length.Px(60),
                Height = Length.Px(60),
                BackgroundColor = Color.Red
            }
        };
        faded.AddChild(child);
        root.AddChild(faded);

        Render(root);

        // overflow:hidden 把子元素裁到 padding box 内，(90,90) 应保持白色。
        Pixel(90, 90).ShouldBe(SKColors.White);
    }
}
