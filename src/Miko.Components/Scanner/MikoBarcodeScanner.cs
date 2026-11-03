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
/// A full-screen barcode scanner: the live camera preview with a framed scan region in the middle and a
/// close button. Raises <see cref="CodeScanned"/> once per code found in the region; <see cref="Reset"/>
/// scans for the next one. The system back button raises <see cref="Close"/> while it has a delegate.
/// </summary>
/// <remarks>
/// Reads QR codes by default; <see cref="Formats"/> adds other symbologies. Codes outside
/// <see cref="ScanRegion"/> are ignored. Decoding runs on the thread pool; <see cref="CodeScanned"/> is
/// raised on the UI thread.
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

    /// <summary>Raised on the UI thread with the code found; not again until <see cref="Reset"/>.</summary>
    [Parameter] public EventCallback<BarcodeResult> CodeScanned { get; set; }

    /// <summary>The close button (top right) and the system back button; the button is hidden without a delegate.</summary>
    [Parameter] public EventCallback Close { get; set; }

    [Inject] public MikoEngine? Engine { get; set; }

    [Inject] public IPlatformService? PlatformService { get; set; }

    /// <summary>The code found; null while scanning.</summary>
    public BarcodeResult? Code => _analyzer?.Found;

    /// <summary>The scan region, normalized to the preview (0–1 on each axis).</summary>
    public static readonly RectF ScanRegion = new(0.3f, 0.375f, 0.4f, 0.25f);

    private static readonly Color GuideColor = Color.FromHex("#00d084");

    private readonly CameraIcons _icons = new();
    private readonly ButtonElement _closeButton = new() { Class = "miko-barcode-close" };
    private readonly DivElement _scanRegion = new() { Class = "miko-barcode-region" };
    private BarcodeAnalyzer? _analyzer;
    private bool _disposed;
    private IDisposable? _back;

    protected override void OnInitialized()
    {
        _closeButton.Style = CloseButtonStyle;
        _closeButton.Children.Add(new CanvasElement
        {
            Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Percent(100), PointerEvents = PointerEvents.None },
            Paint = (c, r) => _icons.Draw(c, r, "close-outline", 34, Color.White),
        });
        _closeButton.OnClick = e => { e.StopPropagation(); _ = Close.InvokeAsync(); };

        _scanRegion.Style = RegionStyle;
        _scanRegion.Children.Add(new CanvasElement { Style = FillStyle, Paint = DrawGuides });
    }

    protected override void OnParametersSet()
    {
        if (_analyzer == null || _analyzer.Formats != Formats)
        {
            if (_analyzer != null) _analyzer.Detected -= OnDetected;
            _analyzer = new BarcodeAnalyzer(Formats) { ScanRegion = ScanRegion };
            _analyzer.Detected += OnDetected;
        }
        SetDisplay(_closeButton, Close.HasDelegate);
        SyncBackButton();
        Engine?.RequestRepaint();
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
        builder.CloseComponent();
        builder.CloseElement();
    }

    private void BuildOverlay(RenderTreeBuilder builder)
    {
        builder.AddElement(20, _scanRegion);
        builder.AddElement(21, _closeButton);
    }

    /// <summary>Clears <see cref="Code"/> and scans for the next code.</summary>
    public void Reset() => _analyzer?.Reset();

    /// <summary>Analysis thread → UI thread. Dropped when the scanner is gone or was reset or reconfigured meanwhile.</summary>
    private void OnDetected(BarcodeResult code)
    {
        var analyzer = _analyzer;
        var engine = Engine;
        if (engine == null) return;
        engine.Dispatcher.Post(() =>
        {
            if (_disposed || !ReferenceEquals(analyzer, _analyzer) || !ReferenceEquals(analyzer.Found, code)) return;
            PlatformService?.Haptics.Impact(HapticImpactStyle.Light);
            if (CodeScanned.HasDelegate) _ = CodeScanned.InvokeAsync(code);
        });
    }

    /// <summary>Four corner brackets around the scan region.</summary>
    private static void DrawGuides(IDrawingContext c, RectF r)
    {
        const float len = 24, thick = 3;
        c.FillRect(new RectF(r.X, r.Y, len, thick), GuideColor);
        c.FillRect(new RectF(r.X, r.Y, thick, len), GuideColor);
        c.FillRect(new RectF(r.Right - len, r.Y, len, thick), GuideColor);
        c.FillRect(new RectF(r.Right - thick, r.Y, thick, len), GuideColor);
        c.FillRect(new RectF(r.Right - len, r.Bottom - thick, len, thick), GuideColor);
        c.FillRect(new RectF(r.Right - thick, r.Bottom - len, thick, len), GuideColor);
        c.FillRect(new RectF(r.X, r.Bottom - thick, len, thick), GuideColor);
        c.FillRect(new RectF(r.X, r.Bottom - len, thick, len), GuideColor);
    }

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

    private static readonly Style CloseButtonStyle = new()
    {
        Display = Display.Block, Position = Position.Absolute, Top = Length.SafeAreaInsetTop + Length.Px(12), Right = Length.SafeAreaInsetRight + Length.Px(12),
        Width = Length.Px(48), Height = Length.Px(48), Padding = Length.Px(0), BorderWidth = Length.Px(0), BackgroundColor = Color.Transparent,
        PointerEvents = PointerEvents.Auto,
    };

    private static readonly Style RegionStyle = new()
    {
        Display = Display.Block, Position = Position.Absolute,
        Left = Length.Percent(ScanRegion.X * 100), Top = Length.Percent(ScanRegion.Y * 100),
        Width = Length.Percent(ScanRegion.Width * 100), Height = Length.Percent(ScanRegion.Height * 100),
        PointerEvents = PointerEvents.None,
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
