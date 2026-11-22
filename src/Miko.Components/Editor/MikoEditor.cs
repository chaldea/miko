using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Highlight;
using Miko.Platform;
using Miko.Styling;

namespace Miko.Components.Editor;

/// <summary>Native, viewport-rendered code and text editor. Supply OriginalValue to compare two snapshots.</summary>
public sealed partial class MikoEditor : ComponentBase
{
    [Inject] public MikoEngine Engine { get; set; } = null!;
    [Inject] public MikoInteractionController Input { get; set; } = null!;
    [Inject] public ISyntaxHighlighter Highlighter { get; set; } = null!;

    [Parameter] public string Value { get; set; } = "";
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    [Parameter] public string? OriginalValue { get; set; }
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public string Language { get; set; } = "plaintext";
    [Parameter] public EditorServices? Services { get; set; }
    [Parameter] public EditorTheme Theme { get; set; } = EditorTheme.Light;
    [Parameter] public string? Class { get; set; }
    [Parameter] public Style? Style { get; set; }

    public EditorDocument Document { get; private set; } = new("");
    public int SelectionStart => Math.Min(_caret, _anchor);
    public int SelectionEnd => Math.Max(_caret, _anchor);
    public bool CanFormat => Editable && Services?.Formatter(Language) != null;

    private bool Editable => !ReadOnly && OriginalValue == null;
    private readonly EditorCanvas _canvas;
    private EditorDocument? _original;
    private List<DiffRow>? _diff;
    private string? _lastValue;
    private int _caret, _anchor;
    private int? _desiredColumn;
    private int? _drag;
    private bool _scrollDrag;
    private float _dragOffset;
    private readonly List<Edit> _undo = [];
    private readonly Stack<Edit> _redo = new();
    private sealed record Edit(int Start, string Removed, string Inserted, int Anchor, int Caret);

    public MikoEditor() => _canvas = new(this);

    protected override void OnInitialized()
    {
        _canvas.Paint = Paint;
        _canvas.OnPointerDown = PointerDown;
        _canvas.OnPointerMove = e =>
        {
            if (_drag != e.PointerId) return;
            if (_scrollDrag) DragScrollbar(e.OffsetY);
            else MoveCaret(Hit(e.OffsetX, e.OffsetY), extend: true);
        };
        _canvas.OnPointerUp = _canvas.OnPointerCancel = e => { if (_drag == e.PointerId) _drag = null; };
        _canvas.OnWheel = e =>
        {
            e.PreventDefault();
            Scroll(e.DeltaX, e.DeltaY);
        };
        _canvas.OnKeyDown = KeyDown;
        _columns = new(Engine.Graphics);
    }

