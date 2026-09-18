using System.Runtime.InteropServices;
using Miko.Common;
using Miko.Platform;

namespace Miko.Windowing.Common;

/// <summary>
/// Silk input endpoint that positions Windows composition and candidate windows at the engine caret.
/// </summary>
public class SilkInputMethod : InputMethodBase
{
    private InputMethodState? _state;
    private IntPtr _windowHandle;
    private int _logicalWidth;
    private int _logicalHeight;
    private float _cursorOffsetX;
    private float _cursorOffsetY;

    public override void SetState(InputMethodState? state) => Volatile.Write(ref _state, state);

    public void SetWindowHandle(IntPtr windowHandle) => _windowHandle = windowHandle;

    public void SetLogicalViewport(int width, int height)
    {
        Volatile.Write(ref _logicalWidth, width);
        Volatile.Write(ref _logicalHeight, height);
    }

    /// <summary>Adds a host-specific client-coordinate offset to the engine caret rectangle.</summary>
    public void SetCursorOffset(float x, float y)
    {
        Volatile.Write(ref _cursorOffsetX, x);
        Volatile.Write(ref _cursorOffsetY, y);
    }

    /// <summary>
    /// Applies the current position on the window thread. Windows resets IMM placement when
    /// composition starts, so hosts call this around each native event pump while focused.
    /// </summary>
    public void ApplyPendingState()
    {
        var state = Volatile.Read(ref _state);
        if (state == null) return;

        var rect = state.CursorRect;
        rect = new RectF(
            rect.X + Volatile.Read(ref _cursorOffsetX),
            rect.Y + Volatile.Read(ref _cursorOffsetY),
            rect.Width,
            rect.Height);
        UpdateWindowsInputPosition(
            _windowHandle,
            rect,
            Volatile.Read(ref _logicalWidth),
            Volatile.Read(ref _logicalHeight));
    }

    public void Commit(string text) => CommitText(text);
    public void BeginComposition() => StartComposition();
    public void UpdateCompositionText(string text) => UpdateComposition(text);
    public void EndCompositionText(string? text = null) => EndComposition(text);

    private static void UpdateWindowsInputPosition(IntPtr hwnd, RectF rect, int logicalWidth, int logicalHeight)
    {
        if (!OperatingSystem.IsWindows() || hwnd == IntPtr.Zero || !IsWindow(hwnd)) return;
        if (GetClientRect(hwnd, out var clientRect) && logicalWidth > 0 && logicalHeight > 0)
        {
            var scaleX = (clientRect.Right - clientRect.Left) / (float)logicalWidth;
            var scaleY = (clientRect.Bottom - clientRect.Top) / (float)logicalHeight;
            rect = new RectF(rect.X * scaleX, rect.Y * scaleY, rect.Width * scaleX, rect.Height * scaleY);
        }

        var himc = ImmGetContext(hwnd);
        if (himc == IntPtr.Zero) return;
        try
        {
            var point = new Point
            {
                X = (int)MathF.Round(rect.Left),
                Y = (int)MathF.Round(rect.Bottom),
            };
            var composition = new CompositionForm { Style = CfsPoint, Position = point };
            ImmSetCompositionWindow(himc, ref composition);

            var candidate = new CandidateForm
            {
                Index = 0,
                Style = CfsExclude,
                Position = point,
                Area = new NativeRect
                {
                    Left = (int)MathF.Floor(rect.Left),
                    Top = (int)MathF.Floor(rect.Top),
                    Right = (int)MathF.Ceiling(rect.Right),
                    Bottom = (int)MathF.Ceiling(rect.Bottom),
                },
            };
            ImmSetCandidateWindow(himc, ref candidate);
        }
        finally
        {
            ImmReleaseContext(hwnd, himc);
        }
    }

    private const int CfsPoint = 0x0002;
    private const int CfsExclude = 0x0080;

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("imm32.dll")]
    private static extern IntPtr ImmGetContext(IntPtr hwnd);

    [DllImport("imm32.dll")]
    private static extern bool ImmReleaseContext(IntPtr hwnd, IntPtr himc);

    [DllImport("imm32.dll")]
    private static extern bool ImmSetCandidateWindow(IntPtr himc, ref CandidateForm form);

    [DllImport("imm32.dll")]
    private static extern bool ImmSetCompositionWindow(IntPtr himc, ref CompositionForm form);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct CandidateForm
    {
        public int Index;
        public int Style;
        public Point Position;
        public NativeRect Area;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CompositionForm
    {
        public int Style;
        public Point Position;
        public NativeRect Area;
    }
}
