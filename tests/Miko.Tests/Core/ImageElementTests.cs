using Miko.Common;
using Miko.Components;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Core;

/// <summary>
/// ImageElement 的标签/属性构建与 replaced 内禀尺寸布局（加载完成后由内禀尺寸驱动）。
/// </summary>
public class ImageElementTests
{
    private readonly LayoutEngine _layoutEngine = new();

    // ---- 标签 / 属性构建 ---------------------------------------------------

    [Fact]
    public void RenderTreeBuilder_ImgTag_BuildsImageElement()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "img");
        builder.AddAttribute(1, "src", "https://x/a.png");
        builder.AddAttribute(2, "placeholder", "res://Assets/spinner.png");
        builder.CloseElement();

        var root = builder.Build();

        var img = root.ShouldBeOfType<ImageElement>();
        img.TagName.ShouldBe("img");
        img.Source.Raw.ShouldBe("https://x/a.png");
        img.Source.Scheme.ShouldBe(MediaSourceScheme.Https);
        img.Placeholder.ShouldBe("res://Assets/spinner.png");
    }

    // ---- replaced 内禀尺寸布局 --------------------------------------------

    [Fact]
    public void Layout_ImageWithIntrinsic_UsesIntrinsicSize()
    {
        var img = new ImageElement();
        SetIntrinsic(img, 640, 360);

        var box = LayoutImage(img);

        box.BoxModel.Content.Width.ShouldBe(640);
        box.BoxModel.Content.Height.ShouldBe(360);
    }

    [Fact]
    public void Layout_ImageWidthOnly_DerivesHeightFromAspectRatio()
    {
        var img = new ImageElement();
        SetIntrinsic(img, 640, 360);   // 16:9

        var box = LayoutImage(img, new Style { Display = Display.Block, Width = Length.Px(320) });

        box.BoxModel.Content.Width.ShouldBe(320);
        box.BoxModel.Content.Height.ShouldBe(180);   // 320 * 360/640
    }

    [Fact]
    public void Layout_ImageHeightOnly_DerivesWidthFromAspectRatio()
    {
        var img = new ImageElement();
        SetIntrinsic(img, 640, 360);

        var box = LayoutImage(img, new Style { Display = Display.Block, Height = Length.Px(180) });

        box.BoxModel.Content.Height.ShouldBe(180);
        box.BoxModel.Content.Width.ShouldBe(320);    // 180 * 640/360
    }

    [Fact]
    public void Layout_ImageExplicitWidthHeight_OverridesIntrinsic()
    {
        var img = new ImageElement();
        SetIntrinsic(img, 640, 360);

        var box = LayoutImage(img, new Style { Display = Display.Block, Width = Length.Px(500), Height = Length.Px(400) });

        box.BoxModel.Content.Width.ShouldBe(500);
        box.BoxModel.Content.Height.ShouldBe(400);
    }

    // ---- max-width 等比缩放（ISSUE-083）-----------------------------------

    [Fact]
    public void Layout_ImageMaxWidthClamps_ScalesHeightProportionally()
    {
        // 默认 <img> 为 display:inline，走 InlineLayout。宽高均 auto，
        // max-width 将内禀宽 1024 夹取到 400，高度须按纵横比等比缩放，而非保持内禀高。
        var img = new ImageElement();
        SetIntrinsic(img, 1024, 512);   // 2:1

        var box = LayoutImage(img, new Style { MaxWidth = Length.Px(400) });

        box.BoxModel.Content.Width.ShouldBe(400);
        box.BoxModel.Content.Height.ShouldBe(200);   // 400 * 512/1024
    }

    [Fact]
    public void Layout_BlockImageMaxWidthClamps_ScalesHeightProportionally()
    {
        // display:block 路径（BlockLayout）同样须在 max-width 夹取后等比缩放高度。
        var img = new ImageElement();
        SetIntrinsic(img, 1024, 512);

        var box = LayoutImage(img, new Style { Display = Display.Block, MaxWidth = Length.Px(400) });

        box.BoxModel.Content.Width.ShouldBe(400);
        box.BoxModel.Content.Height.ShouldBe(200);
    }

    [Fact]
    public void Layout_ImageMaxWidthAboveIntrinsic_KeepsIntrinsicSize()
    {
        // max-width 大于内禀宽时不夹取，维持内禀尺寸。
        var img = new ImageElement();
        SetIntrinsic(img, 1024, 512);

        var box = LayoutImage(img, new Style { MaxWidth = Length.Px(2000) });

        box.BoxModel.Content.Width.ShouldBe(1024);
        box.BoxModel.Content.Height.ShouldBe(512);
    }

    // ---- helpers -----------------------------------------------------------

    private LayoutBox LayoutImage(ImageElement img, Style? style = null)
    {
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("img"),
                        Style = style ?? new Style { Display = Display.Block }
                    }
                }
            }
        };

        return _layoutEngine.Layout(img, styleSheets, 800, 600);
    }

    private static void SetIntrinsic(ImageElement img, int w, int h)
    {
        // IntrinsicWidth/Height 是 internal，测试程序集通过 InternalsVisibleTo 访问。
        img.IntrinsicWidth = w;
        img.IntrinsicHeight = h;
    }
}
