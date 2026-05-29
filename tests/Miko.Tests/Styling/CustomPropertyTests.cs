using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using static Miko.Styling.StyleVar;

namespace Miko.Tests.Styling;

public class CustomPropertyTests
{
    [Fact]
    public void VarBinding_ShouldResolveFromParentCustomProperty()
    {
        var resolver = new StyleResolver();

        var parent = new DivElement { Class = "accordion" };
        var child = new DivElement { Class = "accordion-button" };
        parent.AddChild(child);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".accordion"] = new()
            {
                CustomProperties = new()
                {
                    ["--btn-color"] = Color.Red,
                }
            },
            [".accordion-button"] = new()
            {
                Color = Var("--btn-color"),
            }
        });

        var parentComputed = resolver.Resolve(parent, [sheet]);
        parent.LayoutBox = new LayoutBox { Element = parent, ComputedStyle = parentComputed };

        var childComputed = resolver.Resolve(child, [sheet], parentScope: parentComputed.CustomPropertyScope);
        childComputed.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void VarBinding_ShouldResolveFromOwnCustomProperty()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "self" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".self"] = new()
            {
                CustomProperties = new()
                {
                    ["--bg"] = Color.Blue,
                },
                BackgroundColor = Var("--bg"),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.BackgroundColor.ShouldBe(Color.Blue);
    }

    [Fact]
    public void VarBinding_DescendantShouldOverrideAncestorVariable()
    {
        var resolver = new StyleResolver();

        var grandparent = new DivElement { Class = "theme-light" };
        var parent = new DivElement { Class = "theme-dark" };
        var child = new DivElement { Class = "btn" };
        grandparent.AddChild(parent);
        parent.AddChild(child);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".theme-light"] = new()
            {
                CustomProperties = new() { ["--text-color"] = Color.Black }
            },
            [".theme-dark"] = new()
            {
                CustomProperties = new() { ["--text-color"] = Color.White }
            },
            [".btn"] = new()
            {
                Color = Var("--text-color"),
            }
        });

        var gpComputed = resolver.Resolve(grandparent, [sheet]);
        grandparent.LayoutBox = new LayoutBox { Element = grandparent, ComputedStyle = gpComputed };

        var parentComputed = resolver.Resolve(parent, [sheet], parentScope: gpComputed.CustomPropertyScope);
        parent.LayoutBox = new LayoutBox { Element = parent, ComputedStyle = parentComputed };

        var childComputed = resolver.Resolve(child, [sheet], parentScope: parentComputed.CustomPropertyScope);
        childComputed.Color.ShouldBe(Color.White);
    }

    [Fact]
    public void VarBinding_ShouldUseFallbackWhenVariableNotDefined()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "fallback" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".fallback"] = new()
            {
                BackgroundColor = new VarReference("--undefined-var", Color.Green),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.BackgroundColor.ShouldBe(Color.Green);
    }

    [Fact]
    public void VarBinding_ShouldNotOverrideConcreteValueFromHigherSpecificity()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Id = "specific", Class = "general" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".general"] = new()
            {
                CustomProperties = new() { ["--color"] = Color.Red },
                Color = Var("--color"),
            },
            ["#specific"] = new()
            {
                Color = Color.Blue,
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.Color.ShouldBe(Color.Blue);
    }

    [Fact]
    public void VarBinding_ShouldWorkWithLengthType()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "sized" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".sized"] = new()
            {
                CustomProperties = new()
                {
                    ["--spacing"] = Length.Px(20),
                },
                PaddingTop = Var("--spacing"),
                PaddingBottom = Var("--spacing"),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.PaddingTop.Value.ShouldBe(20);
        computed.PaddingBottom.Value.ShouldBe(20);
    }

    [Fact]
    public void VarBinding_TypeMismatchShouldBeIgnored()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "mismatch" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".mismatch"] = new()
            {
                CustomProperties = new()
                {
                    ["--a-color"] = Color.Red,
                },
                PaddingTop = Var("--a-color"),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.PaddingTop.Value.ShouldBe(0);
    }

    [Fact]
    public void VarBinding_InlineStyleShouldOverrideVarBinding()
    {
        var resolver = new StyleResolver();
        var element = new DivElement
        {
            Class = "var-test",
            Style = new Style { Color = Color.Yellow }
        };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".var-test"] = new()
            {
                CustomProperties = new() { ["--color"] = Color.Red },
                Color = Var("--color"),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.Color.ShouldBe(Color.Yellow);
    }

    [Fact]
    public void CustomPropertyScope_ShouldChainCorrectly()
    {
        var root = new CustomPropertyScope();
        root.Set("--a", Color.Red);
        root.Set("--b", Color.Blue);

        var child = root.CreateChild();
        child.Set("--a", Color.Green);

        root.Get<Color>("--a").ShouldBe(Color.Red);
        root.Get<Color>("--b").ShouldBe(Color.Blue);
        child.Get<Color>("--a").ShouldBe(Color.Green);
        child.Get<Color>("--b").ShouldBe(Color.Blue);
        child.Get<Color>("--c").ShouldBeNull();
    }

    [Fact]
    public void VarBinding_ShouldWorkThroughLayoutEngine()
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
                CustomProperties = new()
                {
                    ["--item-bg"] = Color.Red,
                    ["--item-padding"] = Length.Px(10),
                }
            },
            [".item"] = new()
            {
                BackgroundColor = Var("--item-bg"),
                PaddingTop = Var("--item-padding"),
            }
        });

        var engine = new LayoutEngine();
        engine.Layout(parent, [sheet], 800, 600);

        child.LayoutBox.ShouldNotBeNull();
        child.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.Red);
        child.LayoutBox.ComputedStyle.PaddingTop.Value.ShouldBe(10);
    }

    [Fact]
    public void VarBinding_NoVariablesUsed_ShouldHaveZeroOverhead()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "plain" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".plain"] = new()
            {
                Color = Color.Red,
                Width = Length.Px(100),
            }
        });

        var computed = resolver.Resolve(element, [sheet]);
        computed.Color.ShouldBe(Color.Red);
        computed.Width.Value.ShouldBe(100);
    }

    [Fact]
    public void StyleProperty_ImplicitConversion_ShouldWork()
    {
        var style = new Style();

        style.Color = Color.Red;
        style.Display = Display.Flex;
        style.Width = Length.Px(100);
        style.Opacity = 0.5f;

        style.Color.ShouldNotBeNull();
        style.Color!.Value.TryGetValue(out var color).ShouldBeTrue();
        color.ShouldBe(Color.Red);

        style.Display!.Value.TryGetValue(out var display).ShouldBeTrue();
        display.ShouldBe(Display.Flex);
    }

    [Fact]
    public void StyleProperty_VarReference_ShouldWork()
    {
        var style = new Style();

        style.Color = Var("--my-color");
        style.Width = Var("--my-width");

        style.Color.ShouldNotBeNull();
        style.Color!.Value.IsVar.ShouldBeTrue();
        style.Color!.Value.Var.Name.ShouldBe("--my-color");

        style.Width!.Value.IsVar.ShouldBeTrue();
        style.Width!.Value.Var.Name.ShouldBe("--my-width");
    }
}
