using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;
using static Miko.Styling.Css;

namespace Miko.Tests.Styling;

/// <summary>
/// calc() 表达式（含变量的延迟算术）的单元测试（ISSUE-090）。
/// 覆盖：issue 原样例、直接/Calc 包装/作用域 lambda 三种写法、各类运算符、回退值、
/// 未解析回退默认、类型不匹配、百分比等相对单位保留。
/// </summary>
public class CalcTests
{
    private static ComputedStyle Resolve(Element element, StyleSheet sheet)
        => new StyleResolver().Resolve(element, [sheet]);

    [Fact]
    public void Calc_IssueExample_NegativeVarBorderWidth_Resolves()
    {
        // .page-item { margin-left: calc(-1 * var(--bs-border-width)); }
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".page-item"] = new()
            {
                MarginLeft = -1 * Var("--bs-border-width"),
                Vars = new() { ["--bs-border-width"] = Length.Px(1) }
            }
        });

        var element = new DivElement { Class = "page-item" };
        Resolve(element, sheet).MarginLeft.ShouldBe(Length.Px(-1));
    }

    [Fact]
    public void Calc_DirectExpression_NoLambda_Resolves()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                MarginLeft = -1 * Var("--w"),
                Vars = new() { ["--w"] = Length.Px(4) }
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).MarginLeft.ShouldBe(Length.Px(-4));
    }

    [Fact]
    public void Calc_WrapperWithScopeLambda_FreeVar_Resolves()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                MarginLeft = Calc(s => -1 * Var("--w")),
                Vars = new() { ["--w"] = Length.Px(4) }
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).MarginLeft.ShouldBe(Length.Px(-4));
    }

    [Fact]
    public void Calc_WrapperWithScopeLambda_ScopeVar_Resolves()
    {
        // s.Var(...) 与自由 Var(...) 等价。
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                MarginLeft = Calc(s => -1 * s.Var("--w")),
                Vars = new() { ["--w"] = Length.Px(10) }
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).MarginLeft.ShouldBe(Length.Px(-10));
    }

    [Fact]
    public void Calc_WrapperPassthrough_Resolves()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                MarginLeft = Calc(-1 * Var("--w")),
                Vars = new() { ["--w"] = Length.Px(3) }
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).MarginLeft.ShouldBe(Length.Px(-3));
    }

    [Fact]
    public void Calc_Addition_TwoVars_Resolves()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                Width = Calc(s => s.Var("--x") + s.Var("--y")),
                Vars = new() { ["--x"] = Length.Px(4), ["--y"] = Length.Px(10) }
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).Width.ShouldBe(Length.Px(14));
    }

    [Fact]
    public void Calc_Division_Resolves()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                Width = Var("--x") / 2f,
                Vars = new() { ["--x"] = Length.Px(8) }
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).Width.ShouldBe(Length.Px(4));
    }

    [Fact]
    public void Calc_Multiplication_VarTimesFloat_Resolves()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                Width = Var("--x") * 3f,
                Vars = new() { ["--x"] = Length.Px(5) }
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).Width.ShouldBe(Length.Px(15));
    }

    [Fact]
    public void Calc_UnaryNegate_Resolves()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                MarginLeft = -Var("--x"),
                Vars = new() { ["--x"] = Length.Px(6) }
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).MarginLeft.ShouldBe(Length.Px(-6));
    }

    [Fact]
    public void Calc_Subtraction_VarMinusVar_Resolves()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                Width = Var("--x") - Var("--y"),
                Vars = new() { ["--x"] = Length.Px(10), ["--y"] = Length.Px(3) }
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).Width.ShouldBe(Length.Px(7));
    }

    [Fact]
    public void Calc_VarPlusLiteralLength_Resolves()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                Width = Var("--x") + Length.Px(2),
                Vars = new() { ["--x"] = Length.Px(8) }
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).Width.ShouldBe(Length.Px(10));
    }

    [Fact]
    public void Calc_VarWithFallback_UsesFallbackWhenMissing()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                // --missing 未定义，calc 内变量走回退值 7px。
                MarginLeft = -1 * Var("--missing", Length.Px(7))
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).MarginLeft.ShouldBe(Length.Px(-7));
    }

    [Fact]
    public void Calc_MissingVar_NoFallback_KeepsDefault()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                MarginLeft = -1 * Var("--missing")
            }
        });

        var element = new DivElement { Class = "box" };
        // 未解析 → 属性视为未设置 → 保留 ComputedStyle 的 MarginLeft 默认（0px）。
        Resolve(element, sheet).MarginLeft.ShouldBe(Length.Px(0));
    }

    [Fact]
    public void Calc_WrongTypeVar_KeepsDefault_NoCrash()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                // --c 是颜色，不能作为长度参与 calc → 未解析 → 保留默认（0px）。
                MarginLeft = -1 * Var("--c"),
                Vars = new() { ["--c"] = (Color)"#FF0000" }
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).MarginLeft.ShouldBe(Length.Px(0));
    }

    [Fact]
    public void Calc_PreservesPercentComponent()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                // 百分比在 calc 中不提前折算，一路保留到布局阶段。
                Width = -1 * Var("--x"),
                Vars = new() { ["--x"] = Length.Percent(50) }
            }
        });

        var element = new DivElement { Class = "box" };
        Resolve(element, sheet).Width.ShouldBe(Length.Percent(-50));
    }
}
