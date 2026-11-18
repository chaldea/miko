using Miko.Common;
using Miko.Core.DomElements;
using Miko.Graphics;

namespace Miko.Components.Reader;

/// <summary>A comic reader with single/double spreads, reading order, page axis, fit-width and double-tap zoom.</summary>
public sealed class ComicReader : ReaderComponentBase
{
    [Parameter] public ComicPageMode PageMode { get; set; } = ComicPageMode.Single;
    [Parameter] public ReaderDirection Direction { get; set; } = ReaderDirection.LeftToRight;
    [Parameter] public ReaderOrientation Orientation { get; set; } = ReaderOrientation.Horizontal;
    [Parameter] public bool FitWidth { get; set; }
    [Parameter] public float DoubleTapZoom { get; set; } = 2;
    [Parameter] public Color Background { get; set; } = Color.FromHex("#202124");
    public float Zoom => Images.Zoom;

    private protected override string ComponentClass => "miko-comic-reader";
    private protected override int SpreadSize => PageMode == ComicPageMode.Double ? 2 : 1;
    private protected override ReaderDirection ReadingDirection => Direction;
    private protected override ReaderOrientation PageOrientation => Orientation;
    private protected override Color PaperColor => Background;
    private protected override bool IsNight => Background.R + Background.G + Background.B < 384;
    private protected override void ToggleNight() => Background = IsNight ? Color.FromHex("#faf9f6") : Color.FromHex("#202124");
    private protected override void BuildAppearance(DivElement panel) => ThemeSwatches(panel,
        IsNight ? 3 : Background.Equals(Color.FromHex("#f1e5ce")) ? 1 : Background.Equals(Color.FromHex("#dce8d0")) ? 2 : 0, i =>
        Background = Color.FromHex(new[] { "#faf9f6", "#f1e5ce", "#dce8d0", "#202124" }[i]));
    private protected override void SetContent(ReaderSnapshot snapshot) => Images.SetContent(snapshot.Content);
    private protected override void Draw(IDrawingContext context, RectF box) => Images.Paint(context, box, FitWidth, Direction, Orientation);
    private protected override void DoubleTap() => ToggleZoom();

    public void ToggleZoom() { Images.ToggleZoom(float.IsFinite(DoubleTapZoom) ? DoubleTapZoom : 2); Repaint(); }
    public void ResetZoom() { Images.ResetZoom(); Repaint(); }

    private protected override void BuildSettings(DivElement panel)
    {
        Choice(panel, Texts.Mode, [Texts.Single, Texts.Double], (int)PageMode, i =>
        {
            PageMode = (ComicPageMode)i;
            _ = Controller.GoToAsync(CurrentIndex, SpreadSize);
        });
        Choice(panel, Texts.Direction, [Texts.LeftToRight, Texts.RightToLeft], (int)Direction, i => { Direction = (ReaderDirection)i; Images.ResetZoom(); });
        Choice(panel, Texts.Orientation, [Texts.Horizontal, Texts.Vertical], (int)Orientation, i => { Orientation = (ReaderOrientation)i; Images.ResetZoom(); });
        Choice(panel, Texts.FitWidth, [Texts.Off, Texts.On], FitWidth ? 1 : 0, i => { FitWidth = i == 1; Images.ResetZoom(); });
        panel.Children.Add(Button(Texts.Zoom, "miko-reader-zoom", ToggleZoom));
    }
}
