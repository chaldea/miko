using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

/// <summary>
/// ISSUE-113：规则索引（<c>RuleIndex</c>）只做「快速否定」，级联结果必须与逐条全表匹配完全一致。
/// 这些用例锁定该等价性——索引是本次性能优化中风险最高的一环。
/// </summary>
public class RuleIndexTests
{
    /// <summary>把选择器串编译成规则加入样式表（保持定义顺序 = 级联顺序）。</summary>
    private static StyleSheet SheetOf(params (string selector, Style style)[] rules)
    {
        var sheet = new StyleSheet();
        foreach (var (selector, style) in rules)
            sheet.AddRule(CssSelectorParser.Parse(selector), style);
        return sheet;
    }

    private static ComputedStyle Resolve(Element element, StyleSheet sheet)
        => new StyleResolver().Resolve(element, [sheet]);

    [Fact]
    public void ClassKeyedRule_StillMatches()
    {
        var sheet = SheetOf((".btn", new Style { Width = Length.Px(120) }));
        var button = new DivElement { Class = "btn" };

        Resolve(button, sheet).Width.Value.ShouldBe(120);
    }

    [Fact]
    public void ElementWithoutKey_DoesNotPickUpRule()
    {
        var sheet = SheetOf((".btn", new Style { Width = Length.Px(120) }));
        var plain = new DivElement { Class = "other" };

        // .btn 规则被索引到 "btn" 桶，plain 不含该类名 → 不应命中（宽度保持 auto）。
        Resolve(plain, sheet).Width.IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void MultiClassElement_MatchesRulesFromEveryClassBucket()
    {
        var sheet = SheetOf(
            (".a", new Style { Width = Length.Px(10) }),
            (".b", new Style { Height = Length.Px(20) }));
        // 多类名元素必须从每个类名桶都取到候选。
        var element = new DivElement { Class = "a b" };

        var computed = Resolve(element, sheet);
        computed.Width.Value.ShouldBe(10);
        computed.Height.Value.ShouldBe(20);
    }

    [Theory]
    [InlineData("a b")]
    [InlineData("  a   b  ")]
    [InlineData("a\tb")]
    [InlineData("a\nb")]
    public void ClassTokenizer_HandlesArbitraryWhitespace(string classList)
    {
        // HasClass 改为零分配 span 分词（ISSUE-113），须保持 CSS 的空白分隔语义。
        var element = new DivElement { Class = classList };
        element.HasClass("a").ShouldBeTrue();
        element.HasClass("b").ShouldBeTrue();
        element.HasClass("c").ShouldBeFalse();
    }

    [Fact]
    public void ClassTokenizer_DoesNotMatchPartialToken()
    {
        var element = new DivElement { Class = "btn-primary container" };
        // 前缀/后缀/子串都不算命中——只有完整 token 才匹配。
        element.HasClass("btn").ShouldBeFalse();
        element.HasClass("primary").ShouldBeFalse();
        element.HasClass("contain").ShouldBeFalse();
        element.HasClass("btn-primary").ShouldBeTrue();
        element.HasClass("container").ShouldBeTrue();
    }

    [Fact]
    public void DescendantSelector_KeyedByRightmostCompound()
    {
        var sheet = SheetOf((".card .title", new Style { Width = Length.Px(50) }));
        var card = new DivElement { Class = "card" };
        var title = new DivElement { Class = "title" };
        card.AddChild(title);

        // 索引按最右侧（.title）分桶，祖先约束仍由完整选择器判定。
        Resolve(title, sheet).Width.Value.ShouldBe(50);
        // 同样含 .title 但无 .card 祖先 → 不匹配。
        Resolve(new DivElement { Class = "title" }, sheet).Width.IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void GroupSelector_MatchesFromEveryBranch()
    {
        var sheet = SheetOf(("h1, .lead, #hero", new Style { Width = Length.Px(70) }));

        // 分组的每个分支都要各自入桶。
        Resolve(new H1Element(), sheet).Width.Value.ShouldBe(70);
        Resolve(new DivElement { Class = "lead" }, sheet).Width.Value.ShouldBe(70);
        Resolve(new DivElement { Id = "hero" }, sheet).Width.Value.ShouldBe(70);
        Resolve(new DivElement { Class = "nope" }, sheet).Width.IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void GroupSelector_WithUnbucketableBranch_StillMatchesBucketableBranch()
    {
        // ".a, :hover"：.a 分支可入桶，:hover 分支不可 → 整条规则退回通用桶。
        // 两个分支都必须照常生效。
        var sheet = SheetOf((".a, :hover", new Style { Width = Length.Px(40) }));

        Resolve(new DivElement { Class = "a" }, sheet).Width.Value.ShouldBe(40);

        var hovered = new DivElement();
        hovered.SetState(ElementState.Hover);
        Resolve(hovered, sheet).Width.Value.ShouldBe(40);

        Resolve(new DivElement(), sheet).Width.IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void GroupSelector_UnbucketableBranchFirst_StillMatchesLaterBranch()
    {
        // 分支顺序不应影响结果：不可入桶的分支排在前面时，后续分支同样要生效。
        var sheet = SheetOf((":hover, .a", new Style { Width = Length.Px(41) }));

        Resolve(new DivElement { Class = "a" }, sheet).Width.Value.ShouldBe(41);

        var hovered = new DivElement();
        hovered.SetState(ElementState.Hover);
        Resolve(hovered, sheet).Width.Value.ShouldBe(41);
    }

    [Fact]
    public void GroupSelector_MultipleMatchingBranches_ResolveCorrectly()
    {
        // ".a, .b" 的两个分支分别入 .a / .b 桶；元素同时含两个类名时两个分支都会命中。
        // 级联须照常给出该规则的值（内部另有去重，避免同一条规则被重复求值/重复排序，
        // 那属于性能层面，由 HoverFrameBenchmarks 的 Allocated 列守护）。
        var sheet = SheetOf((".a, .b", new Style { Width = Length.Px(42) }));

        Resolve(new DivElement { Class = "a b" }, sheet).Width.Value.ShouldBe(42);
        Resolve(new DivElement { Class = "a" }, sheet).Width.Value.ShouldBe(42);
        Resolve(new DivElement { Class = "b" }, sheet).Width.Value.ShouldBe(42);
        Resolve(new DivElement { Class = "c" }, sheet).Width.IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void NotSelector_IsNotUsedAsIndexKey()
    {
        // :not(.excluded) 内的类名不能当作关键选择器——不含该类名的元素才匹配。
        var sheet = SheetOf(("div:not(.excluded)", new Style { Width = Length.Px(80) }));

        Resolve(new DivElement { Class = "included" }, sheet).Width.Value.ShouldBe(80);
        Resolve(new DivElement { Class = "excluded" }, sheet).Width.IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void CompoundWithNot_KeysOnRealClass_NotOnNegatedOne()
    {
        // ".item:not(.excluded)" 解析为复合选择器 [ClassSelector(item), NotSelector(.excluded)]。
        // 关键选择器必须取 .item；若误取 :not 内的 .excluded，候选集就会变成「含 excluded 的
        // 元素」——与语义恰好相反，本应匹配的 .item 元素全部漏掉。
        var sheet = SheetOf((".item:not(.excluded)", new Style { Width = Length.Px(80) }));

        Resolve(new DivElement { Class = "item" }, sheet).Width.Value.ShouldBe(80);
        Resolve(new DivElement { Class = "item excluded" }, sheet).Width.IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void NotSelectorBeforeClass_StillKeysOnRealClass()
    {
        // CSS 允许 :not() 写在复合选择器的其他简单选择器之前。此时按声明顺序扫描时
        // :not(.excluded) 先被遇到——必须跳过它继续找到真正的关键选择器 .item，
        // 否则候选集会被错误地限制为「含 excluded 的元素」，.item 元素全部漏匹配。
        var sheet = SheetOf((":not(.excluded).item", new Style { Width = Length.Px(80) }));

        Resolve(new DivElement { Class = "item" }, sheet).Width.Value.ShouldBe(80);
        Resolve(new DivElement { Class = "item excluded" }, sheet).Width.IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void BarePseudoClassRule_GoesToUniversalBucket()
    {
        // 无 tag/class/id 关键选择器的规则必须对每个元素都参与匹配。
        var sheet = SheetOf((":hover", new Style { Width = Length.Px(90) }));
        var element = new DivElement();

        Resolve(element, sheet).Width.IsAuto.ShouldBeTrue();
        element.SetState(ElementState.Hover);
        Resolve(element, sheet).Width.Value.ShouldBe(90);
    }

    [Fact]
    public void UniversalSelector_AppliesToEveryElement()
    {
        var sheet = SheetOf(("*", new Style { Width = Length.Px(5) }));

        Resolve(new DivElement(), sheet).Width.Value.ShouldBe(5);
        Resolve(new SpanElement { Class = "x" }, sheet).Width.Value.ShouldBe(5);
    }

    [Fact]
    public void CascadeOrder_LaterRuleWinsAtEqualSpecificity()
    {
        // 索引改变了规则的遍历顺序，但排序键（定义序号）必须仍反映原始定义顺序。
        var sheet = SheetOf(
            (".a", new Style { Width = Length.Px(1) }),
            (".b", new Style { Width = Length.Px(2) }));
        var element = new DivElement { Class = "a b" };

        // 同特异性 → 后定义的 .b 胜出。
        Resolve(element, sheet).Width.Value.ShouldBe(2);
    }

    [Fact]
    public void CascadeOrder_HigherSpecificityWinsRegardlessOfBucket()
    {
        var sheet = SheetOf(
            ("#hero", new Style { Width = Length.Px(3) }),   // 特异性 100，先定义
            (".a", new Style { Width = Length.Px(4) }));     // 特异性 10，后定义
        var element = new DivElement { Id = "hero", Class = "a" };

        Resolve(element, sheet).Width.Value.ShouldBe(3);
    }

    [Fact]
    public void RulesAddedAfterFirstResolve_AreIndexed()
    {
        // 索引惰性构建；样式表在注册后增量添加规则时必须重建索引。
        var sheet = SheetOf((".a", new Style { Width = Length.Px(1) }));
        var element = new DivElement { Class = "a b" };
        Resolve(element, sheet).Width.Value.ShouldBe(1);

        sheet.AddRule(CssSelectorParser.Parse(".b"), new Style { Width = Length.Px(2) });
        Resolve(element, sheet).Width.Value.ShouldBe(2);
    }

    [Fact]
    public void RulesAssignedViaCollectionInitializer_AreIndexed()
    {
        // 绕过 AddRule 直接写 Rules 集合（测试与 DomBuilder 的惯用写法）也须被索引覆盖。
        var sheet = new StyleSheet
        {
            Rules =
            [
                new StyleRule { Selector = new ClassSelector("direct"), Style = new Style { Width = Length.Px(11) } }
            ]
        };

        Resolve(new DivElement { Class = "direct" }, sheet).Width.Value.ShouldBe(11);
    }

    /// <summary>
    /// 等价性总检：在一张混合了各类选择器的样式表上，对一棵混合 DOM 树逐元素比对
    /// 「经索引的解析结果」与「不经索引的朴素全表解析结果」，两者必须逐属性一致。
    /// </summary>
    [Fact]
    public void IndexedResolution_MatchesNaiveFullScan_ForMixedSelectors()
    {
        var selectors = new[]
        {
            "div", "span", ".card", ".title", "#main", ".card .title", ".card > span",
            "div.card", "span:first-child", "span:last-child", "div:not(.card)",
            "*", ".card, .panel", ".panel .title", "div span", ".missing",
        };

        var sheet = new StyleSheet();
        var naive = new List<StyleRule>();
        for (int i = 0; i < selectors.Length; i++)
        {
            var selector = CssSelectorParser.Parse(selectors[i]);
            // 每条规则写入互不相同的 Width，使级联胜出者可辨认。
            var style = new Style { Width = Length.Px(i + 1) };
            sheet.AddRule(selector, style);
            naive.Add(new StyleRule { Selector = selector, Style = style });
        }

        var root = new DivElement { Id = "main", Class = "panel" };
        var card = new DivElement { Class = "card" };
        card.AddChild(new SpanElement { Class = "title" });
        card.AddChild(new SpanElement());
        card.AddChild(new DivElement { Class = "title" });
        root.AddChild(card);
        root.AddChild(new SpanElement { Class = "title" });
        root.AddChild(new DivElement());

        var elements = new List<Element>();
        void Collect(Element e) { elements.Add(e); foreach (var c in e.Children) Collect(c); }
        Collect(root);

        foreach (var element in elements)
        {
            var indexed = new StyleResolver().Resolve(element, [sheet]);
            var expected = NaiveResolveWidth(element, naive);

            if (expected == null)
                indexed.Width.IsAuto.ShouldBeTrue($"{element} should not match any rule");
            else
                indexed.Width.Value.ShouldBe(expected.Value, $"{element} cascade mismatch");
        }
    }

    /// <summary>
    /// 朴素参照实现：逐条全表匹配，按「特异性 → 定义顺序」取胜出者的 Width。
    /// 与 <see cref="StyleResolver"/> 的排序规则一致（单样式表、无媒体查询、无行内样式）。
    /// </summary>
    private static float? NaiveResolveWidth(Element element, List<StyleRule> rules)
    {
        StyleRule? winner = null;
        int winnerSpecificity = -1;
        int winnerIndex = -1;

        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (!rule.Selector.Matches(element)) continue;
            if (rule.Style.Width == null) continue;

            int specificity = rule.Selector.Specificity;
            if (specificity > winnerSpecificity || (specificity == winnerSpecificity && i > winnerIndex))
            {
                winner = rule;
                winnerSpecificity = specificity;
                winnerIndex = i;
            }
        }

        if (winner?.Style.Width == null) return null;
        return winner.Style.Width.ValueOrNull()?.Value;
    }
}
