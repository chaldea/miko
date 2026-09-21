using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// 导航稳态：反复替换整棵根树后，存活的托管堆不随导航轮数增长（ISSUE-141 的确定性探针，
/// 在 ISSUE-142 换掉 <see cref="Length"/> 的内部表示后复跑）。
///
/// <para>ISSUE-141 的原探针是驱动真实示例应用的一次性脚本，没有进仓库。这里把它的<b>判定方法</b>
/// 固定下来：每轮导航若干路由，轮间做 3 次强制压缩 gen2 回收，只统计<b>回收后仍存活</b>的字节。
/// 单纯看"内存上涨"区分不了缓存增长与真实泄漏——能在强制压缩回收后存活的才算泄漏。</para>
///
/// <para>这条断言也是 <see cref="Length"/> 换表示的一道护栏：新表示为混合分量分配旁路对象，
/// 若哪天让某个长期存活的容器（以元素或长度为键的表）持有它们，泄漏会先在这里显形。</para>
/// </summary>
public class NavigationHeapStabilityTests
{
    private const int ElementsPerPage = 60;

    private static List<StyleSheet> Sheets()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { Width = Length.Percent(100), Height = Length.Px(24) },
            // 含真实混合分量的规则：确保旁路对象也参与这次导航循环。
            [".mixed"] = new() { MarginTop = Length.Percent(-100) + Length.Px(10) },
        });
        return [sheet];
    }

    private static Element BuildPage(int route)
    {
        var root = new DivElement { Class = $"page-{route}" };
        for (int i = 0; i < ElementsPerPage; i++)
        {
            root.AddChild(new DivElement
            {
                Id = $"r{route}-e{i}",
                Class = i % 3 == 0 ? "mixed" : "row",
            });
        }
        return root;
    }

    private static long LiveBytes()
    {
        for (int i = 0; i < 3; i++)
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        return GC.GetTotalMemory(forceFullCollection: true);
    }

    [Fact]
    public void RepeatedRootTreeReplacement_ShouldNotGrowLiveHeap()
    {
        using var bitmap = new SKBitmap(400, 600);
        using var canvas = new SKCanvas(bitmap);

        var engine = new MikoEngineBuilder().Build();
        var sheets = Sheets();

        // 预热若干轮，让池、规则索引、各类缓存都达到稳态后再取基线。
        for (int round = 0; round < 3; round++)
            for (int route = 0; route < 5; route++)
            {
                engine.Initialize(BuildPage(route), sheets, canvas, 400, 600);
                engine.Render(canvas);
            }

        long baseline = LiveBytes();

        const int rounds = 15;
        for (int round = 0; round < rounds; round++)
            for (int route = 0; route < 5; route++)
            {
                engine.Initialize(BuildPage(route), sheets, canvas, 400, 600);
                engine.Render(canvas);
            }

        long after = LiveBytes();

        // 每轮 5 个页面 × 60 个元素。真有泄漏时，15 轮会留下 4,500 个元素连同其计算样式
        // （每个数 KB），即数 MB 的单调增长。留 1 MB 余量吸收缓存与分配器抖动。
        (after - baseline).ShouldBeLessThan(1_000_000,
            $"存活堆从 {baseline:N0} B 涨到 {after:N0} B —— 导航路径上有东西钉住了旧树");
    }
}
