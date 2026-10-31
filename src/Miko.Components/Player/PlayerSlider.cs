using Miko.Common;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;
using Miko.Styling;

namespace Miko.Components.Player;

/// <summary>
/// A horizontal slider drawn on one canvas — track, filled part, thumb — for the settings panel.
/// A native range input is styled for light pages; this one is drawn for a translucent dark panel
/// over the picture, and dragging it never scrolls the panel (the moves are default-prevented).
/// </summary>
internal sealed class PlayerSlider
{
    private const float ThumbRadius = 7;
    private static readonly Color TrackColor = Color.FromRgba(255, 255, 255, .28f);

    private readonly float _min, _max;
    private readonly Color _accent;
    private readonly Action<float> _changed;
    private readonly Action _repaint;
    private bool _dragging;

    public PlayerSlider(float min, float max, float value, Color accent, Action<float> changed, Action repaint)
    {
        _min = min;
        _max = max;
        _accent = accent;
        _changed = changed;
        _repaint = repaint;
        Value = Math.Clamp(value, min, max);
        Element = new CanvasElement
        {
            Class = "miko-player-slider",
            Style = new Style { Display = Display.Block, FlexGrow = 1, FlexBasis = Length.Px(0), MinWidth = Length.Px(0), Height = Length.Px(32) },
            Paint = Draw,
            OnPointerDown = e => { _dragging = true; Update(e); },
            OnPointerMove = e => { if (_dragging) Update(e); },
            OnPointerUp = e => { if (!_dragging) return; Update(e); _dragging = false; },
            OnPointerCancel = _ => _dragging = false,
            // A tap without a drag still clicks; it must not reach the panel as an outside tap.
            OnClick = e => e.StopPropagation(),
        };
    }

    public CanvasElement Element { get; }

    public float Value { get; private set; }

    private void Update(PointerEventArgs e)
    {
        e.PreventDefault();
        e.StopPropagation();
        float t = Math.Clamp((e.OffsetX - ThumbRadius) / Math.Max(1, e.TargetWidth - 2 * ThumbRadius), 0, 1);
        float value = _min + t * (_max - _min);
        if (value == Value) return;
        Value = value;
        _changed(value);
        _repaint();
    }

    private void Draw(IDrawingContext context, RectF bounds)
    {
        float y = bounds.Y + bounds.Height / 2;
        float left = bounds.X + ThumbRadius, width = Math.Max(0, bounds.Width - 2 * ThumbRadius);
        float x = left + (_max > _min ? (Value - _min) / (_max - _min) : 0) * width;
        context.FillRoundRect(new RectF(left, y - 2, width, 4), 2, TrackColor);
        context.FillRoundRect(new RectF(left, y - 2, x - left, 4), 2, _accent);
        context.FillCircle(x, y, ThumbRadius, Color.White);
    }
}
