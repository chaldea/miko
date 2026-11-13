using System.Text.Json;
using Miko.Browser.Native;
using Miko.Native;
using Miko.Native.Device;
using Miko.Native.Network;
using Shouldly;

namespace Miko.Browser.Tests;

public class BrowserNativeModelTests
{
    [Theory]
    [InlineData(true, "wifi", ConnectionType.Wifi)]
    [InlineData(true, "ethernet", ConnectionType.Wifi)]
    [InlineData(true, "cellular", ConnectionType.Cellular)]
    [InlineData(true, "unknown", ConnectionType.Unknown)]
    [InlineData(false, "wifi", ConnectionType.None)]
    public void Should_convert_network_status(bool connected, string type, ConnectionType expected)
    {
        var status = BrowserJsonContext.ToConnectionStatus(new NetworkJson(connected, type));
        status.Connected.ShouldBe(connected);
        status.ConnectionType.ShouldBe(expected);
    }

    [Fact]
    public void Should_deserialize_camel_case_browser_payloads_without_reflection()
    {
        var device = JsonSerializer.Deserialize(
            "{\"name\":\"Mac\",\"model\":\"Arm\",\"operatingSystem\":\"mac\",\"osVersion\":\"17\",\"manufacturer\":\"Apple\",\"memoryUsed\":42,\"userAgent\":\"agent\"}",
            BrowserJsonContext.Default.DeviceJson)!;
        var info = BrowserJsonContext.ToDeviceInfo(device);
        info.Platform.ShouldBe(DevicePlatform.Web);
        info.OperatingSystem.ShouldBe(Miko.Native.Device.OperatingSystem.Mac);
        info.MemUsed.ShouldBe(42);

        var position = JsonSerializer.Deserialize(
            "{\"timestamp\":123,\"coords\":{\"latitude\":1.5,\"longitude\":2.5,\"accuracy\":3,\"altitude\":null,\"altitudeAccuracy\":null,\"speed\":null,\"heading\":90}}",
            BrowserJsonContext.Default.PositionJson)!;
        var converted = BrowserJsonContext.ToPosition(position);
        converted.Timestamp.ShouldBe(123);
        converted.Coords.Latitude.ShouldBe(1.5);
        converted.Coords.Heading.ShouldBe(90);
    }

    [Theory]
    [InlineData("granted", PermissionState.Granted)]
    [InlineData("denied", PermissionState.Denied)]
    [InlineData("prompt", PermissionState.Prompt)]
    [InlineData("unsupported", PermissionState.Prompt)]
    public void Should_convert_permission_state(string state, PermissionState expected)
        => BrowserJsonContext.ToPermissionState(state).ShouldBe(expected);
}
