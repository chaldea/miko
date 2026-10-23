using Microsoft.Extensions.Logging;
using Silk.NET.GLFW;

namespace Miko.Windowing.Common;

/// <summary>
/// Makes GLFW failures visible on Linux.
/// </summary>
/// <remarks>
/// Silk.NET's default GLFW error callback throws only on Windows; on Linux and macOS it queues the
/// error in a list that is never read. A missing display, an unsupported OpenGL version or a
/// Wayland compositor rejecting a request then shows up later as a null window, a misleading
/// "graphics not initialized" error, or a native crash with no message at all. Desktop hosts
/// call <see cref="Install"/> before creating their first window so each GLFW error is written to
/// stderr (the app's logging may have no providers) and to the logger.
/// </remarks>
public static class GlfwDiagnostics
{
    // GLFW keeps only the function pointer: the delegate must stay reachable.
    private static GlfwCallbacks.ErrorCallback? _callback;
    private static ILogger? _logger;

    /// <summary>The last error GLFW reported, if any.</summary>
    public static string? LastError { get; private set; }

    /// <summary>
    /// Installs the logging error callback on Linux; elsewhere it does nothing (Windows keeps
    /// Silk.NET's throwing callback, and macOS presents through Metal). Safe to call repeatedly.
    /// Initializes GLFW, so a session without a usable display fails here with GLFW's own message.
    /// </summary>
    public static void Install(ILogger? logger = null)
    {
        if (!OperatingSystem.IsLinux()) return;
        if (logger is not null) _logger = logger;
        if (_callback is not null) return;

        // Initialize first: initialization installs Silk.NET's callback, which this one replaces.
        var glfw = GlfwProvider.GLFW.Value;
        _callback = OnError;
        glfw.SetErrorCallback(_callback);
    }

    private static void OnError(ErrorCode code, string description)
    {
        LastError = $"{code}: {description}";
        Console.Error.WriteLine($"[Miko] GLFW error {LastError}");
        _logger?.LogWarning("GLFW error {Code}: {Description}", code, description);
    }
}
