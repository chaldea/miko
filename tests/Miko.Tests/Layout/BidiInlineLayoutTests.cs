using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// Bidirectional inline layout (ISSUE-157): paragraph direction, <c>text-align: start/end</c>,
/// visual reordering of mixed-direction lines, and the <c>dir</c> attribute. Assertions compare
/// positions, not glyph widths, so they hold with whatever Hebrew font the machine falls back to.
/// </summary>
public class BidiInlineLayoutTests
{
    private const string Shalom = "שלום";
    private const string Olam = "עולם";

    private readonly LayoutEngine _layoutEngine = new();

    private static List<StyleSheet> Sheets(Style divStyle, Style? spanStyle = null)
    {
        var sheet = new StyleSheet();
        sheet.Rules.Add(new StyleRule { Selector = new TagSelector("div"), Style = divStyle });
        if (spanStyle != null) sheet.Rules.Add(new StyleRule { Selector = new TagSelector("span"), Style = spanStyle });
        return [sheet];
    }

    private static Style Paragraph(float width, Direction direction, TextAlign? align = null) => new()
    {
        Display = Display.Block,
        Width = Length.Px(width),
        FontSize = Length.Px(16),
        LineHeight = Length.Number(1.5f),
        Direction = direction,
        TextAlign = align,
    };

    private static List<TextLineFragment> FragmentsOf(LayoutBox textBox)
        => ((TextNode)textBox.Element).LayoutFragments ?? throw new ShouldAssertException("expected line fragments");

    private static RectF Abs(LayoutBox textBox, TextLineFragment frag)
    {
        var c = textBox.BoxModel.Content;
        return new RectF(c.X + frag.X, c.Y + frag.Y, frag.Width, frag.Height);
    }

    [Fact]
    public void Should_Align_Rtl_Text_To_The_Right_By_Default()
    {
        var div = new DivElement();
        div.AddChild(new TextNode($"{Shalom} {Olam}"));

        var layout = _layoutEngine.Layout(div, Sheets(Paragraph(300, Direction.Rtl)), 800, 600);
        var text = layout.Children[0];

        var frag = FragmentsOf(text).ShouldHaveSingleItem();
        frag.IsRtl.ShouldBeTrue();
        frag.Text.ShouldBe($"{Shalom} {Olam}", "fragment text stays in logical order");
        Abs(text, frag).Right.ShouldBe(layout.BoxModel.Content.Right, 0.5f);
    }

    [Theory]
    [InlineData(TextAlign.End, false)]
    [InlineData(TextAlign.Left, false)]
    [InlineData(TextAlign.Start, true)]
    [InlineData(TextAlign.Right, true)]
    public void Should_Resolve_Logical_Text_Align_Against_The_Direction(TextAlign align, bool atRight)
    {
        var div = new DivElement();
        div.AddChild(new TextNode(Shalom));

        var layout = _layoutEngine.Layout(div, Sheets(Paragraph(300, Direction.Rtl, align)), 800, 600);
        var rect = Abs(layout.Children[0], FragmentsOf(layout.Children[0])[0]);

        if (atRight) rect.Right.ShouldBe(layout.BoxModel.Content.Right, 0.5f);
        else rect.Left.ShouldBe(layout.BoxModel.Content.Left, 0.5f);
    }

    [Fact]
    public void Should_Keep_Ltr_Start_Alignment_On_The_Left()
    {
        var div = new DivElement();
        div.AddChild(new TextNode("hello"));

        var layout = _layoutEngine.Layout(div, Sheets(Paragraph(300, Direction.Ltr, TextAlign.Start)), 800, 600);

        Abs(layout.Children[0], FragmentsOf(layout.Children[0])[0]).Left.ShouldBe(layout.BoxModel.Content.Left, 0.5f);
    }

    [Fact]
    public void Should_Reorder_A_Hebrew_Word_Inside_An_Ltr_Line()
    {
        // "abc שלום def": three fragments — the Hebrew one RTL, all in logical order on one line.
        var div = new DivElement();
        div.AddChild(new TextNode($"abc {Shalom} def"));

        var layout = _layoutEngine.Layout(div, Sheets(Paragraph(400, Direction.Ltr)), 800, 600);
        var text = layout.Children[0];
        var frags = FragmentsOf(text);

        frags.Select(f => f.IsRtl).ShouldBe(new[] { false, true, false });
        frags[1].Text.ShouldBe(Shalom);
        Abs(text, frags[0]).Right.ShouldBe(Abs(text, frags[1]).Left, 0.5f);
        Abs(text, frags[1]).Right.ShouldBe(Abs(text, frags[2]).Left, 0.5f);
    }

    [Fact]
    public void Should_Put_The_First_Word_Of_An_Rtl_Paragraph_On_The_Right()
    {
        // Two inline-blocks in an RTL paragraph: the logically first one is drawn on the right.
        var first = new SpanElement();
        first.AddChild(new TextNode("one"));
        var second = new SpanElement();
        second.AddChild(new TextNode("two"));
        var div = new DivElement();
        div.AddChild(first);
        div.AddChild(new TextNode(" "));
        div.AddChild(second);

        var layout = _layoutEngine.Layout(div,
            Sheets(Paragraph(400, Direction.Rtl), new Style { Display = Display.InlineBlock }), 800, 600);

        var firstBox = layout.Children[0].BoxModel.BorderBox;
        var secondBox = layout.Children[2].BoxModel.BorderBox;
        firstBox.Left.ShouldBeGreaterThan(secondBox.Right - 0.5f);
        firstBox.Right.ShouldBe(layout.BoxModel.Content.Right, 0.5f);
    }

