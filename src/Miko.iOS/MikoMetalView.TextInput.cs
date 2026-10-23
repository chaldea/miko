using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Miko.iOS;

// UIKit 的 UITextInput 把这些返回值与 selectedTextRange 的 setter 声明为 nullable（越界位置、无 marked text
// 时返回 nil），.NET 绑定却标成了非空。按协议返回 null，屏蔽绑定的可空性不匹配。
#pragma warning disable CS8766, CS8767

/// <summary>
/// <see cref="IUITextInput"/>：系统输入法（拼音、假名、注音、韩文……）的组合输入（marked text）、
/// 候选栏定位与光标移动（Release-1.0.0 A3）。只实现 <see cref="IUIKeyInput"/> 时，UIKit 只能逐字
/// <c>InsertText</c>，组合式输入法根本无法工作。
///
/// <para>文档模型 = 已提交文本在光标前的部分 + 正在组合的 marked text + 光标后的部分。已提交文本与光标
/// 来自引擎发布的 <see cref="Platform.InputMethodState"/>；marked text 走引擎已有的组合接口
/// （<c>OnTextCompositionStart/Update/End</c>，Android 同一条路径），在输入框里就地显示。
/// 位置一律是 UTF-16 码元下标，与 NSString 一致。</para>
/// </summary>
public partial class MikoMetalView : IUITextInput
{
    private string _markedText = string.Empty;
    private NSRange _markedSelection;
    private NSObject? _inputDelegate;
    private UITextInputStringTokenizer? _tokenizer;

    // ---- 文档 ---------------------------------------------------------------

    private string Committed => _inputMethod.Text;
    private int Caret => Math.Clamp(_inputMethod.CursorPosition, 0, Committed.Length);
    private string Document => Committed.Insert(Caret, _markedText);
    private int DocumentLength => Committed.Length + _markedText.Length;

    private static int Offset(UITextPosition? position) => (position as MikoTextPosition)?.Offset ?? 0;
    private int Clamp(int offset) => Math.Clamp(offset, 0, DocumentLength);

    /// <summary>引擎发布了新的输入状态（值、光标变化）：告诉系统文档已变，联想与自动更正据此刷新。</summary>
    internal void NotifyTextInputStateChanged()
    {
        // 系统的输入代理是 UIKit 内部对象；按选择器发消息，不依赖它在托管侧被包装成什么类型。
        // 组合进行中不通知：那时的变化就是输入法自己造成的，通知「文档被外部改了」会让它丢掉 marked text
        // （与 Android 上 RestartInput 杀掉组合同理）。
        if (_inputDelegate is not { } d || _markedText.Length > 0) return;
        d.PerformSelector(SelectionWillChange, this);
        d.PerformSelector(TextWillChange, this);
        d.PerformSelector(TextDidChange, this);
        d.PerformSelector(SelectionDidChange, this);
    }

    private static readonly Selector SelectionWillChange = new("selectionWillChange:");
    private static readonly Selector SelectionDidChange = new("selectionDidChange:");
    private static readonly Selector TextWillChange = new("textWillChange:");
    private static readonly Selector TextDidChange = new("textDidChange:");

    public string TextInRange(UITextRange range)
    {
        var r = (MikoTextRange)range;
        int start = Clamp(r.StartOffset), end = Clamp(r.EndOffset);
        return end > start ? Document.Substring(start, end - start) : string.Empty;
    }

    public void ReplaceText(UITextRange range, string text)
    {
        // 自动更正、听写、粘贴走这里。只支持替换光标前的一段（最常见：替换刚打的单词），
        // 换算成「删掉光标前 n 个码元再插入」。
        var r = (MikoTextRange)range;
        EndMarked(commit: false);
        int caret = Caret;
        int start = Math.Clamp(r.StartOffset, 0, Committed.Length);
        int end = Math.Clamp(r.EndOffset, 0, Committed.Length);
        if (end > caret || start > end)
        {
            InsertText(text);
            return;
        }
        _controller.DeleteSurroundingText(caret - start, 0);
        if (!string.IsNullOrEmpty(text)) _inputMethod.Commit(text);
        SetNeedsDisplay();
    }

    public UITextRange SelectedTextRange
    {
        get
        {
            if (_markedText.Length > 0)
            {
                var at = Caret + (int)_markedSelection.Location;
                return new MikoTextRange(at, at + (int)_markedSelection.Length);
            }
            return new MikoTextRange(Caret, Caret);
        }
        set
        {
            if (value is not MikoTextRange r || _markedText.Length > 0) return;
            var at = Math.Clamp(r.StartOffset, 0, Committed.Length);
            _controller.SetTextSelection(at, at);
            SetNeedsDisplay();
        }
    }

    public UITextRange? MarkedTextRange
        => _markedText.Length > 0 ? new MikoTextRange(Caret, Caret + _markedText.Length) : null;

    public NSDictionary? MarkedTextStyle { get; set; }

    public void SetMarkedText(string markedText, NSRange selectedRange)
    {
        markedText ??= string.Empty;
        if (_markedText.Length == 0 && markedText.Length > 0)
            _inputMethod.Begin();
        _markedText = markedText;
        _markedSelection = selectedRange;
        if (markedText.Length > 0)
            _inputMethod.Update(markedText);
        else
            _inputMethod.End(null);
        SetNeedsDisplay();
    }

    public void UnmarkText() => EndMarked(commit: true);

