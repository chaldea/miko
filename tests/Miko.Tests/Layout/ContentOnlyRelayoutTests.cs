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
/// ISSUE-146：只改内容、不改样式的变更（已有文本节点的文字、图片/视频内禀尺寸）不再触发
/// 整树级联，只重排。
///
/// <para>现场：Anime 详情页的播放器每秒刷新一次时间文字（<c>0:01 / 0:03</c>）。那一次
/// <c>TextContent</c> 赋值曾被当成结构变更，让 340 个元素的整树级联每秒在 Android 模拟器上
/// 重跑一次（样式阶段 65–69 ms），恰好落在用户正在看的页面上。</para>
///
/// <para>这类优化的风险全在「漏算」——沿用了一个其实已经变了的计算样式。所以这里一半的用例
/// 在测「该算的时候必须算」：<c>:empty</c> 翻转、属性选择器读文字、结构/类名变化。</para>
/// </summary>
[Collection(Miko.Tests.FrameTimingCollection.Name)] // 用分段探针数布局/样式遍数，需串行
public class ContentOnlyRelayoutTests : IDisposable
{
    private readonly SKBitmap _bitmap = new(300, 200);
    private readonly SKCanvas _canvas;

    public ContentOnlyRelayoutTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private (MikoEngine engine, DivElement label) CreateEngine(StyleSheet sheet, string text = "0:01 / 0:03")
    {
        var label = new DivElement { Class = "time", TextContent = text };
        var root = new DivElement
        {
            Class = "player",
            Children = { label, new DivElement { Class = "controls" } }
        };
        var engine = new MikoEngineBuilder().Build();
        engine.Initialize(root, new List<StyleSheet> { sheet }, _canvas, 300, 200);
        return (engine, label);
    }

