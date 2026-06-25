using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;

namespace Miko.Simulator;

/// <summary>
/// 构建模拟器右侧设置面板的 DOM 树。面板交由 Miko 引擎布局/渲染。
/// 纯静态构建函数；交互由调用方（<see cref="SimulatorHost"/>）通过回调接入。
/// </summary>
internal static class SimulatorPanel
{
    /// <summary>
    /// 构建设置面板。
    /// </summary>
    /// <param name="devices">可选设备列表。</param>
    /// <param name="current">当前选中的设备。</param>
    /// <param name="orientation">当前朝向。</param>
    /// <param name="safeAreaEnabled">是否启用安全区模拟。</param>
    /// <param name="onSelectDevice">选择设备回调。</param>
    /// <param name="onSetOrientation">设置朝向回调。</param>
    /// <param name="onToggleSafeArea">切换安全区回调。</param>
    public static Element Build(
        IReadOnlyList<DeviceProfile> devices,
        DeviceProfile current,
        Orientation orientation,
        bool safeAreaEnabled,
        Action<DeviceProfile> onSelectDevice,
        Action<Orientation> onSetOrientation,
        Action<bool> onToggleSafeArea)
    {
        var panel = new DivElement { Class = "sim-panel" };

        panel.AddChild(BuildHeader());

        var body = new DivElement { Class = "sim-body" };
        body.AddChild(BuildDeviceSection(devices, current, onSelectDevice));
        body.AddChild(BuildOrientationSection(orientation, onSetOrientation));
        body.AddChild(BuildSafeAreaSection(safeAreaEnabled, onToggleSafeArea));
        body.AddChild(BuildInfoSection(current, orientation));
        panel.AddChild(body);

        return panel;
    }

    private static Element BuildHeader()
    {
        var header = new DivElement { Class = "sim-header" };
        header.AddChild(new DivElement { Class = "sim-title", TextContent = "Miko Simulator" });
        header.AddChild(new DivElement { Class = "sim-subtitle", TextContent = "Device preview & settings" });
        return header;
    }

    private static Element BuildDeviceSection(
        IReadOnlyList<DeviceProfile> devices,
        DeviceProfile current,
        Action<DeviceProfile> onSelectDevice)
    {
        var section = new DivElement { Class = "sim-section" };
        section.AddChild(new DivElement { Class = "sim-section-label", TextContent = "DEVICE" });

        foreach (var device in devices)
        {
            bool active = device.Name == current.Name;
            var item = new DivElement
            {
                Class = active ? "sim-device sim-device-active" : "sim-device",
            };
            item.AddChild(new DivElement { Class = "sim-device-name", TextContent = device.Name });
            item.AddChild(new DivElement
            {
                Class = "sim-device-spec",
                TextContent = $"{device.LogicalWidth}×{device.LogicalHeight}  @{device.Scale:0.#}x",
            });

            var captured = device;
            item.OnClick = _ => onSelectDevice(captured);
            section.AddChild(item);
        }

        return section;
    }

    private static Element BuildOrientationSection(Orientation orientation, Action<Orientation> onSetOrientation)
    {
        var section = new DivElement { Class = "sim-section" };
        section.AddChild(new DivElement { Class = "sim-section-label", TextContent = "ORIENTATION" });

        var row = new DivElement { Class = "sim-btn-row" };

        var portrait = new DivElement
        {
            Class = orientation == Orientation.Portrait ? "sim-btn sim-btn-active" : "sim-btn",
            TextContent = "Portrait",
        };
        portrait.OnClick = _ => onSetOrientation(Orientation.Portrait);

        var landscape = new DivElement
        {
            Class = orientation == Orientation.Landscape ? "sim-btn sim-btn-active" : "sim-btn",
            TextContent = "Landscape",
        };
        landscape.OnClick = _ => onSetOrientation(Orientation.Landscape);

        row.AddChild(portrait);
        row.AddChild(landscape);
        section.AddChild(row);
        return section;
    }

    private static Element BuildSafeAreaSection(bool safeAreaEnabled, Action<bool> onToggleSafeArea)
    {
        var section = new DivElement { Class = "sim-section" };
        section.AddChild(new DivElement { Class = "sim-section-label", TextContent = "SAFE AREA" });

        var row = new DivElement { Class = "sim-btn-row" };

        var on = new DivElement
        {
            Class = safeAreaEnabled ? "sim-btn sim-btn-active" : "sim-btn",
            TextContent = "On",
        };
        on.OnClick = _ => onToggleSafeArea(true);

        var off = new DivElement
        {
            Class = !safeAreaEnabled ? "sim-btn sim-btn-active" : "sim-btn",
            TextContent = "Off",
        };
        off.OnClick = _ => onToggleSafeArea(false);

        row.AddChild(on);
        row.AddChild(off);
        section.AddChild(row);
        return section;
    }

    private static Element BuildInfoSection(DeviceProfile device, Orientation orientation)
    {
        var section = new DivElement { Class = "sim-section" };
        section.AddChild(new DivElement { Class = "sim-section-label", TextContent = "INFO" });

        var (w, h) = orientation == Orientation.Portrait
            ? (device.LogicalWidth, device.LogicalHeight)
            : (device.LogicalHeight, device.LogicalWidth);

        section.AddChild(InfoRow("Viewport", $"{w} × {h} pt"));
        section.AddChild(InfoRow("Resolution", $"{(int)(w * device.Scale)} × {(int)(h * device.Scale)} px"));
        section.AddChild(InfoRow("Scale", $"{device.Scale:0.#}x"));
        section.AddChild(InfoRow("Class", device.Kind.ToString()));
        return section;
    }

    private static Element InfoRow(string key, string value)
    {
        var row = new DivElement { Class = "sim-info-row" };
        row.AddChild(new DivElement { Class = "sim-info-key", TextContent = key });
        row.AddChild(new DivElement { Class = "sim-info-val", TextContent = value });
        return row;
    }
}

/// <summary>设备朝向。</summary>
public enum Orientation
{
    Portrait,
    Landscape,
}
