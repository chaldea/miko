// Miko host for the LiveCharts2 motion canvas (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).

using LiveChartsCore.Drawing;
using LiveChartsCore.MikoView.Drawing;
using LiveChartsCore.Motion;
using Miko.Common;
using Miko.Core.DomElements;
using Miko.Graphics;

namespace LiveChartsCore.MikoView.Hosting;

/// <summary>
/// Hosts a <see cref="CoreMotionCanvas"/> in a Miko <see cref="CanvasElement"/>: every paint of the element
/// draws one frame of the canvas, and a frame that leaves animations running asks for the next one.
/// </summary>
/// <remarks>
/// <para>LiveCharts animates by redrawing: while a transition runs the canvas stays invalid. Miko renders on
/// demand (an idle engine skips frames), so the host calls <see cref="RequestFrame"/> after a frame whose canvas
/// is still invalid, and whenever the canvas is invalidated from outside a frame (a data change, a measure).
/// That keeps the engine rendering exactly as long as something moves.</para>
/// <para>The canvas draws in the element's content box: its origin is moved to the box's top left corner and
/// everything outside the box is clipped.</para>
/// </remarks>
public sealed class MotionCanvasHost : IDisposable
{
    private readonly Action _requestFrame;
    private readonly Func<LvcSize>? _fixedSize;
    private LvcSize _size;
    private bool _inFrame;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="MotionCanvasHost"/> class.</summary>
    /// <param name="requestFrame">Asks the host engine for another frame; safe to call from any thread.</param>
    /// <param name="fixedSize">
    /// The size of the canvas when it is not laid out by an engine (charts rendered to an image); the content
    /// box of the element at the last frame when null.
    /// </param>
    public MotionCanvasHost(Action requestFrame, Func<LvcSize>? fixedSize = null)
    {
        ArgumentNullException.ThrowIfNull(requestFrame);
        _requestFrame = requestFrame;
        _fixedSize = fixedSize;

        Element = new CanvasElement { Paint = Paint };
        CanvasCore.Invalidated += OnInvalidated;
    }

    /// <summary>Gets the element the charts are painted in.</summary>
    public CanvasElement Element { get; }

    /// <summary>Gets the LiveCharts motion canvas.</summary>
    public CoreMotionCanvas CanvasCore { get; } = new();

    /// <summary>Gets the size of the content box at the last frame, in logical pixels.</summary>
    public LvcSize Size => _fixedSize?.Invoke() ?? _size;

    /// <summary>Gets or sets the color the canvas is cleared with before each frame; transparent keeps what is behind.</summary>
    public LvcColor Background { get; set; } = new(0, 0, 0, 0);

    /// <summary>Raised during a frame when the content box changed size, before the canvas is drawn.</summary>
    public event Action? SizeChanged;

    /// <summary>Raised after a frame was drawn.</summary>
    public event Action? FrameDrawn;

    /// <summary>Asks the engine to render another frame.</summary>
    public void RequestFrame()
    {
        if (!_disposed) _requestFrame();
    }

    private void Paint(IDrawingContext context, RectF box)
    {
        if (_disposed) return;

        var size = new LvcSize(box.Width, box.Height);
        if (size.Width != _size.Width || size.Height != _size.Height)
        {
            _size = size;
            SizeChanged?.Invoke();
        }

        if (box.Width <= 0 || box.Height <= 0) return;

        var save = context.Save();
        _inFrame = true;
        try
        {
            context.Translate(box.X, box.Y);
            context.ClipRect(new RectF(0, 0, box.Width, box.Height));

            // the element background is painted by the engine, the canvas only clears when asked to.
            CanvasCore.DrawFrame(new MikoDrawingContext(
                CanvasCore, context, Background, clearCanvasOnNewFrame: Background.A > 0));
        }
        finally
        {
            _inFrame = false;
            context.RestoreToCount(save);
        }

        FrameDrawn?.Invoke();

        // a transition is still running: keep the frames coming until the canvas settles.
        if (!CanvasCore.IsValid) RequestFrame();
    }

    private void OnInvalidated(CoreMotionCanvas canvas)
    {
        // invalidations raised while drawing are answered by the check at the end of the frame.
        if (!_inFrame) RequestFrame();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CanvasCore.Invalidated -= OnInvalidated;
        Element.Paint = null;
        CanvasCore.Dispose();
    }
}
