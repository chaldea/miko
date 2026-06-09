using Microsoft.Extensions.Logging;

namespace Miko.Hosting;

/// <summary>
/// Service that handles hot reload notifications from the .NET runtime.
/// This service integrates with dotnet watch to enable hot reload without closing the application window.
/// </summary>
public class HotReloadService
{
    private readonly ILogger<HotReloadService> _logger;
    private Action? _onReloadCallback;

    public HotReloadService(ILogger<HotReloadService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a callback to be invoked when hot reload is triggered.
    /// </summary>
    /// <param name="callback">The callback to invoke on hot reload</param>
    public void OnReload(Action callback)
    {
        _onReloadCallback = callback;
        _logger.LogInformation("[HotReload] Reload callback registered with HotReloadService");
    }

    /// <summary>
    /// Triggers the hot reload callback.
    /// This method should be called by the application's hot reload handler.
    /// </summary>
    public void TriggerReload()
    {
        if (_onReloadCallback == null)
        {
            _logger.LogWarning("[HotReload] TriggerReload called but no reload callback is registered");
            return;
        }

        _logger.LogInformation("[HotReload] TriggerReload invoked - signaling UI rebuild");
        _onReloadCallback.Invoke();
    }
}
