using Miko.Common;
using Miko.Core;
using Miko.Events;
using Miko.Graphics;
using Miko.Highlight;

namespace Miko.Components.Editor;

public sealed partial class MikoEditor
{
    private const int LineHeight = 22;
    private const int Scrollbar = 12;
    private const int HighlightLimit = 4096;
    private EditorColumns _columns = null!;
    private float _width, _height, _top, _left;
    private int _firstColumn, _lastColumn;
    private float _visibleContentWidth;
    private readonly Dictionary<int, PaintedRow> _rows = new();
    private sealed record Run(int Column, TextBlock Text, CodeTokenType Kind);
    private sealed record PaintedRow(TextBlock Number, List<Run> Runs, float Width, int Change);

    private int RowCount => _diff?.Count ?? Document.LineCount;
    private int VisibleRows => Math.Max(1, (int)(_height / LineHeight));
    private float MaxTop => Math.Max(0, RowCount * (float)LineHeight - _height);
    private float ThumbHeight => Math.Min(_height, Math.Max(24, _height * (_height / Math.Max(1, RowCount * (float)LineHeight))));
    private float Gutter => 16 + _columns.Width * (Math.Max(2, RowCount.ToString().Length) + (_diff == null ? 0 : 2));

    private void InvalidateRows()
    {
        _rows.Clear();
        Engine.RequestRepaint();
    }

    private void Paint(IDrawingContext context, RectF box)
    {
        _width = box.Width;
        _height = box.Height;
        _top = Math.Clamp(_top, 0, MaxTop);
        int firstColumn = (int)(_left / _columns.Width);
        int lastColumn = firstColumn + Math.Max(1, (int)Math.Ceiling((_width - Gutter) / _columns.Width)) + 2;
        if (firstColumn != _firstColumn || lastColumn != _lastColumn || _rows.Count > 3 * (VisibleRows + 2))
            _rows.Clear();
        _firstColumn = firstColumn;
        _lastColumn = lastColumn;
        context.FillRect(box, Theme.Background);
        int first = (int)(_top / LineHeight);
        _visibleContentWidth = 0;
        for (int i = first; i < RowCount; i++)
        {
            float y = box.Y + (i - first) * LineHeight - _top % LineHeight;
            if (y >= box.Bottom) break;
            if (!_rows.TryGetValue(i, out var row)) _rows[i] = row = PrepareRow(context.Graphics, i);
            _visibleContentWidth = Math.Max(_visibleContentWidth, row.Width);
            if (row.Change != 0)
                context.FillRect(new(box.X, y, box.Width, LineHeight), row.Change > 0 ? Theme.Added : Theme.Removed);
            context.DrawText(row.Number, box.X + Gutter - 9 - row.Number.Width, y + 16, Theme.Gutter);
            int save = context.Save();
            float left = box.X + Gutter;
            context.ClipRect(new(left, y, Math.Max(0, box.Right - left - Scrollbar), LineHeight));
            float origin = left - _left;
            if (_diff == null && _anchor != _caret) PaintSelection(context, origin, y, i);
            foreach (var run in row.Runs)
                context.DrawText(run.Text, origin + run.Column * _columns.Width, y + 16,
                    Theme.Syntax.ColorFor(run.Kind) ?? Theme.Foreground);
            if (Editable && _canvas.HasState(ElementState.Focus) && Document.GetLineAt(_caret) == i)
            {
                float x = box.X + CaretRect.X;
                context.FillRect(new(x, y + 2, 1.5f, LineHeight - 4), Theme.Foreground);
                if (Input.CompositionText is { Length: > 0 } composition)
                {
                    var preedit = context.Graphics.CreateText(composition, EditorColumns.Font);
                    context.FillRect(new(x, y, preedit.Width, LineHeight), Theme.Background);
                    context.DrawText(preedit, x, y + 16, Theme.Foreground);
                    context.StrokeLine(x, y + 18, x + preedit.Width, y + 18, Theme.Foreground, 1);
                }
            }
            context.RestoreToCount(save);
        }
        if (MaxTop > 0)
            context.FillRoundRect(new(box.Right - Scrollbar + 2, box.Y + _top / MaxTop * (_height - ThumbHeight),
                Scrollbar - 4, ThumbHeight), 4, Theme.Gutter);
    }

