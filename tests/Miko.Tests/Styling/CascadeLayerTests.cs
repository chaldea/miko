using Miko.Common;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Styling;

/// <summary>
/// <see cref="StyleSheet.Layer"/> 级联层语义（ISSUE-107）：层级高的样式表中的规则恒胜于
/// 层级低的规则，与选择器特异性、加载顺序无关；同层内仍按"特异性 → 定义顺序"裁决。
/// 对应浏览器中外层文档规则恒胜于组件 shadow 树 <c>:host</c> 规则的语义（CSS Scoping）。
/// </summary>
public class CascadeLayerTests
{
    private static StyleSheet Sheet(int layer, string selector, float width)
    {
        var sheet = new StyleSheet { Layer = layer };
        sheet.Add(new CssObject { [selector] = new() { Width = Length.Px(width) } });
        return sheet;
    }

    [Fact]
    public void HigherLayer_ShouldOverrideLowerLayer_RegardlessOfSpecificity()
    {
        // 组件层（低层）的复合类选择器特异性更高，但应用层（默认 0 层）的规则必须获胜。
        var element = new DivElement { Class = "icon fancy" };
        var componentSheet = Sheet(-1, ".icon.fancy", 10);   // 特异性 20
        var appSheet = Sheet(0, ".icon", 50);                // 特异性 10

        var computed = new StyleResolver().Resolve(element, [componentSheet, appSheet]);

        computed.Width.Value.ShouldBe(50);
    }

    [Fact]
    public void HigherLayer_ShouldWin_EvenWhenDeclaredFirst()
    {
        // 层裁决与样式表加载顺序无关。
        var element = new DivElement { Class = "icon fancy" };
        var appSheet = Sheet(0, ".icon", 50);
        var componentSheet = Sheet(-1, ".icon.fancy", 10);

        var computed = new StyleResolver().Resolve(element, [appSheet, componentSheet]);

        computed.Width.Value.ShouldBe(50);
    }

    [Fact]
    public void LowerLayerRule_ShouldStillApply_WhenNoHigherLayerCompetes()
    {
        // 高层规则未定义的属性仍由低层规则提供。
        var element = new DivElement { Class = "icon fancy" };
        var componentSheet = Sheet(-1, ".icon.fancy", 10);
        var appSheet = new StyleSheet();
        appSheet.Add(new CssObject { [".icon"] = new() { BackgroundColor = Color.Red } });

        var computed = new StyleResolver().Resolve(element, [componentSheet, appSheet]);

        computed.Width.Value.ShouldBe(10);
        computed.BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void SameLayer_ShouldResolveBySpecificity()
    {
        var element = new DivElement { Class = "icon fancy" };
        var first = Sheet(0, ".icon", 50);
        var second = Sheet(0, ".icon.fancy", 10);

        var computed = new StyleResolver().Resolve(element, [first, second]);

        computed.Width.Value.ShouldBe(10);
    }

    [Fact]
    public void SameLayer_ShouldTieBreakByDeclarationOrder()
    {
        var element = new DivElement { Class = "icon" };
        var first = Sheet(0, ".icon", 50);
        var second = Sheet(0, ".icon", 10);

        var computed = new StyleResolver().Resolve(element, [first, second]);

        computed.Width.Value.ShouldBe(10);
    }

    [Fact]
    public void InlineStyle_ShouldBeatAllLayers()
    {
        var element = new DivElement
        {
            Class = "icon fancy",
            Style = new Style { Width = Length.Px(70) }
        };
        var componentSheet = Sheet(-1, ".icon.fancy", 10);
        var appSheet = Sheet(1, ".icon", 50);

        var computed = new StyleResolver().Resolve(element, [componentSheet, appSheet]);

        computed.Width.Value.ShouldBe(70);
    }
}
