using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using static Miko.Styling.StyleVar;

namespace Miko.Tests.Styling;

public class CalcExpressionTests
{
    /// <summary>
    /// ISSUE-038 主用例：calc(-1 * var(--bs-border-width))
    /// </summary>
    [Fact]
    public void Calc_NegativeTimesVar_ShouldResolve()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "page-item" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".page-item"] = new()
            {
                CustomProperties = new() { ["--bs-border-width"] = Length.Px(1) },
                MarginLeft = Calc(s => -1 * Var("--bs-border-width")),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.MarginLeft.Value.ShouldBe(-1);
    }

    [Fact]
    public void Calc_VarPlusConstant_ShouldResolve()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "box" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                CustomProperties = new() { ["--gap"] = Length.Px(8) },
                PaddingTop = Calc(s => Var("--gap") + 4),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.PaddingTop.Value.ShouldBe(12);
    }

    [Fact]
    public void Calc_VarMinusVar_ShouldResolve()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "box" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                CustomProperties = new()
                {
                    ["--outer"] = Length.Px(20),
                    ["--inner"] = Length.Px(6),
                },
                MarginTop = Calc(s => Var("--outer") - Var("--inner")),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.MarginTop.Value.ShouldBe(14);
    }

    [Fact]
    public void Calc_DivideAndMultiply_ShouldResolve()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "box" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                CustomProperties = new() { ["--w"] = Length.Px(100) },
                Width = Calc(s => Var("--w") / 2 * 3),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.Width.Value.ShouldBe(150);
    }

    [Fact]
    public void Calc_ExplicitWrapperAndImplicitDelegate_ShouldBeEquivalent()
    {
        var resolver = new StyleResolver();

        // 显式 Calc(...) 包装
        var explicitEl = new DivElement { Class = "a" };
        var sheetA = new StyleSheet();
        sheetA.Add(new CssObject
        {
            [".a"] = new()
            {
                CustomProperties = new() { ["--x"] = Length.Px(10) },
                MarginLeft = Calc(s => 2 * Var("--x")),
            }
        });

        // 通过显式委托变量赋值，触发隐式转换 operator
        Func<CustomPropertyScope, CalcExpr> expr = s => 2 * Var("--x");
        var implicitEl = new DivElement { Class = "b" };
        var sheetB = new StyleSheet();
        sheetB.Add(new CssObject
        {
            [".b"] = new()
            {
                CustomProperties = new() { ["--x"] = Length.Px(10) },
                MarginLeft = expr,
            }
        });

        resolver.Resolve(explicitEl, [sheetA]).MarginLeft.Value.ShouldBe(20);
        resolver.Resolve(implicitEl, [sheetB]).MarginLeft.Value.ShouldBe(20);
    }

    [Fact]
    public void Calc_UsesScopeParameter_ShouldResolve()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "box" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                CustomProperties = new() { ["--base"] = Length.Px(16) },
                // 通过 scope 参数的 Var 方法取值（s.Var 返回 CalcExpr）
                PaddingLeft = Calc(s => s.Var("--base") + 4),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.PaddingLeft.Value.ShouldBe(20);
    }

    [Fact]
    public void Calc_FloatProperty_ShouldResolve()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "box" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                CustomProperties = new() { ["--o"] = 1.0f },
                Opacity = Calc(s => Var("--o") * 0.5f),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.Opacity.ShouldBe(0.5f);
    }

    [Fact]
    public void Calc_IntProperty_ShouldRoundToNearest()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "box" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                CustomProperties = new() { ["--z"] = 5 },
                ZIndex = Calc(s => Var("--z") * 2 + 1),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.ZIndex.ShouldBe(11);
    }

    [Fact]
    public void Calc_MissingVariable_ShouldUseZero()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "box" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                // --missing 未定义，应取 0
                MarginLeft = Calc(s => Var("--missing") + 5),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.MarginLeft.Value.ShouldBe(5);
    }

    [Fact]
    public void Calc_MissingVariable_ShouldUseFallback()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "box" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                MarginLeft = Calc(s => -1 * (CalcExpr)new VarReference("--missing", Length.Px(4))),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.MarginLeft.Value.ShouldBe(-4);
    }

    [Fact]
    public void Calc_PreservesNonPixelUnit()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "box" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                CustomProperties = new() { ["--pct"] = Length.Percent(50) },
                Width = Calc(s => 2 * Var("--pct")),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.Width.Value.ShouldBe(100);
        computed.Width.Unit.ShouldBe(LengthUnit.Percent);
    }

    [Fact]
    public void Calc_WorksThroughLayoutEngine()
    {
        var parent = new DivElement { Class = "container" };
        var child = new DivElement { Class = "item" };
        parent.AddChild(child);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".container"] = new()
            {
                Display = Display.Block,
                Width = Length.Px(500),
                CustomProperties = new() { ["--item-padding"] = Length.Px(10) },
            },
            [".item"] = new()
            {
                PaddingTop = Calc(s => Var("--item-padding") * 2),
            }
        });

        var engine = new LayoutEngine();
        engine.Layout(parent, [sheet], 800, 600);

        child.LayoutBox.ShouldNotBeNull();
        child.LayoutBox!.ComputedStyle.PaddingTop.Value.ShouldBe(20);
    }

    [Fact]
    public void Calc_UnaryNegation_ShouldResolve()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "box" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                CustomProperties = new() { ["--x"] = Length.Px(7) },
                MarginTop = Calc(s => -Var("--x")),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.MarginTop.Value.ShouldBe(-7);
    }
}
