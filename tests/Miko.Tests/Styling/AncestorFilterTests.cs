using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

/// <summary>
/// ISSUE-146：祖先过滤器只做「快速否决」，级联结果必须与不用它时逐一相同。
///
/// <para>它否决的依据是「规则要求某个祖先带某类名/ID/标签，而祖先链上根本没有」。风险全在
/// 否决了其实会匹配的规则——因此这里对每种组合器形状都同时测「应否决」与「不得否决」，并用一个
/// 混合形状的随机树做整体等价比对。</para>
/// </summary>
public class AncestorFilterTests
{
    private static StyleSheet SheetOf(params (string selector, Style style)[] rules)
    {
        var sheet = new StyleSheet();
        foreach (var (selector, style) in rules)
            sheet.AddRule(CssSelectorParser.Parse(selector), style);
        return sheet;
    }

    private static Color Red => Color.FromRgb(255, 0, 0);

    /// <summary>整树布局后读目标元素的计算颜色——走的是带过滤器的真实样式阶段。</summary>
    private static Color ColorOf(Element root, Element target, StyleSheet sheet)
    {
        new LayoutEngine().Layout(root, [sheet], 400, 400);
        return target.LayoutBox!.ComputedStyle.Color;
    }

    [Fact]
    public void DescendantRule_MatchesThroughIntermediateAncestors()
    {
        var sheet = SheetOf((".card .title", new Style { Color = Red }));
        var title = new DivElement { Class = "title" };
        var root = new DivElement { Class = "card", Children = { new DivElement { Children = { title } } } };

        ColorOf(root, title, sheet).ShouldBe(Red);
    }

    [Fact]
    public void DescendantRule_WithoutTheAncestorClass_DoesNotMatch()
    {
        var sheet = SheetOf((".card .title", new Style { Color = Red }));
        var title = new DivElement { Class = "title" };
        var root = new DivElement { Class = "list", Children = { title } };

        ColorOf(root, title, sheet).ShouldNotBe(Red);
    }

    [Fact]
    public void AncestorRequirementMetBySelf_IsNotEnough()
    {
        // .a .a 需要一个<b>真</b>祖先带 .a；元素自己带 .a 不算。
        var sheet = SheetOf((".a .a", new Style { Color = Red }));
        var lone = new DivElement { Class = "a" };
        var root = new DivElement { Children = { lone } };

        ColorOf(root, lone, sheet).ShouldNotBe(Red);
    }

    [Fact]
    public void CompoundAncestor_AllPartsMustBeOnOneAncestor_ButFilterOnlyNeedsThemSomewhere()
    {
        // 过滤器只要求 .x 与 .y 都在祖先链上（可在不同祖先）；最终判定仍由完整选择器做。
        var sheet = SheetOf((".x.y .t", new Style { Color = Red }));
        var split = new DivElement { Class = "t" };
        var splitRoot = new DivElement { Class = "x", Children = { new DivElement { Class = "y", Children = { split } } } };
        ColorOf(splitRoot, split, sheet).ShouldNotBe(Red);

        var joined = new DivElement { Class = "t" };
        var joinedRoot = new DivElement { Class = "x y", Children = { joined } };
        ColorOf(joinedRoot, joined, sheet).ShouldBe(Red);
    }

