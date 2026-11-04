using System.Globalization;
using Miko.Common;
using Miko.Components.Camera;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Graphics;
using Miko.Platform;
using Miko.Platform.Camera;
using Miko.Styling;

namespace Miko.Components.Scanner;

/// <summary>
/// A full-screen QR code and barcode scanner: the live camera preview with a large square scan frame
/// in the middle — corner brackets, a scan line sweeping down, a hint below — and a back button at the
/// top left. Raises <see cref="CodeScanned"/> once per code found in the frame; <see cref="Reset"/>
/// scans for the next one. The system back button raises <see cref="Close"/> while it has a delegate.
/// </summary>
/// <remarks>
/// Reads QR codes by default; <see cref="Formats"/> adds other symbologies. Codes are searched in the
/// frame plus a small margin (a barcode's quiet zone may stick out) and read in any orientation:
/// barcodes across the frame or along it, the phone upright or sideways. Decoding runs on the thread
/// pool; <see cref="CodeScanned"/> is raised on the UI thread.
/// </remarks>
public sealed class MikoBarcodeScanner : ComponentBase, IDisposable
{
    /// <summary>The symbologies to read. Changing it restarts scanning.</summary>
    [Parameter] public BarcodeFormats Formats { get; set; } = BarcodeFormats.QrCode;

    [Parameter] public CameraFacing Facing { get; set; } = CameraFacing.Back;

    [Parameter] public CameraResolution Resolution { get; set; } = CameraResolution.Medium;

    /// <summary>Whether the camera runs (false releases it).</summary>
    [Parameter] public bool Active { get; set; } = true;

    [Parameter] public string? Class { get; set; }

    [Parameter] public Style? Style { get; set; }

    [Parameter] public CameraViewTexts Texts { get; set; } = new();

    /// <summary>
    /// The hint under the scan frame; null shows a built-in English hint that follows
    /// <see cref="Formats"/>, an empty string none.
    /// </summary>
    [Parameter] public string? Hint { get; set; }

    /// <summary>Raised on the UI thread with the code found; not again until <see cref="Reset"/>.</summary>
    [Parameter] public EventCallback<BarcodeResult> CodeScanned { get; set; }

    /// <summary>The back button (top left) and the system back button; the button is hidden without a delegate.</summary>
    [Parameter] public EventCallback Close { get; set; }

    [Inject] public MikoEngine? Engine { get; set; }

    [Inject] public IPlatformService? PlatformService { get; set; }

    /// <summary>Times the scan line; the system clock when null.</summary>
    [Inject] public TimeProvider? Clock { get; set; }

    /// <summary>The code found; null while scanning.</summary>
    public BarcodeResult? Code => _analyzer?.Found;

    /// <summary>The preview; its <see cref="MikoCameraView.Camera"/> is the camera element.</summary>
    public MikoCameraView? View { get; private set; }

    /// <summary>
    /// The scan frame in a view of <paramref name="width"/> × <paramref name="height"/>: a square
    /// centered in it, 72% of the width (60% of the height when that is less, at most 480).
    /// </summary>
    public static RectF GetScanFrame(float width, float height)
    {
        float side = Math.Max(0, Math.Min(Math.Min(width * .72f, height * .6f), 480));
        return new RectF((width - side) / 2, (height - side) / 2, side, side);
    }

    /// <summary>How far past the frame codes are searched, as a fraction of its side.</summary>
    private const float SearchMargin = .08f;

    private const float SweepSeconds = 2.4f;
    private const float HintFontSize = 15, HintGap = 24, HintLineHeight = 21;
    private static readonly Color CornerColor = Color.White;
    private static readonly Color BorderColor = Color.FromRgba(255, 255, 255, .3f);
    private static readonly Color HintColor = Color.FromRgba(255, 255, 255, .9f);
    private static readonly TextStyle HintStyle = new("Arial", HintFontSize);

    private readonly CameraIcons _icons = new();
    private readonly ButtonElement _closeButton = new() { Class = "miko-barcode-close" };
    private readonly CanvasElement _frame = new() { Class = "miko-barcode-frame" };
    private BarcodeAnalyzer? _analyzer;
    private bool _disposed;
    private IDisposable? _back;

    // What the analyzer searches; follows the painted frame (render thread == UI thread).
    private RectF? _scanRegion;
    private CameraViewport? _viewport;

