using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// 导航稳态：反复替换整棵根树后，被换下的旧树一棵都不该存活（ISSUE-141 的确定性探针，
/// 在 ISSUE-142 换掉 <see cref="Length"/> 的内部表示后复跑）。
///
/// <para>ISSUE-141 的原探针是驱动真实示例应用的一次性脚本，没有进仓库。这里把它的<b>判定方法</b>
/// 固定下来：每轮导航若干路由，轮间做 3 次强制压缩 gen2 回收，只看<b>回收后仍存活</b>的对象。
/// 单纯看"内存上涨"区分不了缓存增长与真实泄漏——能在强制压缩回收后存活的才算泄漏。</para>
///
/// <para>判定用的是对每棵旧树根的 <see cref="WeakReference"/>，而不是
/// <c>GC.GetTotalMemory</c>：后者量的是<b>整个进程</b>的托管堆，而 <c>Miko.Tests</c> 是并行跑的
/// （见 ISSUE-129），同时在跑的其它用例的分配会直接混进读数，让断言时紧时松。弱引用只回答
/// 「<b>这棵</b>树被回收了吗」，与旁边跑什么无关，而且比字节阈值更严格。</para>
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

    private static void ForceCompactingCollect()
    {
        for (int i = 0; i < 3; i++)
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    [Fact]
    public void RepeatedRootTreeReplacement_ShouldNotRetainOldTrees()
    {
        using var bitmap = new SKBitmap(400, 600);
        using var canvas = new SKCanvas(bitmap);

        var engine = new MikoEngineBuilder().Build();
        var sheets = Sheets();

        // 每棵树只留弱引用：被换下之后若还有人持有，它就活得过强制压缩回收。
        var trees = new List<WeakReference>();

        const int rounds = 15;
        for (int round = 0; round < rounds; round++)
            for (int route = 0; route < 5; route++)
            {
                var page = BuildPage(route);
                trees.Add(new WeakReference(page));
                engine.Initialize(page, sheets, canvas, 400, 600);
                engine.Render(canvas);
                // page 在此出作用域；仅最后一棵仍是引擎的当前根树。
            }

        ForceCompactingCollect();

        // 最后一棵是引擎的当前根树，理应存活；它之前的每一棵都必须已被回收。
        var retained = trees.Take(trees.Count - 1)
            .Select((reference, index) => (reference, index))
            .Where(entry => entry.reference.IsAlive)
            .Select(entry => entry.index)
            .ToList();

        retained.ShouldBeEmpty(
            $"{retained.Count}/{trees.Count - 1} 棵被换下的根树在强制压缩回收后仍存活 —— " +
            "导航路径上有东西钉住了旧树（见 ISSUE-141）");
        trees[^1].IsAlive.ShouldBeTrue("当前根树不该被回收，否则本用例没测到东西");
    }
}
