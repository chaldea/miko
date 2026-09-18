using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Styling;
using Miko.Windowing;

namespace VectorSharpnessProbe;

/// <summary>
/// 桌面端矢量图清晰度诊断程序（ISSUE-137 的桌面侧追查）。
///
/// <para><b>为什么需要它</b>：ISSUE-137 的修复在 Android 真机（3× 密度）上确认有效，
/// 但桌面 1080p/100% 下观感未变。桌面宿主 <c>SilkDesktopHost</c> 不做任何
/// <c>canvas.Scale()</c>，<c>DeviceScale</c> 恒为 1，因此那条"按设备像素栅格化"的
/// 修复在桌面上本来就不产生任何差异——桌面的柔和另有原因。</para>
///
/// <para><b>它做什么</b>：把同一个圆形 SVG 以若干种<b>受控</b>方式并排画出来，每一格只改变
/// 一个变量。哪一格清晰、哪一格发虚，直接指出模糊是在哪一步引入的：</para>
/// <list type="number">
///   <item><b>96→96</b>：按原尺寸绘制（1:1，无重采样）。这是清晰度的上限参照。</item>
///   <item><b>96→48</b>：缩小一半（走 cubic 采样段）。</item>
///   <item><b>96→24</b>：缩小到 1/4（走 mipmap 采样段）。</item>
///   <item><b>96→96 但偏移 0.5px</b>：尺寸不变，只把绘制原点挪半个像素，
///         单独暴露采样相位造成的模糊。</item>
///   <item><b>512→96</b>：大 viewBox 源缩小（<c>&lt;img&gt;</c> 头像的路径）。</item>
/// </list>
///
/// <para><b>用法</b>：设 <c>MIKO_DUMP_FRAME=&lt;输出.png&gt;</c> 后运行，宿主会把真实 GL
/// 帧缓冲转储成 PNG。这样绕开外部截图工具——截图软件的缩放/重编码会吃掉抗锯齿，
/// 拿它判断渲染质量会把工具的产物误当成引擎缺陷。</para>
/// </summary>
internal static class Program
{
    private const string CircleRes = "VectorSharpnessProbe.Assets.probe-circle.svg";
    private const string BigCircleRes = "VectorSharpnessProbe.Assets.probe-circle-512.svg";

    [STAThread]
    public static void Main()
    {
        MikoAppBuilder.CreateDefault()
            .UseTitle("Vector Sharpness Probe (ISSUE-137)")
            .UseSize(640, 260)
            .UseRootComponent(BuildRoot)
            .AddStyleSheet(BuildStyleSheet())
            .Build()
            .RunDesktop();
    }

    private static Element BuildRoot()
    {
        var root = new DivElement { Id = "root" };

        var dump = Environment.GetEnvironmentVariable("MIKO_DUMP_FRAME");
        root.AddChild(new ParagraphElement
        {
            Class = "hint",
            TextContent = string.IsNullOrEmpty(dump)
                ? "Set MIKO_DUMP_FRAME=<path.png> to dump the real framebuffer."
                : $"Frame → {dump}",
        });

        var row = new DivElement { Class = "row" };
        // 每一格只改一个变量，便于把模糊归因到具体某一步。
        row.AddChild(Cell("1:1\n96→96", CircleRes, 96, offsetX: 0));
        row.AddChild(Cell("1/2\n96→48", CircleRes, 48, offsetX: 0));
        row.AddChild(Cell("1/4\n96→24", CircleRes, 24, offsetX: 0));
        row.AddChild(Cell("1:1 +0.5px\nphase", CircleRes, 96, offsetX: 0.5f));
        row.AddChild(Cell("1/5.3\n512→96", BigCircleRes, 96, offsetX: 0));
        root.AddChild(row);

        return root;
    }

    /// <summary>
    /// 一个测试格：固定 <paramref name="drawSize"/> 的盒子里画该 SVG 背景。
    /// <paramref name="offsetX"/> 通过 padding 引入一个小数偏移，用来单独考察采样相位。
    /// </summary>
    private static Element Cell(string label, string resource, int drawSize, float offsetX)
    {
        var cell = new DivElement { Class = "cell" };

        var image = BackgroundImage.FromResource(typeof(Program).Assembly, resource);

        var box = new DivElement { Class = "box" };
        box.Style = new Style
        {
            // 盒子固定 96 逻辑像素见方，背景图按 drawSize 绘制，居左上以免居中把偏移抹掉。
            Width = Length.Px(96),
            Height = Length.Px(96),
            PaddingLeft = Length.Px(offsetX),
            BackgroundImage = image,
            BackgroundRepeat = BackgroundRepeat.NoRepeat,
            BackgroundPosition = BackgroundPosition.LeftTop,
            BackgroundSize = BackgroundSize.Px(drawSize, drawSize),
        };
        cell.AddChild(box);
        cell.AddChild(new ParagraphElement { Class = "label", TextContent = label });
        return cell;
    }

    private static StyleSheet BuildStyleSheet()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["#root"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Padding = new Padding(Length.Px(12)),
                // 纯白背景：让圆的边缘过渡带是画面里唯一的中间色，便于逐像素统计。
                BackgroundColor = Color.FromRgb(255, 255, 255),
            },
            [".row"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
            },
            [".cell"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                MarginRight = Length.Px(8),
            },
            [".box"] = new()
            {
                BackgroundColor = Color.FromRgb(255, 255, 255),
            },
            [".label"] = new()
            {
                FontSize = Length.Px(11),
                Color = Color.FromRgb(90, 90, 90),
            },
            [".hint"] = new()
            {
                FontSize = Length.Px(11),
                Color = Color.FromRgb(120, 120, 120),
                MarginBottom = Length.Px(8),
            },
        });
        return sheet;
    }
}
