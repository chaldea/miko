using Miko.Common;
using Miko.Core.DomElements;
using Miko.Graphics;

namespace Miko.Components.Reader;

/// <summary>
/// A reflowing text reader with measured pagination and scrolling. TXT sections and EPUB spine chapters
/// load on demand. Fixed PDF pages (or other image content) fit the viewport and support zoom/pan.
/// </summary>
public sealed class BookReader : ReaderComponentBase
{
    [Parameter] public string FontFamily { get; set; } = "serif";
    [Parameter] public float FontSize { get; set; } = 20;
    /// <summary>Line height as a multiple of the font size.</summary>
    [Parameter] public float LineSpacing { get; set; } = 1.6f;
    [Parameter] public float ParagraphSpacing { get; set; } = 12;
    [Parameter] public float PageMargin { get; set; } = 24;
    [Parameter] public ReaderTheme Theme { get; set; } = ReaderTheme.Light;
    [Parameter] public bool EyeCare { get; set; }
    [Parameter] public Color? Background { get; set; }
    [Parameter] public Color? Foreground { get; set; }
    [Parameter] public BookReadingMode Mode { get; set; } = BookReadingMode.Paged;
    public int PageIndex => _view.Page;
    public int PageCount => _view.PageCount;

    private readonly BookView _view = new();
    private BookTypography _typography = new("serif", 20, 1.6f, 12, 24);
    private bool _textContent, _enterLastPage;
    private (IReaderSource? Source, int Index, float Progress)? _pendingSeek;
    private ReaderTheme _dayTheme = ReaderTheme.Light;
    private protected override string ComponentClass => "miko-book-reader";
    private protected override Color PaperColor => Background ?? (EyeCare ? Color.FromHex("#dce8d0") : Theme switch
    { ReaderTheme.Dark => Color.FromHex("#1c2025"), ReaderTheme.Sepia => Color.FromHex("#f1e5ce"), _ => Color.FromHex("#faf9f6") });
    private protected override Color InkColor => Foreground ?? (Theme == ReaderTheme.Dark && !EyeCare ? Color.FromHex("#e2e4e8") : Color.FromHex("#292c30"));
    private protected override string PageLabel => _textContent ? $" · {_view.Page + 1}/{_view.PageCount}" : "";
    private protected override string CurrentPageText => _textContent ? $"{CurrentIndex + 1} · {_view.Page + 1} / {_view.PageCount}" : base.CurrentPageText;
    private protected override float ReadingProgress => Snapshot.Document is { Entries.Count: > 0 } doc
        ? _textContent ? (CurrentIndex + (_view.PageCount > 1 ? (float)_view.Page / (_view.PageCount - 1) : 1)) / doc.Entries.Count
            : base.ReadingProgress : 0;
    private protected override bool IsNight => Theme == ReaderTheme.Dark && !EyeCare;
    private protected override void ToggleNight()
    {
        if (IsNight) Theme = _dayTheme;
        else { _dayTheme = Theme == ReaderTheme.Dark ? ReaderTheme.Light : Theme; Theme = ReaderTheme.Dark; }
        EyeCare = false; Background = null; Foreground = null;
    }
    private protected override Task SeekAsync(float progress)
    {
        int count = Snapshot.Document?.Entries.Count ?? 0;
        if (count == 0) return Task.CompletedTask;
        if (!_textContent && Snapshot.State == ReaderState.Ready) return base.SeekAsync(progress);
        int chapter = Math.Min(count - 1, (int)(progress * count));
        if (chapter == CurrentIndex && _textContent)
        { _view.GoToPage((int)Math.Round((progress * count - chapter) * (_view.PageCount - 1))); Repaint(); return Task.CompletedTask; }
        _enterLastPage = false;
        _pendingSeek = (Source, chapter, progress * count - chapter);
        return base.GoToAsync(chapter);
    }
    private protected override bool CanPrevious => (_textContent && _view.CanPrevious) || base.CanPrevious;
    private protected override bool CanNext => (_textContent && _view.CanNext) || base.CanNext;

    private protected override void ApplyParameters()
    {
        _typography = new(string.IsNullOrWhiteSpace(FontFamily) ? "serif" : FontFamily,
            Finite(FontSize, 20, 8, 96), Finite(LineSpacing, 1.6f, 1, 4), Finite(ParagraphSpacing, 12, 0, 100), Finite(PageMargin, 24, 0, 200));
        _view.Mode = Mode;
    }