    // Scan line: the sweep restarts from the top whenever scanning (re)starts.
    private long? _sweepStart;

    // The hint wrapped to the width it was laid out for.
    private string? _hintText;
    private float _hintWidth;
    private List<TextBlock>? _hintLines;

    protected override void OnInitialized()
    {
        _closeButton.Style = CloseButtonStyle;
        _closeButton.Children.Add(new CanvasElement
        {
            Style = FillStyle,
            Paint = (c, r) => _icons.Draw(c, r, "chevron-back-outline", 30, Color.White),
        });
        _closeButton.OnClick = e => { e.StopPropagation(); _ = Close.InvokeAsync(); };

        _frame.Style = FrameStyle;
        _frame.Paint = DrawFrame;
    }

    protected override void OnParametersSet()
    {
        if (_analyzer == null || _analyzer.Formats != Formats)
        {
            if (_analyzer != null) _analyzer.Detected -= OnDetected;
            _analyzer = new BarcodeAnalyzer(Formats);
            _analyzer.SetScanRegion(_scanRegion, _viewport);
            _analyzer.Detected += OnDetected;
        }
        SetDisplay(_closeButton, Close.HasDelegate);
        SyncBackButton();
        Repaint();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var root = new Style
        {
            Display = Display.Block, Position = Position.Relative, Width = Length.Percent(100), Height = Length.Percent(100),
            BackgroundColor = Color.Black, Overflow = Overflow.Hidden,
        };
        if (Style != null) root.Merge(Style);

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", string.IsNullOrEmpty(Class) ? "miko-barcode-scanner" : "miko-barcode-scanner " + Class);
        builder.AddAttribute(2, "style", root);
        builder.OpenComponent<MikoCameraView>(3);
        builder.AddComponentParameter(4, nameof(MikoCameraView.Facing), Facing);
        builder.AddComponentParameter(5, nameof(MikoCameraView.Resolution), Resolution);
        builder.AddComponentParameter(6, nameof(MikoCameraView.Analyzer), _analyzer);
        builder.AddComponentParameter(7, nameof(MikoCameraView.Active), Active);
        builder.AddComponentParameter(8, nameof(MikoCameraView.Texts), Texts);
        builder.AddComponentParameter(9, nameof(MikoCameraView.ChildContent), (RenderFragment)BuildOverlay);
        builder.AddComponentReferenceCapture(10, v => View = (MikoCameraView)v);
        builder.CloseComponent();
        builder.CloseElement();
    }

    private void BuildOverlay(RenderTreeBuilder builder)
    {
        builder.AddElement(20, _frame);
        builder.AddElement(21, _closeButton);
    }

    /// <summary>Clears <see cref="Code"/> and scans for the next code.</summary>
    public void Reset()
    {
        _analyzer?.Reset();
        Repaint();
    }

    /// <summary>Analysis thread → UI thread. Dropped when the scanner is gone or was reset or reconfigured meanwhile.</summary>
    private void OnDetected(BarcodeResult code)
    {
        var analyzer = _analyzer;
        var engine = Engine;
        if (engine == null) return;
        engine.Dispatcher.Post(() =>
        {
            if (_disposed || analyzer is null || !ReferenceEquals(analyzer, _analyzer) || !ReferenceEquals(analyzer.Found, code)) return;
            Repaint();
            PlatformService?.Haptics.Impact(HapticImpactStyle.Light);
            if (CodeScanned.HasDelegate) _ = CodeScanned.InvokeAsync(code);
        });
    }

    private void Repaint()
    {
        if (Engine is not { } engine) return;
        engine.PostInvalidate(_frame);
        engine.RequestRepaint();
    }

    // ---- Overlay (render thread) -----------------------------------------------------------------

