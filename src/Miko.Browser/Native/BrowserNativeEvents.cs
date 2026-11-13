using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Miko.Browser.Native;

internal static class BrowserNativeEvents
{
    internal static event Action<bool>? VisibilityChanged;
    internal static event Action<string>? NetworkChanged;
    internal static event Action<string, string?, string?>? PositionChanged;
    internal static event Action<double, double>? KeyboardInsetChanged;

    internal static void OnVisibilityChanged(bool visible) => VisibilityChanged?.Invoke(visible);
    internal static void OnNetworkChanged(string json) => NetworkChanged?.Invoke(json);
    internal static void OnPositionChanged(string id, string? json, string? error)
        => PositionChanged?.Invoke(id, json, error);
    internal static void OnKeyboardInsetChanged(double previous, double current)
        => KeyboardInsetChanged?.Invoke(previous, current);
}

/// <summary>Native-capability events raised by miko-browser.js.</summary>
[SupportedOSPlatform("browser")]
public static partial class BrowserNativeCallbacks
{
    [JSExport] public static void NetworkChanged(string json) => BrowserNativeEvents.OnNetworkChanged(json);
    [JSExport] public static void PositionChanged(string id, string? json, string? error)
        => BrowserNativeEvents.OnPositionChanged(id, json, error);
}