    protected override void OnParametersSet()
    {
        bool reset = Value != _lastValue && Value != Document.Text;
        _lastValue = Value;
        if (reset)
        {
            Document = new(Value ?? "");
            _caret = _anchor = 0;
            _desiredColumn = null;
            _undo.Clear();
            _redo.Clear();
        }
        if (reset || OriginalValue != _original?.Text)
        {
            _original = OriginalValue == null ? null : new(OriginalValue);
            _diff = _original == null ? null : EditorDiff.Compare(_original, Document);
            _top = _left = 0;
        }
        if (!Editable && _canvas.HasState(ElementState.Focus)) Input.SetFocus(null);
        _canvas.Class = $"miko-editor {Class}".TrimEnd();
        var style = new Style();
        if (Style != null) style.Merge(Style);
        style.Merge(new() { Display = Display.Block, Width = Length.Percent(100), Height = Length.Px(360), Cursor = Cursor.Text });
        _canvas.Style = style;
        InvalidateRows();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddElement(0, _canvas);

    public bool FormatDocument()
    {
        if (!Editable || Services?.Formatter(Language) is not { } formatter) return false;
        string formatted = formatter.Format(Document);
        if (formatted != Document.Text) Replace(0, Document.Text.Length, formatted);
        return true;
    }

    public void Select(int anchor, int caret)
    {
        _anchor = Document.Snap(anchor);
        MoveCaret(caret, extend: true);
    }

    private void MoveCaret(int offset, bool extend = false, bool vertical = false)
    {
        _caret = Document.Snap(offset);
        if (!extend) _anchor = _caret;
        if (!vertical) _desiredColumn = null;
        RevealCaret();
        Engine.RequestRepaint();
    }

    private void Replace(int start, int end, string text)
    {
        if (!Editable || (start == end && text.Length == 0)) return;
        var edit = new Edit(start, Document.Text[start..end], text, _anchor, _caret);
        _redo.Clear();
        if (_undo.Count == 100) _undo.RemoveAt(0);
        _undo.Add(edit);
        Apply(start, end, text, start + text.Length, start + text.Length);
    }

    private void Apply(int start, int end, string text, int anchor, int caret)
    {
        Document = new(string.Concat(Document.Text.AsSpan(0, start), text, Document.Text.AsSpan(end)));
        _anchor = Document.Snap(anchor);
        _rows.Clear();
        MoveCaret(caret, extend: true);
        _ = ValueChanged.InvokeAsync(Document.Text);
    }

    private void Insert(string text) => Replace(SelectionStart, SelectionEnd, text);

    private bool Remove(bool backwards)
    {
        int start = SelectionStart, end = SelectionEnd;
        if (start == end)
        {
            if (backwards) start = Document.Snap(start - 1);
            else end = Document.Next(end);
        }
        if (start == end || !Editable) return false;
        Replace(start, end, "");
        return true;
    }

    private void Undo()
    {
        if (_undo.Count == 0) return;
        var edit = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Push(edit);
        Apply(edit.Start, edit.Start + edit.Inserted.Length, edit.Removed, edit.Anchor, edit.Caret);
    }

    private void Redo()
    {
        if (!_redo.TryPop(out var edit)) return;
        _undo.Add(edit);
        int after = edit.Start + edit.Inserted.Length;
        Apply(edit.Start, edit.Start + edit.Removed.Length, edit.Inserted, after, after);
    }

    private void KeyDown(KeyboardEventArgs e)
    {
        bool command = e.CtrlKey || e.MetaKey;
        int row = Document.GetLineAt(_caret);
        int start = Document.GetLineStart(row);
        switch (e.Code)
        {
            case MikoKey.A when command: Select(0, Document.Text.Length); break;
            case MikoKey.Z when command && !e.ShiftKey: Undo(); break;
            case MikoKey.Z or MikoKey.Y when command: Redo(); break;
            case MikoKey.Tab: Insert("\t"); break;
            case MikoKey.Enter: Insert(Document.NewLine); break;
            case MikoKey.Home: MoveCaret(command ? 0 : start, e.ShiftKey); break;
            case MikoKey.End: MoveCaret(command ? Document.Text.Length : start + Document.GetLine(row).Length, e.ShiftKey); break;
            case MikoKey.Left:
                MoveCaret(!e.ShiftKey && _anchor != _caret ? SelectionStart : Document.Snap(_caret - 1), e.ShiftKey);
                break;
            case MikoKey.Right:
                MoveCaret(!e.ShiftKey && _anchor != _caret ? SelectionEnd : Document.Next(_caret), e.ShiftKey);
                break;
            case MikoKey.Up or MikoKey.Down or MikoKey.PageUp or MikoKey.PageDown:
                int step = e.Code is MikoKey.Up or MikoKey.Down ? 1 : VisibleRows;
                if (e.Code is MikoKey.Up or MikoKey.PageUp) step = -step;
                int target = Math.Clamp(row + step, 0, Document.LineCount - 1);
                _desiredColumn ??= ColumnAt(row, _caret - start);
                int offset = _columns.Locate(Document.GetLine(target).Span, int.MaxValue, _desiredColumn.Value).Offset;
                MoveCaret(Document.GetLineStart(target) + offset, e.ShiftKey, vertical: true);
                break;
            default: return;
        }
        e.PreventDefault();
    }

    protected override void OnDispose()
    {
        if (_canvas.HasState(ElementState.Focus)) Input.SetFocus(null);
        _canvas.Paint = null;
        _rows.Clear();
    }

    private sealed class EditorCanvas(MikoEditor editor) : CanvasElement, ITextEditorClient
    {
        public bool IsEditable => editor.Editable;
        public bool IsMultiline => true;
        public string? Value { get => editor.Document.Text; set => editor.Replace(0, editor.Document.Text.Length, value ?? ""); }
        public int CursorPosition { get => editor._caret; set => editor.MoveCaret(value); }
        public int SelectionAnchor => editor._anchor;
        public void SetSelection(int anchor, int caret) => editor.Select(anchor, caret);
        public RectF CaretRect => editor.CaretRect;
        public void InsertText(string text) => editor.Insert(text);
        public bool Backspace() => editor.Remove(true);
        public bool Delete() => editor.Remove(false);
        public void MoveCursorToEnd() => editor.MoveCaret(editor.Document.Text.Length);
    }
}