    /// <summary>
    /// The scan frame, the scan line and the hint, over the whole preview. Also points the analyzer at
    /// the frame: the frame is sized from the view, which is only known here.
    /// </summary>
    private void DrawFrame(IDrawingContext context, RectF bounds)
    {
        // While the view shows an error (no camera, permission denied) only the message is drawn.
        if (View is { State: CameraSessionState.Error } || bounds.Width <= 0 || bounds.Height <= 0)
        {
            _sweepStart = null;
            return;
        }

        var local = GetScanFrame(bounds.Width, bounds.Height);
        UpdateScanRegion(bounds, local);
        var frame = new RectF(bounds.X + local.X, bounds.Y + local.Y, local.Width, local.Height);

        context.StrokeRect(frame, BorderColor, 1);
        DrawCorners(context, frame);

        bool scanning = Active && View is { State: CameraSessionState.Running } && _analyzer is { Found: null };
        if (scanning)
        {
            long now = Time.GetTimestamp();
            _sweepStart ??= now;
            DrawScanLine(context, frame, (float)Time.GetElapsedTime(_sweepStart.Value, now).TotalSeconds);
            // The next frame moves the line on (and keeps an on-demand host drawing).
            Engine?.PostInvalidate(_frame);
        }
        else
        {
            _sweepStart = null;
        }

        DrawHint(context, bounds, frame);
    }

    private TimeProvider Time => Clock ?? TimeProvider.System;

    private void UpdateScanRegion(RectF bounds, RectF frame)
    {
        float margin = frame.Width * SearchMargin;
        float left = Math.Max(0, frame.X - margin), top = Math.Max(0, frame.Y - margin);
        float right = Math.Min(bounds.Width, frame.Right + margin), bottom = Math.Min(bounds.Height, frame.Bottom + margin);
        var region = new RectF(left / bounds.Width, top / bounds.Height, (right - left) / bounds.Width, (bottom - top) / bounds.Height);
        var viewport = new CameraViewport(bounds.Width, bounds.Height, View?.Camera.Fit ?? CameraFit.Cover);
        // Painted every frame while scanning: compare field by field (RectF has no ==, Equals boxes).
        if (_scanRegion is { } last && viewport == _viewport
            && last.X == region.X && last.Y == region.Y && last.Width == region.Width && last.Height == region.Height)
            return;
        _scanRegion = region;
        _viewport = viewport;
        _analyzer?.SetScanRegion(region, viewport);
    }

    /// <summary>Four corner brackets just outside the frame's border.</summary>
    private static void DrawCorners(IDrawingContext c, RectF r)
    {
        float len = Math.Clamp(r.Width * .09f, 14, 32), thick = 4;
        float x0 = r.X - thick + 1, y0 = r.Y - thick + 1, x1 = r.Right - 1, y1 = r.Bottom - 1;
        float span = len + thick - 1;
        c.FillRect(new RectF(x0, y0, span, thick), CornerColor);
        c.FillRect(new RectF(x0, y0, thick, span), CornerColor);
        c.FillRect(new RectF(x1 + thick - span, y0, span, thick), CornerColor);
        c.FillRect(new RectF(x1, y0, thick, span), CornerColor);
        c.FillRect(new RectF(x1 + thick - span, y1, span, thick), CornerColor);
        c.FillRect(new RectF(x1, y1 + thick - span, thick, span), CornerColor);
        c.FillRect(new RectF(x0, y1, span, thick), CornerColor);
        c.FillRect(new RectF(x0, y1 + thick - span, thick, span), CornerColor);
    }

    /// <summary>
    /// A bright line that sweeps from the top of the frame to the bottom, fading in and out, brightest
    /// in the middle and trailing a faint glow.
    /// </summary>
    private static void DrawScanLine(IDrawingContext c, RectF frame, float seconds)
    {
        float phase = seconds / SweepSeconds % 1f;
        float eased = .5f - .5f * MathF.Cos(MathF.PI * phase);
        float fade = Math.Min(1, Math.Min(phase / .12f, (1 - phase) / .2f));
        if (fade <= 0) return;

        float inset = frame.Width * .04f;
        float left = frame.X + inset, width = frame.Width - 2 * inset;
        float y = frame.Y + 2 + (frame.Height - 6) * eased;
        const int segments = 24;
        float step = width / segments;
        for (int i = 0; i < segments; i++)
        {
            // Bright in the middle, transparent at the ends.
            float profile = MathF.Sin(MathF.PI * (i + .5f) / segments);
            float x = left + i * step;
            c.FillRect(new RectF(x, y, step, 2), Color.FromRgba(255, 255, 255, .95f * profile * fade));
            for (int g = 1; g <= 4; g++)
                c.FillRect(new RectF(x, y - g * 4, step, 4), Color.FromRgba(255, 255, 255, .07f / g * profile * fade));
        }
    }

