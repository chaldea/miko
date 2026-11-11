using System.Text.Json.Serialization;
using Miko.Native;
using Miko.Native.Device;
using Miko.Native.Geolocation;
using Miko.Native.Network;

namespace Miko.Browser.Native;

internal sealed record ClipboardJson(string Value, string Type);
internal sealed record BatteryJson(double Level, bool Charging);
internal sealed record NetworkJson(bool Connected, string Type);
internal sealed record DeviceJson(
    string Name, string Model, string OperatingSystem, string OsVersion,
    string Manufacturer, long MemoryUsed, string UserAgent);
internal sealed record CoordinatesJson(
    double Latitude, double Longitude, double Accuracy,
    double? AltitudeAccuracy, double? Altitude, double? Speed, double? Heading);
internal sealed record PositionJson(long Timestamp, CoordinatesJson Coords);

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ClipboardJson))]
[JsonSerializable(typeof(BatteryJson))]
[JsonSerializable(typeof(NetworkJson))]
[JsonSerializable(typeof(DeviceJson))]
[JsonSerializable(typeof(PositionJson))]
[JsonSerializable(typeof(PositionOptions))]
internal sealed partial class BrowserJsonContext : JsonSerializerContext
{
    internal static ConnectionStatus ToConnectionStatus(NetworkJson value) => new(
        value.Connected,
        !value.Connected ? ConnectionType.None : value.Type switch
        {
            "wifi" or "ethernet" => ConnectionType.Wifi,
            "cellular" => ConnectionType.Cellular,
            _ => ConnectionType.Unknown,
        });

    internal static DeviceInfo ToDeviceInfo(DeviceJson value) => new(
        value.Name,
        value.Model,
        DevicePlatform.Web,
        value.OperatingSystem switch
        {
            "ios" => Miko.Native.Device.OperatingSystem.Ios,
            "android" => Miko.Native.Device.OperatingSystem.Android,
            "windows" => Miko.Native.Device.OperatingSystem.Windows,
            "mac" => Miko.Native.Device.OperatingSystem.Mac,
            _ => Miko.Native.Device.OperatingSystem.Unknown,
        },
        value.OsVersion,
        null,
        null,
        value.Manufacturer,
        false,
        value.MemoryUsed,
        value.UserAgent);

    internal static Position ToPosition(PositionJson value) => new(
        value.Timestamp,
        new Coordinates(
            value.Coords.Latitude, value.Coords.Longitude, value.Coords.Accuracy,
            value.Coords.AltitudeAccuracy, value.Coords.Altitude, value.Coords.Speed, value.Coords.Heading));

    internal static PermissionState ToPermissionState(string value) => value switch
    {
        "granted" => PermissionState.Granted,
        "denied" => PermissionState.Denied,
        _ => PermissionState.Prompt,
    };
}
