using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;
using static Miko.Styling.Css;

namespace Miko.Tests.Styling;

/// <summary>
/// ISSUE-158：<c>::placeholder</c> —— 解析成伪元素规则、完整级联（层 → 特异性 → 顺序）并从宿主继承，
/// 结果挂在 <see cref="LayoutBox.PlaceholderStyle"/> 上，由绘制读取（不再写死灰色）。
/// </summary>
[Collection(Miko.Tests.GlobalFontStateCollection.Name)] // 像素断言依赖进程级字体状态（ISSUE-129）
public class PlaceholderPseudoElementTests
{
    private static readonly Color Red = Color.FromRgb(255, 0, 0);

    private static StyleSheet SheetOf(CssObject css, int layer = 0)
    {
        var sheet = new StyleSheet { Layer = layer };
        sheet.Add(css);
        return sheet;
    }

    private static ComputedStyle? PlaceholderOf(Element root, Element field, params StyleSheet[] sheets)
    {
        new LayoutEngine().Layout(root, sheets.ToList(), 400, 300);
        return field.LayoutBox!.PlaceholderStyle;
    }

    #region Parsing and rule extraction

    [Fact]
    public void Should_ParsePlaceholderPseudoElement()
    {
        CssSelectorParser.Parse("input::placeholder").ShouldBeOfType<CompoundSelector>()
            .Selectors.ShouldContain(s => s is PlaceholderPseudoElement);
        CssSelectorParser.Parse("::placeholder").ShouldBeOfType<PlaceholderPseudoElement>();
    }

    [Fact]
    public void Should_ExtractPlaceholderRulesFromCssObject()
    {
        var sheet = SheetOf(new CssObject
        {
            [".form .field::placeholder"] = new() { Color = Red },
            ["textarea::placeholder"] = new() { Opacity = 0.5f },
        });

        sheet.Rules.ShouldBeEmpty();
        sheet.PseudoElementRules.Count.ShouldBe(2);
        sheet.PseudoElementRules.ShouldAllBe(r => r.Type == PseudoElementType.Placeholder);
        sheet.PseudoElementRules[0].Selector.ShouldBeOfType<DescendantSelector>();
        sheet.PseudoElementRules[1].Selector.ShouldBeOfType<TagSelector>();
    }

    [Fact]
    public void Should_CreatePlaceholderRuleFromFluentBuilder()
    {
        var sheet = new StyleSheet();
#pragma warning disable CS0618 // 与 Before()/After() 同一套（已标记过时的）构建器入口
        sheet.AddRule(Style.For<InputElement>().Placeholder().Set(x => x.Color, Red));
#pragma warning restore CS0618

        sheet.PseudoElementRules.ShouldHaveSingleItem().Type.ShouldBe(PseudoElementType.Placeholder);
    }

    #endregion

    #region Cascade

    [Fact]
    public void Should_LeaveStyleNull_WhenNoRuleMatches()
    {
        var input = new InputElement { Placeholder = "Name" };
        PlaceholderOf(input, input, SheetOf(new CssObject { [".other::placeholder"] = new() { Color = Red } }))
            .ShouldBeNull();
    }

    [Fact]
    public void Should_NotStyleTheFieldItself()
    {
        var input = new InputElement { Placeholder = "Name" };
        var sheet = SheetOf(new CssObject { ["input::placeholder"] = new() { Color = Red, Opacity = 0.5f } });

        var placeholder = PlaceholderOf(input, input, sheet);

        placeholder!.Color.ShouldBe(Red);
        input.LayoutBox!.ComputedStyle.Color.ShouldNotBe(Red);
        input.LayoutBox.ComputedStyle.Opacity.ShouldBe(1f);
    }

    [Fact]
    public void Should_InheritFontFromTheField_AndKeepDeclaredValues()
    {
        var input = new InputElement { Class = "field", Placeholder = "Name" };
        var sheet = SheetOf(new CssObject
        {
            [".field"] = new() { FontSize = Length.Px(22), FontFamily = "Courier New", Color = Color.Blue },
            [".field::placeholder"] = new() { FontWeight = FontWeight.Bold, Opacity = 0.6f },
        });

        var placeholder = PlaceholderOf(input, input, sheet)!;

        placeholder.FontSize.Value.ShouldBe(22f);
        placeholder.FontFamily.ShouldBe("Courier New");
        placeholder.FontWeight.ShouldBe(FontWeight.Bold);
        placeholder.Opacity.ShouldBe(0.6f);
        // 规则没写颜色：UA 默认灰色位于作者规则之下、继承之上——不会变成宿主的蓝色。
        placeholder.Color.ShouldBe(Color.Gray);
    }

