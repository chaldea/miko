using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using static Miko.Common.Length;

namespace Miko.Tests.Layout;

/// <summary>
/// 视窗单位 vw/vh 在 <see cref="LayoutEngine.Layout"/> 中的端到端测试（ISSUE-091）。
/// vw/vh 相对整个视口解析（1vw = 视口宽度的 1%、1vh = 视口高度的 1%），在样式计算阶段折算成 px，
/// 因此可直接用于尺寸/间距，并参与 calc（如 <c>calc(100vw - 240px)</c>）。
/// </summary>
public class ViewportUnitsLayoutTests
{
    private const float ViewportW = 1280f;
    private const float ViewportH = 720f;

    private readonly LayoutEngine _layoutEngine = new();

    private static LayoutBox? FindByClass(LayoutBox box, string cls)
    {
        if (box.Element.Class == cls) return box;
        foreach (var child in box.Children)
        {
            var found = FindByClass(child, cls);
            if (found != null) return found;
        }
        return null;
    }

    private static StyleSheet Sheet(string cls, Style style) => new()
    {
        Rules = new List<StyleRule>
        {
            new() { Selector = new ClassSelector(cls), Style = style }
        }
    };

    [Fact]
    public void Width100Vw_Height100Vh_FillViewport()
    {
        // width: 100vw; height: 100vh;
        var root = new DivElement { Class = "box" };
        var sheet = Sheet("box", new Style
        {
            Display = Display.Block,
            Width = Length.Vw(100),
            Height = Length.Vh(100),
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        layout.BoxModel.Content.Width.ShouldBe(ViewportW);
        layout.BoxModel.Content.Height.ShouldBe(ViewportH);
    }

    [Fact]
    public void HalfViewport_ResolvesAgainstEachAxis()
    {
        // width: 50vw; height: 25vh;
        var root = new DivElement { Class = "box" };
        var sheet = Sheet("box", new Style
        {
            Display = Display.Block,
            Width = Length.Vw(50),
            Height = Length.Vh(25),
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        layout.BoxModel.Content.Width.ShouldBe(ViewportW * 0.5f);   // 640
        layout.BoxModel.Content.Height.ShouldBe(ViewportH * 0.25f); // 180
    }

    [Fact]
    public void CalcVwMinusPx_IssueExample()
    {
        // width: calc(100vw - 240px);  → 1280 - 240 = 1040
        var root = new DivElement { Class = "box" };
        var sheet = Sheet("box", new Style
        {
            Display = Display.Block,
            Width = Length.Vw(100) - Length.Px(240),
            Height = Length.Px(100),
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        layout.BoxModel.Content.Width.ShouldBe(1040f);
    }

    [Fact]
    public void VwWorksForPadding()
    {
        // padding-left: 10vw → 128px（相对视口宽度，与包含块无关）。
        var root = new DivElement { Class = "box" };
        var sheet = Sheet("box", new Style
        {
            Display = Display.Block,
            BoxSizing = BoxSizing.BorderBox,
            Width = Length.Px(400),
            Height = Length.Px(100),
            PaddingLeft = Length.Vw(10),
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        // border-box 下内容左内边距 = 128，内容左边界随之右移。
        layout.BoxModel.Content.Left.ShouldBe(128f);
    }

    [Fact]
    public void VwCssHelper_EquivalentToLengthVw()
    {
        // 通过 Css.Vw(...) 辅助方法书写，等价于 Length.Vw(...)。
        var root = new DivElement { Class = "box" };
        var sheet = Sheet("box", new Style
        {
            Display = Display.Block,
            Width = Vw(100) - Length.Px(240),
            Height = Vh(50),
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        layout.BoxModel.Content.Width.ShouldBe(1040f);
        layout.BoxModel.Content.Height.ShouldBe(ViewportH * 0.5f); // 360
    }

    [Fact]
    public void FontSizeVw_ResolvesAgainstViewportWidth()
    {
        // font-size: 5vw → 64px（1280 的 5%）。验证 font-size 的 vw 先于 em 折算。
        var root = new DivElement { Class = "box" };
        var sheet = Sheet("box", new Style
        {
            Display = Display.Block,
            FontSize = Length.Vw(5),
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        layout.ComputedStyle.FontSize.ToPixels(0).ShouldBe(64f);
    }

    [Fact]
    public void EmResolvesAgainstVwFontSize()
    {
        // 父 font-size: 5vw (=64px)，子 width: 2em → 相对父字体大小 = 128px。
        // 验证父 font-size 的 vw 已折算成 px，供子元素 em 正确解析。
        var parent = new DivElement { Class = "parent" };
        var child = new DivElement { Class = "child" };
        parent.AddChild(child);

        var sheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new() { Selector = new ClassSelector("parent"),
                        Style = new Style { Display = Display.Block, FontSize = Length.Vw(5) } },
                new() { Selector = new ClassSelector("child"),
                        Style = new Style { Display = Display.Block, Width = Length.Em(2) } },
            }
        };

        var layout = _layoutEngine.Layout(parent, new List<StyleSheet> { sheet }, ViewportW, ViewportH);
        var childBox = FindByClass(layout, "child");

        childBox.ShouldNotBeNull();
        childBox!.BoxModel.Content.Width.ShouldBe(128f); // 2 * 64
    }
}
