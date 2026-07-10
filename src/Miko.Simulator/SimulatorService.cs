using Miko.Common;
using Miko.Platform;
using SkiaSharp;

namespace Miko.Simulator;

/// <summary>
/// 模拟器服务实现，通过SimulatorHost暴露调试能力。
/// </summary>
public sealed class SimulatorService : ISimulatorService
{
    private readonly SimulatorHost _host;

    public SimulatorService(SimulatorHost host)
    {
        _host = host;
    }

    public DeviceInfo GetCurrentDevice()
    {
        var device = _host.CurrentDevice;
        return new DeviceInfo(
            device.Name,
            device.LogicalWidth,
            device.LogicalHeight,
            device.Scale,
            device.Kind.ToString(),
            device.Platform.ToString());
    }

    public IReadOnlyList<DeviceInfo> GetAvailableDevices()
    {
        return _host.AvailableDevices.Select(d => new DeviceInfo(
            d.Name,
            d.LogicalWidth,
            d.LogicalHeight,
            d.Scale,
            d.Kind.ToString(),
            d.Platform.ToString())).ToList();
    }

    public bool SelectDevice(string deviceName)
    {
        var device = _host.AvailableDevices.FirstOrDefault(d => d.Name == deviceName);
        if (device == null) return false;

        // 切换设备会重建离屏画布与 DOM，必须在渲染线程执行。
        _host.InvokeOnRenderThread(() => _host.SelectDevice(device));
        return true;
    }

    public Orientation GetOrientation()
    {
        return _host.CurrentOrientation;
    }

    public bool SetOrientation(Orientation orientation)
    {
        _host.InvokeOnRenderThread(() => _host.SetOrientation(orientation));
        return true;
    }

    public bool GetSafeAreaEnabled()
    {
        return _host.SafeAreaEnabled;
    }

    public bool ToggleSafeArea(bool enabled)
    {
        _host.InvokeOnRenderThread(() => _host.ToggleSafeArea(enabled));
        return true;
    }

    public byte[]? CaptureScreenshot()
    {
        // 快照读取 GPU 表面，须在持有 GL 上下文的渲染线程执行。
        return _host.InvokeOnRenderThread(() =>
        {
            var surface = _host.AppSurface;
            if (surface == null) return null;

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data?.ToArray();
        });
    }
}