    private void DrawHint(IDrawingContext context, RectF bounds, RectF frame)
    {
        string text = Hint ?? ((Formats & BarcodeFormats.OneDimensional) != 0
            ? "Place a QR code or barcode in the frame to scan"
            : "Place a QR code in the frame to scan");
        if (text.Length == 0) return;

        float maxWidth = Math.Max(HintFontSize * 4, bounds.Width - 48);
        if (_hintLines == null || _hintText != text || _hintWidth != maxWidth)
        {
            _hintLines = WrapText(context.Graphics, text, HintStyle, maxWidth);
            (_hintText, _hintWidth) = (text, maxWidth);
        }

        float centerX = frame.X + frame.Width / 2;
        float top = frame.Bottom + HintGap;
        foreach (var line in _hintLines)
        {
            float baseline = top + (HintLineHeight + line.Ascent - line.Descent) / 2;
            context.DrawText(line, centerX - line.Width / 2, baseline, HintColor);
            top += HintLineHeight;
        }
    }

    /// <summary>
    /// Breaks <paramref name="text"/> into lines no wider than <paramref name="maxWidth"/>: after a
    /// space where the line has one, between any two characters otherwise (CJK has no spaces).
    /// </summary>
    private static List<TextBlock> WrapText(IGraphicsFactory graphics, string text, TextStyle style, float maxWidth)
    {
        var whole = graphics.CreateText(text, style);
        if (whole.Width <= maxWidth) return [whole];

        var lines = new List<TextBlock>();
        int start = 0;
        while (start < text.Length)
        {
            int end = start, lastBreak = -1, position = start;
            float width = 0;
            var elements = StringInfo.GetTextElementEnumerator(text, start);
            while (elements.MoveNext())
            {
                string element = elements.GetTextElement();
                float advance = graphics.CreateText(element, style).Width;
                if (width + advance > maxWidth && end > start) break;
                width += advance;
                position += element.Length;
                end = position;
                if (char.IsWhiteSpace(element[0])) lastBreak = end;
            }
            if (end < text.Length && lastBreak > start) end = lastBreak;
            string line = text[start..end].Trim();
            if (line.Length > 0) lines.Add(graphics.CreateText(line, style));
            start = end;
            while (start < text.Length && char.IsWhiteSpace(text[start])) start++;
        }
        return lines;
    }

    // ---- Chrome ------------------------------------------------------------------------------------

    private static void SetDisplay(Element element, bool visible)
    {
        var display = visible ? Display.Block : Display.None;
        if (element.Style!.Display.ValueOrNull() == display) return;
        var style = new Style();
        style.Merge(element.Style);
        style.Display = display;
        element.Style = style;
    }

    private void SyncBackButton()
    {
        if (Close.HasDelegate && _back == null && PlatformService is { } platform)
        {
            _back = platform.Back.Register(() =>
            {
                if (_disposed || !Close.HasDelegate) return false;
                _ = Close.InvokeAsync();
                return true;
            }, BackButtonPriority.Overlay);
        }
        else if (!Close.HasDelegate && _back != null)
        {
            _back.Dispose();
            _back = null;
        }
    }

    private static readonly Style FillStyle = new()
    {
        Display = Display.Block, Width = Length.Percent(100), Height = Length.Percent(100), PointerEvents = PointerEvents.None,
    };

    private static readonly Style FrameStyle = new()
    {
        Display = Display.Block, Position = Position.Absolute, Top = Length.Px(0), Left = Length.Px(0),
        Width = Length.Percent(100), Height = Length.Percent(100), PointerEvents = PointerEvents.None,
    };

    private static readonly Style CloseButtonStyle = new()
    {
        Display = Display.Block, Position = Position.Absolute, Top = Length.SafeAreaInsetTop + Length.Px(8), Left = Length.SafeAreaInsetLeft + Length.Px(8),
        Width = Length.Px(48), Height = Length.Px(48), Padding = Length.Px(0), BorderWidth = Length.Px(0), BackgroundColor = Color.Transparent,
        PointerEvents = PointerEvents.Auto,
    };

    protected override void OnDispose() => Dispose();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_analyzer != null) _analyzer.Detected -= OnDetected;
        _back?.Dispose();
        _back = null;
        _icons.Dispose();
    }
}