    [Fact]
    public void Should_Lay_Out_Numbers_Left_To_Right_After_Rtl_Text()
    {
        // "שלום 123" in an RTL paragraph: the digits (level 2) sit left of the Hebrew word.
        var div = new DivElement();
        div.AddChild(new TextNode($"{Shalom} 123"));

        var layout = _layoutEngine.Layout(div, Sheets(Paragraph(400, Direction.Rtl)), 800, 600);
        var text = layout.Children[0];
        var frags = FragmentsOf(text);

        var digits = frags.Single(f => f.Text == "123");
        var hebrew = frags.Single(f => f.Text.StartsWith(Shalom, StringComparison.Ordinal));
        digits.IsRtl.ShouldBeFalse();
        hebrew.IsRtl.ShouldBeTrue();
        Abs(text, digits).Right.ShouldBeLessThanOrEqualTo(Abs(text, hebrew).Left + 0.5f);
    }

    [Fact]
    public void Should_Not_Break_Between_Scripts_Inside_A_Word()
    {
        // "abcשלום" has no break opportunity although its two halves have different levels.
        var div = new DivElement();
        div.AddChild(new TextNode($"abc{Shalom} x"));

        var layout = _layoutEngine.Layout(div, Sheets(Paragraph(20, Direction.Ltr)), 800, 600);
        var frags = FragmentsOf(layout.Children[0]);

        var word = frags.Where(f => f.Text is "abc" || f.Text == Shalom).ToList();
        word.Count.ShouldBe(2);
        word[0].Y.ShouldBe(word[1].Y, 0.01f, "both halves of the word stay on one line");
    }

    [Fact]
    public void Should_Leave_Ltr_Only_Content_On_The_Existing_Path()
    {
        var div = new DivElement();
        div.AddChild(new TextNode("plain text"));

        var layout = _layoutEngine.Layout(div, Sheets(Paragraph(400, Direction.Ltr)), 800, 600);

        FragmentsOf(layout.Children[0]).ShouldAllBe(f => !f.IsRtl);
    }

    [Fact]
    public void Should_Put_Inline_Box_Padding_On_Its_Physical_Side_In_Rtl()
    {
        // padding-left stays on the left of an RTL span (logical edges are not swapped by direction).
        var span = new SpanElement();
        span.AddChild(new TextNode(Shalom));
        var div = new DivElement();
        div.AddChild(span);

        var layout = _layoutEngine.Layout(div,
            Sheets(Paragraph(400, Direction.Rtl), new Style { PaddingLeft = Length.Px(10) }), 800, 600);

        var spanBox = layout.Children[0];
        var frag = spanBox.InlineFragments.ShouldNotBeNull().ShouldHaveSingleItem();
        var text = spanBox.Children[0];
        var textRect = Abs(text, FragmentsOf(text)[0]);
        (textRect.Left - frag.Left).ShouldBe(10, 0.5f);
        frag.Right.ShouldBe(textRect.Right, 0.5f);
        frag.Right.ShouldBe(layout.BoxModel.Content.Right, 0.5f);
    }

    [Fact]
    public void Should_Set_Direction_From_The_Dir_Attribute()
    {
        var inner = new DivElement();
        inner.AddChild(new TextNode(Shalom));
        var outer = new DivElement { Dir = "rtl" };
        outer.AddChild(inner);

        var layout = _layoutEngine.Layout(outer, Sheets(new Style { Display = Display.Block, Width = Length.Px(300) }), 800, 600);

        layout.ComputedStyle.Direction.ShouldBe(Direction.Rtl);
        layout.Children[0].ComputedStyle.Direction.ShouldBe(Direction.Rtl, "direction inherits");
    }

    [Fact]
    public void Should_Let_Author_Styles_Override_The_Dir_Attribute()
    {
        var div = new DivElement { Dir = "rtl" };

        var layout = _layoutEngine.Layout(div, Sheets(new Style { Direction = Direction.Ltr }), 800, 600);

        layout.ComputedStyle.Direction.ShouldBe(Direction.Ltr);
    }

    [Fact]
    public void Should_Place_A_Fixed_Width_Block_At_The_Right_Edge_Of_An_Rtl_Container()
    {
        // Over-constrained margins (CSS 2.1 §10.3.3): in RTL the left margin is the ignored one.
        var child = new DivElement { Class = "child" };
        var root = new DivElement { Class = "root" };
        root.AddChild(child);
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Display = Display.Block, Width = Length.Px(400), Direction = Direction.Rtl },
            [".child"] = new() { Display = Display.Block, Width = Length.Px(100), Height = Length.Px(10) },
        });

        var layout = _layoutEngine.Layout(root, [sheet], 800, 600);

        layout.Children[0].BoxModel.BorderBox.Right.ShouldBe(layout.BoxModel.Content.Right, 0.5f);
    }

    [Fact]
    public void Should_Still_Center_With_Auto_Margins_In_Rtl()
    {
        var child = new DivElement { Class = "child" };
        var root = new DivElement { Class = "root" };
        root.AddChild(child);
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Display = Display.Block, Width = Length.Px(400), Direction = Direction.Rtl },
            [".child"] = new()
            {
                Display = Display.Block, Width = Length.Px(100), Height = Length.Px(10),
                MarginLeft = Length.Auto, MarginRight = Length.Auto,
            },
        });

        var layout = _layoutEngine.Layout(root, [sheet], 800, 600);

        layout.Children[0].BoxModel.BorderBox.Left.ShouldBe(layout.BoxModel.Content.Left + 150, 0.5f);
    }
}
