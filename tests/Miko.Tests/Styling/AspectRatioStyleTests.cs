using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;
using static Miko.Styling.Css;

namespace Miko.Tests.Styling;

/// <summary>
/// ISSUE-150：<c>aspect-ratio</c> 的级联与计算值——初始值 auto、不可继承、支持全局关键词与 float 变量。
/// </summary>
public class AspectRatioStyleTests
{
    private static ComputedStyle Resolve(Element element, StyleSheet sheet)
        => new StyleResolver().Resolve(element, [sheet]);

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
    public void Should_default_to_auto()
    {
        Resolve(new DivElement(), new StyleSheet()).AspectRatio.ShouldBe(AspectRatio.Auto);
    }

    [Fact]
    public void Should_cascade_from_stylesheet_by_specificity()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".card"] = new() { AspectRatio = new AspectRatio(4, 3) },
            ["div"] = new() { AspectRatio = new AspectRatio(1, 1) },
        });

        Resolve(new DivElement { Class = "card" }, sheet).AspectRatio.ShouldBe(new AspectRatio(4, 3));
    }

    [Fact]
    public void Should_let_inline_style_override_stylesheet()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject { [".card"] = new() { AspectRatio = new AspectRatio(4, 3) } });

        var element = new DivElement { Class = "card", Style = new Style { AspectRatio = new AspectRatio(16, 9) } };

        Resolve(element, sheet).AspectRatio.ShouldBe(new AspectRatio(16, 9));
    }

    [Fact]
    public void Should_not_be_inherited()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject { [".frame"] = new() { AspectRatio = new AspectRatio(16, 9) } });

        var parent = ParentWith(sheet, "frame");
        var child = new DivElement();
        parent.AddChild(child);

        Resolve(child, sheet).AspectRatio.ShouldBe(AspectRatio.Auto);
    }

    [Fact]
    public void Should_take_parent_value_with_inherit_keyword()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".frame"] = new() { AspectRatio = new AspectRatio(16, 9) },
            [".inner"] = new() { AspectRatio = Inherit },
        });

        var parent = ParentWith(sheet, "frame");
        var child = new DivElement { Class = "inner" };
        parent.AddChild(child);

        Resolve(child, sheet).AspectRatio.ShouldBe(new AspectRatio(16, 9));
    }

    [Fact]
    public void Should_reset_to_auto_with_initial_keyword()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { AspectRatio = new AspectRatio(1, 1) },
            [".free"] = new() { AspectRatio = Initial },
        });

        Resolve(new DivElement { Class = "free" }, sheet).AspectRatio.ShouldBe(AspectRatio.Auto);
    }

    [Fact]
    public void Should_resolve_float_variable_as_single_number_ratio()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".media"] = new()
            {
                Vars = new() { ["--ratio"] = 1.5f },
                AspectRatio = Var("--ratio"),
            },
        });

        var computed = Resolve(new DivElement { Class = "media" }, sheet);

        computed.AspectRatio.HasRatio.ShouldBeTrue();
        computed.AspectRatio.Value.ShouldBe(1.5f, 0.0001f);
    }

    [Fact]
    public void Should_stay_auto_when_variable_has_incompatible_type()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".media"] = new()
            {
                Vars = new() { ["--ratio"] = Color.Red },
                AspectRatio = Var("--ratio"),
            },
        });

        Resolve(new DivElement { Class = "media" }, sheet).AspectRatio.ShouldBe(AspectRatio.Auto);
    }

    [Fact]
    public void Should_reset_pooled_computed_style_to_auto()
    {
        // 池化复用的 ComputedStyle 必须复位为初始值：上一个元素的比值不得渗进下一个元素（ISSUE-132）。
        var sheet = new StyleSheet();
        sheet.Add(new CssObject { [".media"] = new() { AspectRatio = new AspectRatio(16, 9) } });
        var first = Resolve(new DivElement { Class = "media" }, sheet);
        first.AspectRatio.HasRatio.ShouldBeTrue();
        ComputedStyle.Return(first);

        Resolve(new DivElement(), sheet).AspectRatio.ShouldBe(AspectRatio.Auto);
    }
}