    private PaintedRow PrepareRow(IGraphicsFactory graphics, int row)
    {
        var pair = _diff == null ? new DiffRow(row, row) : _diff[row];
        var doc = pair.Modified < 0 ? _original! : Document;
        int line = pair.Modified < 0 ? pair.Original : pair.Modified;
        int change = pair.Modified < 0 ? -1 : pair.Original < 0 ? 1 : 0;
        string marker = change == 0 ? "" : change > 0 ? " +" : " -";
        var number = graphics.CreateText((line + 1).ToString() + marker, EditorColumns.Font);
        var chars = doc.GetLine(line).Span;
        var (offset, col) = _columns.Locate(chars, int.MaxValue, _firstColumn);
        IReadOnlyList<CodeToken>? tokens = null;
        if (chars.Length <= HighlightLimit && offset < chars.Length)
        {
            tokens = Services?.Language(Language) is { } service
                ? service.TokenizeLine(doc, line)
                : Highlighter.Tokenize(chars.ToString(), Language);
        }

        var runs = new List<Run>();
        int token = 0;
        while (offset < chars.Length && col < _lastColumn)
        {
            int end = chars.Length;
            var kind = CodeTokenType.Plain;
            if (tokens != null)
            {
                while (token < tokens.Count && (long)tokens[token].Start + tokens[token].Length <= offset) token++;
                if (token < tokens.Count)
                {
                    var t = tokens[token];
                    if (t.Start > offset) end = Math.Min(end, t.Start);
                    else { end = (int)Math.Min(end, (long)t.Start + t.Length); kind = t.Type; }
                }
            }
            var (length, cells) = _columns.Run(chars[offset..], col, Math.Min(end - offset, _lastColumn - col));
            if (chars[offset] != '\t')
                runs.Add(new(col, graphics.CreateText(chars.Slice(offset, length).ToString(), EditorColumns.Font), kind));
            offset += length;
            col += cells;
        }
        // A clipped row allows another horizontal page without measuring the rest of a long line.
        float width = col * _columns.Width;
        if (offset < chars.Length) width += _width;
        return new(number, runs, width, change);
    }

    private int ColumnAt(int line, int offset) => _columns.Locate(Document.GetLine(line).Span, offset, int.MaxValue).Column;

    private RectF CaretRect
    {
        get
        {
            int line = Document.GetLineAt(_caret);
            float x = Gutter + ColumnAt(line, _caret - Document.GetLineStart(line)) * _columns.Width - _left;
            return new(x, line * (float)LineHeight - _top, 1.5f, LineHeight);
        }
    }

    private void PaintSelection(IDrawingContext context, float origin, float y, int line)
    {
        int start = Document.GetLineStart(line);
        int end = start + Document.GetLine(line).Length;
        if (SelectionEnd <= start || SelectionStart > end) return;
        float x1 = origin + ColumnAt(line, Math.Max(0, SelectionStart - start)) * _columns.Width;
        float x2 = SelectionEnd > end ? origin + _left + _width : origin + ColumnAt(line, SelectionEnd - start) * _columns.Width;
        context.FillRect(new(x1, y, x2 - x1, LineHeight), Theme.Selection);
    }

    private void Scroll(float dx, float dy)
    {
        _top = Math.Clamp(_top + dy, 0, MaxTop);
        if (dx != 0)
        {
            float maxLeft = Math.Max(0, _visibleContentWidth - _width + Gutter + Scrollbar + _columns.Width);
            _left = Math.Clamp(_left + dx, 0, Math.Max(_left, maxLeft));
        }
        Engine.RequestRepaint();
    }

    private void RevealCaret()
    {
        if (_height <= 0 || _width <= 0) return;
        int row = Document.GetLineAt(_caret);
        float y = row * (float)LineHeight;
        _top = Math.Clamp(_top, Math.Max(0, y - Math.Max(0, _height - LineHeight)), y);
        float x = ColumnAt(row, _caret - Document.GetLineStart(row)) * _columns.Width;
        float available = Math.Max(1, _width - Gutter - Scrollbar - 2);
        _left = Math.Clamp(_left, Math.Max(0, x - available), x);
    }

    private int Hit(float x, float y)
    {
        int line = Math.Clamp((int)((_top + y) / LineHeight), 0, Document.LineCount - 1);
        int column = (int)MathF.Round(Math.Max(0, x - Gutter + _left) / _columns.Width);
        return Document.GetLineStart(line) + _columns.Locate(Document.GetLine(line).Span, int.MaxValue, column).Offset;
    }

    private void PointerDown(PointerEventArgs e)
    {
        if (!e.IsPrimary || e.Button != MouseButton.Left) return;
        e.PreventDefault();
        _drag = e.PointerId;
        _scrollDrag = MaxTop > 0 && e.OffsetX >= _width - Scrollbar;
        if (_scrollDrag)
        {
            float top = _top / MaxTop * (_height - ThumbHeight);
            _dragOffset = e.OffsetY >= top && e.OffsetY < top + ThumbHeight ? e.OffsetY - top : ThumbHeight / 2;
            DragScrollbar(e.OffsetY);
        }
        else if (Editable)
        {
            MoveCaret(Hit(e.OffsetX, e.OffsetY));
            Input.SetFocus(_canvas);
            Input.RequestInputMethodShow();
        }
        else _drag = null;
    }

    private void DragScrollbar(float y)
    {
        _top = MaxTop * Math.Clamp((y - _dragOffset) / Math.Max(1, _height - ThumbHeight), 0, 1);
        Engine.RequestRepaint();
    }
}