    [Fact]
    public void Should_ResolveInheritAndVarsAgainstTheField()
    {
        var input = new InputElement { Class = "field" };
        var sheet = SheetOf(new CssObject
        {
            [".field"] = new() { Color = Color.Blue, Vars = new() { ["--placeholder-size"] = Length.Px(30) } },
            [".field::placeholder"] = new() { Color = Inherit, FontSize = Var("--placeholder-size") },
        });

        var placeholder = PlaceholderOf(input, input, sheet)!;

        placeholder.Color.ShouldBe(Color.Blue);
        placeholder.FontSize.Value.ShouldBe(30f);
    }

    [Fact]
    public void Should_LetMoreSpecificRuleWin_RegardlessOfOrder()
    {
        var input = new InputElement { Class = "field" };
        var sheet = SheetOf(new CssObject
        {
            [".field::placeholder"] = new() { Color = Red },
            ["input::placeholder"] = new() { Color = Color.Blue, FontWeight = FontWeight.Bold },
        });

        var placeholder = PlaceholderOf(input, input, sheet)!;

        placeholder.Color.ShouldBe(Red);
        placeholder.FontWeight.ShouldBe(FontWeight.Bold);
    }

    [Fact]
    public void Should_LetLaterRuleWin_AtEqualSpecificity_AndHigherLayerWinOverall()
    {
        var input = new InputElement { Class = "field" };
        var app = SheetOf(new CssObject { ["input::placeholder"] = new() { Color = Color.Blue } });
        app.Add(new CssObject { ["input::placeholder"] = new() { Color = Red } });
        PlaceholderOf(input, input, app)!.Color.ShouldBe(Red);

        // 组件库层（-1）里更具体的规则也输给应用层（0）。
        var library = SheetOf(new CssObject { ["#id.field::placeholder"] = new() { Color = Color.Green } }, layer: -1);
        input.Id = "id";
        PlaceholderOf(input, input, library, app)!.Color.ShouldBe(Red);
        PlaceholderOf(input, input, library)!.Color.ShouldBe(Color.Green);
    }

    [Fact]
    public void Should_FollowTheFieldState()
    {
        var input = new InputElement { Class = "field" };
        var sheet = SheetOf(new CssObject
        {
            [".field::placeholder"] = new() { Color = Color.Blue },
            [".field:disabled::placeholder"] = new() { Color = Red },
        });

        PlaceholderOf(input, input, sheet)!.Color.ShouldBe(Color.Blue);
        input.SetState(ElementState.Disabled);
        PlaceholderOf(input, input, sheet)!.Color.ShouldBe(Red);
        input.ClearState(ElementState.Disabled);
        PlaceholderOf(input, input, sheet)!.Color.ShouldBe(Color.Blue);
    }

    [Fact]
    public void Should_ApplyToTextareas_ButNotToOtherElements()
    {
        var textarea = new TextAreaElement { Class = "notes", Placeholder = "Notes" };
        var div = new DivElement { Class = "notes" };
        var root = new DivElement { Children = { textarea, div } };
        var sheet = SheetOf(new CssObject { [".notes::placeholder"] = new() { Color = Red } });

        new LayoutEngine().Layout(root, [sheet], 400, 300);

        textarea.LayoutBox!.PlaceholderStyle!.Color.ShouldBe(Red);
        div.LayoutBox!.PlaceholderStyle.ShouldBeNull();
    }

    [Fact]
    public void Should_NotAccumulateIntoLegacyPseudoElementStyles()
    {
        var input = new InputElement();
        PlaceholderOf(input, input, SheetOf(new CssObject { ["input::placeholder"] = new() { Color = Red } }));

        (input.PseudoElementStyles?.ContainsKey(PseudoElementType.Placeholder) ?? false).ShouldBeFalse();
    }

