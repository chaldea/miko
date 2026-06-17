using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Styling;
using Miko.Styling.Selectors;
using Miko.Windowing;
using Miko.Windowing.Video;

namespace MikoApp.Video;

/// <summary>
/// 视频播放最小桌面 demo（Windows）。演示 ISSUE-058 的三个目标：
///   1. &lt;video&gt; 作为 replaced 元素参与盒模型布局；
///   2. FFmpeg 后端（软解基线）解码并合成到 Skia 画布；
///   3. 其它元素（标题遮罩）覆盖在视频之上。
///
/// 用法：MikoApp.Video [视频文件路径]
/// 不传路径时会用 bundled FFmpeg 生成一段 testsrc 测试片段。
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 无窗口自检：编码测试片段 → FFmpeg 后端解码 → 校验帧尺寸。
        // 用于在无显示环境下验证真实解码合成链路（不开窗口）。
        if (args.Contains("--selftest"))
        {
            Environment.Exit(SelfTest.Run());
            return;
        }

        string videoPath = args.Length > 0
            ? args[0]
            : TestClip.EnsureSampleClip();

        Console.WriteLine($"Playing: {videoPath}");

        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("Miko Video Demo");
        builder.UseSize(960, 600);
        builder.UseFFmpegVideo();                       // 注册 FFmpeg 视频后端
        builder.AddStyleSheet(CreateStyles());
        builder.UseRootComponent(() => BuildTree(videoPath));

        builder.Build().RunDesktop();
    }

    private static Element BuildTree(string videoPath)
    {
        return new DivElement
        {
            Class = "page",
            Children =
            {
                new DivElement
                {
                    Class = "stage",
                    Children =
                    {
                        new VideoElement
                        {
                            Class = "player",
                            Source = videoPath,
                            AutoPlay = true,
                            Loop = true,
                            Muted = true,
                        },
                        // 覆盖在视频之上的标题遮罩，证明合成层的 z-order 与覆盖。
                        new DivElement
                        {
                            Class = "caption",
                            TextContent = "Miko <video> — composited over the box model",
                        },
                    },
                },
            },
        };
    }

    private static StyleSheet CreateStyles() => new()
    {
        Rules = new List<StyleRule>
        {
            new()
            {
                Selector = new ClassSelector("page"),
                Style = new Style
                {
                    Display = Display.Flex,
                    Width = Length.Percent(100),
                    Height = Length.Percent(100),
                    BackgroundColor = Color.FromHex("#202830"),
                    Padding = Length.Px(24),
                }
            },
            new()
            {
                // 固定尺寸的视频舞台，圆角 + overflow:hidden 演示盒模型裁剪。
                Selector = new ClassSelector("stage"),
                Style = new Style
                {
                    Display = Display.Block,
                    Position = Position.Relative,
                    Width = Length.Px(640),
                    Height = Length.Px(360),
                    BorderRadius = Length.Px(12),
                    OverflowX = Overflow.Hidden,
                    OverflowY = Overflow.Hidden,
                    MarginLeft = Length.Auto,
                    MarginRight = Length.Auto,
                }
            },
            new()
            {
                Selector = new ClassSelector("player"),
                Style = new Style
                {
                    Display = Display.Block,
                    Width = Length.Percent(100),
                    Height = Length.Percent(100),
                }
            },
            new()
            {
                // 绝对定位的半透明标题，叠在视频帧之上。
                Selector = new ClassSelector("caption"),
                Style = new Style
                {
                    Position = Position.Absolute,
                    Left = Length.Px(0),
                    Right = Length.Px(0),
                    Bottom = Length.Px(0),
                    Padding = Length.Px(12),
                    Color = Color.White,
                    FontSize = Length.Px(16),
                    BackgroundColor = Color.FromRgba(0, 0, 0, 140),
                }
            },
        }
    };
}
