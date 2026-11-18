using Miko.Common;
using Miko.Graphics;
using Miko.Utils;

namespace Miko.Components.Reader;

internal sealed record BookTypography(string FontFamily, float FontSize, float LineSpacing, float ParagraphSpacing, float PageMargin);

/// <summary>Measured lines and page boundaries, shared by scrolling and paged presentation.</summary>
internal sealed class BookLayout
{
    internal sealed record Line(TextBlock Text, float Top, float Height, int Offset);
    public List<Line> Lines { get; } = [];
    public List<int> Pages { get; } = [0];
    public float Margin { get; }
    public float Height { get; }
    public float ContentHeight { get; }

    public BookLayout(string text, BookTypography style, float width, float height, IGraphicsFactory graphics)
    {
        Margin = Math.Min(style.PageMargin, Math.Max(0, Math.Min(width, height) / 4));
        ContentHeight = Math.Max(1, height - Margin * 2);
        float top = 0, pageTop = 0;
        int paragraphOffset = 0;
        var textStyle = new TextStyle(style.FontFamily, style.FontSize);
        foreach (string paragraph in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            int search = 0;
            var wrapped = TextWrapper.WrapText(paragraph, style.FontFamily, style.FontSize, FontWeight.Normal,
                Math.Max(1, width - Margin * 2), WhiteSpace.PreWrap, breakLongWords: true);
            foreach (string value in wrapped)
            {
                var line = graphics.CreateText(value, textStyle);
                float lineHeight = Math.Max(line.Height, style.FontSize * style.LineSpacing);
                if (Lines.Count > Pages[^1] && top - pageTop + lineHeight > ContentHeight)
                {
                    Pages.Add(Lines.Count);
                    pageTop = top;
                }
                int offset = paragraph.IndexOf(value, search, StringComparison.Ordinal);
                if (offset < 0) offset = search;
                Lines.Add(new(line, top, lineHeight, paragraphOffset + offset));
                search = Math.Min(paragraph.Length, offset + value.Length);
                top += lineHeight;
            }
            top += style.ParagraphSpacing;
            paragraphOffset += paragraph.Length + 1;
        }
        Height = Lines.Count == 0 ? 0 : Lines[^1].Top + Lines[^1].Height;
    }

    public int PageAtOffset(int offset)
    {
        int page = 0;
        while (page + 1 < Pages.Count && Lines[Pages[page + 1]].Offset <= offset) page++;
        return page;
    }
}

internal sealed class BookView
{
    private string _text = "";
    private BookTypography? _typography;
    private BookLayout? _layout;
    private float _width, _height, _scroll;
    private bool _endPending;
    private float? _progressPending;
    public int Page { get; private set; }
    public int PageCount => _layout?.Pages.Count ?? 1;
    public BookReadingMode Mode { get; set; }
    public bool CanPrevious => Mode == BookReadingMode.Paged ? Page > 0 : _scroll > 0;
    public bool CanNext => _layout != null && (Mode == BookReadingMode.Paged ? Page + 1 < PageCount : _scroll < MaxScroll);
    private float MaxScroll => Math.Max(0, (_layout?.Height ?? 0) - (_layout?.ContentHeight ?? 0));

    public void SetText(string text, bool end = false, float? progress = null)
    {
        _text = text; _layout = null; Page = 0; _scroll = 0; _endPending = end; _progressPending = progress;
    }

    public bool Move(int direction)
    {
        if (_layout == null || (direction > 0 ? !CanNext : !CanPrevious)) return false;
        if (Mode == BookReadingMode.Paged)
        {
            Page = Math.Clamp(Page + direction, 0, PageCount - 1);
            _scroll = Math.Min(MaxScroll, PageTop());
        }
        else Scroll(direction * _layout.ContentHeight * .9f);
        return true;
    }

    public void GoToPage(int page)
    {
        Page = Math.Clamp(page, 0, PageCount - 1);
        _scroll = Math.Min(MaxScroll, PageTop());
    }

    public void Scroll(float delta)
    {
        _scroll = Math.Clamp(_scroll + delta, 0, MaxScroll);
        if (_layout == null) return;
        Page = 0;
        while (Page + 1 < PageCount && _layout.Lines[_layout.Pages[Page + 1]].Top <= _scroll) Page++;
    }

    public void Paint(IDrawingContext context, RectF box, BookTypography typography, Color color)
    {
        if (_layout == null || _width != box.Width || _height != box.Height || typography != _typography)
        {
            int anchor = _layout is { Lines.Count: > 0 } ? _layout.Lines[_layout.Pages[Page]].Offset : 0;
            _layout = new(_text, typography, box.Width, box.Height, context.Graphics);
            _width = box.Width; _height = box.Height; _typography = typography;
            Page = _endPending ? PageCount - 1 : _layout.PageAtOffset(anchor);
            _scroll = _endPending ? MaxScroll : Math.Min(MaxScroll, PageTop());
            _endPending = false;
            if (_progressPending is { } progress)
            {
                GoToPage((int)Math.Round(progress * (PageCount - 1)));
                _progressPending = null;
            }
        }
        var layout = _layout;
        float origin = Mode == BookReadingMode.Paged ? PageTop() : _scroll;
        int start = Mode == BookReadingMode.Paged ? layout.Pages[Page] : 0;
        int end = Mode == BookReadingMode.Paged && Page + 1 < PageCount ? layout.Pages[Page + 1] : layout.Lines.Count;
        context.Save();
        context.ClipRect(new(box.X + layout.Margin, box.Y + layout.Margin, Math.Max(1, box.Width - layout.Margin * 2), layout.ContentHeight));
        for (int i = start; i < end; i++)
        {
            var line = layout.Lines[i];
            float y = line.Top - origin;
            if (y + line.Height < 0) continue;
            if (y > layout.ContentHeight) break;
            context.DrawText(line.Text, box.X + layout.Margin, box.Y + layout.Margin + y + (line.Height - line.Text.Height) / 2 + line.Text.Ascent, color);
        }
        context.Restore();
    }

    private float PageTop() => _layout is { Lines.Count: > 0 } ? _layout.Lines[_layout.Pages[Page]].Top : 0;
}
