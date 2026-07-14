using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;
using static Miko.Styling.Css;

namespace Miko.Tests.Styling;

/// <summary>
/// 自定义样式变量（CSS custom properties）的单元测试（ISSUE-082）。
/// 覆盖：同元素定义并使用、祖先继承、后代覆盖、回退值、未解析、多类型、级联链、issue 原样例。
/// </summary>
public class CssVariableTests
{
    private static ComputedStyle Resolve(Element element, StyleSheet sheet)
        => new StyleResolver().Resolve(element, [sheet]);

    [Fact]
    public void Var_DefinedAndUsedOnSameElement_Resolves()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".btn"] = new()
            {
                Color = Var("--button-color"),
                Vars = new() { ["--button-color"] = (Color)"#FF0000" }
            }
        });

        var element = new DivElement { Class = "btn" };
        Resolve(element, sheet).Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void Var_InheritedFromAncestorScope_Resolves()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            // 父元素定义变量
            [".theme"] = new()
            {
                Vars = new() { ["--button-color"] = (Color)"#FFFFFF" }
            },
            // 子元素引用变量（自身无定义，靠继承）
            [".btn"] = new()
            {
                Color = Var("--button-color")
            }
        });

        var parent = new DivElement { Class = "theme" };
        var child = new DivElement { Class = "btn" };
        parent.AddChild(child);

        // 需要父元素先算出 ComputedStyle（含变量作用域），布局引擎按此顺序驱动；
        // 这里手动模拟：先解析父，再解析子。
        var resolver = new StyleResolver();
        parent.LayoutBox = new Miko.Layout.LayoutBox
        {
            Element = parent,
            ComputedStyle = resolver.Resolve(parent, [sheet])
        };

        resolver.Resolve(child, [sheet]).Color.ShouldBe(Color.White);
    }

    [Fact]
    public void Var_OverriddenInDescendantScope_UsesNearestDefinition()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new()
            {
                Vars = new() { ["--c"] = (Color)"#FFFFFF" }   // 白
            },
            [".mid"] = new()
            {
                Vars = new() { ["--c"] = (Color)"#FF0000" }   // 覆盖为红
            },
            [".leaf"] = new()
            {
                Color = Var("--c")
            }
        });

        var root = new DivElement { Class = "root" };
        var mid = new DivElement { Class = "mid" };
        var leaf = new DivElement { Class = "leaf" };
        root.AddChild(mid);
        mid.AddChild(leaf);

        var resolver = new StyleResolver();
        root.LayoutBox = new Miko.Layout.LayoutBox { Element = root, ComputedStyle = resolver.Resolve(root, [sheet]) };
        mid.LayoutBox = new Miko.Layout.LayoutBox { Element = mid, ComputedStyle = resolver.Resolve(mid, [sheet]) };

        // leaf 应解析到最近的 .mid 定义（红），而非 .root 的白。
        resolver.Resolve(leaf, [sheet]).Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void Var_Unresolved_WithFallback_UsesFallback()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".btn"] = new()
            {
                Color = Var("--missing", (Color)"#00FF00")   // 变量未定义，用回退绿
            }
        });

        var element = new DivElement { Class = "btn" };
        Resolve(element, sheet).Color.ShouldBe(Color.Green);
    }

    [Fact]
    public void Var_Unresolved_NoFallback_FallsBackToInheritedOrDefault()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".btn"] = new()
            {
                Color = Var("--missing")   // 变量未定义，也无回退
            }
        });

        var element = new DivElement { Class = "btn" };
        // 未解析 → 属性视为未设置 → 回退到 ComputedStyle 的默认色（黑）。
        Resolve(element, sheet).Color.ShouldBe(Color.Black);
    }

    [Fact]
    public void Var_ResolvesLengthValue()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                Width = Var("--w"),
                Vars = new() { ["--w"] = Length.Px(120) }
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).Width.ShouldBe(Length.Px(120));
    }

    [Fact]
    public void Var_ResolvesEnumValue()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                Display = Var("--d"),
                Vars = new() { ["--d"] = VarValue.FromEnum(Display.Flex) }
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).Display.ShouldBe(Display.Flex);
    }

    [Fact]
    public void Var_CascadesThroughNestedSelectorChain()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".a"] = new()
            {
                Vars = new() { ["--accent"] = (Color)"#0000FF" }   // 蓝
            },
            [".a .b .c"] = new()
            {
                Color = Var("--accent")
            }
        });

        var a = new DivElement { Class = "a" };
        var b = new DivElement { Class = "b" };
        var c = new DivElement { Class = "c" };
        a.AddChild(b);
        b.AddChild(c);

        var resolver = new StyleResolver();
        a.LayoutBox = new Miko.Layout.LayoutBox { Element = a, ComputedStyle = resolver.Resolve(a, [sheet]) };
        b.LayoutBox = new Miko.Layout.LayoutBox { Element = b, ComputedStyle = resolver.Resolve(b, [sheet]) };

        resolver.Resolve(c, [sheet]).Color.ShouldBe(Color.Blue);
    }

    [Fact]
    public void Var_RuleWithOnlyVarsDefinition_StillProduced()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            // 仅定义变量、无任何普通属性的规则也应产出（issue 的 .btn-icon 场景）。
            [".btn-icon"] = new()
            {
                Vars = new() { ["--button-color"] = (Color)"#FFFFFF" }
            }
        });

        sheet.Rules.Count.ShouldBe(1);
    }

    [Fact]
    public void Var_IssueExample_EndToEnd()
    {
        // 复现 ISSUE-082 的原样例：.btn 引用变量，.btn-icon 在自身作用域定义/覆盖。
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".btn"] = new()
            {
                Color = Var("--button-color"),
                Width = Length.Px(100),
            },
            [".btn-icon"] = new()
            {
                Vars = new() { ["--button-color"] = (Color)"#FFFFFF" }
            }
        });

        // .btn 且 .btn-icon 的元素：自身定义 --button-color=白，引用解析为白。
        var iconBtn = new DivElement { Class = "btn btn-icon" };
        var computed = Resolve(iconBtn, sheet);
        computed.Color.ShouldBe(Color.White);
        computed.Width.ShouldBe(Length.Px(100));

        // 仅 .btn 的元素：变量未定义 → Color 回退默认（黑）。
        var plainBtn = new DivElement { Class = "btn" };
        Resolve(plainBtn, sheet).Color.ShouldBe(Color.Black);
    }

    [Fact]
    public void Var_LocalDefinitionOverridesInheritedForSameElement()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".theme"] = new()
            {
                Vars = new() { ["--c"] = (Color)"#FFFFFF" }   // 祖先：白
            },
            [".btn"] = new()
            {
                Color = Var("--c"),
                Vars = new() { ["--c"] = (Color)"#FF0000" }   // 本元素覆盖：红
            }
        });

        var theme = new DivElement { Class = "theme" };
        var btn = new DivElement { Class = "btn" };
        theme.AddChild(btn);

        var resolver = new StyleResolver();
        theme.LayoutBox = new Miko.Layout.LayoutBox { Element = theme, ComputedStyle = resolver.Resolve(theme, [sheet]) };

        // 本元素自身定义优先于继承 → 红。
        resolver.Resolve(btn, [sheet]).Color.ShouldBe(Color.Red);
    }

    // ---- VarValue 联合体本身的类型往返（验证 Unsafe 位重解释的正确性） ----

    [Fact]
    public void VarValue_RoundTrips_AllValueTypes()
    {
        VarValue color = (Color)"#123456";
        color.TryGet<Color>(out var c).ShouldBeTrue();
        c.ShouldBe((Color)"#123456");

        VarValue length = Length.Rem(2.5f);
        length.TryGet<Length>(out var l).ShouldBeTrue();
        l.ShouldBe(Length.Rem(2.5f));

        VarValue f = 0.75f;
        f.TryGet<float>(out var fv).ShouldBeTrue();
        fv.ShouldBe(0.75f);

        VarValue i = 42;
        i.TryGet<int>(out var iv).ShouldBeTrue();
        iv.ShouldBe(42);

        VarValue s = "Segoe UI";
        s.TryGet<string>(out var sv).ShouldBeTrue();
        sv.ShouldBe("Segoe UI");
    }

    [Fact]
    public void VarValue_RoundTrips_Enum_ViaBitReinterpret()
    {
        var v = VarValue.FromEnum(JustifyContent.SpaceBetween);
        v.TryGet<JustifyContent>(out var jc).ShouldBeTrue();
        jc.ShouldBe(JustifyContent.SpaceBetween);

        var d = VarValue.FromEnum(Display.Flex);
        d.TryGet<Display>(out var display).ShouldBeTrue();
        display.ShouldBe(Display.Flex);
    }

    [Fact]
    public void VarValue_TypeMismatch_ReturnsFalse()
    {
        VarValue color = (Color)"#FFFFFF";
        color.TryGet<Length>(out _).ShouldBeFalse();
        color.TryGet<int>(out _).ShouldBeFalse();

        VarValue length = Length.Px(10);
        length.TryGet<Color>(out _).ShouldBeFalse();

        // 不同枚举类型精确校验：JustifyContent 不应被当作 Display 读取（零装箱下仍保持类型安全）。
        var jc = VarValue.FromEnum(JustifyContent.Center);
        jc.TryGet<Display>(out _).ShouldBeFalse();
        jc.TryGet<JustifyContent>(out _).ShouldBeTrue();
    }
}
