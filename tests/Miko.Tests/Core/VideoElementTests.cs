using Miko.Common;
using Miko.Components;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Core;

/// <summary>
/// VideoElement 的标签构建、默认样式与 replaced 内禀尺寸布局测试。
/// </summary>
public class VideoElementTests
{
    private readonly LayoutEngine _layoutEngine = new();

    // ---- 标签 / 属性构建 ---------------------------------------------------

    [Fact]
    public void RenderTreeBuilder_VideoTag_BuildsVideoElement()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "video");
        builder.AddAttribute(1, "src", "movie.mp4");
        builder.AddAttribute(2, "autoplay", "true");
        builder.AddAttribute(3, "loop", "true");
        builder.AddAttribute(4, "muted", "true");
        builder.AddAttribute(5, "poster", "poster.png");
        builder.CloseElement();

        var root = builder.Build();

        var video = root.ShouldBeOfType<VideoElement>();
        video.TagName.ShouldBe("video");
        video.Source.ShouldBe("movie.mp4");
        video.AutoPlay.ShouldBeTrue();
        video.Loop.ShouldBeTrue();
        video.Muted.ShouldBeTrue();
        video.Poster.ShouldBe("poster.png");
    }

    [Fact]
    public void RenderTreeBuilder_VideoBooleanFalse_IsFalse()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "video");
        builder.AddAttribute(1, "src", "movie.mp4");
        builder.AddAttribute(2, "autoplay", "false");
        builder.CloseElement();

        var video = builder.Build().ShouldBeOfType<VideoElement>();
        video.AutoPlay.ShouldBeFalse();
    }

    // ---- 默认样式 ----------------------------------------------------------

    [Fact]
    public void Resolve_VideoElement_DefaultsToBlackBackgroundAndNoBorder()
    {
        var computed = new StyleResolver().Resolve(new VideoElement(), []);

        computed.BorderTopWidth.Value.ShouldBe(0);
        computed.BorderTopStyle.ShouldBe(BorderStyle.None);
        computed.BackgroundColor.ShouldBe(Color.Black);
    }

    // ---- replaced 内禀尺寸布局 --------------------------------------------

    [Fact]
    public void Layout_VideoNoIntrinsic_NoStyle_FallsBackToDefault300x150()
    {
        var video = new VideoElement();

        var box = LayoutVideo(video);

        box.BoxModel.Content.Width.ShouldBe(300);
        box.BoxModel.Content.Height.ShouldBe(150);
    }

    [Fact]
    public void Layout_VideoWithIntrinsic_UsesIntrinsicSize()
    {
        var video = new VideoElement();
        SetIntrinsic(video, 640, 360);

        var box = LayoutVideo(video);

        box.BoxModel.Content.Width.ShouldBe(640);
        box.BoxModel.Content.Height.ShouldBe(360);
    }

    [Fact]
    public void Layout_VideoWidthOnly_DerivesHeightFromAspectRatio()
    {
        var video = new VideoElement();
        SetIntrinsic(video, 640, 360);   // 16:9

        var box = LayoutVideo(video, new Style { Display = Display.Block, Width = Length.Px(320) });

        box.BoxModel.Content.Width.ShouldBe(320);
        box.BoxModel.Content.Height.ShouldBe(180);   // 320 * 360/640
    }

    [Fact]
    public void Layout_VideoHeightOnly_DerivesWidthFromAspectRatio()
    {
        var video = new VideoElement();
        SetIntrinsic(video, 640, 360);

        var box = LayoutVideo(video, new Style { Display = Display.Block, Height = Length.Px(180) });

        box.BoxModel.Content.Height.ShouldBe(180);
        box.BoxModel.Content.Width.ShouldBe(320);    // 180 * 640/360
    }

    [Fact]
    public void Layout_VideoExplicitWidthHeight_OverridesIntrinsic()
    {
        var video = new VideoElement();
        SetIntrinsic(video, 640, 360);

        var box = LayoutVideo(video, new Style { Display = Display.Block, Width = Length.Px(500), Height = Length.Px(400) });

        box.BoxModel.Content.Width.ShouldBe(500);
        box.BoxModel.Content.Height.ShouldBe(400);
    }

    // ---- helpers -----------------------------------------------------------

    private LayoutBox LayoutVideo(VideoElement video, Style? style = null)
    {
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("video"),
                        Style = style ?? new Style { Display = Display.Block }
                    }
                }
            }
        };

        return _layoutEngine.Layout(video, styleSheets, 800, 600);
    }

    private static void SetIntrinsic(VideoElement video, int w, int h)
    {
        // IntrinsicWidth/Height 是 internal，测试程序集通过 InternalsVisibleTo 访问。
        video.IntrinsicWidth = w;
        video.IntrinsicHeight = h;
    }
}
