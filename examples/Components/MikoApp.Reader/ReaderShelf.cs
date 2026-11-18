using Miko.Common;
using Miko.Components;
using Miko.Core.DomElements;
using Miko.Graphics;

namespace MikoApp.Reader;

/// <summary>The sample library. Reusable reader components do not own a shelf or navigation stack.</summary>
public sealed class ReaderShelf : ComponentBase
{
    [Parameter] public IReadOnlyList<ShelfBook> Books { get; set; } = [];
    [Parameter] public EventCallback<ShelfBook> Opened { get; set; }
    private int _filter;
    private static readonly Color Ink = Color.FromHex("#293c32"), Muted = Color.FromHex("#899187"), Paper = Color.FromHex("#f7f7f2");

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var root = new DivElement { Class = "reader-shelf", Style = new() { Display = Display.Flex, FlexDirection = FlexDirection.Column, Width = Length.Percent(100), Height = Length.Percent(100), BackgroundColor = Paper, Color = Ink, PaddingTop = Length.SafeAreaInsetTop, PaddingBottom = Length.SafeAreaInsetBottom, PaddingLeft = Length.SafeAreaInsetLeft + Length.Px(24), PaddingRight = Length.SafeAreaInsetRight + Length.Px(24), BoxSizing = BoxSizing.BorderBox } };
        var header = new DivElement { Style = new() { Display = Display.Flex, FlexDirection = FlexDirection.Column, PaddingTop = Length.Px(28), PaddingBottom = Length.Px(22), FlexShrink = 0 } };
        header.Children.Add(Text("MIKO  /  READER", 10, Muted));
        header.Children.Add(new SpanElement { TextContent = "书架", Style = new() { Display = Display.Block, FontSize = Length.Px(32), FontWeight = FontWeight.SemiBold, MarginTop = Length.Px(10), MarginBottom = Length.Px(8) } });
        header.Children.Add(Text("把故事，留在手边。", 13, Muted));
        var filters = new DivElement { Style = new() { Display = Display.Flex, Gap = Length.Px(8), MarginBottom = Length.Px(18), FlexShrink = 0 } };
        string[] labels = ["全部", "电子书", "漫画"];
        for (int i = 0; i < labels.Length; i++)
        {
            int filter = i;
            var button = new ButtonElement { Class = "reader-shelf-filter", TextContent = labels[i], Style = new() { Display = Display.Flex, AlignItems = AlignItems.Center, JustifyContent = JustifyContent.Center, Width = Length.Px(76), Height = Length.Px(44), BorderWidth = Length.Px(0), BorderRadius = Length.Px(22), FontSize = Length.Px(13), BackgroundColor = i == _filter ? Ink : Color.FromHex("#eceee6"), Color = i == _filter ? Color.White : Muted } };
            button.OnClick = _ => { _filter = filter; StateHasChanged(); };
            filters.Children.Add(button);
        }
        var list = new DivElement { Class = "reader-shelf-list", Style = new() { FlexGrow = 1, Height = Length.Px(0), MinHeight = Length.Px(0), OverflowY = Overflow.Auto } };
        var shown = Books.Where(book => _filter == 0 || book.IsComic == (_filter == 2)).ToArray();
        list.Children.Add(new SpanElement { TextContent = $"我的收藏  ·  {shown.Length} 本", Style = new() { Display = Display.Block, FontSize = Length.Px(11), Color = Muted, MarginBottom = Length.Px(14) } });
        foreach (var book in shown) list.Children.Add(Card(book));
        var footer = Text("打开一本书，开始今天的阅读", 11, Muted);
        footer.Style!.TextAlign = TextAlign.Center; footer.Style.PaddingTop = Length.Px(12); footer.Style.PaddingBottom = Length.Px(14); footer.Style.FlexShrink = 0;
        root.Children.AddRange([header, filters, list, footer]);
        builder.AddElement(0, root);
    }

    private ButtonElement Card(ShelfBook book)
    {
        var card = new ButtonElement { Class = "reader-shelf-book", Style = new() { Display = Display.Flex, AlignItems = AlignItems.Center, Width = Length.Percent(100), MinHeight = Length.Px(154), MarginBottom = Length.Px(12), Padding = Length.Px(12), Gap = Length.Px(16), BorderWidth = Length.Px(0), BorderRadius = Length.Px(14), BackgroundColor = Color.White, Color = Ink, TextAlign = TextAlign.Left, BoxSizing = BoxSizing.BorderBox } };
        card.OnClick = e => { _ = Opened.InvokeAsync(book); };
        card.Children.Add(new CanvasElement { Style = new() { Width = Length.Px(84), Height = Length.Px(124), FlexShrink = 0, PointerEvents = PointerEvents.None }, Paint = (context, box) => Cover(context, box, book) });
        var details = new DivElement { Style = new() { FlexGrow = 1, FlexShrink = 1, MinWidth = Length.Px(0), PointerEvents = PointerEvents.None } };
        details.Children.Add(Text(book.IsComic ? "漫画  /  COMIC" : "电子书  /  BOOK", 10, book.Color));
        details.Children.Add(new SpanElement { TextContent = book.Title, Style = new() { Display = Display.Block, FontSize = Length.Px(17), FontWeight = FontWeight.Medium, MarginTop = Length.Px(7), MarginBottom = Length.Px(7), PointerEvents = PointerEvents.None } });
        details.Children.Add(Text(book.Description, 11, Muted));
        details.Children.Add(new SpanElement { TextContent = book.Index > 0 ? $"继续阅读 · 第 {book.Index + 1} {(book.IsComic ? "页" : "章")}  →" : "开始阅读  →", Style = new() { Display = Display.Block, FontSize = Length.Px(11), Color = Ink, MarginTop = Length.Px(12), PointerEvents = PointerEvents.None } });
        card.Children.Add(details);
        return card;
    }

    private static SpanElement Text(string text, float size, Color color) => new() { TextContent = text, Style = new() { Display = Display.Block, FontSize = Length.Px(size), Color = color, PointerEvents = PointerEvents.None } };
    private static void Cover(IDrawingContext context, RectF box, ShelfBook book)
    {
        context.FillRoundRect(box, 5, book.Color);
        context.FillRect(new(box.X + 4, box.Y, 2, box.Height), Color.FromRgba(255, 255, 255, .15f));
        context.FillCircle(box.X + box.Width * .72f, box.Y + box.Height * .55f, 13, Color.FromHex("#e9d8ac"));
        context.Save(); context.ClipRect(new(box.X, box.Y + 60, box.Width, box.Height - 60));
        context.FillCircle(box.X + 18, box.Y + box.Height + 12, 58, Color.FromRgba(30, 58, 46, .42f));
        context.FillCircle(box.X + 80, box.Y + box.Height + 25, 60, Color.FromRgba(30, 58, 46, .32f));
        context.Restore();
        var title = context.Graphics.CreateText(book.Title.Split(' ')[0], new("serif", 11));
        context.DrawText(title, box.X + 10, box.Y + 24, Color.FromHex("#fff6df"));
        var mark = context.Graphics.CreateText(book.IsComic ? "PICTURE BOOK" : "MIKO LIBRARY", new("sans-serif", 5));
        context.DrawText(mark, box.X + 10, box.Y + 36, Color.FromHex("#e6ecdf"));
    }
}