    [Fact]
    public void ChildRule_AndIdAndTagAncestors_Match()
    {
        var sheet = SheetOf(
            ("#shell > .t", new Style { Color = Red }),
            ("nav .u", new Style { BackgroundColor = Red }));
        var t = new DivElement { Class = "t" };
        var u = new DivElement { Class = "u" };
        var root = new DivElement { Id = "shell", Children = { t, new NavElement { Children = { u } } } };

        new LayoutEngine().Layout(root, [sheet], 400, 400);

        t.LayoutBox!.ComputedStyle.Color.ShouldBe(Red);
        u.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Red);
    }

    [Fact]
    public void NegatedAncestorClass_IsNeverUsedAsARequirement()
    {
        // :not(.x) 的祖先侧不能贡献键——那会把「必须没有」当成「必须有」。
        var sheet = SheetOf((":not(.x) .t", new Style { Color = Red }));
        var t = new DivElement { Class = "t" };
        var root = new DivElement { Class = "plain", Children = { t } };

        ColorOf(root, t, sheet).ShouldBe(Red);
    }

    [Fact]
    public void SiblingLeftSide_IsNeverUsedAsAnAncestorRequirement()
    {
        // .a + .t 的 .a 是兄弟而非祖先；把它当祖先键会否决这条本该命中的规则。
        var sheet = SheetOf((".a + .t", new Style { Color = Red }));
        var t = new DivElement { Class = "t" };
        var root = new DivElement { Children = { new DivElement { Class = "a" }, t } };

        ColorOf(root, t, sheet).ShouldBe(Red);
    }

    [Fact]
    public void TagAncestor_IsCaseInsensitive()
    {
        var sheet = SheetOf(("NAV .t", new Style { Color = Red }));
        var t = new DivElement { Class = "t" };
        var root = new NavElement { Children = { t } };

        ColorOf(root, t, sheet).ShouldBe(Red);
    }

    [Fact]
    public void ResolvingOutOfTreeOrder_FallsBackToFullMatching()
    {
        // 过滤器栈顶与被测元素的父元素对不上时（调用方没有按树序递归），不得做任何否决。
        var sheet = SheetOf((".card .title", new Style { Color = Red }));
        var title = new DivElement { Class = "title" };
        var root = new DivElement { Class = "card", Children = { title } };
        var resolver = new StyleResolver();
        var unrelated = new AncestorFilter();
        unrelated.Push(new DivElement { Class = "nothing-here" });

        root.LayoutBox = new LayoutBox { Element = root, ComputedStyle = resolver.Resolve(root, [sheet]) };
        resolver.Resolve(title, [sheet], null, unrelated).Color.ShouldBe(Red);
    }

    [Fact]
    public void RandomTrees_CascadeIsIdenticalWithAndWithoutTheFilter()
    {
        // 整体等价：同一棵树，分别用过滤器（LayoutEngine 的样式阶段）与不用过滤器（逐元素直接
        // Resolve）解析，所有元素的计算样式在被测属性上必须完全一致。
        string[] classes = ["a", "b", "c", "d", "card", "title", "item"];
        var sheet = SheetOf(
            (".a .b", new Style { Color = Color.FromRgb(1, 0, 0) }),
            (".a > .c", new Style { BackgroundColor = Color.FromRgb(2, 0, 0) }),
            (".card .title .item", new Style { Width = Length.Px(3) }),
            (".b.c .d", new Style { Height = Length.Px(4) }),
            ("div .a .b .c", new Style { MarginLeft = Length.Px(5) }),
            (".d ~ .a", new Style { MarginTop = Length.Px(6) }),
            (":not(.a) > .b", new Style { PaddingLeft = Length.Px(7) }),
            (".item", new Style { PaddingTop = Length.Px(8) }),
            ("#root .card", new Style { Opacity = 0.5f }));

        var random = new Random(146);
        for (int trial = 0; trial < 30; trial++)
        {
            var all = new List<Element>();
            var root = new DivElement { Id = "root" };
            all.Add(root);
            Grow(root, depth: 0);

            new LayoutEngine().Layout(root, [sheet], 400, 400);
            var filtered = all.ToDictionary(e => e, e => Snapshot(e.LayoutBox!.ComputedStyle));

            var resolver = new StyleResolver();
            foreach (var element in all)
            {
                // 按树序逐个解析且不传过滤器：父样式先就位，继承链与 LayoutEngine 一致。
                var computed = resolver.Resolve(element, [sheet]);
                element.LayoutBox = new LayoutBox { Element = element, ComputedStyle = computed };
                Snapshot(computed).ShouldBe(filtered[element], $"trial {trial}: {element}");
            }

            void Grow(Element parent, int depth)
            {
                if (depth >= 5) return;
                int count = random.Next(1, 4);
                for (int i = 0; i < count; i++)
                {
                    var tokens = classes.Where(_ => random.Next(4) == 0).ToArray();
                    var child = new DivElement { Class = tokens.Length == 0 ? null : string.Join(' ', tokens) };
                    parent.AddChild(child);
                    all.Add(child);
                    Grow(child, depth + 1);
                }
            }
        }

        static string Snapshot(ComputedStyle s) =>
            $"{s.Color}|{s.BackgroundColor}|{s.Width}|{s.Height}|{s.MarginLeft}|{s.MarginTop}|{s.PaddingLeft}|{s.PaddingTop}|{s.Opacity}";
    }
}
