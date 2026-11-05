using Miko.Common;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;
using Miko.Styling;

namespace Miko.Components.Webcam;

/// <summary>
/// The pan / tilt disk: four arrows around a centre button, drawn on one canvas. Pressing a quarter of
/// the ring starts moving that way, releasing (or cancelling) stops — the camera keeps turning while the
/// finger stays down, as in the vendors' apps. Pointer events stay with the disk while pressed, and
/// moves are default-prevented, so holding never scrolls the page.
/// </summary>
internal sealed class WebcamPtzPad
{
    private static readonly Color DiskColor = Color.White;
    private static readonly Color CentreColor = Color.FromHex("#eef1f6");
    private static readonly Color ArrowColor = Color.FromHex("#9aa1ad");
    private static readonly Color DisabledArrowColor = Color.FromHex("#d3d7de");
    private static readonly Color PressedColor = Color.FromRgba(61, 123, 255, .14f);
    private static readonly Color PressedArrowColor = Color.FromHex("#3d7bff");

    private readonly WebcamIcons _icons;
    private readonly Action<PtzDirection> _start;
    private readonly Action _stop;
    private readonly Action _repaint;
    private readonly bool _translucent;

    public WebcamPtzPad(WebcamIcons icons, float size, bool translucent, Action<PtzDirection> start, Action stop, Action repaint)
    {
        _icons = icons;
        _start = start;
        _stop = stop;
        _repaint = repaint;
        _translucent = translucent;
        var style = new Style
        {
            Display = Display.Block, Width = Length.Px(size), Height = Length.Px(size), FlexShrink = 0, BorderRadius = size / 2,
        };
        if (!translucent) style.BoxShadow = new List<BoxShadow> { new(0, 8, 28, 0, Color.FromRgba(40, 60, 100, .12f)) };
        Element = new CanvasElement
        {
            Class = translucent ? "miko-webcam-ptz-overlay-pad" : "miko-webcam-ptz-pad",
            Style = style,
            Paint = Draw,
            OnPointerDown = OnPointerDown,
            OnPointerMove = e => { if (Pressed != null) { e.PreventDefault(); e.StopPropagation(); } },
            OnPointerUp = _ => Release(),
            OnPointerCancel = _ => Release(),
            OnClick = e => e.StopPropagation(),
        };
    }

    public CanvasElement Element { get; }

    /// <summary>Pan and tilt are available (otherwise the arrows are drawn disabled and do nothing).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Which arrows work.</summary>
    public bool CanPan { get; set; } = true;

    public bool CanTilt { get; set; } = true;

    /// <summary>The direction held down, if any.</summary>
    public PtzDirection? Pressed { get; private set; }

    /// <summary>The direction under a point of a <paramref name="size"/>-wide disk, or null in the centre or outside.</summary>
    internal static PtzDirection? HitTest(float x, float y, float size)
    {
        float r = size / 2, dx = x - r, dy = y - r;
        float distance = MathF.Sqrt(dx * dx + dy * dy);
        if (distance > r || distance < r * .26f) return null;
        return Math.Abs(dx) > Math.Abs(dy)
            ? dx < 0 ? PtzDirection.Left : PtzDirection.Right
            : dy < 0 ? PtzDirection.Up : PtzDirection.Down;
    }

    /// <summary>Lets go of a held direction (also when the camera reported a limit, or the view goes away).</summary>
    public void Release()
    {
        if (Pressed == null) return;
        Pressed = null;
        _stop();
        _repaint();
    }

    private void OnPointerDown(PointerEventArgs e)
    {
        if (!Enabled) return;
        var direction = HitTest(e.OffsetX, e.OffsetY, Math.Min(e.TargetWidth, e.TargetHeight));
        if (direction is null || !Allows(direction.Value)) return;
        e.PreventDefault();
        e.StopPropagation();
        Pressed = direction;
        _start(direction.Value);
        _repaint();
    }

    private bool Allows(PtzDirection direction) =>
        direction is PtzDirection.Left or PtzDirection.Right ? CanPan : CanTilt;

    private void Draw(IDrawingContext context, RectF bounds)
    {
        float size = Math.Min(bounds.Width, bounds.Height);
        float cx = bounds.X + bounds.Width / 2, cy = bounds.Y + bounds.Height / 2, r = size / 2;
        context.FillCircle(cx, cy, r, _translucent ? Color.FromRgba(0, 0, 0, .35f) : DiskColor);
        context.FillCircle(cx, cy, r * .24f, _translucent ? Color.FromRgba(255, 255, 255, .18f) : CentreColor);

        float arrow = r * .68f, icon = Math.Max(14, size * .09f);
        DrawArrow(context, PtzDirection.Up, cx, cy - arrow, "caret-up", icon);
        DrawArrow(context, PtzDirection.Down, cx, cy + arrow, "caret-down", icon);
        DrawArrow(context, PtzDirection.Left, cx - arrow, cy, "caret-back", icon);
        DrawArrow(context, PtzDirection.Right, cx + arrow, cy, "caret-forward", icon);
    }

    private void DrawArrow(IDrawingContext context, PtzDirection direction, float x, float y, string name, float size)
    {
        bool usable = Enabled && Allows(direction);
        bool pressed = Pressed == direction;
        if (pressed) context.FillCircle(x, y, size * 2.1f, _translucent ? Color.FromRgba(255, 255, 255, .22f) : PressedColor);
        var color = !usable ? (_translucent ? Color.FromRgba(255, 255, 255, .3f) : DisabledArrowColor)
            : pressed ? (_translucent ? Color.White : PressedArrowColor)
            : _translucent ? Color.FromRgba(255, 255, 255, .85f) : ArrowColor;
        _icons.Draw(context, x, y, name, size, color);
    }
}
