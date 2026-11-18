using Miko.Common;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;

namespace Miko.Components.Reader;

internal sealed class ReaderIcons : IDisposable
{
    private readonly Dictionary<string, Image?> _images = [];
    public void Draw(IDrawingContext context, RectF box, string name, Color color)
    {
        if (!_images.TryGetValue(name, out var image))
        {
            string family = name is "close-outline" or "chevron-back-outline" ? "Camera" : "Reader";
            using var stream = typeof(ReaderIcons).Assembly.GetManifestResourceStream($"Miko.{family}.Icons.{name}.svg")!;
            _images[name] = image = context.Graphics.DecodeImage(stream);
        }
        if (image != null) context.DrawImage(image, new(box.X + (box.Width - 23) / 2, box.Y + (box.Height - 23) / 2, 23, 23), tint: color);
    }
    public void Dispose() { foreach (var image in _images.Values) image?.Dispose(); _images.Clear(); }
}

/// <summary>A touch-sized range with separate preview and commit, so dragging progress never floods the source.</summary>
internal sealed class ReaderSlider
{
    private bool _dragging;
    private readonly Func<Color> _ink;
    private readonly Func<Color> _track;
    private readonly Action<float> _preview, _commit;
    private readonly Action _repaint;
    public float Value { get; set; }
    public CanvasElement Element { get; }
    public ReaderSlider(string name, Func<Color> ink, Func<Color> track, Action<float> preview, Action<float> commit, Action repaint)
    {
        _ink = ink; _track = track; _preview = preview; _commit = commit; _repaint = repaint;
        Element = new CanvasElement
        {
            Class = name,
            Style = new() { Display = Display.Block, FlexGrow = 1, FlexBasis = Length.Px(0), MinWidth = Length.Px(0), Height = Length.Px(44) },
            Paint = Paint,
            OnPointerDown = e => { if (!e.IsPrimary || e.Button != MouseButton.Left) return; _dragging = true; Update(e); },
            OnPointerMove = e => { if (_dragging) Update(e); },
            OnPointerUp = e => { if (!_dragging) return; Update(e); _dragging = false; _commit(Value); },
            OnPointerCancel = _ => _dragging = false,
            OnClick = e => e.StopPropagation(),
        };
    }
    public void Sync(float value) { if (!_dragging) Value = Math.Clamp(value, 0, 1); }
    private void Update(PointerEventArgs e)
    {
        e.PreventDefault(); e.StopPropagation();
        Value = Math.Clamp((e.OffsetX - 12) / Math.Max(1, e.TargetWidth - 24), 0, 1);
        _preview(Value); _repaint();
    }
    private void Paint(IDrawingContext context, RectF box)
    {
        float left = box.X + 12, width = Math.Max(0, box.Width - 24), y = box.Y + box.Height / 2, x = left + width * Value;
        context.FillRoundRect(new(left, y - 3, width, 6), 3, _track());
        context.FillRoundRect(new(left, y - 3, x - left, 6), 3, _ink());
        context.FillCircle(x, y + 1, 11, Color.FromRgba(0, 0, 0, .08f));
        context.FillCircle(x, y, 9, Color.White);
        context.FillCircle(x, y, 4, _ink());
    }
}
