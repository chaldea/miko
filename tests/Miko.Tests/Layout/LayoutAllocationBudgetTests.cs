using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// 冷样式解析的分配预算（ISSUE-142）。
///
/// <para>结构体尺寸的断言（见 <c>StyleStorageSizeTests</c>）只能证明单实例变小了，证明不了
/// 它真的传导到布局路径上——中间可能存在装箱、或每个长度都分配一个旁路对象这类把收益吃回去
/// 的写法。这里因此量<b>整条路径</b>的实际分配字节数。</para>
///
/// <para>必须在<b>新线程</b>上量：<see cref="ComputedStyle"/> 的实例池是
/// <c>[ThreadStatic]</c> 的（ISSUE-132），池一旦填满，稳态重解析就不再新建计算样式
/// ——那条路径上的分配与 <see cref="Length"/> 的体积无关，实测换表示前后同为 40,656 B。
/// 真正按元素数分配计算样式的是<b>冷路径</b>：一个页面首次解析、或池被导航挤空之后。
/// 这也修正了 ISSUE-142 的一处预期：它把「降低分配速率」列为主要动机，但那条速率早已由
/// ISSUE-132 的池化摊平；本次瘦身的收益落在冷启动分配与稳态占用上。</para>
/// </summary>
public class LayoutAllocationBudgetTests
{
    private const int ElementCount = 100;

    private static Element CreateTree()
    {
        var root = new DivElement { Id = "root", Class = "container" };
        for (int i = 0; i < ElementCount; i++)
        {
            root.AddChild(new DivElement
            {
                Id = $"child-{i}",
                Class = i % 2 == 0 ? "row even" : "row odd",
            });
        }
        return root;
    }

    private static List<StyleSheet> CreateStyleSheet()
    {
        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("container"), new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Column,
            Padding = new Padding(Length.Px(20)),
            Width = Length.Percent(100),
        });
        sheet.AddRule(new ClassSelector("row"), new Style
        {
            Height = Length.Px(24),
            MarginBottom = Length.Px(4),
            PaddingLeft = Length.Rem(0.5f),
        });
        // 真实混合分量（实测组件库里唯一的两种形状之一），确认旁路路径也在预算内。
        sheet.AddRule(new ClassSelector("even"), new Style
        {
            MarginTop = (Length.Percent(-100) + Length.Px(10) * 2f) / 2f,
        });
        return [sheet];
    }

    [Fact]
    public void ColdStyleResolution_ShouldStayWithinAllocationBudget()
    {
        long perPass = 0;

        // 新线程 = 空的 ComputedStyle 池，于是这一遍解析真的为每个元素新建一个计算样式。
        var thread = new Thread(() =>
        {
            var tree = CreateTree();
            var sheets = CreateStyleSheet();
            var engine = new LayoutEngine();

            // 预热在另一棵树上进行，只为触发 JIT 与规则索引构建，不预热本线程的池。
            new LayoutEngine().Layout(new DivElement(), sheets, 800, 600);

            long before = GC.GetAllocatedBytesForCurrentThread();
            engine.Layout(tree, sheets, 800, 600);
            perPass = GC.GetAllocatedBytesForCurrentThread() - before;
        }, maxStackSize: 4 * 1024 * 1024);

        thread.Start();
        thread.Join();

        // 101 个元素的冷解析：ISSUE-142 之前实测 994,240 B，之后 654,448 B（−34%）。
        // 上界不是精确契约，而是一道「收益别被悄悄吃回去」的护栏。
        perPass.ShouldBeLessThan(720_000);
    }
}