    private void EndMarked(bool commit)
    {
        if (_markedText.Length == 0) return;
        var text = _markedText;
        _markedText = string.Empty;
        _markedSelection = default;
        _inputMethod.End(commit ? text : null);
        SetNeedsDisplay();
    }

    // ---- 位置与范围 ---------------------------------------------------------

    public UITextPosition BeginningOfDocument => new MikoTextPosition(0);
    public UITextPosition EndOfDocument => new MikoTextPosition(DocumentLength);

    public UITextRange GetTextRange(UITextPosition fromPosition, UITextPosition toPosition)
        => new MikoTextRange(Offset(fromPosition), Offset(toPosition));

    public UITextPosition? GetPosition(UITextPosition fromPosition, nint offset)
    {
        var target = Offset(fromPosition) + (int)offset;
        return target < 0 || target > DocumentLength ? null : new MikoTextPosition(target);
    }

    public UITextPosition? GetPosition(UITextPosition fromPosition, UITextLayoutDirection inDirection, nint offset)
        => inDirection switch
        {
            UITextLayoutDirection.Left or UITextLayoutDirection.Up => GetPosition(fromPosition, -offset),
            _ => GetPosition(fromPosition, offset),
        };

    public NSComparisonResult ComparePosition(UITextPosition first, UITextPosition second)
        => Offset(first).CompareTo(Offset(second)) switch
        {
            < 0 => NSComparisonResult.Ascending,
            > 0 => NSComparisonResult.Descending,
            _ => NSComparisonResult.Same,
        };

    public nint GetOffsetFromPosition(UITextPosition fromPosition, UITextPosition toPosition)
        => Offset(toPosition) - Offset(fromPosition);

    // 绑定把 inputDelegate 声明为 NSObject；类型不一致的话它不算实现了接口成员，选择器也不会导出，
    // UIKit 在视图成为第一响应者时调用 inputDelegate 就会因 unrecognized selector 崩溃。
    public NSObject? WeakInputDelegate
    {
        get => _inputDelegate;
        set => _inputDelegate = value;
    }

    public NSObject WeakTokenizer => _tokenizer ??= new UITextInputStringTokenizer(this);

    public UITextPosition? GetPositionWithinRange(UITextRange range, UITextLayoutDirection direction)
    {
        var r = (MikoTextRange)range;
        return direction is UITextLayoutDirection.Left or UITextLayoutDirection.Up
            ? new MikoTextPosition(r.StartOffset)
            : new MikoTextPosition(r.EndOffset);
    }

    public UITextRange? GetCharacterRange(UITextPosition byExtendingPosition, UITextLayoutDirection direction)
    {
        var at = Offset(byExtendingPosition);
        return direction is UITextLayoutDirection.Left or UITextLayoutDirection.Up
            ? new MikoTextRange(Math.Max(0, at - 1), at)
            : new MikoTextRange(at, Math.Min(DocumentLength, at + 1));
    }

    public NSWritingDirection GetBaseWritingDirection(UITextPosition forPosition, UITextStorageDirection direction)
        => NSWritingDirection.LeftToRight;

    public void SetBaseWritingDirectionforRange(NSWritingDirection writingDirection, UITextRange range) { }

    // ---- 几何：候选栏、放大镜、选区菜单的锚点 ---------------------------------

    /// <summary>
    /// 光标所在的矩形（逻辑点，即 UIKit 的点）。引擎只发布光标处的矩形，组合中的范围以它为起点、
    /// 按 marked text 的长度粗略外延——候选栏只需要一个靠近输入位置的锚点。
    /// </summary>
    private CGRect CaretRect
    {
        get
        {
            var r = _inputMethod.CursorRect;
            return new CGRect(r.Left, r.Top, Math.Max(2, r.Width), Math.Max(1, r.Height));
        }
    }

    public CGRect GetFirstRectForRange(UITextRange range)
    {
        var caret = CaretRect;
        var length = Math.Max(1, ((MikoTextRange)range).EndOffset - ((MikoTextRange)range).StartOffset);
        return new CGRect(caret.X, caret.Y, caret.Height * 0.6 * length, caret.Height);
    }

    public CGRect GetCaretRectForPosition(UITextPosition? position) => CaretRect;

    public UITextSelectionRect[] GetSelectionRects(UITextRange range) => [];

    public UITextPosition? GetClosestPositionToPoint(CGPoint point) => new MikoTextPosition(Caret);

    public UITextPosition? GetClosestPositionToPoint(CGPoint point, UITextRange withinRange)
        => new MikoTextPosition(Math.Clamp(Caret, ((MikoTextRange)withinRange).StartOffset, ((MikoTextRange)withinRange).EndOffset));

    public UITextRange? GetCharacterRangeAtPoint(CGPoint point) => new MikoTextRange(Caret, Caret);

    public UIView TextInputView => this;

    // ---- 位置 / 范围对象 ----------------------------------------------------

    private sealed class MikoTextPosition(int offset) : UITextPosition
    {
        public int Offset { get; } = offset;
    }

    private sealed class MikoTextRange : UITextRange
    {
        public MikoTextRange(int start, int end)
        {
            StartOffset = Math.Min(start, end);
            EndOffset = Math.Max(start, end);
        }

        public int StartOffset { get; }
        public int EndOffset { get; }

        public override UITextPosition Start => new MikoTextPosition(StartOffset);
        public override UITextPosition End => new MikoTextPosition(EndOffset);
        public override bool IsEmpty => StartOffset == EndOffset;
    }
}
