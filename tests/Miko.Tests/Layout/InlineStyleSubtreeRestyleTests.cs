using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Diagnostics;
using Miko.Hosting;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-146：替换一个元素的行内样式只重算<b>它这棵子树</b>的计算样式。
///
/// <para>选择器读不到行内样式，所以别的元素的级联结果不可能因此改变；受影响的只有该元素自身与
/// 它的后代（可继承属性、em、变量作用域）。现场：详情页播放器首帧按宽度写入 16:9 高度
/// （<c>ChangeStyle(_viewport, …)</c>），导航那一帧因此把 340 个元素的整树级联跑了两遍。</para>
///
/// <para>风险同样全在「漏算」：后代必须拿到新的继承值；兄弟与祖先的样式对象必须原样保留
/// ——保留的是同一个实例，这是「没有重算」唯一可靠的观测方式。</para>
/// </summary>
[Collection(Miko.Tests.FrameTimingCollection.Name)]
public class InlineStyleSubtreeRestyleTests : IDisposable
{
    private readonly SKBitmap _bitmap = new(300, 300);
    private readonly SKCanvas _canvas;

    public InlineStyleSubtreeRestyleTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private sealed record Tree(MikoEngine Engine, DivElement Root, DivElement Target, DivElement Child, DivElement Grandchild, DivElement Sibling);

    private Tree Create(StyleSheet? sheet = null)
    {
        var grandchild = new DivElement { Class = "grandchild", TextContent = "g" };
        var child = new DivElement { Class = "child", Children = { grandchild } };
        var target = new DivElement { Class = "target", Children = { child } };
        var sibling = new DivElement { Class = "sibling", TextContent = "s" };
        var root = new DivElement { Class = "root", Children = { target, sibling } };

        sheet ??= new StyleSheet();
        sheet.AddRule(new ClassSelector("root"), new Style { Color = Color.FromRgb(1, 1, 1), FontSize = Length.Px(10) });
        sheet.AddRule(new DescendantSelector(new ClassSelector("root"), new ClassSelector("grandchild")),
            new Style { PaddingLeft = Length.Em(2) });

        var engine = new MikoEngineBuilder().Build();
        engine.Initialize(root, new List<StyleSheet> { sheet }, _canvas, 300, 300);
        return new Tree(engine, root, target, child, grandchild, sibling);
    }

    private long CountResolved(MikoEngine engine)
    {
        LayoutAllocationDiagnostics.Begin();
        engine.Render(_canvas);
        var alloc = LayoutAllocationDiagnostics.End();
        return alloc.ComputedStyleRentNew + alloc.ComputedStyleRentReused;
    }

    [Fact]
    public void InlineStyleChange_RestylesOnlyTheSubtree()
    {
        var tree = Create();
        var rootStyle = tree.Root.LayoutBox!.ComputedStyle;
        var siblingStyle = tree.Sibling.LayoutBox!.ComputedStyle;

        tree.Target.Style = new Style { Height = Length.Px(120) };
        long resolved = CountResolved(tree.Engine);

        // target + child + grandchild + grandchild 的文本节点 = 4；整树是 7（root/sibling 及其文本节点）。
        resolved.ShouldBe(4);
        tree.Root.LayoutBox!.ComputedStyle.ShouldBeSameAs(rootStyle);
        tree.Sibling.LayoutBox!.ComputedStyle.ShouldBeSameAs(siblingStyle);
        tree.Target.LayoutBox!.ComputedStyle.Height.Value.ShouldBe(120f);
        tree.Target.LayoutBox.BoxModel.BorderBox.Height.ShouldBe(120f);
    }

