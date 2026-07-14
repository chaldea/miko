using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;
using static Miko.Styling.Css;

namespace Miko.Tests.Styling;

/// <summary>
/// CSS 全局关键词（initial / inherit / unset / revert / revert-layer）的单元测试（ISSUE-082 新任务1）。
/// 覆盖：initial 重置默认、inherit 取父值（可继承与不可继承属性）、unset 的分流、
/// revert/revert-layer 等价于 unset、无父元素回退、以及 StyleProperty&lt;T&gt; 联合体本身的关键词路。
/// </summary>
public class StyleKeywordTests
{
    private static ComputedStyle Resolve(Element element, StyleSheet sheet)
        => new StyleResolver().Resolve(element, [sheet]);

    /// <summary>构造一个已算好 ComputedStyle 的父元素，供子元素解析 inherit/unset 时读取。</summary>
    private static DivElement ParentWith(StyleSheet sheet, string @class)
    {
        var parent = new DivElement { Class = @class };
        parent.LayoutBox = new Miko.Layout.LayoutBox
        {
            Element = parent,
            ComputedStyle = new StyleResolver().Resolve(parent, [sheet])
        };
        return parent;
    }

    [Fact]
    public void Initial_ResetsAuthorValueToDefault()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            // 低特异性：把颜色设为红。
            ["div"] = new() { Color = Color.Red },
            // 高特异性：用 initial 显式重置回默认（黑）。
            [".btn"] = new() { Color = Initial }
        });

        var element = new DivElement { Class = "btn" };
        // initial → 保持 ComputedStyle 默认色（黑），而非低特异性规则的红。
        Resolve(element, sheet).Color.ShouldBe(Color.Black);
    }

    [Fact]
    public void Inherit_OnInheritableProperty_TakesParentValue()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".theme"] = new() { Color = Color.Red },
            [".btn"] = new() { Color = Inherit }
        });

        var parent = ParentWith(sheet, "theme");
        var child = new DivElement { Class = "btn" };
        parent.AddChild(child);

        Resolve(child, sheet).Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void Inherit_OnNonInheritableProperty_TakesParentValue()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            // width 不可继承；显式 inherit 仍应取父元素的计算宽度。
            [".theme"] = new() { Width = Length.Px(250) },
            [".btn"] = new() { Width = Inherit }
        });

        var parent = ParentWith(sheet, "theme");
        var child = new DivElement { Class = "btn" };
        parent.AddChild(child);

        Resolve(child, sheet).Width.ShouldBe(Length.Px(250));
    }

    [Fact]
    public void Unset_OnInheritableProperty_BehavesAsInherit()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".theme"] = new() { Color = Color.Red },
            [".btn"] = new() { Color = Unset }
        });

        var parent = ParentWith(sheet, "theme");
        var child = new DivElement { Class = "btn" };
        parent.AddChild(child);

        // 可继承属性 → unset 等价 inherit → 取父值（红）。
        Resolve(child, sheet).Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void Unset_OnNonInheritableProperty_BehavesAsInitial()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".theme"] = new() { Width = Length.Px(250) },
            [".btn"] = new() { Width = Unset }
        });

        var parent = ParentWith(sheet, "theme");
        var child = new DivElement { Class = "btn" };
        parent.AddChild(child);

        // 不可继承属性 → unset 等价 initial → 保持默认（Auto），不取父值。
        Resolve(child, sheet).Width.ShouldBe(Length.Auto);
    }

    [Fact]
    public void Revert_BehavesAsUnset_ForBothInheritanceKinds()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".theme"] = new() { Color = Color.Red, Width = Length.Px(250) },
            [".btn"] = new() { Color = Revert, Width = Revert }
        });

        var parent = ParentWith(sheet, "theme");
        var child = new DivElement { Class = "btn" };
        parent.AddChild(child);

        var computed = Resolve(child, sheet);
        computed.Color.ShouldBe(Color.Red);       // 可继承 → 取父值
        computed.Width.ShouldBe(Length.Auto);     // 不可继承 → 默认
    }

    [Fact]
    public void RevertLayer_BehavesAsUnset_ForBothInheritanceKinds()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".theme"] = new() { Color = Color.Red, Width = Length.Px(250) },
            [".btn"] = new() { Color = RevertLayer, Width = RevertLayer }
        });

        var parent = ParentWith(sheet, "theme");
        var child = new DivElement { Class = "btn" };
        parent.AddChild(child);

        var computed = Resolve(child, sheet);
        computed.Color.ShouldBe(Color.Red);       // 可继承 → 取父值
        computed.Width.ShouldBe(Length.Auto);     // 不可继承 → 默认
    }

    [Fact]
    public void Inherit_OnRootElement_NoParent_FallsBackToDefault()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".btn"] = new() { Color = Inherit }
        });

        // 无父元素：inherit 无来源 → 回退默认色（黑），且不抛异常。
        var element = new DivElement { Class = "btn" };
        Resolve(element, sheet).Color.ShouldBe(Color.Black);
    }

    [Fact]
    public void Keyword_CoexistsWithVarsAndValuesOnSameElement()
    {
        // 同一元素上关键词、变量、具体值混用互不干扰。
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { Color = Color.Red },
            [".btn"] = new()
            {
                Color = Initial,                                   // 关键词：重置为黑
                BackgroundColor = Var("--bg"),                     // 变量：解析为蓝
                Width = Length.Px(100),                            // 具体值
                Vars = new() { ["--bg"] = (Color)"#0000FF" }
            }
        });

        var element = new DivElement { Class = "btn" };
        var computed = Resolve(element, sheet);
        computed.Color.ShouldBe(Color.Black);
        computed.BackgroundColor.ShouldBe(Color.Blue);
        computed.Width.ShouldBe(Length.Px(100));
    }

    // ---- StyleProperty<T> 联合体的关键词路（不经解析器） ----

    [Fact]
    public void StyleProperty_KeywordArm_ReportsKeywordAndBlocksValue()
    {
        StyleProperty<Color> prop = Initial;

        prop.IsKeyword.ShouldBeTrue();
        prop.IsVar.ShouldBeFalse();
        prop.Keyword.ShouldBe(StyleKeyword.Initial);

        // 关键词路不是具体值：TryGetValue 返回 false，ValueOrNull 视为“未设置”。
        prop.TryGetValue(out _).ShouldBeFalse();
        ((StyleProperty<Color>?)prop).ValueOrNull().ShouldBeNull();
    }

    [Fact]
    public void StyleProperty_ImplicitConversion_FromEachKeyword()
    {
        StyleProperty<Length> initial = Initial;
        StyleProperty<Length> inherit = Inherit;
        StyleProperty<Length> unset = Unset;
        StyleProperty<Length> revert = Revert;
        StyleProperty<Length> revertLayer = RevertLayer;

        initial.Keyword.ShouldBe(StyleKeyword.Initial);
        inherit.Keyword.ShouldBe(StyleKeyword.Inherit);
        unset.Keyword.ShouldBe(StyleKeyword.Unset);
        revert.Keyword.ShouldBe(StyleKeyword.Revert);
        revertLayer.Keyword.ShouldBe(StyleKeyword.RevertLayer);
    }
}
