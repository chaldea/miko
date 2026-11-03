using Android.Hardware.Camera2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Android.Native;
using Miko.Native;
using Miko.Platform.Camera;

namespace Miko.Android.Camera;

/// <summary>
/// Android camera backend on Camera2 (a system API: no AndroidX/CameraX dependency). Preview frames go
/// straight from the camera into an OES texture (zero copy), as video frames do; analysis frames are
/// YUV images whose luminance plane is handed to the analyzer; stills are JPEGs.
/// <para>
/// The app needs <c>android.permission.CAMERA</c> in its manifest. When it is not granted yet, the
/// session asks for it itself (the host Activity need not forward
/// <c>OnRequestPermissionsResult</c>) and reports <see cref="CameraErrorKind.PermissionDenied"/> when
/// the user declines.
/// </para>
/// </summary>
public sealed class AndroidCameraBackend : ICameraBackend
{
    private readonly ILogger _logger;
    private readonly IServiceProvider? _services;

    public AndroidCameraBackend(IServiceProvider? services = null, ILogger<AndroidCameraBackend>? logger = null)
    {
        _services = services;
        _logger = logger ?? NullLogger<AndroidCameraBackend>.Instance;
    }

    private static CameraManager? Manager
        => global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.CameraService) as CameraManager;

    public IReadOnlyList<CameraDeviceInfo> GetDevices()
    {
        var manager = Manager;
        if (manager == null) return Array.Empty<CameraDeviceInfo>();
        try
        {
            return ListDevices(manager).Select(d => d.Info).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cameras could not be listed");
            return Array.Empty<CameraDeviceInfo>();
        }
    }

    public ICameraSession CreateSession(CameraSessionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var manager = Manager;
        if (manager == null)
            return new AndroidCameraSession(CameraErrorKind.NoDevice, "This device has no camera service.", null, _logger);

        List<(CameraDeviceInfo Info, CameraCharacteristics Characteristics)> devices;
        try
        {
            devices = ListDevices(manager);
        }
        catch (Exception ex)
        {
            return new AndroidCameraSession(CameraErrorKind.Unavailable, "The cameras could not be listed: " + ex.Message, null, _logger);
        }

        var chosen = options.DeviceId is { } id
            ? devices.FirstOrDefault(d => d.Info.Id == id)
            : devices.FirstOrDefault(d => d.Info.Facing == options.Facing);
        if (chosen.Info == null && options.DeviceId == null) chosen = devices.FirstOrDefault();
        if (chosen.Info == null)
        {
            string wanted = options.DeviceId != null ? $"camera '{options.DeviceId}'" : $"{options.Facing.ToString().ToLowerInvariant()} camera";
            return new AndroidCameraSession(CameraErrorKind.NoDevice, $"There is no {wanted}.", null, _logger);
        }

        try
        {
            return new AndroidCameraSession(manager, chosen.Info.Id, chosen.Characteristics, chosen.Info, options.Resolution, FindActivity, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Camera {Id} could not be set up", chosen.Info.Id);
            return new AndroidCameraSession(CameraErrorKind.Failed, ex.Message, chosen.Info, _logger);
        }
    }

    /// <summary>
    /// The Activity to ask for the permission from: the one the Miko view was attached with
    /// (<see cref="AndroidNativeHost"/>, registered by <c>AddMikoNative()</c> / <c>UseAndroidNative()</c>).
    /// </summary>
    private global::Android.App.Activity? FindActivity()
        => (_services?.GetService<INativeHostContext>()?.Host as AndroidNativeHost)?.Activity
            ?? MikoSurfaceView.LastActivity;

    private static List<(CameraDeviceInfo Info, CameraCharacteristics Characteristics)> ListDevices(CameraManager manager)
    {
        var result = new List<(CameraDeviceInfo, CameraCharacteristics)>();
        int back = 0, front = 0, external = 0;
        foreach (var id in manager.GetCameraIdList())
        {
            var characteristics = manager.GetCameraCharacteristics(id);
            var facing = AndroidCameraSession.GetInt(characteristics, CameraCharacteristics.LensFacing) switch
            {
                (int)LensFacing.Front => CameraFacing.Front,
                (int)LensFacing.Back => CameraFacing.Back,
                _ => CameraFacing.External,
            };
            string name = facing switch
            {
                CameraFacing.Front => front++ == 0 ? "Front Camera" : $"Front Camera {front}",
                CameraFacing.Back => back++ == 0 ? "Back Camera" : $"Back Camera {back}",
                _ => external++ == 0 ? "External Camera" : $"External Camera {external}",
            };
            result.Add((new CameraDeviceInfo(id, name, facing), characteristics));
        }
        return result;
    }
}
