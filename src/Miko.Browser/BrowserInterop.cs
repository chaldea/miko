using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Miko.Browser;

/// <summary>The JavaScript half of the browser host. The module is installed by miko-browser.js.</summary>
[SupportedOSPlatform("browser")]
internal static partial class BrowserInterop
{
    private const string Module = "miko-browser";

    [JSImport("initialize", Module)]
    internal static partial void Initialize(string canvasId, string title);

    [JSImport("requestFrame", Module)]
    internal static partial void RequestFrame();

    [JSImport("present", Module)]
    internal static partial void Present(
        [JSMarshalAs<JSType.MemoryView>] Span<byte> rgba, int width, int height);

    [JSImport("setCursor", Module)]
    internal static partial void SetCursor(string cursor);

    [JSImport("setInputMethodState", Module)]
    internal static partial void SetInputMethodState(
        string text, int cursor, bool multiline, string inputType,
        double left, double top, double width, double height);

    [JSImport("showKeyboard", Module)]
    internal static partial void ShowKeyboard();

    [JSImport("hideKeyboard", Module)]
    internal static partial void HideKeyboard();
}