    [Fact]
    public void Should_KeepPlaceholderStyle_WhenTheEngineReusesComputedStyles()
    {
        // 只改文字（非空 → 非空）按内容记账：样式阶段沿用各元素的计算样式、只换新布局盒
        // （ISSUE-146）。新盒必须带上占位符样式，否则下一帧占位符退回灰色。
        var label = new SpanElement { TextContent = "before" };
        var input = new InputElement { Placeholder = "Name" };
        var root = new DivElement { Children = { label, input } };
        var sheet = SheetOf(new CssObject { ["input::placeholder"] = new() { Color = Red } });
        var engine = new MikoEngineBuilder().Build();
        using var surface = SKSurface.Create(new SKImageInfo(200, 200));

        engine.Initialize(root, [sheet], surface.Canvas, 200, 200);
        var firstBox = input.LayoutBox!;
        firstBox.PlaceholderStyle!.Color.ShouldBe(Red);

        ((TextNode)label.Children[0]).Text = "after";
        engine.Render(surface.Canvas);

        input.LayoutBox.ShouldNotBeSameAs(firstBox);   // 走了重排……
        input.LayoutBox!.ComputedStyle.ShouldBeSameAs(firstBox.ComputedStyle);   // ……且沿用了计算样式
        input.LayoutBox.PlaceholderStyle!.Color.ShouldBe(Red);
    }

    #endregion

    #region Painting

    private static SKBitmap Paint(Element root, StyleSheet sheet)
    {
        var bitmap = new SKBitmap(240, 60);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        var renderEngine = new RenderEngine();
        renderEngine.SetCanvas(canvas);
        var layout = new LayoutEngine().Layout(root, [sheet], 240, 60);
        renderEngine.Render(layout);
        canvas.Flush();
        return bitmap;
    }

    private static int CountPixels(SKBitmap bitmap, Func<SKColor, bool> predicate)
    {
        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if (predicate(bitmap.GetPixel(x, y))) count++;
        return count;
    }

    private static bool IsReddish(SKColor c) => c.Red > 150 && c.Green < 110 && c.Blue < 110;

    private static bool IsInk(SKColor c) => c.Red < 230 || c.Green < 230 || c.Blue < 230;

    private static InputElement Field(string placeholder) => new()
    {
        Class = "field",
        Placeholder = placeholder,
        Style = new Style { Width = Length.Px(220), Height = Length.Px(40), FontSize = Length.Px(20), BorderWidth = Length.Px(0) },
    };

    [Fact]
    public void Should_PaintPlaceholderInGray_ByDefault()
    {
        using var bitmap = Paint(Field("Placeholder"), SheetOf(new CssObject { [".unrelated"] = new() { Color = Red } }));

        CountPixels(bitmap, IsInk).ShouldBeGreaterThan(0);
        CountPixels(bitmap, IsReddish).ShouldBe(0);
    }

    [Fact]
    public void Should_PaintPlaceholderWithItsStyledColor()
    {
        using var bitmap = Paint(Field("Placeholder"), SheetOf(new CssObject { [".field::placeholder"] = new() { Color = Red } }));

        CountPixels(bitmap, IsReddish).ShouldBeGreaterThan(20);
    }

    [Fact]
    public void Should_PaintNothing_WhenPlaceholderOpacityIsZero()
    {
        using var bitmap = Paint(Field("Placeholder"), SheetOf(new CssObject { [".field::placeholder"] = new() { Color = Red, Opacity = 0f } }));

        CountPixels(bitmap, IsInk).ShouldBe(0);
    }

    [Fact]
    public void Should_PaintTextareaPlaceholderWithItsStyledColor()
    {
        var textarea = new TextAreaElement
        {
            Placeholder = "Notes",
            Style = new Style { Width = Length.Px(220), Height = Length.Px(50), FontSize = Length.Px(20), BorderWidth = Length.Px(0) },
        };

        using var bitmap = Paint(textarea, SheetOf(new CssObject { ["textarea::placeholder"] = new() { Color = Red } }));

        CountPixels(bitmap, IsReddish).ShouldBeGreaterThan(20);
    }

    #endregion
}