    private static StyleSheet PlayerSheet()
    {
        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("player"), new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column });
        sheet.AddRule(new DescendantSelector(new ClassSelector("player"), new ClassSelector("time")),
            new Style { Color = Color.FromRgb(10, 20, 30), FontSize = Length.Px(14) });
        return sheet;
    }

    /// <summary>跑一帧，返回（样式阶段是否真的跑了、布局是否真的跑了）。</summary>
    private (bool styled, bool laidOut) RenderAndMeasure(MikoEngine engine)
    {
        FrameTimingDiagnostics.Begin();
        LayoutAllocationDiagnostics.Begin();
        engine.Render(_canvas);
        var alloc = LayoutAllocationDiagnostics.End();
        var stages = FrameTimingDiagnostics.End();
        long resolved = alloc.ComputedStyleRentNew + alloc.ComputedStyleRentReused;
        return (resolved > 0, stages.LayoutCount > 0);
    }

    [Fact]
    public void ChangingExistingText_RelayoutsWithoutRestyling()
    {
        var (engine, label) = CreateEngine(PlayerSheet());
        var styleBefore = label.LayoutBox!.ComputedStyle;

        label.TextContent = "0:02 / 0:03";
        var (styled, laidOut) = RenderAndMeasure(engine);

        laidOut.ShouldBeTrue("text is a layout input; the frame must relayout");
        styled.ShouldBeFalse("only text changed; no element's computed style can differ");
        label.LayoutBox!.ComputedStyle.ShouldBeSameAs(styleBefore);
        label.LayoutBox.ComputedStyle.Color.ShouldBe(Color.FromRgb(10, 20, 30));
    }

    [Fact]
    public void ChangingExistingText_UpdatesTheLaidOutText()
    {
        var (engine, label) = CreateEngine(PlayerSheet(), "short");
        float narrow = label.Children.OfType<TextNode>().Single().LayoutBox!.BoxModel.Content.Width;

        label.TextContent = "a considerably longer label";
        engine.Render(_canvas);

        var textNode = label.Children.OfType<TextNode>().Single();
        textNode.Text.ShouldBe("a considerably longer label");
        textNode.LayoutBox!.BoxModel.Content.Width.ShouldBeGreaterThan(narrow);
    }

    [Fact]
    public void ChangingExistingText_KeepsTheSameTextNode()
    {
        // 就地改写而非删除重建：重建会是一次结构变更，快路径就失效了。
        var (engine, label) = CreateEngine(PlayerSheet());
        var node = label.Children.OfType<TextNode>().Single();

        label.TextContent = "0:02 / 0:03";

        label.Children.OfType<TextNode>().Single().ShouldBeSameAs(node);
        label.Children[0].ShouldBeSameAs(node);
    }

    [Fact]
    public void ClassChange_StillRestyles()
    {
        var sheet = PlayerSheet();
        sheet.AddRule(new ClassSelector("live"), new Style { BackgroundColor = Color.FromRgb(200, 0, 0) });
        var (engine, label) = CreateEngine(sheet);

        label.Class = "time live";
        var (styled, _) = RenderAndMeasure(engine);

        styled.ShouldBeTrue();
        label.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.FromRgb(200, 0, 0));
    }

    [Fact]
    public void TextThenClassChangeInOneFrame_Restyles()
    {
        // 同一帧里先有内容变更、后有样式变更：样式版本号已变，必须整树重算。
        var sheet = PlayerSheet();
        sheet.AddRule(new ClassSelector("live"), new Style { BackgroundColor = Color.FromRgb(200, 0, 0) });
        var (engine, label) = CreateEngine(sheet);

        label.TextContent = "0:02 / 0:03";
        label.Class = "time live";
        engine.Render(_canvas);

        label.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.FromRgb(200, 0, 0));
    }

    [Fact]
    public void TextBecomingEmpty_RestylesForEmptyPseudoClass()
    {
        // :empty 读文本节点是否为空：空↔非空的切换必须按样式变更记账。
        var sheet = PlayerSheet();
        sheet.AddRule(new CompoundSelector(new ClassSelector("time"), new EmptySelector()),
            new Style { BackgroundColor = Color.FromRgb(0, 0, 255) });
        var (engine, label) = CreateEngine(sheet);
        var node = label.Children.OfType<TextNode>().Single();

        node.Text = "";
        engine.Render(_canvas);
        label.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.FromRgb(0, 0, 255));

        node.Text = "0:03 / 0:03";
        engine.Render(_canvas);
        label.LayoutBox!.ComputedStyle.BackgroundColor.ShouldNotBe(Color.FromRgb(0, 0, 255));
    }

    [Fact]
    public void AttributeSelectorOnText_DisablesTheShortcut()
    {
        // 属性选择器能读到文本节点的 Text；表里有这种规则时，文字变化就不再是「纯内容」。
        var sheet = PlayerSheet();
        sheet.AddRule(new AttributeSelector("text", AttributeMatchOperator.Equals, "LIVE"),
            new Style { Color = Color.FromRgb(0, 128, 0) });
        var (engine, label) = CreateEngine(sheet);

        label.TextContent = "LIVE";
        var (styled, _) = RenderAndMeasure(engine);

        styled.ShouldBeTrue();
        label.Children.OfType<TextNode>().Single().LayoutBox!.ComputedStyle.Color.ShouldBe(Color.FromRgb(0, 128, 0));
    }

    [Fact]
    public void AddingText_ToAnElementWithoutText_IsAStructuralChange()
    {
        var sheet = PlayerSheet();
        sheet.AddRule(new CompoundSelector(new ClassSelector("controls"), new EmptySelector()),
            new Style { Height = Length.Px(33) });
        var (engine, _) = CreateEngine(sheet);
        var controls = engine.GetRoot()!.Children[1];
        controls.LayoutBox!.ComputedStyle.Height.Value.ShouldBe(33f);

        controls.TextContent = "now has text";
        engine.Render(_canvas);

        controls.LayoutBox!.ComputedStyle.Height.IsAuto.ShouldBeTrue();
    }

    [Fact]
    public void RuleAddedDirectlyToRules_IsPickedUpByTheNextTextOnlyFrame()
    {
        // 直接往 Rules 集合里 Add 不递增样式表的 Version（测试与 DomBuilder 惯用这种写法），
        // 但级联结果变了。沿用样式的判定必须把规则条数也算进去。
        var sheet = PlayerSheet();
        var (engine, label) = CreateEngine(sheet);

        sheet.Rules.Add(new StyleRule { Selector = new ClassSelector("time"), Style = new Style { BackgroundColor = Color.FromRgb(9, 9, 9) } });
        // 不调用 InvalidateLayoutCache：改动前，这次文字变更会顺带触发整树重算、把新规则算进去；
        // 沿用样式的快路径不能让这种写法悄悄失效。
        label.TextContent = "0:02 / 0:03";
        engine.Render(_canvas);

        label.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.FromRgb(9, 9, 9));
    }

    [Fact]
    public void RestyleAfterContentOnlyFrames_StillSeesLaterStyleChanges()
    {
        // 连续几帧只改文字（都走快路径），之后的样式变更照样生效——快路径不能把
        // 「上次算过样式」的记录停在旧值上。
        var sheet = PlayerSheet();
        sheet.AddRule(new ClassSelector("live"), new Style { BackgroundColor = Color.FromRgb(200, 0, 0) });
        var (engine, label) = CreateEngine(sheet);

        for (int i = 2; i < 5; i++)
        {
            label.TextContent = $"0:0{i} / 0:05";
            engine.Render(_canvas);
        }
        label.Class = "time live";
        engine.Render(_canvas);

        label.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.FromRgb(200, 0, 0));
    }
}