    [Fact]
    public void InheritedValues_ReachEveryDescendant()
    {
        var tree = Create();

        tree.Target.Style = new Style { Color = Color.FromRgb(200, 10, 10), FontSize = Length.Px(20) };
        tree.Engine.Render(_canvas);

        tree.Child.LayoutBox!.ComputedStyle.Color.ShouldBe(Color.FromRgb(200, 10, 10));
        tree.Grandchild.LayoutBox!.ComputedStyle.Color.ShouldBe(Color.FromRgb(200, 10, 10));
        // em 按继承来的字号解析：padding-left: 2em = 40px。
        tree.Grandchild.LayoutBox.ComputedStyle.PaddingLeft.ToPixels(0, tree.Grandchild.LayoutBox.ComputedStyle.FontSize.Value)
            .ShouldBe(40f);
        tree.Grandchild.Children.OfType<TextNode>().Single().LayoutBox!.ComputedStyle.Color.ShouldBe(Color.FromRgb(200, 10, 10));
        tree.Sibling.LayoutBox!.ComputedStyle.Color.ShouldBe(Color.FromRgb(1, 1, 1));
    }

    [Fact]
    public void DescendantCombinatorRules_StillMatchInsideTheRestyledSubtree()
    {
        // 子树重算要带着正确的祖先链去匹配后代组合器（.root .grandchild 的祖先在子树之外）。
        var tree = Create();

        tree.Target.Style = new Style { Width = Length.Px(200) };
        tree.Engine.Render(_canvas);

        tree.Grandchild.LayoutBox!.ComputedStyle.PaddingLeft.IsAuto.ShouldBeFalse();
        tree.Grandchild.LayoutBox.ComputedStyle.PaddingLeft.ToPixels(0, 10).ShouldBe(20f);
    }

    [Fact]
    public void InlineStyleChangeTogetherWithClassChange_RestylesEverything()
    {
        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("hot"), new Style { BackgroundColor = Color.FromRgb(9, 9, 9) });
        var tree = Create(sheet);

        tree.Target.Style = new Style { Height = Length.Px(50) };
        tree.Sibling.Class = "sibling hot";
        tree.Engine.Render(_canvas);

        tree.Sibling.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.FromRgb(9, 9, 9));
        tree.Target.LayoutBox!.ComputedStyle.Height.Value.ShouldBe(50f);
    }

    [Fact]
    public void SelectorThatMayReadInlineStyle_DisablesTheShortcut()
    {
        // 任意谓词选择器可能读取行内样式：表里出现它时，行内样式变更必须整树重算。
        var sheet = new StyleSheet();
#pragma warning disable CS0618
        sheet.AddRule(Style.For<DivElement>()
            .Where(d => d.Parent != null && d.Parent.Style != null && d.Parent.Style.Height != null)
            .Set(s => s.BackgroundColor, Color.FromRgb(0, 99, 0)));
#pragma warning restore CS0618
        var tree = Create(sheet);
        var siblingStyle = tree.Sibling.LayoutBox!.ComputedStyle;

        tree.Root.Style = new Style { Height = Length.Px(250) };
        tree.Engine.Render(_canvas);

        // root 的行内样式变了，谓词让它的<b>子元素</b>（sibling 在 root 子树内——这里的重点是
        // 快路径被关闭后仍然正确）拿到新背景色。
        tree.Sibling.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.FromRgb(0, 99, 0));
        tree.Sibling.LayoutBox.ComputedStyle.ShouldNotBeSameAs(siblingStyle);
    }

    [Fact]
    public void RepeatedInlineStyleChanges_OnDetachedElement_DoNotBreakTheNextFrame()
    {
        // 登记过行内样式变更的元素随后被移出 DOM：它不在树上，子树重算找不到它，
        // 帧必须照常完成，且留在树上的元素样式不受影响。
        var tree = Create();

        tree.Sibling.Style = new Style { Height = Length.Px(10) };
        tree.Root.RemoveChild(tree.Sibling);
        tree.Engine.Render(_canvas);

        tree.Root.Children.ShouldNotContain(tree.Sibling);
        tree.Target.LayoutBox!.ComputedStyle.Color.ShouldBe(Color.FromRgb(1, 1, 1));
    }
}