    private protected override void SetContent(ReaderSnapshot snapshot)
    {
        if (snapshot.Document == null) { _enterLastPage = false; _pendingSeek = null; }
        _textContent = snapshot.Content.FirstOrDefault() is ReaderTextContent;
        if (_textContent)
        {
            float? progress = _pendingSeek is { } seek && ReferenceEquals(seek.Source, Source) && seek.Index == snapshot.Index
                ? seek.Progress : null;
            _view.SetText(((ReaderTextContent)snapshot.Content[0]).Text, _enterLastPage, progress);
            _pendingSeek = null;
            _enterLastPage = false;
            Images.SetContent([]);
        }
        else { _view.SetText(""); Images.SetContent(snapshot.Content); }
    }

    public override Task GoToAsync(int index) { _enterLastPage = false; _pendingSeek = null; return base.GoToAsync(index); }

    private protected override void Draw(IDrawingContext context, RectF box)
    {
        if (_textContent) _view.Paint(context, box, _typography, InkColor);
        else Images.Paint(context, box, false, ReaderDirection.LeftToRight, ReaderOrientation.Horizontal);
    }

    public override Task NextAsync()
    {
        _pendingSeek = null;
        if (Snapshot.State != ReaderState.Ready) return Task.CompletedTask;
        if (_textContent && _view.Move(1)) { Repaint(); return Task.CompletedTask; }
        _enterLastPage = false;
        return base.NextAsync();
    }

    public override Task PreviousAsync()
    {
        _pendingSeek = null;
        if (Snapshot.State != ReaderState.Ready) return Task.CompletedTask;
        if (_textContent && _view.Move(-1)) { Repaint(); return Task.CompletedTask; }
        if (CurrentIndex > 0) _enterLastPage = true;
        return base.PreviousAsync();
    }

    private protected override bool Drag(float dx, float dy)
    {
        if (!_textContent) return base.Drag(dx, dy);
        if (Mode != BookReadingMode.Scroll) return false;
        _view.Scroll(-dy);
        return true;
    }

    private protected override bool Scroll(float delta)
    {
        if (!_textContent) return base.Scroll(delta);
        if (Mode != BookReadingMode.Scroll) return false;
        _view.Scroll(delta);
        return true;
    }

    private protected override void DoubleTap() { if (!_textContent) Images.ToggleZoom(2); }

    private protected override void BuildSettings(DivElement panel)
    {
        Choice(panel, Texts.Font, [Texts.Serif, Texts.SansSerif, Texts.Monospace], FontFamily == "sans-serif" ? 1 : FontFamily == "monospace" ? 2 : 0, i => FontFamily = new[] { "serif", "sans-serif", "monospace" }[i]);
        Stepper(panel, Texts.FontSize, _typography.FontSize, v => FontSize = v, 2, 8, 96);
        Stepper(panel, Texts.LineSpacing, _typography.LineSpacing, v => LineSpacing = v, .1f, 1, 4);
        Stepper(panel, Texts.ParagraphSpacing, _typography.ParagraphSpacing, v => ParagraphSpacing = v, 4, 0, 100);
        Stepper(panel, Texts.Margin, _typography.PageMargin, v => PageMargin = v, 4, 0, 200);
        Choice(panel, Texts.Mode, [Texts.Paged, Texts.Scroll], (int)Mode, i => Mode = (BookReadingMode)i);
    }

    private protected override void BuildAppearance(DivElement panel)
    {
        ThemeSwatches(panel, EyeCare ? 2 : Theme == ReaderTheme.Sepia ? 1 : Theme == ReaderTheme.Dark ? 3 : 0, i =>
        {
            EyeCare = i == 2; Theme = i == 3 ? ReaderTheme.Dark : i == 1 ? ReaderTheme.Sepia : ReaderTheme.Light;
            Background = null; Foreground = null;
        });
        Choice(panel, Texts.EyeCare, [Texts.Off, Texts.On], EyeCare ? 1 : 0, i => EyeCare = i == 1);
    }

    private static float Finite(float value, float fallback, float min, float max) => float.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
}
